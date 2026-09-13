using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Data;

public static class DocumentSchemaInitializer
{
    public static void EnsureCreated(ApplicationDbContext context)
    {
        context.Database.ExecuteSqlRaw("""
            IF OBJECT_ID(N'[Documents]', N'U') IS NULL
            BEGIN
                CREATE TABLE [Documents] (
                    [DocumentId] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Documents] PRIMARY KEY,
                    [Title] nvarchar(255) NOT NULL,
                    [Description] nvarchar(2000) NULL,
                    [Category] nvarchar(100) NOT NULL,
                    [Tags] nvarchar(2000) NULL,
                    [OriginalFileName] nvarchar(255) NOT NULL,
                    [StorageKey] nvarchar(1000) NOT NULL,
                    [FileSize] bigint NOT NULL,
                    [FileType] nvarchar(255) NOT NULL,
                    [UploadedByUserId] int NOT NULL,
                    [ProjectId] int NULL,
                    [UploadedDate] datetime2 NOT NULL,
                    [UpdatedDate] datetime2 NOT NULL,
                    [ScanStatus] int NOT NULL,
                    [ScanAttempt] int NOT NULL,
                    [ScanCompletedDate] datetime2 NULL,
                    [ScanFailureReason] nvarchar(500) NULL,
                    CONSTRAINT [FK_Documents_Users_UploadedByUserId] FOREIGN KEY ([UploadedByUserId]) REFERENCES [Users] ([UserId]),
                    CONSTRAINT [FK_Documents_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([ProjectId])
                );
            END
            """);

        context.Database.ExecuteSqlRaw("""
            IF OBJECT_ID(N'[DocumentShares]', N'U') IS NULL
            BEGIN
                CREATE TABLE [DocumentShares] (
                    [DocumentShareId] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_DocumentShares] PRIMARY KEY,
                    [DocumentId] int NOT NULL,
                    [SharedWithUserId] int NULL,
                    [SharedWithDepartment] nvarchar(100) NULL,
                    [SharedByUserId] int NOT NULL,
                    [CreatedDate] datetime2 NOT NULL,
                    CONSTRAINT [FK_DocumentShares_Documents_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [Documents] ([DocumentId]) ON DELETE CASCADE,
                    CONSTRAINT [FK_DocumentShares_Users_SharedWithUserId] FOREIGN KEY ([SharedWithUserId]) REFERENCES [Users] ([UserId])
                );
            END
            """);

        context.Database.ExecuteSqlRaw("""
            IF OBJECT_ID(N'[DocumentActivities]', N'U') IS NULL
            BEGIN
                CREATE TABLE [DocumentActivities] (
                    [DocumentActivityId] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_DocumentActivities] PRIMARY KEY,
                    [DocumentId] int NULL,
                    [UserId] int NOT NULL,
                    [Action] nvarchar(50) NOT NULL,
                    [CreatedDate] datetime2 NOT NULL,
                    CONSTRAINT [FK_DocumentActivities_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId])
                );
            END
            """);

        context.Database.ExecuteSqlRaw("""
            IF OBJECT_ID(N'[DocumentTaskAssociations]', N'U') IS NULL
            BEGIN
                CREATE TABLE [DocumentTaskAssociations] (
                    [DocumentTaskAssociationId] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_DocumentTaskAssociations] PRIMARY KEY,
                    [DocumentId] int NOT NULL,
                    [TaskId] int NOT NULL,
                    [CreatedByUserId] int NOT NULL,
                    [CreatedDate] datetime2 NOT NULL,
                    CONSTRAINT [FK_DocumentTaskAssociations_Documents_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [Documents] ([DocumentId]) ON DELETE CASCADE,
                    CONSTRAINT [FK_DocumentTaskAssociations_Tasks_TaskId] FOREIGN KEY ([TaskId]) REFERENCES [Tasks] ([TaskId])
                );
            END
            """);

        context.Database.ExecuteSqlRaw("""
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Documents_UploadedByUserId_UploadedDate' AND object_id = OBJECT_ID(N'[Documents]'))
                CREATE INDEX [IX_Documents_UploadedByUserId_UploadedDate] ON [Documents] ([UploadedByUserId], [UploadedDate]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Documents_ProjectId_ScanStatus' AND object_id = OBJECT_ID(N'[Documents]'))
                CREATE INDEX [IX_Documents_ProjectId_ScanStatus] ON [Documents] ([ProjectId], [ScanStatus]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DocumentShares_DocumentId_SharedWithUserId' AND object_id = OBJECT_ID(N'[DocumentShares]'))
                CREATE INDEX [IX_DocumentShares_DocumentId_SharedWithUserId] ON [DocumentShares] ([DocumentId], [SharedWithUserId]);
            """);
    }
}
