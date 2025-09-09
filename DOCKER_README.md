# MyTrader Docker Setup

This document describes how to run the MyTrader application using Docker and PostgreSQL.

## Project Structure

The project is organized into backend and frontend components:
- `backend/` - .NET API with its own docker-compose.yml
- `frontend/web/` - Web application (coming soon)
- `frontend/mobile/` - Mobile application (coming soon)
- Root `docker-compose.yml` - Full stack orchestration

## Prerequisites

- Docker
- Docker Compose

## Quick Start

1. **Build and start all services:**
   ```bash
   docker-compose up --build
   ```

2. **Start services in background:**
   ```bash
   docker-compose up -d --build
   ```

3. **View logs:**
   ```bash
   docker-compose logs -f
   ```

4. **Stop services:**
   ```bash
   docker-compose down
   ```

5. **Stop services and remove volumes (cleans database):**
   ```bash
   docker-compose down -v
   ```

## Services

### PostgreSQL Database
- **Container:** mytrader_postgres
- **Port:** 5434 (external) -> 5432 (internal)
- **Database:** mytrader
- **Username:** postgres
- **Password:** password

### MyTrader API
- **Container:** mytrader_api
- **Port:** 8080
- **Environment:** Production
- **Health check:** Depends on PostgreSQL

## Database Migrations

If you're using Entity Framework migrations, you'll need to run them after the containers are up:

```bash
# Enter the API container
docker exec -it mytrader_api bash

# Run migrations (if using EF Core)
dotnet ef database update
```

Alternatively, you can run migrations from your local machine if you have the .NET SDK installed:

```bash
# Make sure PostgreSQL is running via Docker
docker-compose up postgres -d

# Run migrations from your local machine
cd MyTrader.Api
dotnet ef database update
```

## Environment Variables

The following environment variables can be customized in the docker-compose.yml:

- `POSTGRES_DB`: Database name
- `POSTGRES_USER`: Database user
- `POSTGRES_PASSWORD`: Database password
- `ConnectionStrings__DefaultConnection`: .NET connection string

## Database Connection with DBeaver

To connect to the PostgreSQL database using DBeaver:

**Connection Settings:**
- **Host:** localhost (or 127.0.0.1)
- **Port:** 5434
- **Database:** mytrader
- **Username:** postgres
- **Password:** password

**Connection URL:** `jdbc:postgresql://localhost:5434/mytrader`

**Note:** Make sure the Docker containers are running before attempting to connect.

## Troubleshooting

1. **Database connection issues:**
   - Ensure PostgreSQL container is healthy: `docker-compose ps`
   - Check logs: `docker-compose logs postgres`
   - Verify port 5434 is accessible: `docker-compose ps`

2. **DBeaver connection issues:**
   - Ensure containers are running: `docker-compose ps`
   - Try refreshing the connection in DBeaver
   - Check if another PostgreSQL instance is running on port 5434
   - Make sure you're using port 5434, not the default 5432

3. **API not starting:**
   - Check API logs: `docker-compose logs mytrader_api`
   - Verify database is accessible

4. **Port conflicts:**
   - Change ports in docker-compose.yml if 5434 or 8080 are in use
   - Note: Port 5432 is often used by local PostgreSQL installations

## Development

For development, you might want to:

1. **Use volume mounts for hot reload:**
   ```yaml
   volumes:
     - .:/app/src
   ```

2. **Override environment to Development:**
   ```yaml
   environment:
     - ASPNETCORE_ENVIRONMENT=Development
   ```

3. **Access PostgreSQL directly:**
   ```bash
   docker exec -it mytrader_postgres psql -U postgres -d mytrader
   ```