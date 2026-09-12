<!--
Sync Impact Report
Version change: 0.0.0 → 1.0.0
Modified principles: initial constitution established for the repository
Added sections: Security and User Isolation, User-Scoped Authorization, Spec-Driven Delivery, Quality and Verification, Simplicity and Maintainability, Additional Constraints, Development Workflow
Removed sections: none
Deferred items: TODO(RATIFICATION_DATE): date not yet recorded for this repository
-->

# ContosoDashboard Constitution

## Core Principles

### I. Security and User Isolation
ContosoDashboard MUST treat security as a first-class requirement for every feature, even in a training-only environment. Authentication and authorization MUST be enforced at both the UI and service boundaries, and every user MUST see only data scoped to their role, department, or project membership. This protects the learning experience from unsafe patterns and keeps the app aligned with real-world access-control expectations.

### II. User-Scoped Authorization
All data access MUST be evaluated against the current authenticated user before data is returned, updated, or displayed. Services MUST verify identity and authorization instead of trusting client-side state, and direct object references MUST not expose another user’s records without explicit permission. This minimizes IDOR-style risk and preserves the training scenario’s integrity as a secure sample application.

### III. Spec-Driven Delivery
All feature work MUST begin from a clear, testable specification that states user scenarios, requirements, and success criteria before implementation begins. The repository’s Spec Kit workflow MUST be used to record intent, plan the work, and break the feature into actionable tasks. This keeps the project reviewable, traceable, and consistent with the repo’s purpose as a Spec-Driven Development learning environment.

### IV. Quality and Verification
Changes MUST be validated with the smallest relevant build or test command before completion. When a feature adds behavior, the implementation MUST be checked against the stated acceptance criteria and the relevant code path MUST compile without introducing new regressions. Quality gates are mandatory because teaching accuracy depends on correctness as well as clarity.

### V. Simplicity and Maintainability
The codebase MUST favor clear separation of concerns, readable component boundaries, and minimal unnecessary abstraction. New features MUST remain understandable for training use, with strong naming, direct intent, and straightforward dependencies. Complexity MUST be justified by a real need; otherwise the repo MUST remain simple enough for learners to reason about confidently.

## Additional Constraints

ContosoDashboard is a training application built with ASP.NET Core and Blazor Server. The project MUST remain self-contained and offline-friendly, using mock authentication and local data patterns unless the work explicitly introduces a controlled external dependency. The application MUST preserve a clear boundary between business logic, data access, and UI concerns, and it MUST not rely on production-only security assumptions or cloud services in default training scenarios.

## Development Workflow

All work in this repository MUST follow the same sequence: clarify the requirement, define the feature with measurable acceptance criteria, plan the implementation, break the work into tasks, and verify the result before completion. Documentation and configuration changes MUST remain aligned with the actual codebase, and any design decisions that affect security, data access, or user experience MUST be visible in the relevant spec or workflow artifacts.

## Governance

This Constitution governs the repository’s engineering decisions and supersedes ad hoc practices that conflict with its rules. Amendments MUST be recorded in the constitution itself, include a version bump, and explain the rationale for any change in principle, policy, or workflow. A change is a PATCH when it clarifies existing guidance; MINOR when it adds a principle or materially expands governance; and MAJOR when it removes or redefines a non-negotiable rule.

All pull requests, feature work, and repo updates MUST be reviewed for compliance with this Constitution. If a project decision conflicts with the Constitution, the team MUST either change the design to comply or document an explicit, justified exception with a clear governance rationale.

**Version**: 1.0.0 | **Ratified**: TODO(RATIFICATION_DATE): date not yet recorded for this repository | **Last Amended**: 2026-09-12
