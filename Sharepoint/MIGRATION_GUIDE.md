# ?? Entity Framework Migration Guide

## Quick Start

After applying all code changes, run these commands to set up the database:

### Step 1: Create Migration
```bash
cd Sharepoint
dotnet ef migrations add AddGuestUsersAndCommentEdits
```

### Step 2: Apply to Database
```bash
dotnet ef database update
```

### Step 3: Verify (Optional)
Open SQL Server Management Studio or use:
```bash
# List all migrations
dotnet ef migrations list
```

---

## What's Being Added

### 1. GuestUser Table
```sql
CREATE TABLE [GuestUsers] (
    [Id] int NOT NULL IDENTITY,
    [Nickname] nvarchar(max) NOT NULL,
    [SessionId] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [IsBanned] bit NOT NULL DEFAULT 0,
    [BannedAt] datetime2 NULL,
    [BannedReason] nvarchar(max) NULL,
    CONSTRAINT [PK_GuestUsers] PRIMARY KEY ([Id])
);
```

### 2. ModuleComment Updates
```sql
-- Adds these columns to [ModuleComments] table:
ALTER TABLE [ModuleComments] 
ADD [EditedAt] datetime2 NULL;
```

---

## Rollback (If Needed)

To undo the migration:

```bash
dotnet ef database update PreviousMigration
dotnet ef migrations remove
```

---

## Troubleshooting

### Error: "No migrations found"
**Solution**: Make sure you're in the project directory with the DbContext

### Error: "Cannot add NOT NULL column to existing table"
**Solution**: This is already handled - EditedAt is nullable (`datetime2 NULL`)

### Error: "The DbContext doesn't contain migrations"
**Solution**: Make sure `AppDbContext` has been configured in Program.cs

### Success Check
After running update, you should see:
```
Done. Successfully updated the database.
```

---

## Manual SQL (If Needed)

If you need to apply manually:

```sql
-- Create GuestUsers table
CREATE TABLE [GuestUsers] (
    [Id] int NOT NULL IDENTITY(1,1),
    [Nickname] nvarchar(max) NOT NULL,
    [SessionId] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
    [IsBanned] bit NOT NULL DEFAULT 0,
    [BannedAt] datetime2 NULL,
    [BannedReason] nvarchar(max) NULL,
    PRIMARY KEY ([Id])
);

-- Add EditedAt to ModuleComments (if not exists)
IF COL_LENGTH('dbo.ModuleComments', 'EditedAt') IS NULL
BEGIN
    ALTER TABLE [ModuleComments] ADD [EditedAt] datetime2 NULL;
END;

-- Add record to __EFMigrationsHistory
INSERT INTO [__EFMigrationsHistory] 
([MigrationId], [ProductVersion]) 
VALUES ('20240115_AddGuestUsersAndCommentEdits', '8.0.0');
```

---

## Next Steps

After migration:

1. ? Start your application
2. ? Navigate to /Account/Login
3. ? Click "Continue as Guest"
4. ? Create a guest account
5. ? Post a comment
6. ? Try editing it
7. ? Go to /Account/ManageUsers as admin
8. ? See your guest in the list

Enjoy! ??
