using System.ComponentModel.DataAnnotations;

namespace MaverickBank.Models
{
    public class Role
    {
        [Key]
        public int RoleId { get; set; }
        [Required]
        [StringLength(50)]
        public string RoleName { get; set; } = string.Empty;
    }
}
