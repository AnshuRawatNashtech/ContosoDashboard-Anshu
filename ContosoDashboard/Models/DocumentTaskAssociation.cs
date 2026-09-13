using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class DocumentTaskAssociation
{
    [Key]
    public int DocumentTaskAssociationId { get; set; }

    [Required]
    public int DocumentId { get; set; }

    [Required]
    public int TaskId { get; set; }

    [Required]
    public int CreatedByUserId { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(DocumentId))]
    public virtual Document Document { get; set; } = null!;

    [ForeignKey(nameof(TaskId))]
    public virtual TaskItem Task { get; set; } = null!;
}
