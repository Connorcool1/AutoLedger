using BookkeepingApp.Models;
using BookkeepingApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookkeepingApp.Pages;

public class TypeAssignModel : PageModel
{
    private readonly FileProcessingService _fileProcessingService;
    private readonly SessionService _session;
    public List<Item> FilteredItems { get; set; } = new List<Item>();
    public string? Message { get; set; }

    [BindProperty]
    public Dictionary<int, TransactionType> ItemTypes { get; set; } = new Dictionary<int, TransactionType>();

    public TypeAssignModel(FileProcessingService fileProcessingService, SessionService session)
    {
        _fileProcessingService = fileProcessingService;
        _session = session;
    }
    public void OnGet()
    {
        FilteredItems = GetFilteredItems();
    }

    public async Task<IActionResult> OnPostDownloadAsync()
    {
        try
        {
            FilteredItems = GetFilteredItems();
            if (!FilteredItems.Any())
            {
                Message = $"No Items Found";
                return RedirectToPage();
            }

            for (int i = 0; i < FilteredItems.Count; i++)
            {
                FilteredItems[i].Type = ItemTypes[FilteredItems[i].Id.Value];
            }

            int year = FilteredItems[0].Date.Year;
            string month = FilteredItems[0].Date.ToString("MMMM").ToUpper();
            
            var csvContent = await _fileProcessingService.GenerateCSVContentAsync(FilteredItems);

            var fileName = $"{month}_{year}.csv";
            return File(System.Text.Encoding.UTF8.GetBytes(csvContent), "text/csv", fileName);
        }
        catch (Exception ex)
        {
            Message = $"Error generating download: {ex.Message}";
            return RedirectToPage();
        }
    }
    private List<Item> GetFilteredItems()
    {
        var parsedItems = _session.GetItems();
        var filteredIndices = _session.GetIndices();

        return filteredIndices.Select(i => parsedItems[i]).ToList();
    }
}