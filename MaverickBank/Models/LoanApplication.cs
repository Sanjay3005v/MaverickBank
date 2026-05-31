using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaverickBank.Models
{
    public class LoanApplication
    {
        [Key]
        public long LoanApplicationId { get; set; }

        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required]
        [ForeignKey("LoanType")]
        public int LoanTypeId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal RequestedAmount { get; set; }

        [Required]
        public int TenureMonths { get; set; }

        [Required]
        [StringLength(255)]
        public string Purpose { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyIncome { get; set; }

        [Required]
        [StringLength(20)]
        public string ApplicationStatus { get; set; } = string.Empty;

        [Required]
        public DateTime AppliedDate { get; set; }

        public int? ReviewedBy { get; set; }

        public DateTime? ReviewedDate { get; set; }

        [StringLength(255)]
        public string? Remarks { get; set; }
    }
}
