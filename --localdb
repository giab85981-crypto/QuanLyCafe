BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003090000_DashboardSalesTracking'
)
BEGIN
    ALTER TABLE [Bill] ADD [GuestCount] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003090000_DashboardSalesTracking'
)
BEGIN
    ALTER TABLE [BillInfo] ADD [UnitPrice] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003090000_DashboardSalesTracking'
)
BEGIN
    ALTER TABLE [Food] ADD [MenuKind] nvarchar(20) NOT NULL DEFAULT N'Khác';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003090000_DashboardSalesTracking'
)
BEGIN
    CREATE TABLE [ItemType] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_ItemType] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003090000_DashboardSalesTracking'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ItemType_Name] ON [ItemType] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003090000_DashboardSalesTracking'
)
BEGIN
    INSERT INTO ItemType (Name) SELECT DISTINCT ItemType FROM Food WHERE LTRIM(RTRIM(ItemType)) <> ''
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003090000_DashboardSalesTracking'
)
BEGIN
    INSERT INTO ItemType (Name) SELECT Name FROM (VALUES (N'Món chế biến'), (N'Hàng hóa'), (N'Dịch vụ')) AS Defaults(Name) WHERE NOT EXISTS (SELECT 1 FROM ItemType t WHERE t.Name = Defaults.Name)
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003090000_DashboardSalesTracking'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261003090000_DashboardSalesTracking', N'8.0.11');
END;
GO

COMMIT;
GO

