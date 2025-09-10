# myTrader API Scaffold (Controllers + Services + SignalR)

This pack adds minimal **Controllers**, **Services**, and a **SignalR hub** that match your parity plan.

## Contents
- `Api/Controllers/*` — Auth, Strategy Templates, User Strategies, Strategies (backtest start), Backtests (get), Market
- `Application/Interfaces/*` — service contracts (`IAuthService`, `ITokenIssuer`, `IStrategyService`, `IBacktestService`, `IMarketDataService`)
- `Application/Services/*` — minimal implementations (EF Core)
- `Realtime/TradingHub.cs` — SignalR hub (`/hub/trading`)
- `Infrastructure/ServiceCollectionExtensions.cs` — DI registration

## Wire-up

1. **Add DI + SignalR in Program.cs**
   ```csharp
   builder.Services.AddMyTraderCore();
   builder.Services.AddSignalR();
   // AddAuthentication(...); // your existing JWT setup
   // AddAuthorization();
   var app = builder.Build();
   app.MapControllers();
   app.MapHub<MyTrader.Realtime.TradingHub>("/hub/trading");
   app.Run();
   ```

2. **Token Issuing**
   - Implement `ITokenIssuer` using your existing JWT code (return `(accessToken, refreshToken, expiresAt, jwtId)`).
   - Register it: `builder.Services.AddScoped<ITokenIssuer, YourTokenIssuer>();`

3. **Claims**
   - Controllers expect the user id in claim `"sub"` **or** `ClaimTypes.NameIdentifier`. Ensure your JWT adds it.

4. **Refresh Rotation & Reuse Detection**
   - `AuthService` rotates tokens and revokes the previous session record.
   - If a used refresh token is invalid/expired, it revokes the entire token family *when possible*.

5. **Backtests**
   - `BacktestService.StartAsync` currently simulates completion and pushes SignalR events:
     - `BacktestStatusUpdated`
     - `BacktestMetricsUpdated`
   - Replace the `Task.Run` with your actual worker/queue.

6. **Market Data**
   - `MarketDataService.GetCandlesAsync` uses `symbols` + `candles` tables (from the migration pack we made earlier).

7. **Security**
   - Endpoints are `[Authorize]`. Adjust as needed (e.g., allow anonymous on `/auth/refresh` if you encode userId inside refresh token claims).

8. **Next**
   - Add FluentValidation or JSON Schema validation for strategy parameters.
   - Emit domain events as needed.
