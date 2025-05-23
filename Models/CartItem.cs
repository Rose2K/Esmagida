using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EsmaGida.Models
{
    [Table("cart_items")]
    public class CartItem
    {
        [Key]
        public int id { get; set; }
        
        [Required]
        public int cart_id { get; set; }
        
        [Required]
        public int product_id { get; set; }
        
        [Required]
        public int quantity { get; set; } = 1;
        
        public DateTime added_at { get; set; } = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
        
        // Ürünün eklendiği andaki fiyatı (fiyat değişikliklerine karşı)
        [Column(TypeName = "decimal(18,2)")]
        public decimal price_at_addition { get; set; }
    }
} 