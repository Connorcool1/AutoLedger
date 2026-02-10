namespace BookkeepingApp.Models;

public class Ledger
{
    public DateTime Date { get; set; }
    public Item[] Items { get; set; }
    public double Balance { get; set; }
}
