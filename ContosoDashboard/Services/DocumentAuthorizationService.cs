using ContosoDashboard.Data;
using ContosoDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Services;

public sealed class DocumentAuthorizationService
{
    private readonly ApplicationDbContext _context;

    public DocumentAuthorizationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> CanViewAsync(Document document, int userId, CancellationToken cancellationToken = default)
    {
        if (document.UploadedByUserId == userId || document.ScanStatus != DocumentScanStatus.Clean)
            return document.UploadedByUserId == userId && document.ScanStatus == DocumentScanStatus.Clean;

        if (document.ProjectId.HasValue && await _context.Projects.AnyAsync(p => p.ProjectId == document.ProjectId && (p.ProjectManagerId == userId || p.ProjectMembers.Any(m => m.UserId == userId)), cancellationToken))
            return true;

        return await _context.DocumentShares.AnyAsync(s => s.DocumentId == document.DocumentId && (s.SharedWithUserId == userId || (s.SharedWithDepartment != null && _context.Users.Any(u => u.UserId == userId && u.Department == s.SharedWithDepartment))), cancellationToken);
    }

    public async Task<bool> IsProjectManagerAsync(int? projectId, int userId, CancellationToken cancellationToken = default)
    {
        return projectId.HasValue && await _context.Projects.AnyAsync(p => p.ProjectId == projectId && p.ProjectManagerId == userId, cancellationToken);
    }

    public async Task<bool> CanManageAsync(Document document, int userId, CancellationToken cancellationToken = default)
    {
        return document.UploadedByUserId == userId || await IsProjectManagerAsync(document.ProjectId, userId, cancellationToken);
    }

    public Task<bool> IsAdministratorAsync(int userId, CancellationToken cancellationToken = default) =>
        _context.Users.AnyAsync(u => u.UserId == userId && u.Role == UserRole.Administrator, cancellationToken);
}
