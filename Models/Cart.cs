using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EsmaGida.Models
{
    [Table("carts")]
    public class Cart
    {
        [Key]
        public int id { get; set; }
        
        [Required]
        public int user_id { get; set; }
        
        public DateTime created_at { get; set; } = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
        
        public DateTime? updated_at { get; set; }
        
        // Sepet durumu (aktif, tamamlandı, iptal edildi vb.)
        public string status { get; set; } = "active";
        
        // Sepetteki öğelerin listesi
        public virtual ICollection<CartItem>? CartItems { get; set; }
    }
} 