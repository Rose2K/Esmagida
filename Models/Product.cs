using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EsmaGida.Models
{
    [Table("products")]
    public class Product
    {
        public int id { get; set; }
        
        [Required(ErrorMessage = "Ürün Adı zorunludur")]
        public string name { get; set; } = string.Empty;
        
        public string? description { get; set; }
        
        [Required(ErrorMessage = "Fiyat zorunludur")]
        public decimal price { get; set; }
        
        [Required(ErrorMessage = "Stok miktarı zorunludur")]
        public int stock_quantity { get; set; }
        
        public int? category_id { get; set; }
        
        public string? image_url { get; set; }
        
        public DateTime? created_at { get; set; } = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
    }
} 