using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaverickBank.Models
{
    public class PasswordResetOtp
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required]
        [StringLength(6)]
        public string Otp { get; set; } = string.Empty;

        [Required]
        public DateTime ExpiryDate { get; set; }

        [Required]
        public bool IsUsed { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }
    }
}