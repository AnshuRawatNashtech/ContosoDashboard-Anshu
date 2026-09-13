# Protected File Access Contract

## Endpoint Behavior

- `GET /documents/{documentId}/content` serves an authorized file for preview or download.
- The endpoint requires authentication and asks `IDocumentService.GetContentAsync` to re-evaluate ownership, project membership, or share access.
- The endpoint obtains the MIME type from validated metadata, sets an attachment filename from a safely encoded original name, and never exposes the storage root or storage key.
- PDF and image consumers may request inline content; other supported files are returned as downloads.
- Unauthorized and missing documents return the same not-found outcome.
- Successful content access writes a download activity.

## Storage Contract

`IFileStorageService` must support upload, delete, download, and optional temporary URL operations using relative storage keys. Implementations must never accept a user-controlled absolute path.

`IFileScanner` must inspect a stream before storage is committed and return clean, rejected, or unavailable outcomes. The default training implementation is local and deterministic; production may replace it with an approved malware scanner.