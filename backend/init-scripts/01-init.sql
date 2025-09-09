-- MyTrader Database Initialization Script
-- This script will be run when the PostgreSQL container starts for the first time

-- Create database if it doesn't exist (though it should already be created by POSTGRES_DB)
-- SELECT 'CREATE DATABASE mytrader' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'mytrader')\gexec

-- Connect to the mytrader database
\c mytrader;

-- Create extensions that might be useful for a trading application
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Grant all privileges to postgres user (if using different user, adjust accordingly)
GRANT ALL PRIVILEGES ON DATABASE mytrader TO postgres;

-- Note: Entity Framework will handle the table creation through migrations
-- This script is mainly for setting up database extensions and initial configuration