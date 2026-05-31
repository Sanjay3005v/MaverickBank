using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaverickBank.Models
{
    public class Beneficiary
    {
        [Key]
        public int BeneficiaryId { get; set; }

        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required]
        [StringLength(150)]
        public string BeneficiaryName { get; set; } = string.Empty;

        [Required]
        [StringLength(18)]
        public string AccountNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string BankName { get; set;  } = string.Empty;

        [Required]
        [StringLength(11)]
        public string BranchName {  get; set; } = string.Empty;

        [Required]
        [StringLength(11)]
        public string IFSCCode { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; set; }
    }
}
