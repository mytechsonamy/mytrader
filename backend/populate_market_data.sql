-- =====================================================
-- MARKET DATA POPULATION SCRIPT
-- Purpose: Populate market_data table with initial data for dashboard
-- Database: mytrader (PostgreSQL)
-- Date: 2025-10-09
-- =====================================================

-- Check current state
SELECT 'Current market_data count: ' || COUNT(*) FROM market_data;
SELECT 'Current symbols count: ' || COUNT(*) FROM symbols WHERE is_active = true;

-- =====================================================
-- PART 1: Populate BIST (Borsa Istanbul) Stock Data
-- =====================================================
-- Insert sample 5-minute interval data for BIST stocks

INSERT INTO market_data ("Id", "Symbol", "Timeframe", "Timestamp", "Open", "High", "Low", "Close", "Volume")
VALUES
-- THYAO (Türk Hava Yolları) - Turkish Airlines
(gen_random_uuid(), 'THYAO', '5MIN', NOW() - INTERVAL '5 minutes', 285.50, 286.20, 285.10, 285.80, 1250000),
(gen_random_uuid(), 'THYAO', '5MIN', NOW() - INTERVAL '10 minutes', 285.00, 285.70, 284.80, 285.50, 1180000),
(gen_random_uuid(), 'THYAO', '5MIN', NOW() - INTERVAL '15 minutes', 284.50, 285.20, 284.30, 285.00, 1320000),
(gen_random_uuid(), 'THYAO', '5MIN', NOW() - INTERVAL '20 minutes', 284.80, 285.00, 284.40, 284.50, 980000),

-- GARAN (Garanti BBVA)
(gen_random_uuid(), 'GARAN', '5MIN', NOW() - INTERVAL '5 minutes', 108.50, 108.80, 108.30, 108.60, 2500000),
(gen_random_uuid(), 'GARAN', '5MIN', NOW() - INTERVAL '10 minutes', 108.20, 108.60, 108.10, 108.50, 2350000),
(gen_random_uuid(), 'GARAN', '5MIN', NOW() - INTERVAL '15 minutes', 108.00, 108.40, 107.90, 108.20, 2680000),
(gen_random_uuid(), 'GARAN', '5MIN', NOW() - INTERVAL '20 minutes', 107.80, 108.10, 107.70, 108.00, 2120000),

-- SISE (Şişe Cam)
(gen_random_uuid(), 'SISE', '5MIN', NOW() - INTERVAL '5 minutes', 52.75, 52.95, 52.60, 52.80, 1850000),
(gen_random_uuid(), 'SISE', '5MIN', NOW() - INTERVAL '10 minutes', 52.50, 52.80, 52.40, 52.75, 1720000),
(gen_random_uuid(), 'SISE', '5MIN', NOW() - INTERVAL '15 minutes', 52.30, 52.65, 52.20, 52.50, 1960000),
(gen_random_uuid(), 'SISE', '5MIN', NOW() - INTERVAL '20 minutes', 52.40, 52.50, 52.15, 52.30, 1580000)

ON CONFLICT ("Symbol", "Timeframe", "Timestamp") DO NOTHING;

-- =====================================================
-- PART 2: Populate NASDAQ Stock Data
-- =====================================================

INSERT INTO market_data ("Id", "Symbol", "Timeframe", "Timestamp", "Open", "High", "Low", "Close", "Volume")
VALUES
-- AAPL (Apple)
(gen_random_uuid(), 'AAPL', '5MIN', NOW() - INTERVAL '5 minutes', 229.50, 230.20, 229.30, 229.85, 15500000),
(gen_random_uuid(), 'AAPL', '5MIN', NOW() - INTERVAL '10 minutes', 229.20, 229.70, 229.10, 229.50, 14800000),
(gen_random_uuid(), 'AAPL', '5MIN', NOW() - INTERVAL '15 minutes', 228.90, 229.40, 228.80, 229.20, 16200000),
(gen_random_uuid(), 'AAPL', '5MIN', NOW() - INTERVAL '20 minutes', 229.10, 229.20, 228.70, 228.90, 13900000),

