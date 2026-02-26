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
    public List<Item> ParsedItems { get; set; } = new List<Item>();
    public ProcessFileModel(FileProcessingService fileProcessingService, SessionService session)
    {
        _fileProcessingService = fileProcessingService;
        _session = session;
    }

    public void OnGet()
    {
        ParsedItems = _session.GetItems();
        Console.WriteLine($"Retrieved {ParsedItems.Count} items from session.");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostDownloadAsync(int[] selectedIndices)
    {
        List<Item> FilteredItems = new List<Item>();
        try
        {
            for (int i = 0; i < selectedIndices.Length; i++)
            {
                Item item = ParsedItems[selectedIndices[i]];
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
