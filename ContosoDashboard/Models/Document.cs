using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public enum DocumentScanStatus
{
    PendingScan,
    Clean,
    Rejected,
    ScanUnavailable
}

public static class DocumentCategories
{
    public static readonly string[] All =
    [
        "Project Documents",
        "Team Resources",
        "Personal Files",
        "Reports",
        "Presentations",
        "Other"
    ];
}

public class Document
{
    [Key]
    public int DocumentId { get; set; }

    [Required, MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required, MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Tags { get; set; }

    [Required, MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required, MaxLength(1000)]
    public string StorageKey { get; set; } = string.Empty;

    public long FileSize { get; set; }

    [Required, MaxLength(255)]
    public string FileType { get; set; } = string.Empty;

    [Required]
    public int UploadedByUserId { get; set; }

    public int? ProjectId { get; set; }

    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    public DocumentScanStatus ScanStatus { get; set; } = DocumentScanStatus.PendingScan;
    public int ScanAttempt { get; set; }
    public DateTime? ScanCompletedDate { get; set; }

    [MaxLength(500)]
    public string? ScanFailureReason { get; set; }

    [ForeignKey(nameof(UploadedByUserId))]
    public virtual User UploadedByUser { get; set; } = null!;

    [ForeignKey(nameof(ProjectId))]
    public virtual Project? Project { get; set; }

    public virtual ICollection<DocumentShare> Shares { get; set; } = new List<DocumentShare>();
    public virtual ICollection<DocumentTaskAssociation> TaskAssociations { get; set; } = new List<DocumentTaskAssociation>();
}
