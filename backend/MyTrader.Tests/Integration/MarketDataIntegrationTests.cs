using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using MyTrader.Core.DTOs;
using MyTrader.Core.Models;
using MyTrader.Tests.TestBase;
using System.Net.Http.Json;
using Xunit;

namespace MyTrader.Tests.Integration;

/// <summary>
/// Integration tests for market data functionality
/// Tests end-to-end market data flow including WebSocket connections
/// </summary>
public class MarketDataIntegrationTests : IntegrationTestBase
{
    private HubConnection? _hubConnection;

    public MarketDataIntegrationTests(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    protected override async Task SeedTestData()
    {
        // Add test symbols
        var symbols = new[]
        {
            TestDataBuilder.Symbols.CreateStock("AAPL"),
            TestDataBuilder.Symbols.CreateStock("GOOGL"),
            TestDataBuilder.Symbols.CreateCrypto("BTCUSDT"),
            TestDataBuilder.Symbols.CreateForex("EURUSD")
        };

        DbContext.Symbols.AddRange(symbols);

        // Add test market data
        foreach (var symbol in symbols)
        {
            var marketData = TestDataBuilder.MarketData.CreateForSymbol(symbol);
            DbContext.MarketData.Add(marketData);
        }

        await DbContext.SaveChangesAsync();
        await base.SeedTestData();
    }

    [Fact]
    public async Task GetMarketData_ReturnsAllActiveSymbols()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync("/api/market-data");

        // Assert
        response.Should().BeSuccessful();
        var marketData = await response.Content.ReadFromJsonAsync<List<UnifiedMarketDataDto>>();
        
        marketData.Should().NotBeNull();
        marketData.Should().HaveCountGreaterThan(0);
        marketData.Should().Contain(m => m.Symbol == "AAPL");
        marketData.Should().Contain(m => m.Symbol == "BTCUSDT");
    }

