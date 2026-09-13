# Tasks: Document Upload and Management

**Input**: Design documents from `/specs/001-document-upload-management/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

## Phase 1: Setup

- [X] T001 Create the `ContosoDocumentScanner.Functions/` .NET isolated worker project and add it to `ContosoDashboard.sln` with Azure Functions host v4 configuration in `ContosoDocumentScanner.Functions/ContosoDocumentScanner.Functions.csproj` and `ContosoDocumentScanner.Functions/host.json`
- [X] T002 [P] Create local scanner configuration placeholders, excluding secrets, in `ContosoDocumentScanner.Functions/local.settings.json.example` and document required queue/storage settings in `specs/001-document-upload-management/quickstart.md`
- [X] T003 [P] Add the document runtime storage root and quarantine/available directory configuration keys to `ContosoDashboard/appsettings.json` and `ContosoDashboard/appsettings.Development.json`

## Phase 2: Foundational

- [X] T004 Add integer-key document entities, relationships, validation attributes, and scan state fields to `ContosoDashboard/Models/Document.cs`, `ContosoDashboard/Models/DocumentShare.cs`, `ContosoDashboard/Models/DocumentActivity.cs`, and `ContosoDashboard/Models/DocumentTaskAssociation.cs`
- [X] T005 Add document DbSets, relationships, indexes, delete behavior, and notification enum values to `ContosoDashboard/Data/ApplicationDbContext.cs` and `ContosoDashboard/Models/Notification.cs`
- [X] T006 [P] Add storage, scanner, and queue message contracts in `ContosoDashboard/Services/FileStorageService.cs`, `ContosoDashboard/Services/FileScannerService.cs`, and `ContosoDashboard/Services/ScanQueueService.cs`; define the message fields `DocumentId`, `StorageKey`, `ScanAttempt`, and `RequestedUtc`
- [X] T007 [P] Register document, storage, scanner, queue, and audit services in `ContosoDashboard/Program.cs` using local implementations by default and configuration seams for production adapters
- [X] T008 Add centralized document authorization helpers for owner, project member, project manager, recipient, and administrator checks in `ContosoDashboard/Services/DocumentAuthorizationService.cs`; ensure direct document identifiers are always re-authorized
- [X] T009 Create a focused test project or test harness for document service and scan state transitions in `tests/ContosoDashboard.DocumentTests/ContosoDashboard.DocumentTests.csproj`, including in-memory/local storage doubles and deterministic scanner results

## Phase 3: User Story 1 - Upload and Organize Work Documents (Priority: P1) 🎯 MVP

**Goal**: Employees can upload supported documents into quarantine, receive per-file progress/results, wait for asynchronous scanning, and see clean documents in My Documents or an authorized project view.

**Independent Test**: Upload a valid PDF under 25 MB as an authenticated employee, verify `PendingScan`, process the scan message, verify `Clean` and My Documents visibility, then verify oversized, unsupported, and rejected files remain inaccessible.

- [X] T010 [P] [US1] Implement generated relative quarantine and available storage keys, safe filenames, stream upload/download/delete, and path traversal protection in `ContosoDashboard/Services/FileStorageService.cs`
- [X] T011 [P] [US1] Implement supported MIME/extension validation, 25 MB per-file validation, required title/category validation, and deterministic local clean/rejected/unavailable scanner behavior in `ContosoDashboard/Services/FileScannerService.cs`
- [X] T012 [US1] Implement `DocumentService.UploadAsync` to validate authorization, write the file to quarantine before metadata commit, persist `PendingScan`, publish one scan message, and clean up uncommitted storage failures in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T013 [US1] Implement the .NET isolated Queue Storage-triggered scan worker to read only the referenced quarantine object, invoke scanning, promote clean content, persist `Clean`/`Rejected`/`ScanUnavailable`, and remain idempotent in `ContosoDocumentScanner.Functions/ScanQuarantinedFileFunction.cs`
- [X] T014 [US1] Configure bounded queue retries, visibility timeout, poison-queue handling, and Application Insights logging for scan failures in `ContosoDocumentScanner.Functions/host.json` and `ContosoDocumentScanner.Functions/ScanQuarantinedFileFunction.cs`
- [X] T015 [US1] Add the local queue adapter that executes the same scan message and state transition contract without cloud services in `ContosoDashboard/Services/ScanQueueService.cs`
- [X] T016 [US1] Implement My Documents and project document queries with server-side paging, category/project/date filters, title/date/category/size sorting, and exclusion of non-clean documents in `ContosoDashboard/Services/DocumentService.cs`
- [X] T017 [US1] Build the authenticated upload and document browsing UI with multi-file selection, `@key` InputFile reset, copied browser streams, metadata fields, progress/results, pending scan status, and project/category filters in `ContosoDashboard/Pages/Documents.razor`
- [X] T018 [US1] Add project document listing and authorized upload entry points to `ContosoDashboard/Pages/ProjectDetails.razor`
- [X] T019 [US1] Add document service transition tests for valid upload, unsupported type, over-25-MB rejection, quarantine-before-metadata ordering, pending visibility, clean promotion, and rejected scan behavior in `tests/ContosoDashboard.DocumentTests/DocumentUploadTests.cs`

**Checkpoint**: A clean scanned document can be uploaded and found by its owner or authorized project members; unscanned or rejected content is never accessible.

## Phase 4: User Story 2 - Find, Preview, and Manage Accessible Documents (Priority: P1)

**Goal**: Authorized users can search, sort, filter, preview, download, edit, replace, and permanently delete accessible documents while all direct file requests enforce authorization.

**Independent Test**: Seed clean documents for multiple users/projects, verify authorized search/list results and performance, preview/download a PDF/image, edit and replace an owned document, delete it, and verify an unauthorized user receives no document existence signal.

- [ ] T020 [US2] Implement authorization-filtered search across title, description, tags, uploader, and project with indexed server-side query composition in `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Data/ApplicationDbContext.cs`
- [ ] T021 [US2] Implement metadata update, clean-file replacement through quarantine and re-scan, and permanent delete with storage cleanup and audit preservation in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T022 [US2] Implement the protected preview/download endpoint at `GET /documents/{documentId}/content` with identical not-found behavior for missing/unauthorized documents, validated MIME handling, safe attachment names, and download auditing in `ContosoDashboard/Controllers/DocumentFileController.cs`
- [ ] T023 [P] [US2] Add search/list/preview/download/edit/replace/delete controls and empty/loading states to `ContosoDashboard/Pages/Documents.razor`
- [ ] T024 [US2] Add project-manager document management controls and authorization-aware document actions to `ContosoDashboard/Pages/ProjectDetails.razor`
- [ ] T025 [US2] Add performance indexes and query projections needed for 500-document lists and authorized searches in `ContosoDashboard/Data/ApplicationDbContext.cs` and `ContosoDashboard/Services/DocumentService.cs`
- [ ] T026 [US2] Add tests for owner/project-manager permissions, revoked access, IDOR-resistant direct content requests, search field coverage, preview MIME behavior, replacement scan lifecycle, concurrent stale mutations, and permanent deletion in `tests/ContosoDashboard.DocumentTests/DocumentManagementTests.cs`

**Checkpoint**: User Story 1 remains functional, and authorized users can efficiently manage clean documents without exposing unauthorized metadata or content.

## Phase 5: User Story 3 - Share Documents and Use Them in Workflows (Priority: P2)

**Goal**: Owners share clean documents with authorized users/teams, recipients receive notifications, and clean documents can be attached to tasks and surfaced in dashboard/project workflows.

**Independent Test**: Share a clean document with a user and authorized team, verify notifications and Shared with Me visibility, attach it to a task, upload from task context, and verify project association and access.

- [ ] T027 [US3] Implement share grants, duplicate-target prevention, recipient/team authorization, Shared with Me queries, and clean-status requirement in `ContosoDashboard/Models/DocumentShare.cs` and `ContosoDashboard/Services/DocumentService.cs`
- [ ] T028 [US3] Implement document share, new project document, and scan-result notification creation using existing notification patterns in `ContosoDashboard/Services/NotificationService.cs` and `ContosoDashboard/Models/Notification.cs`
- [ ] T029 [US3] Implement task attachment authorization, task/project consistency validation, and document-task association persistence in `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Data/ApplicationDbContext.cs`
- [ ] T030 [US3] Add Shared with Me, sharing recipient selection, notification feedback, and document share actions to `ContosoDashboard/Pages/Documents.razor`
- [ ] T031 [US3] Add task document list, attach existing document, and upload-from-task flow to the task detail surface in `ContosoDashboard/Pages/TaskDetails.razor` or the repository’s task detail component
- [ ] T032 [US3] Add project document notifications and the Recent Documents widget plus document count to `ContosoDashboard/Pages/Index.razor` and `ContosoDashboard/Services/DashboardService.cs`
- [ ] T033 [US3] Add sharing, recipient authorization, notification, task association, project notification, and dashboard integration tests in `tests/ContosoDashboard.DocumentTests/DocumentCollaborationTests.cs`

**Checkpoint**: Clean documents can move through sharing and task/dashboard workflows without weakening project or user isolation.

## Phase 6: User Story 4 - Monitor Document Activity (Priority: P3)

**Goal**: Administrators can audit document actions and view document usage reports; non-administrators cannot access administrative reporting.

**Independent Test**: Perform upload, download, delete, share, and scan actions, verify immutable activity records and administrator summaries, then verify report access is denied for other roles.

- [ ] T034 [US4] Implement immutable upload, download, deletion, sharing, and scan activity recording with actor, document reference, action, and UTC timestamp in `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Models/DocumentActivity.cs`
- [ ] T035 [US4] Implement administrator-only document type, uploader activity, access-pattern, scan outcome, and poison-message operational reports in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T036 [US4] Build the administrator document audit/report view with role enforcement, filters, summary tables, and empty/error states in `ContosoDashboard/Pages/DocumentReports.razor`
- [ ] T037 [US4] Add audit/report authorization and completeness tests, including non-administrator denial and deletion audit retention, in `tests/ContosoDashboard.DocumentTests/DocumentAuditTests.cs`

**Checkpoint**: Document activity is accountable and administrator reporting is isolated from ordinary users.

## Phase 7: Polish and Cross-Cutting Validation

- [ ] T038 [P] Add security headers, HTTPS/TLS configuration notes, storage encryption configuration, and production Azure identity/private-endpoint settings to `ContosoDashboard/Program.cs`, `ContosoDashboard/appsettings.json`, and `specs/001-document-upload-management/research.md`
- [ ] T039 [P] Add production Function App deployment configuration for authenticated .NET isolated Functions, Flex Consumption, Queue Storage, Application Insights, and secure settings in `ContosoDocumentScanner.Functions/infra/` and `ContosoDocumentScanner.Functions/README.md`
- [ ] T040 [P] Update navigation and user-facing documentation for Documents, Shared with Me, project/task attachments, and administrator reports in `ContosoDashboard/Shared/NavMenu.razor` and `README.md`
- [ ] T041 Run `dotnet build` for `ContosoDashboard/ContosoDashboard.csproj` and `ContosoDocumentScanner.Functions/ContosoDocumentScanner.Functions.csproj`, then fix only feature-related compile errors
- [ ] T042 Run the scenarios in `specs/001-document-upload-management/quickstart.md`, including the async queue retry/poison/idempotency checks, and record any environment limitations in `specs/001-document-upload-management/quickstart.md`
- [ ] T043 Verify list/search/upload/preview performance against SC-005 through SC-008 with representative 25 MB files, 500-document lists, and authorized/unauthorized search datasets in `specs/001-document-upload-management/quickstart.md`

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; T001 establishes the Functions project and T002-T003 can run in parallel.
- **Foundational (Phase 2)**: Depends on Setup; T004-T008 establish shared model, storage, queue, registration, and authorization boundaries. T009 can begin after the contracts are defined and blocks automated story validation.
- **User Stories**: Depend on Foundational. US1 is the MVP and must establish the clean-document lifecycle before US2-US4 consume it.
- **Polish (Phase 7)**: Depends on the desired user stories and both application/worker builds.

### User Story Dependencies

- **US1 (P1)**: Starts after Phase 2. No story dependency; delivers the MVP upload and organization slice.
- **US2 (P1)**: Starts after Phase 2 but requires the clean-document state and service contracts from US1 before its integration checkpoint.
- **US3 (P2)**: Requires clean accessible documents and the service/query boundaries from US1-US2.
- **US4 (P3)**: Can implement audit primitives during Phase 2/US1 but its complete reporting depends on activity-producing operations from US1-US3.

### Parallel Execution Examples

- **US1**: T010, T011, and T017 can proceed in parallel after T006-T008; T012-T015 then converge on the upload/scan lifecycle. T019 follows the service contracts.
- **US2**: T020, T022, and T023 can proceed in parallel after T012; T021 and T024 depend on authorization/service behavior. T026 validates the complete slice.
- **US3**: T027, T028, and T029 can proceed in parallel after US1/US2 service contracts; T030-T032 follow their respective service work. T033 validates integration.
- **US4**: T034 and T035 can proceed in parallel after the activity model exists; T036 follows report queries and T037 validates authorization.
- **Polish**: T038-T040 can proceed in parallel; T041 and T042 follow implementation, and T043 follows representative data setup.

## Implementation Strategy

### MVP First

1. Complete T001-T009.
2. Complete US1, especially T012-T015 so the asynchronous scan lifecycle is functional.
3. Validate the US1 checkpoint with the quickstart scenarios.
4. Stop for stakeholder review before expanding to search, management, sharing, and reporting.

### Incremental Delivery

1. Foundation plus US1: secure upload, quarantine, async scan, and organization.
2. US2: search, preview, download, metadata management, replacement, and deletion.
3. US3: sharing, notifications, task attachments, and dashboard integration.
4. US4: audit reporting.
5. Polish: production Azure configuration, performance validation, and documentation.

## Notes

- Every task includes an exact file path and follows the required checklist format.
- Tests are included because the feature has high authorization and file-lifecycle risk; the repository currently lacks a test project, so T009 establishes the focused harness before story tests.
- Files remain inaccessible until `ScanStatus` is `Clean`; Queue Storage retries and poison handling must not bypass this rule.
