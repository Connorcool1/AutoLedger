using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class Program {
    public static void Main(string[] args) {
        Ledger ledger = new Ledger();
        List<Item> items = new List<Item>();
        try {
            StreamReader sr = new StreamReader("statement.txt", System.Text.Encoding.UTF8);
            string line;
            Item currentItem = null;
            while ((line = sr.ReadLine()) != null) {
                line = Regex.Replace(line, @"[\u00A0\uFFFD\t\r]", " ");
                line = Regex.Replace(line, @"\s+", " ");
                line = line.Trim();
                if (line.StartsWith("Date:")) {
                    if (currentItem != null) {
                        items.Add(currentItem);
                    }                     
                    currentItem = new Item();
                    
                    string dateStr = line.Replace("Date:", "").Trim();
                    currentItem.date = DateTime.Parse(dateStr);                
                }
                else if (line.StartsWith("Description:") && currentItem != null)
                {
                    string descStr = line.Replace("Description:", "").Trim();
                    currentItem.description = descStr;

                    // Check updated date in desc
                    Match dateMatch = Regex.Match(descStr, @"(\d{2}-\d{2}-\d{4})\s*$");
                    if (dateMatch.Success) {
                        string dateInDesc = dateMatch.Groups[1].Value;
                        if (DateTime.TryParseExact(dateInDesc, "dd-MM-yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate)) {
                            currentItem.date = parsedDate;
                        }
                    }
                }
                else if (line.StartsWith("Amount:") && currentItem != null) {
                    string amountStr = line.Replace("Amount:", "").Trim();
                    amountStr = amountStr.Replace("GBP", "").Trim();
                    currentItem.amount = double.Parse(amountStr);                        
                }               
            }
            sr.Close();
            System.Console.WriteLine($"Successfully parsed {items.Count} transactions:\n");
            
            // Write to CSV file
            WriteToCSV(items);
        } catch(Exception e) {
            Console.WriteLine("Exception: " + e.Message);
        }
    }
    
    public static void WriteToCSV(List<Item> items) {
        try {
            using (StreamWriter sw = new StreamWriter("output.csv")) {
                // Write header
                sw.WriteLine("Date,Description,Amount");
                
                // Write data rows
                foreach (Item item in items) {
                    string escapedDescription = item.description.Replace("\"", "\"\"");
                    sw.WriteLine($"{item.date:dd/MM/yyyy},\"{escapedDescription}\",{item.amount}");
                }
            }
            System.Console.WriteLine("\nCSV file 'output.csv' created successfully.");
        } catch (Exception e) {
            Console.WriteLine("Error writing CSV: " + e.Message);
        }
    }
}

public class Ledger
{
    public DateTime date { get; set; }
    public Item[] items { get; set; }
    public double balance { get; set; }
}

public class Item
{
    public DateTime date { get; set; }
    public string description { get; set; }
    public double amount { get; set; }
    public string type { get; set; }
}