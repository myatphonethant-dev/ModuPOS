-- ============================================================================
-- ModuPOS - MSSQL Database Script
-- Architecture: Database-First
-- Rules: Singular naming, No Foreign Keys, No Indexes (PK only), 3NF, MSSQL standard
-- Removed: Sale and SaleItem
-- ============================================================================

-- 1. Role Table
IF OBJECT_ID(N'[dbo].[Role]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Role] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [name] VARCHAR(50) NOT NULL,
        [is_deleted] BIT NOT NULL CONSTRAINT [DF_Role_is_deleted] DEFAULT (0),
        CONSTRAINT [PK_Role] PRIMARY KEY CLUSTERED ([id])
    );
END
GO

-- 2. Permission Table
IF OBJECT_ID(N'[dbo].[Permission]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Permission] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [name] VARCHAR(50) NOT NULL,
        [is_deleted] BIT NOT NULL CONSTRAINT [DF_Permission_is_deleted] DEFAULT (0),
        CONSTRAINT [PK_Permission] PRIMARY KEY CLUSTERED ([id])
    );
END
GO

-- 3. Role_Permission Table
IF OBJECT_ID(N'[dbo].[Role_Permission]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Role_Permission] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [role_id] INT NOT NULL,
        [permission_id] INT NOT NULL,
        [is_deleted] BIT NOT NULL CONSTRAINT [DF_Role_Permission_is_deleted] DEFAULT (0),
        CONSTRAINT [PK_Role_Permission] PRIMARY KEY CLUSTERED ([id])
    );
END
GO

-- 4. User Table
IF OBJECT_ID(N'[dbo].[User]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[User] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [role_id] INT NOT NULL,
        [username] VARCHAR(50) NOT NULL,
        [password_hash] VARCHAR(512) NOT NULL,
        [email] VARCHAR(100) NULL,
        [is_active] BIT NOT NULL CONSTRAINT [DF_User_is_active] DEFAULT (1),
        [is_deleted] BIT NOT NULL CONSTRAINT [DF_User_is_deleted] DEFAULT (0),
        [created_at] DATETIME2 NOT NULL CONSTRAINT [DF_User_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by] VARCHAR(50) NULL,
        [updated_at] DATETIME2 NULL,
        [updated_by] VARCHAR(50) NULL,
        CONSTRAINT [PK_User] PRIMARY KEY CLUSTERED ([id])
    );
END
GO

-- 5. Refresh_Token Table
IF OBJECT_ID(N'[dbo].[Refresh_Token]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Refresh_Token] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [user_id] INT NULL,
        [refresh_token] VARCHAR(512) NULL,
        [expires_at] DATETIME2 NULL,
        [created_at] DATETIME2 NOT NULL CONSTRAINT [DF_Refresh_Token_created_at] DEFAULT (SYSUTCDATETIME()),
        [revoked_at] DATETIME2 NULL,
        CONSTRAINT [PK_Refresh_Token] PRIMARY KEY CLUSTERED ([id])
    );
END
GO

-- 6. Branch Table
IF OBJECT_ID(N'[dbo].[Branch]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Branch] (
        [BranchId] INT IDENTITY(1,1) NOT NULL,
        [BranchCode] VARCHAR(50) NOT NULL,
        [BranchName] NVARCHAR(200) NOT NULL,
        [Address] NVARCHAR(500) NULL,
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_Branch_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] VARCHAR(50) NULL,
        [UpdatedAt] DATETIME2 NULL,
        [UpdatedBy] VARCHAR(50) NULL,
        CONSTRAINT [PK_Branch] PRIMARY KEY CLUSTERED ([BranchId])
    );
END
GO

-- 7. Staff Table
IF OBJECT_ID(N'[dbo].[Staff]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Staff] (
        [StaffId] INT IDENTITY(1,1) NOT NULL,
        [StaffCode] VARCHAR(50) NOT NULL,
        [StaffName] NVARCHAR(200) NOT NULL,
        [DateOfBirth] DATETIME2 NULL,
        [Phone] VARCHAR(50) NULL,
        [Email] VARCHAR(100) NULL,
        [Address] NVARCHAR(500) NULL,
        [BranchCode] VARCHAR(50) NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Staff_IsActive] DEFAULT (1),
        [DeleteFlag] BIT NOT NULL CONSTRAINT [DF_Staff_DeleteFlag] DEFAULT (0),
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_Staff_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] VARCHAR(50) NULL,
        [UpdatedAt] DATETIME2 NULL,
        [UpdatedBy] VARCHAR(50) NULL,
        CONSTRAINT [PK_Staff] PRIMARY KEY CLUSTERED ([StaffId])
    );
END
GO

