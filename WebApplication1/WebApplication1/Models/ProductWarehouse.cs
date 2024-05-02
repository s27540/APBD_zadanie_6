namespace WebApplication1.Models;

public class ProductWarehouse
{
    public int IdProductwarehouse { get; set; }
    public int IdWarehouse { get; set; }
    public int IdProduct { get; set; }
    public int IdOrder { get; set; }
    public int Amount { get; set; }
    public Decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }

}