using WebApplication1.Models;
using System.Collections.Generic;

public class ProductsPageViewModel
{
    public bool ShowLoginWall { get; set; }
    public List<Product> Products { get; set; } = new();
}