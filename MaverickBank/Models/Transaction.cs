using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaverickBank.Models
{
    [Index(nameof(TransactionReference), IsUnique = true)]
    public class Transaction
    {
        [Key]
        public long TransactionId { get; set; }

        [Required]
        [ForeignKey("TransactionType")]
        public int TransactionTypeId { get; set; }

        public long? FromAccountId { get; set; }

        public long? ToAccountId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(100)]
        public string TransactionReference { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        [Required]
        [StringLength(20)]
        public string TransactionStatus { get; set; } = string.Empty;

        [Required]
        public DateTime TransactionDate { get; set; }

    }
}
