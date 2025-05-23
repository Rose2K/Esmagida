using System;
using System.ComponentModel.DataAnnotations;

namespace EsmaGida.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public required string name { get; set; }

        [Required]
        public required string surname { get; set; }

        [Required]
        [EmailAddress]
        public required string email { get; set; }

        [Required]
        public required string password { get; set; } // Şifre Hash olarak saklanacak

        [Required]
        public string Role { get; set; } = "User"; // Default olarak "User", admin için "Admin" olacak

        public string? ResetPasswordToken { get; set; }
        public DateTime? ResetPasswordTokenExpiry { get; set; }
    }
}
