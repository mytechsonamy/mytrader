# Mobile App Enhancement Design Document

## Overview

Bu tasarım dokümanı MyTrader mobile app'inin geliştirilmesi için gerekli mimari değişiklikleri, component tasarımlarını ve implementasyon stratejilerini tanımlar. Ana odak noktaları real-time veri akışı, çoklu piyasa desteği, dark mode ve haber entegrasyonudur.

## Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Mobile App (React Native)                │
├─────────────────────────────────────────────────────────────┤
│  ┌──────────────────────────────────────────────────────┐   │
│  │           UI Layer (Screens & Components)            │   │
│  │  ┌────────────┬────────────┬────────────┬─────────┐  │   │
│  │  │  Binance   │    BIST    │   NASDAQ   │   NYSE  │  │   │
│  │  │ Accordion  │ Accordion  │ Accordion  │Accordion│  │   │
│  │  └────────────┴────────────┴────────────┴─────────┘  │   │
│  │  ┌──────────────────────────────────────────────────┐  │   │
│  │  │           News Feed Component                    │  │   │
│  │  └──────────────────────────────────────────────────┘  │   │
│  └──────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────┐   │
│  │         Context Layer (State Management)             │   │
│  │  ┌──────────────┬──────────────┬──────────────────┐  │   │
│  │  │ PriceContext │ NewsContext  │  ThemeContext    │  │   │
│  │  │(Auto-subscribe)│(Polling)   │ (Dark Mode)      │  │   │
│  │  └──────────────┴──────────────┴──────────────────┘  │   │
│  └──────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────┐   │
│  │            Service Layer                             │   │
│  │  ┌──────────────┬──────────────┬──────────────────┐  │   │
│  │  │  SignalR     │  HTTP API    │  Storage         │  │   │
│  │  │  Service     │  Service     │  Service         │  │   │
│  │  └──────────────┴──────────────┴──────────────────┘  │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Backend API (.NET 9)                     │
├─────────────────────────────────────────────────────────────┤
│  ┌──────────────────────────────────────────────────────┐   │
│  │         SignalR Hub (Real-time Crypto)               │   │
│  │         /hubs/market-data                            │   │
│  └──────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────┐   │
│  │      Market Data Provider Abstraction                │   │
│  │  ┌──────────────┬──────────────┬──────────────────┐  │   │
│  │  │   Binance    │Yahoo Finance │  Future Provider │  │   │
│  │  │   Provider   │   Provider   │   (Pluggable)    │  │   │
│  │  └──────────────┴──────────────┴──────────────────┘  │   │
│  └──────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────┐   │
│  │         News Scraper Background Service              │   │
│  │  ┌──────────────┬──────────────────────────────────┐  │   │
│  │  │ Investing.com│  Bloomberg HT Scraper            │  │   │
│  │  │ Scraper(15m) │  (5m intervals)                  │  │   │
│  │  └──────────────┴──────────────────────────────────┘  │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

## Components and Interfaces

### 1. Enhanced PriceContext with Auto-Subscribe

```typescript
interface EnhancedPriceContextType {
  prices: Record<string, MarketPrice>;
  isConnected: boolean;
  isLoading: boolean;
  error: string | null;
  
  // Auto-subscribe on mount
  subscribeToMarket: (market: MarketType) => Promise<void>;
  unsubscribeFromMarket: (market: MarketType) => Promise<void>;
  
  // Manual refresh
  refreshPrices: () => Promise<void>;
}

// Auto-subscribe implementation
useEffect(() => {
  const initializeSubscriptions = async () => {
    if (isConnected) {
      // Auto-subscribe to all tracked markets
      await subscribeToMarket('CRYPTO');
      await subscribeToMarket('STOCK_BIST');
      await subscribeToMarket('STOCK_NASDAQ');
      await subscribeToMarket('STOCK_NYSE');
    }
  };
  
  initializeSubscriptions();
}, [isConnected]);
```

### 2. Market Data Provider Interface (Backend)

```csharp
public interface IMarketDataProvider
{
    string ProviderName { get; }
    MarketType SupportedMarket { get; }
    TimeSpan UpdateInterval { get; }
    
    Task<List<MarketPrice>> GetPricesAsync(List<string> symbols, CancellationToken cancellationToken = default);
    Task<bool> IsAvailableAsync();
    Task<MarketStatus> GetMarketStatusAsync(string market);
}

public class YahooFinanceProvider : IMarketDataProvider
{
    public string ProviderName => "Yahoo Finance";
    public MarketType SupportedMarket => MarketType.Stocks;
    public TimeSpan UpdateInterval => TimeSpan.FromMinutes(1);
    
    public async Task<List<MarketPrice>> GetPricesAsync(List<string> symbols, CancellationToken cancellationToken)
    {
        // Implementation with 15-minute delayed data
        // Uses Yahoo Finance API v8
    }
}
```

### 3. News Scraper Service (Backend)

```csharp
public interface INewsScraperService
{
    Task<List<NewsArticle>> ScrapeInvestingNewsAsync();
    Task<List<NewsArticle>> ScrapeBloombergBreakingNewsAsync();
}

public class NewsScraperBackgroundService : BackgroundService
{
    private readonly INewsScraperService _scraperService;
    private readonly INewsRepository _newsRepository;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Investing.com - every 15 minutes
        var investingTimer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        
        // Bloomberg HT - every 5 minutes
        var bloombergTimer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        
        // Run both scrapers concurrently
    }
}
```

### 4. Theme Context (Dark Mode)

