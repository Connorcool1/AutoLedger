using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BookkeepingApp.Services;
using BookkeepingApp.Models;
using System.Text.Json;
using System.Security.Cryptography.X509Certificates;

namespace BookkeepingApp.Pages;

public class IndexModel : PageModel
{
    private readonly FileProcessingService _fileProcessingService;
    private const string ParsedItemsSessionKey = "ParsedItems";

    public string? Message { get; set; }
    public List<Item>? ParsedItems { get; set; }

    [BindProperty]
    public Dictionary<int, TransactionType> ItemTypes { get; set; }

    public IndexModel(FileProcessingService fileProcessingService)
    {
        _fileProcessingService = fileProcessingService;
    }

    public void OnGet()
    {
        // Load parsed items from session if available
        LoadParsedItemsFromSession();
    }

    public async Task<IActionResult> OnPostAsync(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                Message = "Please select a file to upload.";
                return Page();
            }

            var filePath = await _fileProcessingService.SaveFileAsync(file);

            ParsedItems = await _fileProcessingService.ParseStatementFileAsync(filePath);

            if (ParsedItems.Count == 0)
            {
                Message = "No transactions found in the file.";
                return Page();
            }

            // Store parsed items in session
            StoreParsedItemsInSession(ParsedItems);

            Message = $"Successfully parsed {ParsedItems.Count} transactions!";
        }
        catch (Exception ex)
        {
            Message = $"Error processing file: {ex.Message}";
        }

        return Page();
    }

    // public async Task<IActionResult> OnPostFilterAsync(int[] selectedIndices)
    // {
    //     if (selectedIndices == null || selectedIndices.Length == 0)
    //     {
    //         Message = "Please select at least one transaction to download.";
    //         return RedirectToPage();
    //     }

    //     // Get parsed items from session
    //     LoadParsedItemsFromSession();
        
    //     if (ParsedItems == null || ParsedItems.Count == 0)
    //     {
    //         Message = "No parsed items available. Please upload a file first.";
    //         return RedirectToPage();
    //     }

    //     // Filter items based on selected indices
    //     for (int i = 0; i < ParsedItems.Count; i++) { ParsedItems[i].IsSelected = false; }
        
    //     var selectedItems = new List<Item>();
    //     foreach (var index in selectedIndices)
    //     {
    //         if (index >= 0 && index < ParsedItems.Count)
    //         {
    //             ParsedItems[index].IsSelected = true;
    //             selectedItems.Add(ParsedItems[index]);
    //         }

    //     }

    //     if (selectedItems.Count == 0)
    //     {
    //         Message = "No valid indices selected.";
    //         return RedirectToPage();
    //     }

    //     StoreParsedItemsInSession(ParsedItems);
    //     Filter = true;
    //     return Page();
    // }

    public async Task<IActionResult> OnPostDownloadAsync(int[] selectedIndices)
    {
        List<Item> FilteredItems = new List<Item>();
        try
        {
            LoadParsedItemsFromSession();

            for (int i = 0; i < selectedIndices.Length; i++)
            {
                Item item = ParsedItems[selectedIndices[i]];
                item.Type = ItemTypes[item.Id.Value];
                FilteredItems.Add(item);
            }

            // Generate CSV content
            var csvContent = await _fileProcessingService.GenerateCSVContentAsync(FilteredItems);

            // Return file for download
            var fileName = $"transactions_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            return File(System.Text.Encoding.UTF8.GetBytes(csvContent), "text/csv", fileName);
        }
        catch (Exception ex)
        {
            Message = $"Error generating download: {ex.Message}";
            return RedirectToPage();
        }
    }

    private void StoreParsedItemsInSession(List<Item> items)
    {
        var json = JsonSerializer.Serialize(items);
        HttpContext.Session.SetString(ParsedItemsSessionKey, json);
    }

    private void LoadParsedItemsFromSession()
    {
        var json = HttpContext.Session.GetString(ParsedItemsSessionKey);
        if (!string.IsNullOrEmpty(json))
        {
            ParsedItems = JsonSerializer.Deserialize<List<Item>>(json);
        }
    }
}
