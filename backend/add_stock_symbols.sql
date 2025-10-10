-- Add sample stock symbols for testing

-- BIST symbols
INSERT INTO symbols (
    "Id", ticker, venue, asset_class, market_id, base_currency, quote_currency,
    full_name, display, is_active, is_tracked, is_popular, price_precision,
    quantity_precision, display_order, created_at, updated_at, broadcast_priority, is_default_symbol
) VALUES
-- BIST stocks (Turkish market)
(gen_random_uuid(), 'THYAO', 'BIST', 'STOCK_BIST', 'b9aeb9e8-ce92-403b-83ed-bd6886029876', 'TRY', 'TRY',
 'Türk Hava Yolları', 'THYAO', true, true, true, 2, 0, 1, NOW(), NOW(), 90, true),
(gen_random_uuid(), 'GARAN', 'BIST', 'STOCK_BIST', 'b9aeb9e8-ce92-403b-83ed-bd6886029876', 'TRY', 'TRY',
 'Garanti Bankası', 'GARAN', true, true, true, 2, 0, 2, NOW(), NOW(), 85, true),
(gen_random_uuid(), 'AKBNK', 'BIST', 'STOCK_BIST', 'b9aeb9e8-ce92-403b-83ed-bd6886029876', 'TRY', 'TRY',
 'Akbank', 'AKBNK', true, true, true, 2, 0, 3, NOW(), NOW(), 85, true),
(gen_random_uuid(), 'ISCTR', 'BIST', 'STOCK_BIST', 'b9aeb9e8-ce92-403b-83ed-bd6886029876', 'TRY', 'TRY',
 'İş Bankası', 'ISCTR', true, true, true, 2, 0, 4, NOW(), NOW(), 80, true),
(gen_random_uuid(), 'TUPRS', 'BIST', 'STOCK_BIST', 'b9aeb9e8-ce92-403b-83ed-bd6886029876', 'TRY', 'TRY',
 'Tüpraş', 'TUPRS', true, true, true, 2, 0, 5, NOW(), NOW(), 80, true),

-- NASDAQ stocks (US tech)
(gen_random_uuid(), 'AAPL', 'NASDAQ', 'STOCK_NASDAQ', '3970be72-7a37-4d9e-8fe9-b8cfcee22310', 'USD', 'USD',
 'Apple Inc.', 'AAPL', true, true, true, 2, 2, 10, NOW(), NOW(), 95, true),
(gen_random_uuid(), 'MSFT', 'NASDAQ', 'STOCK_NASDAQ', '3970be72-7a37-4d9e-8fe9-b8cfcee22310', 'USD', 'USD',
 'Microsoft Corporation', 'MSFT', true, true, true, 2, 2, 11, NOW(), NOW(), 95, true),
(gen_random_uuid(), 'GOOGL', 'NASDAQ', 'STOCK_NASDAQ', '3970be72-7a37-4d9e-8fe9-b8cfcee22310', 'USD', 'USD',
 'Alphabet Inc.', 'GOOGL', true, true, true, 2, 2, 12, NOW(), NOW(), 90, true),
(gen_random_uuid(), 'AMZN', 'NASDAQ', 'STOCK_NASDAQ', '3970be72-7a37-4d9e-8fe9-b8cfcee22310', 'USD', 'USD',
 'Amazon.com Inc.', 'AMZN', true, true, true, 2, 2, 13, NOW(), NOW(), 90, true),
(gen_random_uuid(), 'NVDA', 'NASDAQ', 'STOCK_NASDAQ', '3970be72-7a37-4d9e-8fe9-b8cfcee22310', 'USD', 'USD',
 'NVIDIA Corporation', 'NVDA', true, true, true, 2, 2, 14, NOW(), NOW(), 95, true),

-- NYSE stocks (US traditional)
(gen_random_uuid(), 'JPM', 'NYSE', 'STOCK_NYSE', '1bc859f0-d623-4c87-b199-ae13cfbd5fc6', 'USD', 'USD',
 'JPMorgan Chase & Co.', 'JPM', true, true, true, 2, 2, 20, NOW(), NOW(), 85, true),
(gen_random_uuid(), 'V', 'NYSE', 'STOCK_NYSE', '1bc859f0-d623-4c87-b199-ae13cfbd5fc6', 'USD', 'USD',
 'Visa Inc.', 'V', true, true, true, 2, 2, 21, NOW(), NOW(), 90, true),
(gen_random_uuid(), 'WMT', 'NYSE', 'STOCK_NYSE', '1bc859f0-d623-4c87-b199-ae13cfbd5fc6', 'USD', 'USD',
 'Walmart Inc.', 'WMT', true, true, true, 2, 2, 22, NOW(), NOW(), 80, true),
(gen_random_uuid(), 'JNJ', 'NYSE', 'STOCK_NYSE', '1bc859f0-d623-4c87-b199-ae13cfbd5fc6', 'USD', 'USD',
 'Johnson & Johnson', 'JNJ', true, true, true, 2, 2, 23, NOW(), NOW(), 80, true),
(gen_random_uuid(), 'BAC', 'NYSE', 'STOCK_NYSE', '1bc859f0-d623-4c87-b199-ae13cfbd5fc6', 'USD', 'USD',
 'Bank of America Corp', 'BAC', true, true, true, 2, 2, 24, NOW(), NOW(), 80, true)
ON CONFLICT DO NOTHING;
