using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ContosoDocumentScanner.Functions;

public sealed class ScanQuarantinedFileFunction
{
    private readonly ILogger<ScanQuarantinedFileFunction> _logger;

    public ScanQuarantinedFileFunction(ILogger<ScanQuarantinedFileFunction> logger)
    {
        _logger = logger;
    }

    [Function(nameof(ScanQuarantinedFileFunction))]
    public Task RunAsync(
        [QueueTrigger("document-scan-requests", Connection = "AzureWebJobsStorage")] string message)
    {
        _logger.LogInformation("Received document scan request: {Message}", message);
        // The production implementation will use the shared scan contract and application data access.
        // Keeping the trigger isolated allows the web app to remain offline-capable during training.
        return Task.CompletedTask;
    }
}
