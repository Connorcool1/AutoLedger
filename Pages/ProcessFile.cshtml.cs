using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BookkeepingApp.Services;
using BookkeepingApp.Models;
using System.Text.Json;
using System.Security.Cryptography.X509Certificates;

namespace BookkeepingApp.Pages;

public class ProcessFileModel : PageModel
{
    private readonly FileProcessingService _fileProcessingService;
    private readonly SessionService _session;
    private const string ParsedItemsSessionKey = "ParsedItems";

    public string? Message { get; set; }
    public List<Item>? ParsedItems { get; set; }

    [BindProperty]
    public Dictionary<int, TransactionType> ItemTypes { get; set; }

    public ProcessFileModel(FileProcessingService fileProcessingService, SessionService session)
    {
        _fileProcessingService = fileProcessingService;
        _session = session;
    }

    public void OnGet()
    {
        ParsedItems = _session.GetItems();
    }

    public async Task<IActionResult> OnPostAsync()
    {
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
            for (int i = 0; i < selectedIndices.Length; i++)
            {
                Item item = ParsedItems[selectedIndices[i]];
                item.Type = ItemTypes[item.Id.Value];
                FilteredItems.Add(item);
            }

            var csvContent = await _fileProcessingService.GenerateCSVContentAsync(FilteredItems);

            var fileName = $"transactions_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            return File(System.Text.Encoding.UTF8.GetBytes(csvContent), "text/csv", fileName);
        }
        catch (Exception ex)
        {
            Message = $"Error generating download: {ex.Message}";
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostFilterAsync(int[] selectedIndices)
    {
        if (selectedIndices == null || selectedIndices.Length == 0) {
            Message = $"Select some transactions to be download";
            return Page();
        }

        _session.StoreIndices(selectedIndices);

        return RedirectToPage("TypeAssign");
    }
}
