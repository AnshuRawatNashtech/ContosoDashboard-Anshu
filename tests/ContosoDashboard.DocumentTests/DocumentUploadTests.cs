using ContosoDashboard.Models;
using ContosoDashboard.Services;
using Xunit;

namespace ContosoDashboard.DocumentTests;

public class DocumentUploadTests
{
    [Fact]
    public void RejectsFilesOver25Mb()
    {
        var error = DocumentFileValidation.Validate("report.pdf", "application/pdf", DocumentFileValidation.MaxFileSize + 1);

        Assert.NotNull(error);
    }

    [Fact]
    public void AcceptsSupportedPdf()
    {
        var error = DocumentFileValidation.Validate("report.pdf", "application/pdf", 1024);

        Assert.Null(error);
    }

    [Fact]
    public void RejectsMismatchedMimeType()
    {
        var error = DocumentFileValidation.Validate("report.pdf", "text/plain", 1024);

        Assert.NotNull(error);
    }

    [Fact]
    public void NewDocumentsStartPendingScan()
    {
        var document = new Document();

        Assert.Equal(DocumentScanStatus.PendingScan, document.ScanStatus);
    }
}
