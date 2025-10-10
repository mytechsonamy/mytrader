# Mobile App Enhancement - Implementation Plan

- [x] 1. Fix Real-time Data Integration
  - Fix PriceContext to auto-subscribe to markets on connection
  - Add missing SignalR event handlers (connectionstatus, heartbeat)
  - Implement proper loading states and skeleton loaders
  - Test SignalR reconnection and re-subscription logic
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_

- [x] 1.1 Implement Auto-Subscribe in PriceContext
  - Modify PriceContext.tsx to automatically subscribe on SignalR connection
  - Subscribe to CRYPTO market with tracked symbols (BTC, ETH, ADA, SOL, AVAX)
  - Add subscription state management (subscribed symbols tracking)
  - _Requirements: 1.1, 1.4_

- [x] 1.2 Add Missing SignalR Event Handlers
  - Add 'connectionstatus' event handler in websocketService.ts
  - Add 'heartbeat' event handler for connection health monitoring
  - Update event handler logging for debugging
  - _Requirements: 1.2_

- [x] 1.3 Implement Loading States
  - Replace "Veri yok" with "Yükleniyor..." when data is loading
  - Create SkeletonLoader component for price list
  - Add loading indicators to accordion headers
  - _Requirements: 1.5, 5.4_

- [x] 1.4 Write unit tests for auto-subscribe
  - Test auto-subscribe triggers on connection
  - Test re-subscription after reconnection
  - Test subscription cleanup on unmount
  - _Requirements: 1.1, 1.3_

- [ ] 2. Add Multi-Market Support (4 Markets)
  - Add NYSE market accordion to dashboard
  - Update market names (Kripto → Binance)
  - Implement market-specific data filtering
  - Add market status indicators (open/closed)
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 4.1, 4.2, 4.3_

- [ ] 2.1 Add NYSE Market Accordion
  - Add NYSE to market list configuration
  - Create NYSE symbol list in database
  - Update dashboard to render 4 accordions
  - _Requirements: 2.1, 2.4_

- [ ] 2.2 Update Market Display Names
  - Replace "Kripto" with "Binance" in UI
  - Update market name mapping in constants
  - Ensure consistent naming across app
  - _Requirements: 2.2_

- [ ] 2.3 Implement Market Status Indicators
  - Create MarketStatusIndicator component (green/red/gray)
  - Add market hours calculation logic
  - Integrate with MarketStatusService from backend
  - Display indicator in accordion headers
  - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

- [ ] 2.4 Remove Duplicate Symbols from Headers
  - Update AccordionHeader component to remove duplicate symbol display
  - Keep only left-side symbol icon
  - Adjust header layout and spacing
  - _Requirements: 5.1, 5.2_

- [ ]* 2.5 Write tests for multi-market display
  - Test 4 accordions render correctly
  - Test market status indicator colors
  - Test market name display
  - _Requirements: 2.1, 2.2, 4.1_

- [x] 3. Implement Yahoo Finance Stock Data Provider
  - Create IMarketDataProvider interface (backend)
  - Implement YahooFinanceProvider for BIST, NASDAQ, NYSE
  - Add background service for 1-minute polling
  - Implement provider abstraction for easy switching
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 8.1, 8.2, 8.3, 8.4, 8.5_

- [x] 3.1 Create Market Data Provider Interface
  - Define IMarketDataProvider interface in backend
  - Add ProviderName, SupportedMarket, UpdateInterval properties
  - Define GetPricesAsync, IsAvailableAsync, GetMarketStatusAsync methods
  - _Requirements: 8.1, 8.2_

- [x] 3.2 Implement Yahoo Finance Provider
  - Create YahooFinanceProvider class implementing IMarketDataProvider
  - Integrate with Yahoo Finance API v8
  - Handle 15-minute delayed data
  - Add error handling and retry logic
  - _Requirements: 3.1, 3.2, 3.4_

- [x] 3.3 Create Stock Data Polling Service
  - Implement background service for 1-minute stock data updates
  - Poll Yahoo Finance for BIST, NASDAQ, NYSE symbols
  - Broadcast updates via SignalR to mobile clients
  - Add logging and error handling
  - _Requirements: 3.2, 3.3_

- [x] 3.4 Implement Provider Configuration System
  - Add provider configuration in appsettings.json
  - Create provider factory for dynamic provider selection
  - Implement fallback provider logic
  - _Requirements: 3.5, 8.3, 8.4, 8.5_

- [ ]* 3.5 Write tests for Yahoo Finance provider
  - Test API integration with mock responses
  - Test error handling and retries
  - Test provider fallback logic
  - _Requirements: 3.1, 3.4, 8.3_

- [x] 4. Implement Dark Mode Support
  - Create ThemeContext with light/dark themes
  - Implement theme toggle UI
  - Persist theme preference to AsyncStorage
  - Apply theme colors to all components
  - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5_

- [x] 4.1 Create ThemeContext and Theme Definitions
  - Create ThemeContext.tsx with light and dark color schemes
  - Define ThemeColors interface with all color tokens
  - Implement system theme detection
  - _Requirements: 6.1, 6.3_

