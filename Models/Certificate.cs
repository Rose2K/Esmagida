using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EsmaGida.Models
{
    [Table("certificates")]
    public class Certificate
    {
        public int id { get; set; }
        
        [Required(ErrorMessage = "Sertifika adı zorunludur")]
        public string name { get; set; } = string.Empty;
        
        public string? description { get; set; }
        
        [Required(ErrorMessage = "Kategori zorunludur")]
        public string category { get; set; } = string.Empty;
        
        // Kategori logosu için alan
        public string? category_logo_url { get; set; }
        
        // Zorunluluğu kaldırıldı
        public string image_url { get; set; } = string.Empty;
        
        public DateTime created_at { get; set; } = DateTime.UtcNow;
        
        public bool is_active { get; set; } = true;
        
        // İsteğe bağlı olarak sertifikanın PDF dosya yolu
        public string? document_url { get; set; }
    }
} 