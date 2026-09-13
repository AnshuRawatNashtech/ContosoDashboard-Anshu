# Document Service Contract

The application exposes this contract through the scoped `IDocumentService`. Every method receives the authenticated requesting user context or derives it from the trusted authentication state; callers must not supply an arbitrary user scope.

## Operations

- `UploadAsync(requestingUserId, uploadRequest)`: validate metadata, size, supported type, and project authority; write content to quarantine, persist `PendingScan` metadata, enqueue a scan message, and return a pending document result. The document is not accessible until the worker records `Clean`.
- `GetMyDocumentsAsync(requestingUserId, query)`: return only documents uploaded by the user, with server-side filters, sorting, and paging.
- `GetProjectDocumentsAsync(requestingUserId, projectId, query)`: return only documents visible to a project member or manager.
- `GetSharedDocumentsAsync(requestingUserId, query)`: return only documents granted to the user or the user’s authorized team.
- `SearchAsync(requestingUserId, query)`: apply authorization scope before matching title, description, tags, uploader, or project.
- `GetDocumentAsync(requestingUserId, documentId)`: return metadata only when the user may access it.
- `GetContentAsync(requestingUserId, documentId)`: return a readable stream and content type only after authorization; write a download activity.
- `UpdateMetadataAsync(requestingUserId, documentId, metadata)`: permit owner or authorized project manager only.
- `ReplaceFileAsync(requestingUserId, documentId, file)`: scan and store the replacement before switching the document key; no history is retained.
- `DeleteAsync(requestingUserId, documentId)`: permit owner or authorized project manager, permanently remove content and metadata, and retain audit activity.
- `ShareAsync(requestingUserId, documentId, recipients)`: validate recipient authority, persist grants, and create in-app notifications.
- `AttachToTaskAsync(requestingUserId, documentId, taskId)`: validate task/project access and create the association.
- `GetAdminReportAsync(requestingUserId, reportQuery)`: administrator-only aggregate document type, uploader, and access activity.

## Scan Queue Contract

- Message fields: `DocumentId`, `StorageKey`, `ScanAttempt`, and `RequestedUtc`.
- The web application publishes one message after the quarantine file and pending metadata are durable.
- A .NET isolated Azure Function with a Queue Storage trigger consumes the message, reads only the referenced quarantine object, invokes `IFileScanner`, and updates the document status idempotently.
- Clean results promote the file and make it accessible; rejected results delete or retain quarantined content according to retention policy while keeping the document inaccessible; transient failures throw for retry.
- After the configured dequeue limit, the message is moved to a poison queue and the document becomes `ScanUnavailable` with an administrator-visible operational event.
- The offline adapter must preserve the same message and state-transition contract without requiring Azure resources.

## Error Behavior

- Invalid files return a user-readable validation result without creating a document.
- Malware or scanner failure makes the file unavailable and returns a non-sensitive failure message.
- Missing or unauthorized documents behave as not found to avoid disclosing their existence.
- Storage failure does not commit document metadata; cleanup is attempted for any newly written uncommitted file.