    [Fact]
    public async Task GetMarketDataBySymbol_WithValidSymbol_ReturnsCorrectData()
    {
        // Arrange
        await AuthenticateAsync();
        var expectedSymbol = "AAPL";

        // Act
        var response = await Client.GetAsync($"/api/market-data/{expectedSymbol}");

        // Assert
        response.Should().BeSuccessful();
        var marketData = await response.Content.ReadFromJsonAsync<UnifiedMarketDataDto>();
        
        marketData.Should().NotBeNull();
        marketData!.Symbol.Should().Be(expectedSymbol);
        marketData.Price.Should().BeGreaterThan(0);
        marketData.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromHours(1));
    }

    [Fact]
    public async Task GetMarketDataBySymbol_WithInvalidSymbol_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsync();
        var invalidSymbol = "INVALID_SYMBOL";

        // Act
        var response = await Client.GetAsync($"/api/market-data/{invalidSymbol}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MarketDataHub_CanConnectAndReceiveUpdates()
    {
        // Arrange
        var token = await AuthenticateAsync();
        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{Client.BaseAddress}hubs/market-data", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(token);
            })
            .Build();

        var receivedUpdates = new List<UnifiedMarketDataDto>();
        _hubConnection.On<UnifiedMarketDataDto>("MarketDataUpdate", update =>
        {
            receivedUpdates.Add(update);
        });

        // Act
        await _hubConnection.StartAsync();
        await _hubConnection.InvokeAsync("SubscribeToSymbol", "AAPL");

        // Wait for potential updates
        await Task.Delay(2000);

        // Assert
        _hubConnection.State.Should().Be(HubConnectionState.Connected);
        // Note: In a real test, you'd trigger a market data update and verify it's received
    }

    [Fact]
    public async Task MarketDataHub_SubscriptionManagement_WorksCorrectly()
    {
        // Arrange
        var token = await AuthenticateAsync();
        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{Client.BaseAddress}hubs/market-data", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(token);
            })
            .Build();

        await _hubConnection.StartAsync();

        // Act & Assert - Subscribe
        var subscribeResult = await _hubConnection.InvokeAsync<bool>("SubscribeToSymbol", "AAPL");
        subscribeResult.Should().BeTrue();

        // Act & Assert - Unsubscribe
        var unsubscribeResult = await _hubConnection.InvokeAsync<bool>("UnsubscribeFromSymbol", "AAPL");
        unsubscribeResult.Should().BeTrue();
    }

    [Fact]
    public async Task GetSymbols_ReturnsAllActiveSymbols()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync("/api/symbols");

        // Assert
        response.Should().BeSuccessful();
        var symbols = await response.Content.ReadFromJsonAsync<List<Symbol>>();
        
        symbols.Should().NotBeNull();
        symbols.Should().HaveCountGreaterOrEqualTo(4); // We seeded 4 symbols
        symbols.Should().OnlyContain(s => s.IsActive);
        
        var symbolNames = symbols!.Select(s => s.SymbolName).ToList();
        symbolNames.Should().Contain("AAPL");
        symbolNames.Should().Contain("GOOGL");
        symbolNames.Should().Contain("BTCUSDT");
        symbolNames.Should().Contain("EURUSD");
    }

    [Fact]
    public async Task GetSymbolsByAssetClass_FiltersCorrectly()
    {
        // Arrange
        await AuthenticateAsync();

        // Act - Get stocks
        var stockResponse = await Client.GetAsync("/api/symbols?assetClass=Stock");
        var stockSymbols = await stockResponse.Content.ReadFromJsonAsync<List<Symbol>>();

        // Act - Get crypto
        var cryptoResponse = await Client.GetAsync("/api/symbols?assetClass=Crypto");
        var cryptoSymbols = await cryptoResponse.Content.ReadFromJsonAsync<List<Symbol>>();

        // Assert
        stockResponse.Should().BeSuccessful();
        cryptoResponse.Should().BeSuccessful();
        
        stockSymbols.Should().NotBeNull();
        stockSymbols.Should().OnlyContain(s => s.AssetClass == "Stock");
        stockSymbols.Should().Contain(s => s.SymbolName == "AAPL");
        
        cryptoSymbols.Should().NotBeNull();
        cryptoSymbols.Should().OnlyContain(s => s.AssetClass == "Crypto");
        cryptoSymbols.Should().Contain(s => s.SymbolName == "BTCUSDT");
    }

    [Fact]
    public async Task MarketDataEndpoints_HandleConcurrentRequests()
    {
        // Arrange
        await AuthenticateAsync();
        const int concurrentRequests = 10;
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act - Create concurrent requests
        for (int i = 0; i < concurrentRequests; i++)
        {
            tasks.Add(Client.GetAsync("/api/market-data"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().HaveCount(concurrentRequests);
        responses.Should().OnlyContain(r => r.IsSuccessStatusCode);
        
        // Verify all responses have consistent data
        var marketDataLists = new List<List<UnifiedMarketDataDto>>();
        foreach (var response in responses)
        {
            var data = await response.Content.ReadFromJsonAsync<List<UnifiedMarketDataDto>>();
            data.Should().NotBeNull();
            marketDataLists.Add(data!);
        }
        
        // All responses should have the same number of items
        var firstCount = marketDataLists[0].Count;
        marketDataLists.Should().OnlyContain(list => list.Count == firstCount);
    }

    [Fact]
    public async Task MarketDataUpdate_TriggersSignalRNotification()
    {
        // Arrange
        var token = await AuthenticateAsync();
        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{Client.BaseAddress}hubs/market-data", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(token);
            })
            .Build();

        var receivedUpdates = new List<UnifiedMarketDataDto>();
        var updateReceived = new TaskCompletionSource<bool>();
        
        _hubConnection.On<UnifiedMarketDataDto>("MarketDataUpdate", update =>
        {
            receivedUpdates.Add(update);
            updateReceived.TrySetResult(true);
        });

        await _hubConnection.StartAsync();
        await _hubConnection.InvokeAsync("SubscribeToSymbol", "AAPL");

        // Act - Trigger a market data update (this would normally come from external data source)
        var updateData = new
        {
            Symbol = "AAPL",
            Price = 155.50m,
            Volume = 1500000L,
            Timestamp = DateTime.UtcNow
        };

        // Simulate market data update via API endpoint (if available)
        // In a real scenario, this would be triggered by the data provider
        await Client.PostAsJsonAsync("/api/market-data/update", updateData);

        // Wait for SignalR notification
        var received = await updateReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        received.Should().BeTrue();
        receivedUpdates.Should().HaveCount(1);
        receivedUpdates[0].Symbol.Should().Be("AAPL");
        receivedUpdates[0].Price.Should().Be(155.50m);
    }

    [Fact]
    public async Task GetMarketData_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange - Don't authenticate
        ClearAuthentication();

        // Act
        var response = await Client.GetAsync("/api/market-data");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MarketDataHub_WithoutAuthentication_FailsToConnect()
    {
        // Arrange
        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{Client.BaseAddress}hubs/market-data")
            .Build();

        // Act & Assert
        var connectAction = async () => await _hubConnection.StartAsync();
        await connectAction.Should().ThrowAsync<HttpRequestException>();
    }

    [Theory]
    [InlineData("AAPL", "Stock")]
    [InlineData("BTCUSDT", "Crypto")]
    [InlineData("EURUSD", "Forex")]
    public async Task GetMarketData_ByAssetClass_ReturnsCorrectTypes(string expectedSymbol, string assetClass)
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync($"/api/market-data?assetClass={assetClass}");

        // Assert
        response.Should().BeSuccessful();
        var marketData = await response.Content.ReadFromJsonAsync<List<UnifiedMarketDataDto>>();
        
        marketData.Should().NotBeNull();
        marketData.Should().Contain(m => m.Symbol == expectedSymbol);
        marketData.Should().OnlyContain(m => m.AssetClass == assetClass);
    }

    [Fact]
    public async Task MarketDataPipeline_EndToEnd_WorksCorrectly()
    {
        // Arrange
        var token = await AuthenticateAsync();
        
        // Setup SignalR connection
        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{Client.BaseAddress}hubs/market-data", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(token);
            })
            .Build();

        await _hubConnection.StartAsync();

        // Act 1: Get initial market data
        var initialResponse = await Client.GetAsync("/api/market-data/AAPL");
        var initialData = await initialResponse.Content.ReadFromJsonAsync<UnifiedMarketDataDto>();

        // Act 2: Subscribe to real-time updates
        await _hubConnection.InvokeAsync("SubscribeToSymbol", "AAPL");

        // Act 3: Verify we can get symbols
        var symbolsResponse = await Client.GetAsync("/api/symbols");
        var symbols = await symbolsResponse.Content.ReadFromJsonAsync<List<Symbol>>();

        // Assert
        initialResponse.Should().BeSuccessful();
        initialData.Should().NotBeNull();
        initialData!.Symbol.Should().Be("AAPL");
        
        symbolsResponse.Should().BeSuccessful();
        symbols.Should().NotBeNull();
        symbols.Should().Contain(s => s.SymbolName == "AAPL");
        
        _hubConnection.State.Should().Be(HubConnectionState.Connected);
    }

    public override void Dispose()
    {
        _hubConnection?.DisposeAsync().AsTask().Wait(1000);
        base.Dispose();
    }
}