-- MSFT (Microsoft)
(gen_random_uuid(), 'MSFT', '5MIN', NOW() - INTERVAL '5 minutes', 418.50, 419.20, 418.30, 418.90, 8500000),
(gen_random_uuid(), 'MSFT', '5MIN', NOW() - INTERVAL '10 minutes', 418.00, 418.70, 417.90, 418.50, 8200000),
(gen_random_uuid(), 'MSFT', '5MIN', NOW() - INTERVAL '15 minutes', 417.80, 418.30, 417.70, 418.00, 9100000),
(gen_random_uuid(), 'MSFT', '5MIN', NOW() - INTERVAL '20 minutes', 417.50, 418.00, 417.40, 417.80, 7800000),

-- GOOGL (Google/Alphabet)
(gen_random_uuid(), 'GOOGL', '5MIN', NOW() - INTERVAL '5 minutes', 165.80, 166.20, 165.60, 165.95, 12500000),
(gen_random_uuid(), 'GOOGL', '5MIN', NOW() - INTERVAL '10 minutes', 165.50, 165.90, 165.40, 165.80, 11900000),
(gen_random_uuid(), 'GOOGL', '5MIN', NOW() - INTERVAL '15 minutes', 165.20, 165.70, 165.10, 165.50, 13200000),
(gen_random_uuid(), 'GOOGL', '5MIN', NOW() - INTERVAL '20 minutes', 165.40, 165.50, 165.00, 165.20, 10800000),

-- NVDA (NVIDIA)
(gen_random_uuid(), 'NVDA', '5MIN', NOW() - INTERVAL '5 minutes', 136.50, 137.20, 136.30, 136.85, 45000000),
(gen_random_uuid(), 'NVDA', '5MIN', NOW() - INTERVAL '10 minutes', 136.00, 136.70, 135.90, 136.50, 42500000),
(gen_random_uuid(), 'NVDA', '5MIN', NOW() - INTERVAL '15 minutes', 135.80, 136.40, 135.70, 136.00, 48000000),
(gen_random_uuid(), 'NVDA', '5MIN', NOW() - INTERVAL '20 minutes', 136.10, 136.20, 135.60, 135.80, 39500000),

-- TSLA (Tesla)
(gen_random_uuid(), 'TSLA', '5MIN', NOW() - INTERVAL '5 minutes', 248.50, 249.80, 248.20, 249.35, 28000000),
(gen_random_uuid(), 'TSLA', '5MIN', NOW() - INTERVAL '10 minutes', 247.80, 248.70, 247.60, 248.50, 26500000),
(gen_random_uuid(), 'TSLA', '5MIN', NOW() - INTERVAL '15 minutes', 247.20, 248.10, 247.00, 247.80, 29800000),
(gen_random_uuid(), 'TSLA', '5MIN', NOW() - INTERVAL '20 minutes', 247.50, 247.70, 247.00, 247.20, 24200000)

ON CONFLICT ("Symbol", "Timeframe", "Timestamp") DO NOTHING;

-- =====================================================
-- PART 3: Populate NYSE Stock Data
-- =====================================================

INSERT INTO market_data ("Id", "Symbol", "Timeframe", "Timestamp", "Open", "High", "Low", "Close", "Volume")
VALUES
-- JPM (JPMorgan Chase)
(gen_random_uuid(), 'JPM', '5MIN', NOW() - INTERVAL '5 minutes', 215.50, 216.20, 215.30, 215.90, 5500000),
(gen_random_uuid(), 'JPM', '5MIN', NOW() - INTERVAL '10 minutes', 215.00, 215.70, 214.90, 215.50, 5200000),
(gen_random_uuid(), 'JPM', '5MIN', NOW() - INTERVAL '15 minutes', 214.80, 215.40, 214.70, 215.00, 5800000),
(gen_random_uuid(), 'JPM', '5MIN', NOW() - INTERVAL '20 minutes', 214.50, 215.00, 214.40, 214.80, 4900000),

-- BA (Boeing)
(gen_random_uuid(), 'BA', '5MIN', NOW() - INTERVAL '5 minutes', 148.50, 149.20, 148.30, 148.85, 8500000),
(gen_random_uuid(), 'BA', '5MIN', NOW() - INTERVAL '10 minutes', 148.00, 148.70, 147.90, 148.50, 8100000),
(gen_random_uuid(), 'BA', '5MIN', NOW() - INTERVAL '15 minutes', 147.80, 148.40, 147.70, 148.00, 9200000),
(gen_random_uuid(), 'BA', '5MIN', NOW() - INTERVAL '20 minutes', 147.50, 148.00, 147.40, 147.80, 7600000)

