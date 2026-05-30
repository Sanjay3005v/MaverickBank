using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaverickBank.Models
{
    public class Loan
    {
        [Key]
        public long LoanId { get; set; }

        [Required]
        [ForeignKey("LoanApplication")]
        public long LoanApplicationId { get; set; }

        [Required]
        [ForeignKey("Account")]
        public long AccountId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ApprovedAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal InterestRate { get; set; }

        [Required]
        public int TenureMonths { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal EMIAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal OutstandingAmount { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        [StringLength(20)]
        public string LoanStatus { get; set; } = string.Empty;
    }
}
