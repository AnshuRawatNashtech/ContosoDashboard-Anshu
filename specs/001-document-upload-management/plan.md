# Implementation Plan: Document Upload and Management

**Branch**: `001-document-upload-management` | **Date**: 2026-09-12 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-document-upload-management/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Add secure, searchable document storage to the existing Blazor Server dashboard while preserving user and project isolation. Implement the first release with EF Core metadata, quarantine storage outside `wwwroot`, an asynchronous scan queue, service-level authorization, protected file endpoints, and integrations with projects, tasks, notifications, and the dashboard. In production, an Azure Functions .NET isolated worker consumes Azure Queue Storage scan messages; the offline training path uses the same queue contract with a local adapter.

## Technical Context

<!--
  ACTION REQUIRED: Replace the content in this section with the technical details
  for the project. The structure here is presented in advisory capacity to guide
  the iteration process.
-->

**Language/Version**: C# on .NET 10.0  
**Primary Dependencies**: ASP.NET Core Blazor Server, EF Core 10 SQL Server provider, existing cookie authentication and Bootstrap UI  
**Storage**: SQL Server LocalDB for metadata; local filesystem outside `wwwroot` for training files; Azure Blob Storage and Queue Storage in the production scan pipeline; `IFileStorageService` and `IScanQueue` boundaries keep implementations replaceable  
**Testing**: `dotnet build`; focused service/component tests if a test project is added during implementation; manual quickstart scenarios for authorization and file workflows  
**Target Platform**: Windows-friendly ASP.NET Core web application, offline-capable for training  
**Project Type**: Single web application  
**Performance Goals**: 25 MB upload within 30 seconds, 500-document lists within 2 seconds, authorized searches within 2 seconds, PDF/image preview within 3 seconds  
**Constraints**: Maximum 25 MB per file; supported PDF, Office, JPEG, PNG, and text types; scan completion is asynchronous and files remain unavailable while pending; no storage quotas, version history, soft delete, collaborative editing, external integrations, or mobile app  
**Scale/Scope**: Up to 5,000 employees; document metadata, sharing, task/project/dashboard integration, notifications, audit events, and administrator reports

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- PASS: Security and User Isolation. Every document query and mutation will use the authenticated user and project/share scope, and file responses will re-authorize access.
- PASS: User-Scoped Authorization. The service boundary will own authorization; UI visibility will not be treated as security.
- PASS: Spec-Driven Delivery. Design artifacts and implementation tasks remain under the feature directory.
- PASS: Quality and Verification. Build validation and executable quickstart scenarios are defined before implementation.
- PASS: Simplicity and Maintainability. The change stays in the existing models/data/services/pages boundaries and adds only storage/scanning abstractions required by the specification.
- PASS: Offline Training Constraint. The default implementation uses local storage and a replaceable scanner/storage contract without cloud SDK dependencies.
- PASS: Async Scan Safety. Uploads enter quarantine and remain inaccessible until a scan result is persisted; queue retries and poison-message handling prevent silent availability of unscanned files.

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)
<!--
  ACTION REQUIRED: Replace the placeholder tree below with the concrete layout
  for this feature. Delete unused options and expand the chosen structure with
  real paths (e.g., apps/admin, packages/something). The delivered plan must
  not include Option labels.
-->

```text
ContosoDashboard/
├── Data/ApplicationDbContext.cs
├── Models/
│   ├── Document.cs
│   ├── DocumentShare.cs
│   ├── DocumentActivity.cs
│   └── existing User, Project, TaskItem, Notification models
├── Services/
│   ├── DocumentService.cs
│   ├── FileStorageService.cs
│   ├── FileScannerService.cs
│   ├── ScanQueueService.cs
│   ├── DashboardService.cs
│   └── NotificationService.cs
├── Pages/
│   ├── Documents.razor
│   ├── ProjectDetails.razor
│   ├── Tasks.razor
│   └── Index.razor
├── Controllers/DocumentFileController.cs
├── Program.cs
└── AppData/uploads/ (runtime-only, outside wwwroot)

ContosoDocumentScanner.Functions/
├── ScanQuarantinedFileFunction.cs
├── host.json
└── local.settings.json (local development only)
```

**Structure Decision**: Extend the existing ASP.NET Core project for the user-facing workflow and add a small separate .NET isolated Azure Functions project for the production scan worker. EF Core entities and relationships belong in `Models` and `Data`; authorization and orchestration belong in `Services`; Blazor pages own presentation state; a controller or minimal endpoint serves files so every download and preview is authorized server-side. Runtime uploads remain outside `wwwroot` and are represented in the database by relative storage keys. The app writes a pending document and quarantine storage key, enqueues a scan message, and exposes no content until the worker marks the document clean.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | N/A | The feature fits the existing single-project structure and does not require a new application or repository layer. |
