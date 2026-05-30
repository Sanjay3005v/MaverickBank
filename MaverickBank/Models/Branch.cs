using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MaverickBank.Models
{
    [Index(nameof(IFSCCode), IsUnique = true)]
    public class Branch
    {
        [Key]
        public int BranchId { get; set; }

        [Required]
        [StringLength(150)]
        public string BranchName { get; set; } = string.Empty;

        [Required]
        [StringLength(11)]
        public string IFSCCode { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string AddressLine1 { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string State { get; set; } = string.Empty;

        [Required]
        [StringLength(6)]
        public string Pincode { get; set; } = string.Empty;

        [Required]
        [StringLength(15)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; set; }

    }
}
