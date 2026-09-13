# Quickstart Validation: Document Upload and Management

## Prerequisites

- .NET 10 SDK and SQL Server LocalDB are available.
- Run from `ContosoDashboard/`.
- Use the existing mock users in the login page: Employee, Team Lead, Project Manager, and Administrator.
- Start from a clean local database and an empty document upload directory for repeatable runs.

## Build and Run

```powershell
dotnet build
dotnet run
```

Open the HTTPS URL printed by the application and sign in through `/login`.

## Validation Scenarios

1. **Valid upload**: As an employee, upload a PDF under 25 MB with a title and category. Confirm progress, a `PendingScan` state, no preview/download while pending, eventual clean status, My Documents visibility, metadata, and a generated quarantine/available path outside `wwwroot`.
2. **Validation rejection**: Attempt a file over 25 MB, an unsupported extension, and a scanner-rejected sample. Confirm each file is rejected, no accessible file is created, and the result explains the failure.
3. **Async production scan path**: Publish a scan message to the configured Azure Queue Storage queue. Confirm the .NET isolated Azure Function consumes it, retries a transient scanner failure, moves a poison message after the retry limit, records `ScanUnavailable`, and emits Application Insights telemetry. Confirm repeated delivery of a completed message is idempotent.
4. **Project isolation**: Upload a project document as a project member. Confirm other project members can list, preview, and download it after scanning, while a non-member cannot discover it through the project page, search, or direct content URL.
5. **Ownership and manager actions**: Confirm the owner can edit metadata, replace the file, and permanently delete it. Confirm the project manager can manage project documents and an unrelated manager cannot.
6. **Sharing and notifications**: Share a clean document with a user and an authorized team. Confirm recipients receive notifications and see Shared with Me; confirm unauthorized recipients cannot be selected.
7. **Task and dashboard integration**: Attach a clean document to a project task and upload one from task context. Confirm task visibility, project association, the dashboard document count, and the five-item Recent Documents widget.
8. **Search performance and scope**: Seed up to 500 accessible documents and unauthorized documents. Search by each supported field and verify results are authorization-filtered and complete within 2 seconds in the target environment.
9. **Audit and reports**: Perform upload, download, share, and delete actions. As an administrator, confirm all activities and summary reports are present. As a non-administrator, confirm reports are denied.

## References

- Entity and relationship rules: [data-model.md](data-model.md)
- Service behavior: [contracts/document-service.md](contracts/document-service.md)
- Protected content behavior: [contracts/file-access.md](contracts/file-access.md)