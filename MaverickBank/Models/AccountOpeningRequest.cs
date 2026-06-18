using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaverickBank.Models
{
    public class AccountOpeningRequest
    {
        [Key]
        public long RequestId { get; set; }

        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required]
        [ForeignKey("Branch")]
        public int BranchId { get; set; }

        [Required]
        [ForeignKey("AccountType")]
        public int AccountTypeId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal InitialDeposit { get; set; }

        [Required]
        public DateTime RequestDate { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = string.Empty;

        public int? ReviewedBy { get; set; }

        public DateTime? ReviewedDate { get; set; }

        [StringLength(255)]
        public string? Remarks { get; set; }

        public long? CreatedAccountId { get; set; }
    }
}
