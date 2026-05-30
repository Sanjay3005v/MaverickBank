using System.ComponentModel.DataAnnotations;

namespace MaverickBank.Models
{
    public class TransactionType
    {
        [Key]
        public int TransactionTypeId { get; set; }

        [Required]
        [StringLength(50)]
        public string TypeName { get; set; } = string.Empty;
    }
}
