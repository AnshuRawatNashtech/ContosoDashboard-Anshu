using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Options;
using System.Security;

namespace ContosoDashboard.Services;

public sealed class DocumentStorageOptions
{
    public string RootPath { get; set; } = "AppData/uploads";
    public int QueuePollMilliseconds { get; set; } = 250;
}

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream content, int userId, int? projectId, string originalFileName, bool quarantine, CancellationToken cancellationToken = default);
    Task<Stream> DownloadAsync(string storageKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
    Task PromoteAsync(string quarantineKey, string availableKey, CancellationToken cancellationToken = default);
}

public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string _rootPath;

    public LocalFileStorageService(IOptions<DocumentStorageOptions> options, IWebHostEnvironment environment)
    {
        var configuredPath = options.Value.RootPath;
        _rootPath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(environment.ContentRootPath, configuredPath);
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> UploadAsync(Stream content, int userId, int? projectId, string originalFileName, bool quarantine, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var folder = quarantine ? "quarantine" : "available";
        var scope = projectId?.ToString() ?? "personal";
        var key = Path.Combine(folder, userId.ToString(), scope, $"{Guid.NewGuid():N}{extension}").Replace('\\', '/');
        var fullPath = GetSafePath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var output = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await content.CopyToAsync(output, cancellationToken);
        return key;
    }

    public Task<Stream> DownloadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = GetSafePath(storageKey);
        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = GetSafePath(storageKey);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }

    public async Task PromoteAsync(string quarantineKey, string availableKey, CancellationToken cancellationToken = default)
    {
        var source = GetSafePath(quarantineKey);
        var destination = GetSafePath(availableKey);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        await using var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        await using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await input.CopyToAsync(output, cancellationToken);
        File.Delete(source);
    }

    private string GetSafePath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || Path.IsPathRooted(storageKey))
            throw new SecurityException("Storage key must be relative.");

        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, storageKey.Replace('/', Path.DirectorySeparatorChar)));
        var root = Path.GetFullPath(_rootPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new SecurityException("Storage key resolved outside the storage root.");
        return fullPath;
    }
}
