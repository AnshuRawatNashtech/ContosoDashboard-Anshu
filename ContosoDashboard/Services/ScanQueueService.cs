using System.Threading.Channels;
using ContosoDashboard.Data;
using ContosoDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Services;

public sealed record DocumentScanMessage(int DocumentId, string StorageKey, int ScanAttempt, DateTime RequestedUtc);

public interface IScanQueue
{
    ValueTask EnqueueAsync(DocumentScanMessage message, CancellationToken cancellationToken = default);
}

public sealed class LocalScanQueueService : BackgroundService, IScanQueue
{
    private readonly Channel<DocumentScanMessage> _messages = Channel.CreateUnbounded<DocumentScanMessage>();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LocalScanQueueService> _logger;

    public LocalScanQueueService(IServiceScopeFactory scopeFactory, ILogger<LocalScanQueueService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public ValueTask EnqueueAsync(DocumentScanMessage message, CancellationToken cancellationToken = default) =>
        _messages.Writer.WriteAsync(message, cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _messages.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IDocumentScanProcessor>();
                await processor.ProcessAsync(message, stoppingToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Document scan failed for document {DocumentId}.", message.DocumentId);
            }
        }
    }
}

public interface IDocumentScanProcessor
{
    Task ProcessAsync(DocumentScanMessage message, CancellationToken cancellationToken = default);
}

public sealed class DocumentScanProcessor : IDocumentScanProcessor
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _storage;
    private readonly IFileScanner _scanner;
    private readonly ILogger<DocumentScanProcessor> _logger;

    public DocumentScanProcessor(ApplicationDbContext context, IFileStorageService storage, IFileScanner scanner, ILogger<DocumentScanProcessor> logger)
    {
        _context = context;
        _storage = storage;
        _scanner = scanner;
        _logger = logger;
    }

    public async Task ProcessAsync(DocumentScanMessage message, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == message.DocumentId, cancellationToken);
        if (document is null || document.ScanStatus != DocumentScanStatus.PendingScan || document.ScanAttempt != message.ScanAttempt)
            return;

        await using var content = await _storage.DownloadAsync(message.StorageKey, cancellationToken);
        var result = await _scanner.ScanAsync(content, cancellationToken);
        document.ScanCompletedDate = DateTime.UtcNow;
        document.UpdatedDate = DateTime.UtcNow;

        if (result == FileScanResult.Clean)
        {
            var availableKey = message.StorageKey.Replace("quarantine/", "available/", StringComparison.OrdinalIgnoreCase);
            await _storage.PromoteAsync(message.StorageKey, availableKey, cancellationToken);
            document.StorageKey = availableKey;
            document.ScanStatus = DocumentScanStatus.Clean;
            document.ScanFailureReason = null;
        }
        else
        {
            document.ScanStatus = result == FileScanResult.Rejected ? DocumentScanStatus.Rejected : DocumentScanStatus.ScanUnavailable;
            document.ScanFailureReason = result == FileScanResult.Rejected ? "The file did not pass malware scanning." : "The malware scanner was unavailable.";
            await _storage.DeleteAsync(message.StorageKey, cancellationToken);
        }

        _context.DocumentActivities.Add(new DocumentActivity
        {
            DocumentId = document.DocumentId,
            UserId = document.UploadedByUserId,
            Action = $"Scan{document.ScanStatus}"
        });
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Document {DocumentId} scan completed with status {Status}.", document.DocumentId, document.ScanStatus);
    }
}
