using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaverickBank.Models
{
    public class AccountClosureRequest
    {
        [Key]
        public long RequestId { get; set; }

        [Required]
        [ForeignKey("Account")]
        public long AccountId { get; set; }

        [Required]
        public int RequestedBy { get; set; }

        [Required]
        public DateTime RequestDate { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = string.Empty;

        public int? ReviewedBy { get; set; }

        public DateTime? ReviewedDate { get; set; }

        [StringLength(255)]
        public string? Remarks { get; set; }
    }
}
