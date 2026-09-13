using ContosoDashboard.Data;
using ContosoDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Services;

public sealed record DocumentUploadRequest(string Title, string? Description, string Category, string? Tags, int? ProjectId, string FileName, string ContentType, long FileSize, Stream Content);
public sealed record DocumentListQuery(string? Search = null, string? Category = null, int? ProjectId = null, string Sort = "date", bool Descending = true, int Page = 1, int PageSize = 50);
public sealed record DocumentContent(Stream Content, string ContentType, string FileName);

public interface IDocumentService
{
    Task<(Document? Document, string? Error)> UploadAsync(int requestingUserId, DocumentUploadRequest request, CancellationToken cancellationToken = default);
    Task<List<Document>> GetMyDocumentsAsync(int requestingUserId, DocumentListQuery query, CancellationToken cancellationToken = default);
    Task<List<Document>> GetProjectDocumentsAsync(int requestingUserId, int projectId, DocumentListQuery query, CancellationToken cancellationToken = default);
    Task<List<Document>> SearchAsync(int requestingUserId, DocumentListQuery query, CancellationToken cancellationToken = default);
    Task<Document?> GetDocumentAsync(int requestingUserId, int documentId, CancellationToken cancellationToken = default);
}

public sealed class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _storage;
    private readonly IScanQueue _scanQueue;
    private readonly DocumentAuthorizationService _authorization;

    public DocumentService(ApplicationDbContext context, IFileStorageService storage, IScanQueue scanQueue, DocumentAuthorizationService authorization)
    {
        _context = context;
        _storage = storage;
        _scanQueue = scanQueue;
        _authorization = authorization;
    }

    public async Task<(Document? Document, string? Error)> UploadAsync(int requestingUserId, DocumentUploadRequest request, CancellationToken cancellationToken = default)
    {
        var validationError = DocumentFileValidation.Validate(request.FileName, request.ContentType, request.FileSize);
        if (validationError != null)
            return (null, validationError);
        if (string.IsNullOrWhiteSpace(request.Title) || !DocumentCategories.All.Contains(request.Category, StringComparer.Ordinal))
            return (null, "A title and valid document category are required.");

        if (request.ProjectId.HasValue && !await _context.Projects.AnyAsync(p => p.ProjectId == request.ProjectId && (p.ProjectManagerId == requestingUserId || p.ProjectMembers.Any(m => m.UserId == requestingUserId)), cancellationToken))
            return (null, "You are not authorized to upload to this project.");

        var quarantineKey = await _storage.UploadAsync(request.Content, requestingUserId, request.ProjectId, request.FileName, true, cancellationToken);
        var document = new Document
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Category = request.Category,
            Tags = request.Tags?.Trim(),
            OriginalFileName = Path.GetFileName(request.FileName),
            StorageKey = quarantineKey,
            FileSize = request.FileSize,
            FileType = request.ContentType,
            UploadedByUserId = requestingUserId,
            ProjectId = request.ProjectId,
            ScanStatus = DocumentScanStatus.PendingScan,
            ScanAttempt = 1
        };

        try
        {
            _context.Documents.Add(document);
            await _context.SaveChangesAsync(cancellationToken);
            _context.DocumentActivities.Add(new DocumentActivity { DocumentId = document.DocumentId, UserId = requestingUserId, Action = "Upload" });
            await _context.SaveChangesAsync(cancellationToken);
            await _scanQueue.EnqueueAsync(new DocumentScanMessage(document.DocumentId, quarantineKey, document.ScanAttempt, DateTime.UtcNow), cancellationToken);
            return (document, null);
        }
        catch
        {
            await _storage.DeleteAsync(quarantineKey, cancellationToken);
            throw;
        }
    }

    public Task<List<Document>> GetMyDocumentsAsync(int requestingUserId, DocumentListQuery query, CancellationToken cancellationToken = default) =>
        QueryAccessible(_context.Documents.Where(d => d.UploadedByUserId == requestingUserId), query, cancellationToken);

    public Task<List<Document>> GetProjectDocumentsAsync(int requestingUserId, int projectId, DocumentListQuery query, CancellationToken cancellationToken = default) =>
        QueryAccessible(_context.Documents.Where(d => d.ProjectId == projectId && d.ScanStatus == DocumentScanStatus.Clean && (d.UploadedByUserId == requestingUserId || d.Project!.ProjectManagerId == requestingUserId || d.Project.ProjectMembers.Any(m => m.UserId == requestingUserId))), query, cancellationToken);

    public Task<List<Document>> SearchAsync(int requestingUserId, DocumentListQuery query, CancellationToken cancellationToken = default)
    {
        var documents = _context.Documents
            .Where(d => d.ScanStatus == DocumentScanStatus.Clean && (d.UploadedByUserId == requestingUserId ||
                (d.ProjectId.HasValue && (d.Project!.ProjectManagerId == requestingUserId || d.Project.ProjectMembers.Any(m => m.UserId == requestingUserId))) ||
                d.Shares.Any(s => s.SharedWithUserId == requestingUserId)));
        return QueryAccessible(documents, query, cancellationToken);
    }

    public async Task<Document?> GetDocumentAsync(int requestingUserId, int documentId, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.Include(d => d.Project).FirstOrDefaultAsync(d => d.DocumentId == documentId, cancellationToken);
        return document != null && await _authorization.CanViewAsync(document, requestingUserId, cancellationToken) ? document : null;
    }

    private static async Task<List<Document>> QueryAccessible(IQueryable<Document> query, DocumentListQuery options, CancellationToken cancellationToken)
    {
        query = query.Include(d => d.Project).Include(d => d.UploadedByUser).Where(d => d.ScanStatus == DocumentScanStatus.Clean);
        if (!string.IsNullOrWhiteSpace(options.Search))
        {
            var search = options.Search.Trim();
            query = query.Where(d => d.Title.Contains(search) || (d.Description != null && d.Description.Contains(search)) || (d.Tags != null && d.Tags.Contains(search)) || d.OriginalFileName.Contains(search));
        }
        if (!string.IsNullOrWhiteSpace(options.Category))
            query = query.Where(d => d.Category == options.Category);
        if (options.ProjectId.HasValue)
            query = query.Where(d => d.ProjectId == options.ProjectId);

        query = options.Sort.ToLowerInvariant() switch
        {
            "title" => options.Descending ? query.OrderByDescending(d => d.Title) : query.OrderBy(d => d.Title),
            "category" => options.Descending ? query.OrderByDescending(d => d.Category) : query.OrderBy(d => d.Category),
            "size" => options.Descending ? query.OrderByDescending(d => d.FileSize) : query.OrderBy(d => d.FileSize),
            _ => options.Descending ? query.OrderByDescending(d => d.UploadedDate) : query.OrderBy(d => d.UploadedDate)
        };
        return await query.Skip(Math.Max(0, options.Page - 1) * Math.Clamp(options.PageSize, 1, 100)).Take(Math.Clamp(options.PageSize, 1, 100)).ToListAsync(cancellationToken);
    }
}