```typescript
interface ThemeContextType {
  theme: 'light' | 'dark';
  toggleTheme: () => void;
  colors: ThemeColors;
}

interface ThemeColors {
  background: string;
  surface: string;
  primary: string;
  text: string;
  textSecondary: string;
  border: string;
  success: string;
  error: string;
  warning: string;
}

const lightTheme: ThemeColors = {
  background: '#FFFFFF',
  surface: '#F8FAFC',
  primary: '#3B82F6',
  text: '#1F2937',
  textSecondary: '#6B7280',
  border: '#E5E7EB',
  success: '#10B981',
  error: '#EF4444',
  warning: '#F59E0B',
};

const darkTheme: ThemeColors = {
  background: '#111827',
  surface: '#1F2937',
  primary: '#60A5FA',
  text: '#F9FAFB',
  textSecondary: '#D1D5DB',
  border: '#374151',
  success: '#34D399',
  error: '#F87171',
  warning: '#FBBF24',
};
```

### 5. Market Accordion Component

```typescript
interface MarketAccordionProps {
  market: {
    name: string; // "Binance", "BIST", "NASDAQ", "NYSE"
    type: MarketType;
    isOpen: boolean;
    status: 'open' | 'closed' | 'unknown';
  };
  symbols: MarketSymbol[];
  prices: Record<string, MarketPrice>;
  isLoading: boolean;
}

const MarketAccordion: React.FC<MarketAccordionProps> = ({ market, symbols, prices, isLoading }) => {
  return (
    <Accordion>
      <AccordionHeader>
        <MarketStatusIndicator status={market.status} />
        <MarketName>{market.name}</MarketName>
        {/* Removed duplicate symbol */}
      </AccordionHeader>
      <AccordionContent>
        {isLoading ? (
          <SkeletonLoader count={5} />
        ) : symbols.length === 0 ? (
          <EmptyState message="Veri yok" />
        ) : (
          <SymbolList symbols={symbols} prices={prices} />
        )}
      </AccordionContent>
    </Accordion>
  );
};
```

## Data Models

### Market Price Model

```typescript
interface MarketPrice {
  symbol: string;
  price: number;
  change: number;
  changePercent: number;
  volume: number;
  timestamp: Date;
  market: MarketType;
  isStale: boolean; // For delayed data
  dataSource: string; // "Binance", "Yahoo Finance", etc.
}
```

### News Article Model

```typescript
interface NewsArticle {
  id: string;
  title: string;
  summary: string;
  url: string;
  source: 'Investing.com' | 'Bloomberg HT';
  publishedAt: Date;
  category: string;
  imageUrl?: string;
}
```

### Market Status Model

```csharp
public class MarketStatus
{
    public string Market { get; set; }
    public bool IsOpen { get; set; }
    public DateTime? NextOpen { get; set; }
    public DateTime? NextClose { get; set; }
    public string Timezone { get; set; }
    public string TradingHours { get; set; }
}
```

## Implementation Strategy

### Phase 1: Real-time Data Fix (Week 1)
1. Fix PriceContext auto-subscribe
2. Add missing event handlers (connectionstatus, heartbeat)
3. Implement proper loading states
4. Test SignalR reconnection

### Phase 2: Multi-Market Support (Week 1-2)
1. Add NYSE accordion
2. Implement Yahoo Finance provider
3. Add market status indicators
4. Update UI to show 4 markets

### Phase 3: UI/UX Improvements (Week 2)
1. Remove duplicate symbols from accordion headers
2. Replace "Kripto" with "Binance"
3. Implement skeleton loaders
4. Add price update animations

### Phase 4: Dark Mode (Week 2)
1. Create ThemeContext
2. Implement light/dark themes
3. Add theme toggle UI
4. Persist theme preference

### Phase 5: News Integration (Week 3)
1. Implement news scraper service
2. Create news API endpoints
3. Build news feed UI component
4. Add pull-to-refresh

### Phase 6: Provider Abstraction (Week 3)
1. Create IMarketDataProvider interface
2. Refactor Binance as provider
3. Implement Yahoo Finance provider
4. Add provider configuration system

### Phase 7: Testing & Polish (Week 4)
1. Performance testing
2. Memory leak detection
3. Error handling improvements
4. User acceptance testing

## Error Handling

### SignalR Connection Errors

```typescript
const handleConnectionError = (error: Error) => {
  console.error('SignalR connection error:', error);
  
  // Show user-friendly message
  showToast({
    type: 'error',
    message: 'Bağlantı hatası. Yeniden bağlanılıyor...',
  });
  
  // Implement exponential backoff
  const retryDelay = Math.min(1000 * Math.pow(2, retryCount), 30000);
  setTimeout(() => reconnect(), retryDelay);
};
```

### Data Provider Fallback

```csharp
public class MarketDataService
{
    private readonly List<IMarketDataProvider> _providers;
    
    public async Task<List<MarketPrice>> GetPricesWithFallbackAsync(List<string> symbols)
    {
        foreach (var provider in _providers)
        {
            try
            {
                if (await provider.IsAvailableAsync())
                {
                    return await provider.GetPricesAsync(symbols);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Provider {Provider} failed, trying next", provider.ProviderName);
                continue;
            }
        }
        
        // Return cached data if all providers fail
        return await _cache.GetCachedPricesAsync(symbols);
    }
}
```

## Testing Strategy

### Unit Tests
- PriceContext auto-subscribe logic
- Theme switching
- News scraper parsing
- Provider abstraction

### Integration Tests
- SignalR end-to-end flow
- Yahoo Finance API integration
- News scraping with real URLs
- Multi-market data flow

### Performance Tests
- Memory usage under sustained load
- SignalR message throughput
- UI rendering performance
- App startup time

## Monitoring and Metrics

### Key Metrics to Track
- SignalR connection uptime
- Price update latency
- News scraper success rate
- API response times
- Memory usage trends
- Crash-free sessions

### Logging Strategy
- Structured logging with correlation IDs
- Error tracking with stack traces
- Performance metrics logging
- User action analytics
