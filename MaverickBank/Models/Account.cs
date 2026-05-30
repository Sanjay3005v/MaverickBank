using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaverickBank.Models
{
    [Index(nameof(AccountNumber), IsUnique = true)]
    public class Account
    {
        [Key]
        public long AccountId { get; set; }

        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required]
        [ForeignKey("Branch")]
        public int BranchID { get; set; }

        [Required]
        [ForeignKey("AccountType")]
        public int AccountTypeId { get; set; }

        [Required]
        [StringLength(18)]
        public string AccountNumber { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = string.Empty;

        [Required]
        public DateTime OpenedDate { get; set; }

        public DateTime? ClosedDate { get; set; }

    }
}
