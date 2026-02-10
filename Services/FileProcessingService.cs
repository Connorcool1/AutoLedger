namespace BookkeepingApp.Services;

using BookkeepingApp.Models;
using System.Text.RegularExpressions;

public class FileProcessingService
{
    public async Task<string> SaveFileAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is null or empty.");
        }

        // Define the upload directory
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");

        // Create the directory if it doesn't exist
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        // Save the file
        var fileName = Path.GetFileName(file.FileName);
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        return filePath;
    }

    public async Task<List<Item>> ParseStatementFileAsync(string filePath)
    {
        var items = new List<Item>();

        try
        {
            using (var sr = new StreamReader(filePath, System.Text.Encoding.UTF8))
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
                        string dateStr = line.Replace("Date:", "").Trim();
                        currentItem.Date = DateTime.Parse(dateStr);
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

                // Add the last item if exists
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
                // Write header
                await sw.WriteLineAsync("Date,Description,Amount");

                // Write data rows
                foreach (var item in items)
                {
                    string escapedDescription = item.Description.Replace("\"", "\"\"");
                    await sw.WriteLineAsync($"{item.Date:dd/MM/yyyy},\"{escapedDescription}\",{item.Amount}");
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

            // Write header
            csvBuilder.AppendLine("Date,Description,Amount");

            // Write data rows
            foreach (var item in items)
            {
                string escapedDescription = item.Description.Replace("\"", "\"\"");
                csvBuilder.AppendLine($"{item.Date:dd/MM/yyyy},\"{escapedDescription}\",{item.Amount}");
            }

            return await Task.FromResult(csvBuilder.ToString());
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error generating CSV content: {ex.Message}", ex);
        }
    }
}