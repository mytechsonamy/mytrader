using AutoFixture;
using MyTrader.Core.DTOs.Authentication;
using MyTrader.Core.Models;

namespace MyTrader.Tests.TestBase;

/// <summary>
/// Builder class for creating consistent test data across all test suites
/// </summary>
public static class TestDataBuilder
{
    private static readonly IFixture Fixture = new Fixture();

    static TestDataBuilder()
    {
        // Configure AutoFixture to handle circular references
        Fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList().ForEach(b => Fixture.Behaviors.Remove(b));
        Fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    public static class Users
    {
        public static User CreateValid(string? email = null)
        {
            return Fixture.Build<User>()
                .With(x => x.Email, email ?? "testuser@example.com")
                .With(x => x.FirstName, "Test")
                .With(x => x.LastName, "User")
                .With(x => x.Phone, "+1234567890")
                .With(x => x.IsActive, true)
                .With(x => x.IsEmailVerified, true)
                .With(x => x.CreatedAt, DateTime.UtcNow)
                .With(x => x.UpdatedAt, DateTime.UtcNow)
                .Create();
        }

        public static User CreateInactive()
        {
            return CreateValid().With(x => x.IsActive, false);
        }

        public static User CreateUnverified()
        {
            return CreateValid().With(x => x.IsEmailVerified, false);
        }
    }

    public static class Authentication
    {
        public static LoginRequest CreateValidLoginRequest()
        {
            return new LoginRequest
            {
                Email = "testuser@example.com",
                Password = "ValidPassword123!"
            };
        }

        public static LoginRequest CreateInvalidLoginRequest()
        {
            return new LoginRequest
            {
                Email = "invalid@example.com",
                Password = "wrongpassword"
            };
        }

        public static RegisterRequest CreateValidRegisterRequest()
        {
            return new RegisterRequest
            {
                Email = "newuser@example.com",
                Password = "StrongPassword123!",
                FirstName = "New",
                LastName = "User",
                Phone = "+1234567890"
            };
        }

        public static LoginResponse CreateLoginResponse(User user)
        {
            return new LoginResponse
            {
                Token = "mock-jwt-token",
                RefreshToken = "mock-refresh-token",
                User = user,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };
        }
    }

    public static class Symbols
    {
        public static Symbol CreateStock(string symbolName = "AAPL")
        {
            return Fixture.Build<Symbol>()
                .With(x => x.SymbolName, symbolName)
                .With(x => x.Name, GetCompanyName(symbolName))
                .With(x => x.AssetClass, "Stock")
                .With(x => x.Exchange, "NASDAQ")
                .With(x => x.IsActive, true)
                .With(x => x.CreatedAt, DateTime.UtcNow)
                .Create();
        }

        public static Symbol CreateCrypto(string symbolName = "BTCUSDT")
        {
            return Fixture.Build<Symbol>()
                .With(x => x.SymbolName, symbolName)
                .With(x => x.Name, GetCryptoName(symbolName))
                .With(x => x.AssetClass, "Crypto")
                .With(x => x.Exchange, "Binance")
                .With(x => x.IsActive, true)
                .With(x => x.CreatedAt, DateTime.UtcNow)
                .Create();
        }

        public static Symbol CreateForex(string symbolName = "EURUSD")
        {
            return Fixture.Build<Symbol>()
                .With(x => x.SymbolName, symbolName)
                .With(x => x.Name, GetForexName(symbolName))
                .With(x => x.AssetClass, "Forex")
                .With(x => x.Exchange, "FX")
                .With(x => x.IsActive, true)
                .With(x => x.CreatedAt, DateTime.UtcNow)
                .Create();
        }

        private static string GetCompanyName(string symbol) => symbol switch
        {
            "AAPL" => "Apple Inc.",
            "GOOGL" => "Alphabet Inc.",
            "MSFT" => "Microsoft Corporation",
            "TSLA" => "Tesla Inc.",
            _ => $"{symbol} Corporation"
        };

        private static string GetCryptoName(string symbol) => symbol switch
        {
            "BTCUSDT" => "Bitcoin/USDT",
            "ETHUSDT" => "Ethereum/USDT",
            "ADAUSDT" => "Cardano/USDT",
            _ => $"{symbol.Replace("USDT", "")}/USDT"
        };

        private static string GetForexName(string symbol) => symbol switch
        {
            "EURUSD" => "EUR/USD",
            "GBPUSD" => "GBP/USD",
            "USDJPY" => "USD/JPY",
            _ => $"{symbol[..3]}/{symbol[3..]}"
        };
    }

    public static class MarketData
    {
        public static Core.Models.MarketData CreateForSymbol(Symbol symbol, decimal price = 100m)
        {
            return Fixture.Build<Core.Models.MarketData>()
                .With(x => x.SymbolId, symbol.Id)
                .With(x => x.Symbol, symbol.SymbolName)
                .With(x => x.Price, price)
                .With(x => x.Volume, Fixture.Create<long>())
                .With(x => x.Change24h, price * 0.05m)
                .With(x => x.ChangePercent24h, 5.0m)
                .With(x => x.High24h, price * 1.1m)
                .With(x => x.Low24h, price * 0.9m)
                .With(x => x.Timestamp, DateTime.UtcNow)
                .With(x => x.AssetClass, symbol.AssetClass)
                .Create();
        }

        public static Core.Models.MarketData CreateVolatile(Symbol symbol)
        {
            var basePrice = 100m;
            var volatilityFactor = 0.2m; // 20% volatility
            
            return CreateForSymbol(symbol, basePrice)
                .With(x => x.Change24h, basePrice * volatilityFactor)
                .With(x => x.ChangePercent24h, volatilityFactor * 100)
                .With(x => x.High24h, basePrice * (1 + volatilityFactor))
                .With(x => x.Low24h, basePrice * (1 - volatilityFactor));
        }
    }

    public static class Portfolio
    {
        public static Core.Models.Portfolio CreateForUser(User user, decimal initialBalance = 10000m)
        {
            return Fixture.Build<Core.Models.Portfolio>()
                .With(x => x.UserId, user.Id)
                .With(x => x.TotalValue, initialBalance)
                .With(x => x.CashBalance, initialBalance)
                .With(x => x.CreatedAt, DateTime.UtcNow)
                .With(x => x.UpdatedAt, DateTime.UtcNow)
                .Create();
        }
    }

    /// <summary>
    /// Extension method to modify objects in a fluent way
    /// </summary>
    public static T With<T, TProperty>(this T obj, Func<T, TProperty> propertyPicker, TProperty value)
    {
        var propertyInfo = typeof(T).GetProperty(((MemberExpression)((LambdaExpression)propertyPicker).Body).Member.Name);
        propertyInfo?.SetValue(obj, value);
        return obj;
    }
}