ON CONFLICT ("Symbol", "Timeframe", "Timestamp") DO NOTHING;

-- =====================================================
-- PART 4: Populate CRYPTO (Binance) Data
-- =====================================================

INSERT INTO market_data ("Id", "Symbol", "Timeframe", "Timestamp", "Open", "High", "Low", "Close", "Volume")
VALUES
-- BTCUSDT (Bitcoin)
(gen_random_uuid(), 'BTCUSDT', '5MIN', NOW() - INTERVAL '5 minutes', 123450.00, 123580.00, 123380.00, 123500.00, 850.5),
(gen_random_uuid(), 'BTCUSDT', '5MIN', NOW() - INTERVAL '10 minutes', 123300.00, 123480.00, 123250.00, 123450.00, 920.3),
(gen_random_uuid(), 'BTCUSDT', '5MIN', NOW() - INTERVAL '15 minutes', 123200.00, 123350.00, 123150.00, 123300.00, 1150.8),
(gen_random_uuid(), 'BTCUSDT', '5MIN', NOW() - INTERVAL '20 minutes', 123100.00, 123250.00, 123050.00, 123200.00, 890.6),

-- ETHUSDT (Ethereum)
(gen_random_uuid(), 'ETHUSDT', '5MIN', NOW() - INTERVAL '5 minutes', 4390.00, 4398.00, 4385.00, 4392.00, 12500.5),
(gen_random_uuid(), 'ETHUSDT', '5MIN', NOW() - INTERVAL '10 minutes', 4380.00, 4395.00, 4375.00, 4390.00, 13200.8),
(gen_random_uuid(), 'ETHUSDT', '5MIN', NOW() - INTERVAL '15 minutes', 4370.00, 4385.00, 4365.00, 4380.00, 14800.2),
(gen_random_uuid(), 'ETHUSDT', '5MIN', NOW() - INTERVAL '20 minutes', 4365.00, 4375.00, 4360.00, 4370.00, 11900.7),

-- XRPUSDT (Ripple)
(gen_random_uuid(), 'XRPUSDT', '5MIN', NOW() - INTERVAL '5 minutes', 0.6250, 0.6280, 0.6240, 0.6265, 8500000),
(gen_random_uuid(), 'XRPUSDT', '5MIN', NOW() - INTERVAL '10 minutes', 0.6230, 0.6265, 0.6220, 0.6250, 8200000),
(gen_random_uuid(), 'XRPUSDT', '5MIN', NOW() - INTERVAL '15 minutes', 0.6210, 0.6245, 0.6200, 0.6230, 9100000),
(gen_random_uuid(), 'XRPUSDT', '5MIN', NOW() - INTERVAL '20 minutes', 0.6220, 0.6230, 0.6195, 0.6210, 7800000),

-- SOLUSDT (Solana)
(gen_random_uuid(), 'SOLUSDT', '5MIN', NOW() - INTERVAL '5 minutes', 168.50, 169.20, 168.30, 168.85, 125000),
(gen_random_uuid(), 'SOLUSDT', '5MIN', NOW() - INTERVAL '10 minutes', 168.00, 168.70, 167.90, 168.50, 118000),
(gen_random_uuid(), 'SOLUSDT', '5MIN', NOW() - INTERVAL '15 minutes', 167.80, 168.40, 167.70, 168.00, 135000),
(gen_random_uuid(), 'SOLUSDT', '5MIN', NOW() - INTERVAL '20 minutes', 167.50, 168.00, 167.40, 167.80, 102000),

-- AVAXUSDT (Avalanche)
(gen_random_uuid(), 'AVAXUSDT', '5MIN', NOW() - INTERVAL '5 minutes', 38.50, 38.80, 38.40, 38.65, 95000),
(gen_random_uuid(), 'AVAXUSDT', '5MIN', NOW() - INTERVAL '10 minutes', 38.30, 38.65, 38.20, 38.50, 88000),
(gen_random_uuid(), 'AVAXUSDT', '5MIN', NOW() - INTERVAL '15 minutes', 38.10, 38.45, 38.00, 38.30, 102000),
(gen_random_uuid(), 'AVAXUSDT', '5MIN', NOW() - INTERVAL '20 minutes', 38.20, 38.30, 38.00, 38.10, 76000),

