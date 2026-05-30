using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaverickBank.Models
{
    public class AuditLog
    {
        [Key]
        public long AuditLogId { get; set; }

        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required]
        [StringLength(255)]
        public string Action { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string EntityName { get; set; } = string.Empty;

        [Required]
        public long EntityId { get; set; }

        public string? OldValues { get; set; }

        public string? NewValues { get; set; }

        [Required]
        public DateTime ActionDate { get; set; }
    }
}
