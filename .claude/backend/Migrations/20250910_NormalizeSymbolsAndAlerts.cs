using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyTrader.Migrations
{
    public partial class NormalizeSymbolsAndAlerts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add shadow columns to symbols if not present
            migrationBuilder.Sql(@"DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='symbols' AND column_name='display') THEN
        ALTER TABLE symbols ADD COLUMN display VARCHAR(50);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='symbols' AND column_name='is_tracked') THEN
        ALTER TABLE symbols ADD COLUMN is_tracked BOOLEAN NOT NULL DEFAULT true;
    END IF;
END $$;");

            // Strategy table: drop string Symbol, enforce SymbolId
            migrationBuilder.Sql(@"DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='strategies' AND column_name='symbol') THEN
        -- Map existing string symbols to SymbolId
        UPDATE strategies s SET symbol_id = sym.id
        FROM symbols sym
        WHERE s.symbol = sym.ticker AND sym.venue = 'BINANCE';

        ALTER TABLE strategies ALTER COLUMN symbol_id DROP NOT NULL;
        ALTER TABLE strategies DROP COLUMN symbol;
        ALTER TABLE strategies ALTER COLUMN symbol_id SET NOT NULL;
    END IF;
END $$;");

            // user_alerts table
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS user_alerts (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    symbol_id UUID NOT NULL REFERENCES symbols(id),
    condition_json JSONB NOT NULL,
    channels VARCHAR(128) NOT NULL,
    quiet_hours VARCHAR(32),
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS gin_user_alerts_condition ON user_alerts USING GIN (condition_json);
");

            // View for backward compatibility (market_data)
            migrationBuilder.Sql(@"
CREATE OR REPLACE VIEW market_data AS
SELECT c.symbol_id, s.ticker, s.venue, c.timeframe, c.ts, c.open, c.high, c.low, c.close, c.volume
FROM candles c
JOIN symbols s ON s.id = c.symbol_id;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS market_data;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS user_alerts;");
            migrationBuilder.Sql(@"ALTER TABLE strategies ADD COLUMN symbol VARCHAR(20);");
        }
    }
}
