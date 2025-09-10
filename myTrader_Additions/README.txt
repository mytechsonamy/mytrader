# myTrader add-ons (EF Core entities, migration, and API smoke tests)

## How to integrate

1. **Copy entities**
   - Move `Domain/Entities/*.cs` into your domain/entities project/namespace.
   - Add `AppDbContext.Partial.cs` next to your existing DbContext (or merge the code into your context).

2. **Install Npgsql & JSONB support**
   ```bash
   dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
   ```

3. **Add the migration**
   - Option A: Use the provided migration file `Migrations/20250909_AddTradingCoreEntities.cs` in your Migrations project.
   - Option B (recommended): Run a new migration after adding entities:
     ```bash
     dotnet ef migrations add AddTradingCoreEntities
     dotnet ef database update
     ```
     Use the provided migration file as a reference for indexes (GIN/BRIN) if EF scaffolding misses provider-specific annotations.

4. **Connection & snake_case**
   - Ensure you configure Npgsql & EF Core with your connection string.
   - If you use SnakeCaseNaming convention elsewhere, align table/column names accordingly.

5. **JSON columns**
   - `parameters`, `param_schema`, `config_snapshot`, `indicator_versions` use PostgreSQL **jsonb**.
   - The provided model uses `System.Text.Json.JsonDocument` for safe mapping.

6. **Postman/Bruno**
   - Import `Collections/myTrader.postman_collection.json` into Postman.
   - Or open `Collections/bruno/myTrader` with Bruno.
   - Set environment variables:
     - `baseUrl` (e.g., http://localhost:5000)
     - `accessToken`, `refreshToken` (once you have them)
     - `templateId`, `strategyId`, `backtestId` as needed.

7. **Next steps**
   - Implement controllers/services to back these endpoints:
     - `POST /auth/refresh`
     - `GET /auth/sessions`
     - `DELETE /auth/logout-all`
     - `DELETE /auth/sessions/{sessionId}`
     - `POST /strategy-templates`
     - `POST /user-strategies`
     - `POST /strategies/{id}/backtest`
     - `GET /backtests/{id}`
     - `GET /api/market/candles`
   - Emit SignalR events for backtest status/metrics as runs proceed.
