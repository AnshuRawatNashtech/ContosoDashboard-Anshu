namespace ContosoDashboard.Services;

public enum FileScanResult
{
    Clean,
    Rejected,
    Unavailable
}

public interface IFileScanner
{
    Task<FileScanResult> ScanAsync(Stream content, CancellationToken cancellationToken = default);
}

public sealed class LocalFileScanner : IFileScanner
{
    private const string EicarTestSignature = "X5O!P%@AP[4\\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*";

    public async Task<FileScanResult> ScanAsync(Stream content, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(content, leaveOpen: true);
        var text = await reader.ReadToEndAsync(cancellationToken);
        content.Position = 0;
        return text.Contains(EicarTestSignature, StringComparison.Ordinal)
            ? FileScanResult.Rejected
            : FileScanResult.Clean;
    }
}

public static class DocumentFileValidation
{
    public const long MaxFileSize = 25 * 1024 * 1024;

    private static readonly IReadOnlyDictionary<string, string[]> SupportedTypes =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = ["application/pdf"],
            [".doc"] = ["application/msword"],
            [".docx"] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
            [".xls"] = ["application/vnd.ms-excel"],
            [".xlsx"] = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"],
            [".ppt"] = ["application/vnd.ms-powerpoint"],
            [".pptx"] = ["application/vnd.openxmlformats-officedocument.presentationml.presentation"],
            [".txt"] = ["text/plain"],
            [".jpg"] = ["image/jpeg"],
            [".jpeg"] = ["image/jpeg"],
            [".png"] = ["image/png"]
        };

    public static string? Validate(string fileName, string contentType, long size)
    {
        if (size <= 0 || size > MaxFileSize)
            return "Each document must be larger than zero and no more than 25 MB.";

        var extension = Path.GetExtension(fileName);
        if (!SupportedTypes.TryGetValue(extension, out var mimeTypes) || !mimeTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            return "This file type is not supported. Upload a PDF, Office document, image, or text file.";

        return null;
    }
}