- [x] 4.2 Implement Theme Toggle
  - Add theme toggle button to settings/profile screen
  - Implement smooth theme transition animation
  - Update all components to use theme colors
  - _Requirements: 6.2, 6.5_

- [x] 4.3 Persist Theme Preference
  - Save theme preference to AsyncStorage
  - Load saved theme on app startup
  - Handle system theme changes
  - _Requirements: 6.4_

- [x] 4.4 Apply Theme to All Components
  - Update all screens to use ThemeContext colors
  - Ensure WCAG AA contrast compliance
  - Test dark mode on all screens
  - _Requirements: 6.3, 6.5_

- [ ]* 4.5 Write tests for dark mode
  - Test theme switching
  - Test theme persistence
  - Test color contrast ratios
  - _Requirements: 6.1, 6.2, 6.4_

- [ ] 5. Implement News Feed Integration
  - Create news scraper service for Investing.com and Bloomberg HT
  - Implement background service with 15min and 5min intervals
  - Create news API endpoints
  - Build news feed UI component in mobile app
  - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 9.1, 9.2, 9.3, 9.4, 9.5_

- [ ] 5.1 Create News Scraper Service
  - Create INewsScraperService interface
  - Implement InvestingComScraper for https://tr.investing.com/news/economy
  - Implement BloombergHTScraper for https://www.bloomberght.com/sondakika
  - Add HTML parsing with error handling
  - _Requirements: 7.2, 7.3, 9.1, 9.2_

- [ ] 5.2 Implement News Background Service
  - Create NewsScraperBackgroundService with PeriodicTimer
  - Schedule Investing.com scraping every 15 minutes
  - Schedule Bloomberg HT scraping every 5 minutes
  - Store scraped news in database with duplicate detection
  - _Requirements: 7.2, 7.3, 9.3, 9.5_

- [ ] 5.3 Create News API Endpoints
  - Add GET /api/news endpoint for latest news
  - Add filtering by source (Investing.com, Bloomberg HT)
  - Add pagination support
  - Implement caching for 5 minutes
  - _Requirements: 7.1, 9.5_

- [ ] 5.4 Build News Feed UI Component
  - Create NewsFeed component for mobile app
  - Display news with title, summary, source, and timestamp
  - Add pull-to-refresh functionality
  - Implement news article detail view
  - _Requirements: 7.1, 7.4_

- [ ]* 5.5 Write tests for news scraping
  - Test HTML parsing with sample pages
  - Test duplicate detection
  - Test error handling and retries
  - _Requirements: 9.1, 9.2, 9.3, 9.4_

- [ ] 6. Performance Optimization and Polish
  - Optimize app startup time (target: 3 seconds)
  - Implement memory usage monitoring
  - Add error boundaries and friendly error messages
  - Optimize SignalR message handling
  - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5_

- [ ] 6.1 Optimize App Startup
  - Lazy load non-critical components
  - Optimize initial data fetching
  - Reduce bundle size with code splitting
  - Measure and optimize to 3-second target
  - _Requirements: 10.1_

- [ ] 6.2 Implement Memory Monitoring
  - Add memory usage tracking
  - Set 150MB memory limit alert
  - Optimize price update handling to prevent memory leaks
  - Test 24+ hour app runtime
  - _Requirements: 10.2, 10.3_

- [ ] 6.3 Improve Error Handling
  - Add error boundaries to all major components
  - Implement user-friendly error messages
  - Add error logging with context
  - Create error recovery mechanisms
  - _Requirements: 10.5_

- [ ] 6.4 Add Loading Indicators
  - Implement loading indicators for slow network
  - Add timeout handling for API requests
  - Prevent UI freezing during data loads
  - _Requirements: 10.4_

- [ ]* 6.5 Performance testing
  - Load test with 100+ symbols
  - Memory leak detection
  - Network throttling tests
  - _Requirements: 10.1, 10.2, 10.3_

- [ ] 7. Integration Testing and Documentation
  - Create end-to-end tests for critical flows
  - Write user documentation
  - Create developer setup guide
  - Perform user acceptance testing
  - _Requirements: All requirements validation_

- [ ] 7.1 Create Integration Tests
  - Test real-time data flow from backend to mobile
  - Test multi-market data display
  - Test dark mode switching
  - Test news feed updates
  - _Requirements: 1.1-1.5, 2.1-2.5, 6.1-6.5, 7.1-7.5_

- [ ] 7.2 Write User Documentation
  - Create user guide for mobile app features
  - Document dark mode usage
  - Document news feed sources
  - Create FAQ for common issues
  - _Requirements: All requirements_

- [ ] 7.3 Create Developer Documentation
  - Document provider abstraction architecture
  - Create setup guide for new data providers
  - Document SignalR event flow
  - Add troubleshooting guide
  - _Requirements: 8.1-8.5_

- [ ]* 7.4 User acceptance testing
  - Test with real users
  - Gather feedback on UI/UX
  - Validate performance on different devices
  - _Requirements: All requirements_
