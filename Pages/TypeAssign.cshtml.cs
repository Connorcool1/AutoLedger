using BookkeepingApp.Models;
using BookkeepingApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookkeepingApp.Pages;

public class TypeAssignModel : PageModel
{
    private readonly FileProcessingService _fileProcessingService;
    private readonly SessionService _session;
    public List<Item>? ParsedItems { get; set; }
    public List<Item>? FilteredItems { get; set; }
    public int[]? FilteredIndices { get; set; }
    public string? Message { get; set; }

    [BindProperty]
    public Dictionary<int, TransactionType> ItemTypes { get; set; }

    public TypeAssignModel(FileProcessingService fileProcessingService, SessionService session)
    {
        _fileProcessingService = fileProcessingService;
        _session = session;
    }
    public void OnGet()
    {
        ParsedItems = _session.GetItems();
        FilteredIndices = _session.GetIndices();

        FilteredItems = new List<Item>();

        FilteredItems = FilteredIndices.Select(i => ParsedItems[i]).ToList();
    }

    public async Task<IActionResult> OnPostDownloadAsync()
    {
        try
        {
            for (int i = 0; i < FilteredItems.Count; i++)
            {
                FilteredItems[i].Type = ItemTypes[FilteredItems[i].Id.Value];
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
}