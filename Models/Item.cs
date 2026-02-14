namespace BookkeepingApp.Models;


public enum TransactionType
{
    Ingredients,
    Packaging,
    Utilities,
    Advertising,
    Artwork,
    Default
}

public class Item
{
    public int? Id { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; }
    public double Amount { get; set; }
    public TransactionType Type { get; set; }
}
