# Research: Document Upload and Management

## Decision: Use local filesystem storage behind `IFileStorageService`

**Rationale**: The constitution and stakeholder requirements require offline training without cloud services. Store only generated relative keys such as `{userId}/{projectId-or-personal}/{guid}.{extension}` and keep the physical root outside `wwwroot`. The interface leaves room for a later Azure Blob implementation without coupling business logic to an SDK.

**Alternatives considered**: Storing files under `wwwroot` was rejected because it bypasses authorization. Direct Azure Blob Storage was rejected for the default implementation because it breaks offline training and introduces an unnecessary dependency.

## Decision: Authorize through `DocumentService` and the file-serving endpoint

**Rationale**: Existing services receive a requesting user ID and enforce project membership or ownership before returning data. Document reads, writes, shares, task attachments, search results, and report access will use the same pattern. The download/preview endpoint must repeat authorization because a protected page or hidden button is not a security boundary.

**Alternatives considered**: Relying only on Blazor page attributes or client-side filtering was rejected because direct object references could expose files and metadata.

## Decision: Use an asynchronous scan queue with an Azure Functions worker

**Rationale**: Malware scanning is a required behavior and may exceed an interactive request duration. The web app will validate the file, write it to quarantine, create a document with `PendingScan` status, and enqueue a message containing the document ID and quarantine key. A .NET isolated Azure Function with an Azure Queue Storage trigger will scan the quarantined content, update the document to `Clean` or `Rejected`, move/delete content as appropriate, and emit the existing notification/audit outcomes. Queue-trigger retries, poison-message handling, idempotent status transitions, and Application Insights telemetry are required. Local training uses the same message contract with a local queue/scanner adapter so no cloud dependency is required by default.

**Alternatives considered**: Synchronous scanning in the Blazor request was rejected because it makes 25 MB uploads vulnerable to request timeouts and ties user responsiveness to scanner latency. Treating file extension validation as virus scanning was rejected because extensions do not establish content safety. Requiring Azure services in the training build was rejected by the offline constraint.

## Decision: Use Azure Queue Storage trigger reliability controls

**Rationale**: The worker must tolerate transient scanner and storage failures. Configure bounded dequeue retries, a poison queue, an explicit visibility timeout longer than the expected scan duration, and idempotent processing keyed by `DocumentId` plus the scan attempt/version. Never mark a document clean unless the scan and quarantine read succeed. Secure production settings through managed identity, private storage access where available, HTTPS-only storage, and Application Insights exception/dependency monitoring. Use the .NET isolated worker model and Functions host v4; deployment guidance targets Flex Consumption, with Elastic Premium as fallback.

**Alternatives considered**: An unbounded retry loop was rejected because poison messages could block processing. A blob trigger alone was rejected because the feature requires explicit scan work status, retry semantics, and a stable application-owned message contract.

## Decision: Keep category as text and keys as integers

**Rationale**: Existing entities use integer keys, and the stakeholder requirements explicitly require integer document IDs and text category values. This keeps EF relationships and seeded data consistent with the current model.

**Alternatives considered**: A category enum was rejected because it conflicts with the requested text storage and makes future category changes require code changes.

## Decision: Use indexed, server-side filtered queries

**Rationale**: Search and list requirements apply to up to 500 documents and 5,000 employees. Query authorization scope first, then apply search/filter/sort and pagination in SQL. Add indexes for uploader, project, upload date, category, and searchable fields where supported by the existing provider.

**Alternatives considered**: Loading all accessible documents into the browser was rejected because it weakens performance and risks leaking unauthorized data into client state.

## Decision: Validate with build plus executable workflow scenarios

**Rationale**: The repository currently has no test project. `dotnet build` is the narrow compile gate, while the quickstart records manual scenarios for upload validation, authorization, file serving, notifications, and audit logging. A focused test project may be introduced only if implementation complexity warrants it.

**Alternatives considered**: Claiming automated coverage without an existing test harness was rejected; broad end-to-end tooling would be disproportionate before the core service contracts exist.