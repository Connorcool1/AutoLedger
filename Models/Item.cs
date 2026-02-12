namespace BookkeepingApp.Models;

public class Item
{
    public int? Id { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; }
    public double Amount { get; set; }
    public string Type { get; set; }
}
