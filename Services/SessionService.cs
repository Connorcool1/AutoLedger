using BookkeepingApp.Pages;
using BookkeepingApp.Models;
using System.Text.Json;

public class SessionService {
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string ParsedItemsSessionKey = "ParsedItems";
    private const string IndicesSessionKey = "indices";

    public SessionService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void StoreItems(List<Item>? items)
    {
        var json = JsonSerializer.Serialize(items);
        _httpContextAccessor.HttpContext.Session.SetString(ParsedItemsSessionKey, json);
    }

    public void StoreIndices(int[] indices)
    {
        var json = JsonSerializer.Serialize(indices);
        _httpContextAccessor.HttpContext.Session.SetString(IndicesSessionKey, json);
    }

    public List<Item>? GetItems()
    {
        var json = _httpContextAccessor.HttpContext.Session.GetString(ParsedItemsSessionKey);
        if (!string.IsNullOrEmpty(json))
        {
            return JsonSerializer.Deserialize<List<Item>>(json);
        }
        return null;
    }
    public int[]? GetIndices()
    {
        var json = _httpContextAccessor.HttpContext.Session.GetString(IndicesSessionKey);
        if (!string.IsNullOrEmpty(json))
        {
            return JsonSerializer.Deserialize<int[]>(json);
        }
        return null;
    }

    // ask about the null values ^^ why quesion mark
}