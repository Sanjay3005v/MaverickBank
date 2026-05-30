using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MaverickBank.Models
{
    [Index(nameof(TypeName), IsUnique = true)]
    public class AccountType
    {
        [Key]
        public int AccountTypeId { get; set; }

        [Required]
        [StringLength(50)]
        public string TypeName { get; set; } = string.Empty;
    }
}
