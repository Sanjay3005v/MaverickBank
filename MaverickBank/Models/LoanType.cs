using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaverickBank.Models
{
    public class LoanType
    {
        [Key]
        public int LoanTypeId { get; set; }

        [Required]
        [StringLength(100)]
        public string LoanName { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal InterestRate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MinimumAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MaximumAmount { get; set; }

        [Required]
        public int MinimumTenureMonths { get; set; }

        [Required]
        public int MaximumTenureMonths { get; set; }

    }
}
