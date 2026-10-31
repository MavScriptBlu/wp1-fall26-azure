-- SQL Server schema for Azure deployment
-- Run this in your Azure SQL database (wp1_f26_s06) using SSMS or Azure Portal Query Editor

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'service_records')
BEGIN
    CREATE TABLE service_records (
        id INT IDENTITY(1,1) PRIMARY KEY,
        service_name VARCHAR(100) NOT NULL,
        environment VARCHAR(50) NOT NULL,
        status VARCHAR(50) NOT NULL,
        created_at DATETIME2 DEFAULT GETUTCDATE()
    );

    CREATE INDEX idx_services_env ON service_records(environment);
END
GO