-- SUIUSDT (Sui)
(gen_random_uuid(), 'SUIUSDT', '5MIN', NOW() - INTERVAL '5 minutes', 3.65, 3.68, 3.63, 3.66, 2500000),
(gen_random_uuid(), 'SUIUSDT', '5MIN', NOW() - INTERVAL '10 minutes', 3.62, 3.67, 3.61, 3.65, 2350000),
(gen_random_uuid(), 'SUIUSDT', '5MIN', NOW() - INTERVAL '15 minutes', 3.60, 3.64, 3.59, 3.62, 2680000),
(gen_random_uuid(), 'SUIUSDT', '5MIN', NOW() - INTERVAL '20 minutes', 3.61, 3.62, 3.58, 3.60, 2120000),

-- ENAUSDT (Ethena)
(gen_random_uuid(), 'ENAUSDT', '5MIN', NOW() - INTERVAL '5 minutes', 1.15, 1.16, 1.14, 1.15, 3500000),
(gen_random_uuid(), 'ENAUSDT', '5MIN', NOW() - INTERVAL '10 minutes', 1.14, 1.15, 1.13, 1.15, 3200000),
(gen_random_uuid(), 'ENAUSDT', '5MIN', NOW() - INTERVAL '15 minutes', 1.13, 1.14, 1.12, 1.14, 3800000),
(gen_random_uuid(), 'ENAUSDT', '5MIN', NOW() - INTERVAL '20 minutes', 1.13, 1.13, 1.12, 1.13, 2900000),

-- UNIUSDT (Uniswap)
(gen_random_uuid(), 'UNIUSDT', '5MIN', NOW() - INTERVAL '5 minutes', 11.50, 11.58, 11.45, 11.53, 850000),
(gen_random_uuid(), 'UNIUSDT', '5MIN', NOW() - INTERVAL '10 minutes', 11.45, 11.55, 11.42, 11.50, 780000),
(gen_random_uuid(), 'UNIUSDT', '5MIN', NOW() - INTERVAL '15 minutes', 11.40, 11.50, 11.38, 11.45, 920000),
(gen_random_uuid(), 'UNIUSDT', '5MIN', NOW() - INTERVAL '20 minutes', 11.42, 11.45, 11.37, 11.40, 670000),

-- BNBUSDT (Binance Coin)
(gen_random_uuid(), 'BNBUSDT', '5MIN', NOW() - INTERVAL '5 minutes', 625.50, 627.20, 625.00, 626.30, 45000),
(gen_random_uuid(), 'BNBUSDT', '5MIN', NOW() - INTERVAL '10 minutes', 624.80, 626.50, 624.50, 625.50, 42000),
(gen_random_uuid(), 'BNBUSDT', '5MIN', NOW() - INTERVAL '15 minutes', 624.00, 625.80, 623.80, 624.80, 48000),
(gen_random_uuid(), 'BNBUSDT', '5MIN', NOW() - INTERVAL '20 minutes', 624.50, 624.80, 623.50, 624.00, 38000)

ON CONFLICT ("Symbol", "Timeframe", "Timestamp") DO NOTHING;

-- =====================================================
-- VERIFICATION QUERIES
-- =====================================================

-- Check results by exchange/venue
SELECT
    CASE
        WHEN "Symbol" IN ('THYAO', 'GARAN', 'SISE') THEN 'BIST'
        WHEN "Symbol" IN ('AAPL', 'MSFT', 'GOOGL', 'NVDA', 'TSLA') THEN 'NASDAQ'
        WHEN "Symbol" IN ('JPM', 'BA') THEN 'NYSE'
        WHEN "Symbol" LIKE '%USDT' THEN 'BINANCE'
        ELSE 'OTHER'
    END AS exchange,
    COUNT(*) as record_count,
    MIN("Timestamp") as oldest_timestamp,
    MAX("Timestamp") as newest_timestamp
FROM market_data
GROUP BY exchange
ORDER BY exchange;

-- Check total records
SELECT
    'Total market_data records: ' || COUNT(*) as summary,
    'Unique symbols: ' || COUNT(DISTINCT "Symbol") as symbols
FROM market_data;

-- Show sample of recent data
SELECT
    "Symbol",
    "Close" as price,
    "Volume",
    "Timestamp"
FROM market_data
ORDER BY "Timestamp" DESC
LIMIT 20;

-- Success message
SELECT '✅ Market data population completed successfully!' as status;
