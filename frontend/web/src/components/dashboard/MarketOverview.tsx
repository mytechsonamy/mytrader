import { useState, useEffect } from 'react';
import { useMarketOverview } from '../../hooks/useMarketData';
import { formatCurrency, formatPercentage, getChangeColor } from '../../utils';
import { apiService } from '../../services/api';
import type { Symbol, StockPriceData } from '../../types';
import DataSourceBadge from './DataSourceBadge';

// Price data interface from WebSocket - extends StockPriceData
interface PriceData extends Partial<StockPriceData> {
  symbol: string;
  price: number;
  change: number;
  changePercent: number;
  volume: number;
  lastUpdate?: string;
  timestamp?: string;
}

interface MarketOverviewProps {
  className?: string;
}

// Section state structure using unique section type keys
interface SectionState {
  crypto: boolean;
  bist: boolean;
  nasdaq: boolean;
  nyse: boolean;
}

// Helper function to safely get market field from symbol
const getMarketField = (symbol: Symbol): string => {
  const market = symbol.market || symbol.marketName || symbol.venue || '';

  if (!market && import.meta.env.DEV) {
    console.warn('[MarketOverview] Symbol missing market field:', symbol);
  }

  return market;
};

const MarketOverview: React.FC<MarketOverviewProps> = ({ className = '' }) => {
  // Changed from AssetClassType-based keys to section-type-based keys
  const [expandedSections, setExpandedSections] = useState<SectionState>({
    crypto: true,  // Default to crypto expanded
    bist: false,
    nasdaq: false,
    nyse: false,
  });

  // Symbol arrays for each section
  const [bistSymbols, setBistSymbols] = useState<Symbol[]>([]);
  const [nasdaqSymbols, setNasdaqSymbols] = useState<Symbol[]>([]);
  const [nyseSymbols, setNyseSymbols] = useState<Symbol[]>([]);

  // Use React Query hooks for data
  const { data: marketData, isLoading: marketLoading, error: marketError } = useMarketOverview();

  // Create market data display from API response (prices are PriceData objects)
  const marketDataArray: PriceData[] = marketData ? Object.values(marketData as Record<string, PriceData>) : [];

  // Fetch symbols for stock markets
  useEffect(() => {
    const fetchStockSymbols = async () => {
      try {
        // Fetch all stock symbols
        const response = await apiService.get<Symbol[]>('/api/v1/symbols/by-asset-class/STOCK');

        // Backend returns array directly in response.data
        let allStocks: Symbol[] = [];

        if (Array.isArray(response.data)) {
          allStocks = response.data;
        } else if (response.data && typeof response.data === 'object') {
          // Handle wrapped response
          allStocks = (response.data as any).symbols || response.data as Symbol[] || [];
        }

        console.log('[MarketOverview] Fetched all stocks:', allStocks.length);

        // Debug first symbol to check field structure
        if (allStocks.length > 0 && import.meta.env.DEV) {
          const sample = allStocks[0];
          if (sample) {
            console.log('[MarketOverview] Sample symbol fields:', {
              symbol: sample.symbol,
              name: sample.name,
              hasMarket: 'market' in sample,
              hasMarketName: 'marketName' in sample,
              hasVenue: 'venue' in sample,
              market: sample.market,
              marketName: sample.marketName,
              venue: sample.venue,
            });
          }
        }

        // Filter symbols by market using helper function
        const bist = allStocks.filter(symbol => {
          if (!symbol) return false;
          const marketField = getMarketField(symbol).toUpperCase();
          return marketField.includes('BIST') || marketField.includes('TURKEY');
        });

        const nasdaq = allStocks.filter(symbol => {
          if (!symbol) return false;
          const marketField = getMarketField(symbol).toUpperCase();
          return marketField.includes('NASDAQ');
        });

        const nyse = allStocks.filter(symbol => {
          if (!symbol) return false;
          const marketField = getMarketField(symbol).toUpperCase();
          return marketField.includes('NYSE');
        });

        setBistSymbols(bist);
        setNasdaqSymbols(nasdaq);
        setNyseSymbols(nyse);

        console.log('[MarketOverview] Filtered stock symbols:', {
          total: allStocks.length,
          bist: bist.length,
          nasdaq: nasdaq.length,
          nyse: nyse.length,
        });

        // Log any symbols that didn't match any exchange
        const unmatched = allStocks.filter(symbol => {
          const marketField = getMarketField(symbol).toUpperCase();
          return !marketField.includes('BIST') &&
                 !marketField.includes('TURKEY') &&
                 !marketField.includes('NASDAQ') &&
                 !marketField.includes('NYSE');
        });

        if (unmatched.length > 0 && import.meta.env.DEV) {
          console.warn('[MarketOverview] Unmatched symbols:', unmatched.map(s => ({
            symbol: s.symbol,
            name: s.name || s.displayName,
            market: getMarketField(s)
          })));
        }
      } catch (error) {
        console.error('[MarketOverview] Failed to fetch stock symbols:', error);
      }
    };

    fetchStockSymbols();
  }, []);

  // Toggle handler now uses section type (string) instead of AssetClassType
  const toggleSection = (sectionType: keyof SectionState) => {
    setExpandedSections(prev => ({
      ...prev,
      [sectionType]: !prev[sectionType],
    }));
  };

  const isSectionExpanded = (sectionType: keyof SectionState): boolean => {
    return expandedSections[sectionType];
  };

  return (
    <div className={`market-accordion-section ${className}`}>
      <div className="market-accordion-header">
        <h2 className="market-accordion-title">
          <div className="market-title-content">
            <div className="market-title-icon">📊</div>
            <span className="market-title-text">Live Market Data</span>
          </div>
          <div className="market-status-indicators">
            <div className="connection-status">
              {!marketLoading && marketDataArray.length > 0 && (
                <>
                  <div className="status-dot connected"></div>
                  <span>Connected</span>
                </>
              )}
              {marketLoading && (
                <>
                  <div className="status-dot connecting"></div>
                  <span>Loading...</span>
                </>
              )}
              {!marketLoading && marketDataArray.length === 0 && (
                <>
                  <div className="status-dot disconnected"></div>
                  <span>No Data</span>
                </>
              )}
            </div>
            {marketDataArray.length > 0 && (
              <span className="last-update">
                {marketDataArray.length} symbols available
              </span>
            )}
          </div>
        </h2>
      </div>

      {marketError && marketDataArray.length === 0 ? (
        <div className="category-content expanded">
          <div className="market-data-grid">
            <div className="error-state">
              <h3>Unable to Load Market Data</h3>
              <p>Failed to fetch market data from API</p>
            </div>
          </div>
        </div>
      ) : (
        <div className="market-categories">
          {/* Cryptocurrency Section */}
          <div className="market-category">
            <button
              className={`category-header ${isSectionExpanded('crypto') ? 'expanded' : ''}`}
              onClick={() => toggleSection('crypto')}
              aria-expanded={isSectionExpanded('crypto')}
              aria-controls="category-crypto"
            >
              <div className="category-info">
                <span className="category-icon">₿</span>
                <div className="category-details">
                  <h3 className="category-name">Cryptocurrency</h3>
                  <p className="category-description">Digital assets and crypto prices</p>
                </div>
              </div>
              <div className="category-meta">
                <div className="category-status">
                  <span className="category-count">{marketDataArray.length} symbols</span>
                  <div className={`connection-dot ${marketDataArray.length > 0 ? 'connected' : 'disconnected'}`}></div>
                  <span className="connection-label">
                    {marketLoading ? 'Loading...' : marketDataArray.length > 0 ? 'Live' : 'Offline'}
                  </span>
                </div>
                <span className="expand-icon">▼</span>
              </div>
            </button>

            <div
              id="category-crypto"
              className={`category-content ${isSectionExpanded('crypto') ? 'expanded' : ''}`}
            >
              {marketLoading && marketDataArray.length === 0 ? (
                <div className="market-data-grid">
                  <div className="loading-state">
                    <div className="spinner"></div>
                    <p>Loading market data...</p>
                  </div>
                </div>
              ) : (
                <div className="market-data-grid">
                  {marketDataArray.length > 0 ? (
                    marketDataArray.slice(0, 6).map((priceItem: PriceData) => {
                      const priceChange = priceItem.changePercent || 0;
                      const cardClass = priceChange > 0 ? 'price-up' : priceChange < 0 ? 'price-down' : 'price-neutral';

                      return (
                        <div key={priceItem.symbol} className={`market-card ${cardClass}`}>
                          <div className="symbol-header">
                            <div className="symbol-info">
                              <h4 className="symbol-name">{priceItem.symbol}</h4>
                              <p className="symbol-description">Cryptocurrency</p>
                            </div>
                            <div className="symbol-badges">
                              <span className="symbol-badge">CRYPTO</span>
                              <span className="source-badge live">Live</span>
                              <DataSourceBadge
                                source={priceItem.source}
                                isRealtime={priceItem.isRealtime}
                                qualityScore={priceItem.qualityScore}
                                timestamp={priceItem.timestamp || priceItem.lastUpdate}
                              />
                            </div>
                          </div>

                          <div className="price-data">
                            <p className="current-price">
                              {formatCurrency(priceItem.price)}
                            </p>
                            <div className="price-change">
                              <span className={`change-amount ${priceChange >= 0 ? 'positive' : 'negative'}`}>
                                {priceChange >= 0 ? '+' : ''}{formatCurrency(Math.abs(priceItem.change))}
                              </span>
                              <span className={`change-percent ${priceChange >= 0 ? 'positive' : 'negative'} ${getChangeColor(priceChange)}`}>
                                {formatPercentage(priceChange)}
                              </span>
                            </div>
                            <div className="market-metrics">
                              <div className="metric-item">
                                <span className="metric-label">Volume</span>
                                <span className="metric-value">{priceItem.volume.toLocaleString()}</span>
                              </div>
                              <div className="metric-item">
                                <span className="metric-label">Updated</span>
                                <span className="metric-value">Live</span>
                              </div>
                            </div>
                          </div>
                        </div>
                      );
                    })
                  ) : (
                    <div className="empty-state">
                      <h4>No Market Data Available</h4>
                      <p>Market data is currently unavailable.</p>
                    </div>
                  )}
                </div>
              )}
            </div>
          </div>

          {/* BIST Section */}
          <div className="market-category">
            <button
              className={`category-header ${isSectionExpanded('bist') ? 'expanded' : ''}`}
              onClick={() => toggleSection('bist')}
              aria-expanded={isSectionExpanded('bist')}
              aria-controls="category-bist"
            >
              <div className="category-info">
                <span className="category-icon">🏢</span>
                <div className="category-details">
                  <h3 className="category-name">BIST Hisseleri</h3>
                  <p className="category-description">Turkish Stock Exchange</p>
                </div>
              </div>
              <div className="category-meta">
                <div className="category-status">
                  <span className="category-count">{bistSymbols.length} symbols</span>
                  <div className={`connection-dot ${bistSymbols.length > 0 ? 'connected' : 'disconnected'}`}></div>
                  <span className="connection-label">
                    {bistSymbols.length > 0 ? 'Ready' : 'Loading...'}
                  </span>
                </div>
                <span className="expand-icon">▼</span>
              </div>
            </button>

            <div
              id="category-bist"
              className={`category-content ${isSectionExpanded('bist') ? 'expanded' : ''}`}
            >
              <div className="market-data-grid">
                {bistSymbols.length > 0 ? (
                  bistSymbols.slice(0, 6).map((symbol: Symbol) => (
                    <div key={symbol.id} className="market-card price-neutral">
                      <div className="symbol-header">
                        <div className="symbol-info">
                          <h4 className="symbol-name">{symbol.symbol}</h4>
                          <p className="symbol-description">{symbol.name}</p>
                        </div>
                        <div className="symbol-badges">
                          <span className="symbol-badge">BIST</span>
                        </div>
                      </div>
                    </div>
                  ))
                ) : (
                  <div className="empty-state">
                    <h4>No BIST Stocks Available</h4>
                    <p>Turkish stock data is currently unavailable.</p>
                  </div>
                )}
              </div>
            </div>
          </div>

          {/* NASDAQ Section */}
          <div className="market-category">
            <button
              className={`category-header ${isSectionExpanded('nasdaq') ? 'expanded' : ''}`}
              onClick={() => toggleSection('nasdaq')}
              aria-expanded={isSectionExpanded('nasdaq')}
              aria-controls="category-nasdaq"
            >
              <div className="category-info">
                <span className="category-icon">🇺🇸</span>
                <div className="category-details">
                  <h3 className="category-name">NASDAQ Stocks</h3>
                  <p className="category-description">US Technology Stock Exchange</p>
                </div>
              </div>
              <div className="category-meta">
                <div className="category-status">
                  <span className="category-count">{nasdaqSymbols.length} symbols</span>
                  <div className={`connection-dot ${nasdaqSymbols.length > 0 ? 'connected' : 'disconnected'}`}></div>
                  <span className="connection-label">
                    {nasdaqSymbols.length > 0 ? 'Ready' : 'Loading...'}
                  </span>
                </div>
                <span className="expand-icon">▼</span>
              </div>
            </button>

            <div
              id="category-nasdaq"
              className={`category-content ${isSectionExpanded('nasdaq') ? 'expanded' : ''}`}
            >
              <div className="market-data-grid">
                {nasdaqSymbols.length > 0 ? (
                  nasdaqSymbols.slice(0, 6).map((symbol: Symbol) => (
                    <div key={symbol.id} className="market-card price-neutral">
                      <div className="symbol-header">
                        <div className="symbol-info">
                          <h4 className="symbol-name">{symbol.symbol}</h4>
                          <p className="symbol-description">{symbol.name}</p>
                        </div>
                        <div className="symbol-badges">
                          <span className="symbol-badge">NASDAQ</span>
                        </div>
                      </div>
                    </div>
                  ))
                ) : (
                  <div className="empty-state">
                    <h4>No NASDAQ Stocks Available</h4>
                    <p>NASDAQ stock data is currently unavailable.</p>
                  </div>
                )}
              </div>
            </div>
          </div>

          {/* NYSE Section */}
          <div className="market-category">
            <button
              className={`category-header ${isSectionExpanded('nyse') ? 'expanded' : ''}`}
              onClick={() => toggleSection('nyse')}
              aria-expanded={isSectionExpanded('nyse')}
              aria-controls="category-nyse"
            >
              <div className="category-info">
                <span className="category-icon">🗽</span>
                <div className="category-details">
                  <h3 className="category-name">NYSE Stocks</h3>
                  <p className="category-description">New York Stock Exchange</p>
                </div>
              </div>
              <div className="category-meta">
                <div className="category-status">
                  <span className="category-count">{nyseSymbols.length} symbols</span>
                  <div className={`connection-dot ${nyseSymbols.length > 0 ? 'connected' : 'disconnected'}`}></div>
                  <span className="connection-label">
                    {nyseSymbols.length > 0 ? 'Ready' : 'Loading...'}
                  </span>
                </div>
                <span className="expand-icon">▼</span>
              </div>
            </button>

            <div
              id="category-nyse"
              className={`category-content ${isSectionExpanded('nyse') ? 'expanded' : ''}`}
            >
              <div className="market-data-grid">
                {nyseSymbols.length > 0 ? (
                  nyseSymbols.slice(0, 6).map((symbol: Symbol) => (
                    <div key={symbol.id} className="market-card price-neutral">
                      <div className="symbol-header">
                        <div className="symbol-info">
                          <h4 className="symbol-name">{symbol.symbol}</h4>
                          <p className="symbol-description">{symbol.name}</p>
                        </div>
                        <div className="symbol-badges">
                          <span className="symbol-badge">NYSE</span>
                        </div>
                      </div>
                    </div>
                  ))
                ) : (
                  <div className="empty-state">
                    <h4>No NYSE Stocks Available</h4>
                    <p>NYSE stock data is currently unavailable.</p>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default MarketOverview;