-- 8. StaffRole Table
IF OBJECT_ID(N'[dbo].[StaffRole]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[StaffRole] (
        [StaffRoleId] INT IDENTITY(1,1) NOT NULL,
        [StaffRoleCode] VARCHAR(50) NOT NULL,
        [StaffRoleName] NVARCHAR(100) NOT NULL,
        [Salary] DECIMAL(18,2) NULL,
        [DeleteFlag] BIT NOT NULL CONSTRAINT [DF_StaffRole_DeleteFlag] DEFAULT (0),
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_StaffRole_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] VARCHAR(50) NULL,
        [UpdatedAt] DATETIME2 NULL,
        [UpdatedBy] VARCHAR(50) NULL,
        CONSTRAINT [PK_StaffRole] PRIMARY KEY CLUSTERED ([StaffRoleId])
    );
END
GO

-- 9. Shift Table
IF OBJECT_ID(N'[dbo].[Shift]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Shift] (
        [ShiftId] INT IDENTITY(1,1) NOT NULL,
        [ShiftCode] VARCHAR(50) NOT NULL,
        [ShiftType] VARCHAR(50) NULL,
        [ShiftStartTime] DATETIME2 NULL,
        [ShiftEndTime] DATETIME2 NULL,
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_Shift_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] VARCHAR(50) NULL,
        [UpdatedAt] DATETIME2 NULL,
        [UpdatedBy] VARCHAR(50) NULL,
        CONSTRAINT [PK_Shift] PRIMARY KEY CLUSTERED ([ShiftId])
    );
END
GO

-- 10. StaffShift Table
IF OBJECT_ID(N'[dbo].[StaffShift]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[StaffShift] (
        [StaffShiftId] INT IDENTITY(1,1) NOT NULL,
        [StaffShiftCode] VARCHAR(50) NOT NULL,
        [StaffCode] VARCHAR(50) NOT NULL,
        [ShiftCode] VARCHAR(50) NOT NULL,
        CONSTRAINT [PK_StaffShift] PRIMARY KEY CLUSTERED ([StaffShiftId])
    );
END
GO

-- 11. Category Table
IF OBJECT_ID(N'[dbo].[Category]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Category] (
        [CategoryId] INT IDENTITY(1,1) NOT NULL,
        [CategoryCode] VARCHAR(50) NOT NULL,
        [CategoryName] NVARCHAR(200) NOT NULL,
        [DeleteFlag] BIT NOT NULL CONSTRAINT [DF_Category_DeleteFlag] DEFAULT (0),
        CONSTRAINT [PK_Category] PRIMARY KEY CLUSTERED ([CategoryId])
    );
END
GO

-- 12. Product Table
IF OBJECT_ID(N'[dbo].[Product]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Product] (
        [ProductId] INT IDENTITY(1,1) NOT NULL,
        [ProductCode] VARCHAR(50) NOT NULL,
        [CategoryId] INT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [ImageUrl] VARCHAR(500) NULL,
        [Quantity] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_Product_Quantity] DEFAULT (0.00),
        [Price] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_Product_Price] DEFAULT (0.00),
        [DeleteFlag] BIT NOT NULL CONSTRAINT [DF_Product_DeleteFlag] DEFAULT (0),
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_Product_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] VARCHAR(50) NULL,
        [UpdatedAt] DATETIME2 NULL,
        [UpdatedBy] VARCHAR(50) NULL,
        CONSTRAINT [PK_Product] PRIMARY KEY CLUSTERED ([ProductId])
    );
END
GO

-- 13. CustomerOrder Table
IF OBJECT_ID(N'[dbo].[CustomerOrder]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CustomerOrder] (
        [Id] VARCHAR(50) NOT NULL,
        [OrderNumber] VARCHAR(40) NOT NULL,
        [CustomerName] NVARCHAR(200) NOT NULL,
        [CustomerPhone] NVARCHAR(50) NOT NULL,
        [CustomerEmail] NVARCHAR(200) NULL,
        [DeliveryAddress] NVARCHAR(1000) NULL,
        [DeliveryInstructions] NVARCHAR(500) NULL,
        [CustomerNote] NVARCHAR(1000) NULL,
        [RiderName] NVARCHAR(200) NULL,
        [RiderPhone] NVARCHAR(50) NULL,
        [PickupCodeHash] VARCHAR(64) NULL,
        [Subtotal] DECIMAL(18,2) NOT NULL,
        [DeliveryFee] DECIMAL(18,2) NOT NULL,
        [Total] DECIMAL(18,2) NOT NULL,
        [BranchId] INT NOT NULL,
        [Status] INT NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_CustomerOrder_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_CustomerOrder] PRIMARY KEY CLUSTERED ([Id])
    );
END
GO

-- 14. CustomerOrderItem Table
IF OBJECT_ID(N'[dbo].[CustomerOrderItem]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CustomerOrderItem] (
        [Id] VARCHAR(50) NOT NULL,
        [CustomerOrderId] VARCHAR(50) NOT NULL,
        [ProductCode] NVARCHAR(50) NOT NULL,
        [ProductName] NVARCHAR(200) NOT NULL,
        [UnitPrice] DECIMAL(18,2) NOT NULL,
        [Quantity] DECIMAL(18,3) NOT NULL,
        [LineTotal] DECIMAL(18,2) NOT NULL,
        CONSTRAINT [PK_CustomerOrderItem] PRIMARY KEY CLUSTERED ([Id])
    );
END
GO
