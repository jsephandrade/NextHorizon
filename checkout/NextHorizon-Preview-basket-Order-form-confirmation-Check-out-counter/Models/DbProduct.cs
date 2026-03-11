using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyAspNetApp.Models
{
    [Table("Products")]
    public class DbProduct
    {
        [Key]
        public int ProductId { get; set; }

        [Required]
        public string ProductName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Discount { get; set; }

        public string? Category { get; set; }

        public string? Details { get; set; }

        public string? ImagePath { get; set; }

        public string? Status { get; set; } = "active";

        public string? Brand { get; set; }

        public int Stock { get; set; } = 0;

        public string? Colors { get; set; }

        public string? Sizes { get; set; }

        public string? ColorStocks { get; set; }

        public string? ColorSizes { get; set; }
    }

    [Table("ProductColorImages")]
    public class DbProductColorImage
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }

        [Required]
        public string ColorName { get; set; } = string.Empty;

        [Required]
        public string ImagePath { get; set; } = string.Empty;
    }
}
