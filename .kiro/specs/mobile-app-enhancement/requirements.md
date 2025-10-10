# Mobile App Enhancement Requirements

## Introduction

MyTrader mobile app'inin kullanıcı deneyimini iyileştirmek ve eksik özellikleri tamamlamak için kapsamlı geliştirmeler yapılması gerekiyor. Uygulama şu anda backend'e bağlanabiliyor ancak real-time veri akışı, market organizasyonu, dark mode ve haber özellikleri eksik veya çalışmıyor.

## Requirements

### Requirement 1: Real-time Market Data Integration

**User Story:** As a mobile app user, I want to see real-time cryptocurrency prices automatically, so that I can track market movements without manual refresh.

#### Acceptance Criteria

1. WHEN the app starts THEN it SHALL automatically subscribe to tracked crypto symbols (BTC, ETH, ADA, SOL, AVAX)
2. WHEN a price update is received from SignalR THEN it SHALL be displayed in the UI within 500ms
3. WHEN the SignalR connection is lost THEN the app SHALL automatically reconnect and re-subscribe
4. WHEN new symbols are added to tracking THEN they SHALL be automatically subscribed
5. IF no price data is available THEN the accordion SHALL show "Yükleniyor..." instead of "Veri yok"

### Requirement 2: Multi-Market Support (4 Markets)

**User Story:** As a trader, I want to see data from 4 different markets (Binance, BIST, NASDAQ, NYSE), so that I can track all my investments in one place.

#### Acceptance Criteria

1. WHEN the dashboard loads THEN it SHALL display 4 accordion sections: Binance, BIST, NASDAQ, NYSE
2. WHEN displaying market names THEN "Kripto" SHALL be replaced with "Binance"
3. WHEN a market accordion is expanded THEN it SHALL show symbols specific to that market
4. IF a market has no data THEN the accordion SHALL show "Veri yok" message
5. WHEN market data is available THEN the "Veri yok" message SHALL be removed

### Requirement 3: Stock Market Data Integration (Yahoo Finance)

**User Story:** As a stock trader, I want to see delayed stock prices from BIST, NASDAQ, and NYSE, so that I can track stock market movements.

#### Acceptance Criteria

1. WHEN the app requests stock data THEN it SHALL fetch from Yahoo Finance API
2. WHEN stock data is fetched THEN it SHALL be updated every 60 seconds (1 minute interval)
3. WHEN displaying stock prices THEN they SHALL show 15-minute delayed data disclaimer
4. IF Yahoo Finance is unavailable THEN the system SHALL log error and retry after 5 minutes
5. WHEN a new data provider is configured THEN the system SHALL switch without code changes (provider abstraction)

### Requirement 4: Market Status Indicators

**User Story:** As a user, I want to see if markets are open or closed, so that I know if prices are updating in real-time.

#### Acceptance Criteria

1. WHEN a market is open THEN the accordion SHALL show a green indicator
2. WHEN a market is closed THEN the accordion SHALL show a red indicator
3. WHEN market status changes THEN the indicator SHALL update within 10 seconds
4. IF market status is unknown THEN the indicator SHALL show gray color
5. WHEN hovering over indicator THEN it SHALL show market hours and timezone

### Requirement 5: UI/UX Improvements

**User Story:** As a user, I want a clean and consistent UI, so that the app is easy to use and visually appealing.

#### Acceptance Criteria

1. WHEN viewing accordion headers THEN duplicate symbols SHALL be removed (keep only left side symbol)
2. WHEN accordion is collapsed THEN it SHALL show market name and status indicator
3. WHEN accordion is expanded THEN it SHALL show symbol list with prices
4. IF data is loading THEN the UI SHALL show skeleton loaders instead of "Veri yok"
5. WHEN prices update THEN they SHALL animate with a subtle flash effect

### Requirement 6: Dark Mode Support

**User Story:** As a user, I want to use dark mode, so that I can use the app comfortably in low-light conditions.

#### Acceptance Criteria

1. WHEN the app starts THEN it SHALL detect system dark mode preference
2. WHEN user toggles dark mode THEN all screens SHALL switch to dark theme
3. WHEN in dark mode THEN colors SHALL have sufficient contrast (WCAG AA compliant)
4. IF dark mode is enabled THEN the preference SHALL be saved and persist across app restarts
5. WHEN switching themes THEN the transition SHALL be smooth (animated)

### Requirement 7: News Feed Integration

**User Story:** As a trader, I want to see latest financial news, so that I can make informed trading decisions.

#### Acceptance Criteria

1. WHEN the news feed loads THEN it SHALL show latest headlines from Investing.com and Bloomberg HT
2. WHEN fetching economy news THEN it SHALL scrape https://tr.investing.com/news/economy every 15 minutes
3. WHEN fetching breaking news THEN it SHALL scrape https://www.bloomberght.com/sondakika every 5 minutes
4. WHEN displaying news THEN each item SHALL show source (Investing.com or Bloomberg HT)
5. IF scraping fails THEN the system SHALL log error and retry on next scheduled interval

### Requirement 8: Data Provider Abstraction

**User Story:** As a developer, I want a flexible data provider architecture, so that we can easily switch between data sources without code changes.

#### Acceptance Criteria

1. WHEN implementing data providers THEN they SHALL follow IMarketDataProvider interface
2. WHEN switching providers THEN only configuration SHALL need to change
3. WHEN a provider fails THEN the system SHALL fallback to secondary provider if configured
4. IF no provider is available THEN the system SHALL show cached data with "stale data" warning
5. WHEN adding new providers THEN they SHALL be plug-and-play without modifying existing code

### Requirement 9: News Scraper Service

**User Story:** As a system, I want to reliably scrape news from multiple sources, so that users always have fresh financial news.

#### Acceptance Criteria

1. WHEN scraping news THEN the service SHALL handle rate limiting and timeouts gracefully
2. WHEN parsing HTML THEN the scraper SHALL extract title, summary, link, and timestamp
3. WHEN storing news THEN duplicate articles SHALL be detected and skipped
4. IF scraping fails 3 times consecutively THEN the system SHALL send alert notification
5. WHEN news is scraped THEN it SHALL be cached for 5 minutes to reduce load

### Requirement 10: Performance and Reliability

**User Story:** As a user, I want the app to be fast and reliable, so that I can trust it for trading decisions.

#### Acceptance Criteria

1. WHEN the app starts THEN it SHALL load initial data within 3 seconds
2. WHEN receiving price updates THEN memory usage SHALL not exceed 150MB
3. WHEN the app runs for 24+ hours THEN performance SHALL not degrade
4. IF network is slow THEN the app SHALL show loading indicators and not freeze
5. WHEN errors occur THEN they SHALL be logged and user SHALL see friendly error messages
