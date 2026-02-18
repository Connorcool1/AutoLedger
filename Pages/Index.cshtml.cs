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
    private readonly SessionService _session;

    public string? Message { get; set; }
    public List<Item>? ParsedItems { get; set; }

    [BindProperty]
    public Dictionary<int, TransactionType> ItemTypes { get; set; }

    public IndexModel(FileProcessingService fileProcessingService)
    {
        _fileProcessingService = fileProcessingService;
        _session = new SessionService(new HttpContextAccessor());
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
            _session.StoreItems(ParsedItems);

            Message = $"Successfully parsed {ParsedItems.Count} transactions!";
        }
        catch (Exception ex)
        {
            Message = $"Error processing file: {ex.Message}";
        }

        return RedirectToPage("ProcessFile");
    }
}
