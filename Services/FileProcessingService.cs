namespace BookkeepingApp.Services;

using BookkeepingApp.Models;
using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;

public class FileProcessingService
{
    public async Task<string> SaveFileAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is null or empty.");
        }

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");

        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        // Save
        var fileName = Path.GetFileName(file.FileName);
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        return filePath;
    }

    public async Task<List<Item>> ParseStatementFileAsync(IFormFile file)
    {
        var items = new List<Item>();
        int idCount = 0;

        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("No file / file is empty.");
        }
        try
        {
            using (var sr = new StreamReader(file.OpenReadStream(), System.Text.Encoding.UTF8))
            {
                string line;
                Item currentItem = null;

                while ((line = await sr.ReadLineAsync()) != null)
                {

                    line = Regex.Replace(line, @"[\u00A0\uFFFD\t\r]", " ");
                    line = Regex.Replace(line, @"\s+", " ");
                    line = line.Trim();

                    if (line.StartsWith("Date:"))
                    {
                        if (currentItem != null)
                        {
                            items.Add(currentItem);
                        }
                    
                        currentItem = new Item();
                        currentItem.Id = idCount;
                        idCount++;
                        currentItem.Type = TransactionType.Default;
                        string dateStr = line.Replace("Date:", "").Trim();
                        currentItem.Date = DateTime.Parse(dateStr, new CultureInfo("en-GB"));
                    }
                    else if (line.StartsWith("Description:") && currentItem != null)
                    {
                        string descStr = line.Replace("Description:", "").Trim();
                        currentItem.Description = descStr;

                        // Check updated date in desc
                        Match dateMatch = Regex.Match(descStr, @"(\d{2}-\d{2}-\d{4})\s*$");
                        if (dateMatch.Success)
                        {
                            string dateInDesc = dateMatch.Groups[1].Value;
                            if (DateTime.TryParseExact(dateInDesc, "dd-MM-yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
                            {
                                currentItem.Date = parsedDate;
                            }
                        }
                    }
                    else if (line.StartsWith("Amount:") && currentItem != null)
                    {
                        string amountStr = line.Replace("Amount:", "").Trim();
                        amountStr = amountStr.Replace("GBP", "").Trim();
                        currentItem.Amount = double.Parse(amountStr);
                    }
                }

                if (currentItem != null)
                {
                    items.Add(currentItem);
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error parsing file: {ex.Message}", ex);
        }

        return items;
    }

    public async Task<string> WriteToCSVAsync(List<Item> items)
    {
        try
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var csvFilePath = Path.Combine(uploadsFolder, $"output_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

            using (var sw = new StreamWriter(csvFilePath))
            {
                // Header
                await sw.WriteLineAsync("Date,Description,Amount,Type");

                // Data
                foreach (var item in items)
                {
                    string escapedDescription = item.Description.Replace("\"", "\"\"");
                    await sw.WriteLineAsync($"{item.Date:dd/MM/yyyy},\"{escapedDescription}\",{item.Amount},{item.Type}");
                }
            }

            return csvFilePath;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error writing CSV: {ex.Message}", ex);
        }
    }

    public async Task<string> GenerateCSVContentAsync(List<Item> items)
    {
        try
        {
            var csvBuilder = new System.Text.StringBuilder();

            // Header
            csvBuilder.AppendLine("Date,Description,Amount,Type,Total Expenditure,Total Income,Total Balance");

            // Balance
            double totalAmount = CalculateTotalAmount(items);
            double totalIncome = getTotalIncome(items);
            double totalExpenditure = getTotalExpenditure(items);
            csvBuilder.AppendLine($",,,,£{totalExpenditure},£{totalIncome},£{totalAmount}");
            // Data
            foreach (var item in items)
            {
                string escapedDescription = item.Description.Replace("\"", "\"\"");
                csvBuilder.AppendLine($"{item.Date:dd-MM},\"{escapedDescription}\",{item.Amount},{item.Type}");
            }

            return await Task.FromResult(csvBuilder.ToString());
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error generating CSV content: {ex.Message}", ex);
        }
    }

    public async Task<XLWorkbook> GenerateXLContentAsync(List<Item> items)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Transactions");

        // Titles
        worksheet.Cell(1, 1).Value = "GENERAL LEDGER";
        worksheet.Cell(2, 1).Value = "ACCOUNT: SANTANDER CURRENT/PAYPAL";
        worksheet.Cell(3, 1).Value = "COCONUTBLUSH";

        // Header
        worksheet.Cell(4, 1).Value = "Date";
        worksheet.Cell(4, 2).Value = "Description";
        worksheet.Cell(4, 3).Value = "Amount";
        worksheet.Cell(4, 4).Value = "Type";

        // Data
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var row = i + 5;
            worksheet.Cell(row, 1).Value = item.Date;
            worksheet.Cell(row, 1).Style.DateFormat.Format = "dd-MM";
            worksheet.Cell(row, 2).Value = item.Description;
            worksheet.Cell(row, 3).Value = item.Amount;
            worksheet.Cell(row, 3).Style.NumberFormat.Format = "£#,##0.00";
            worksheet.Cell(row, 4).Value = item.Type.ToString();
        }

        // Balance calculations
        double totalAmount = CalculateTotalAmount(items);
        double totalIncome = getTotalIncome(items);
        double totalExpenditure = getTotalExpenditure(items);
        worksheet.Cell(items.Count + 6, 1).Value = "Total Expenditure";
        worksheet.Cell(items.Count + 6, 2).Value = "Total Income";
        worksheet.Cell(items.Count + 6, 3).Value = "Total Balance";
        worksheet.Cell(items.Count + 7, 1).Value = totalExpenditure;
        worksheet.Cell(items.Count + 7, 1).Style.NumberFormat.Format = "£#,##0.00";
        worksheet.Cell(items.Count + 7, 2).Value = totalIncome;
        worksheet.Cell(items.Count + 7, 2).Style.NumberFormat.Format = "£#,##0.00";
        worksheet.Cell(items.Count + 7, 3).Value = totalAmount;
        worksheet.Cell(items.Count + 7, 3).Style.NumberFormat.Format = "£#,##0.00";

        worksheet.Columns().AdjustToContents();

        return workbook;
    }

    private double getTotalExpenditure(List<Item> items)
    {
        double total = 0;
        foreach (var item in items)
        {
            if (item.Amount < 0)
            {
                total += item.Amount;
            }
        }
        return Math.Round(total, 2);
    }

    private double getTotalIncome(List<Item> items)
    {
        double total = 0;
        foreach (var item in items)
        {
            if (item.Amount > 0)
            {
                total += item.Amount;
            }
        }
        return Math.Round(total, 2);
    }

    private double CalculateTotalAmount(List<Item> items)
    {
        double total = 0;
        foreach (var item in items)
        {
            total += item.Amount;
        }
        return Math.Round(total, 2);
    }
}