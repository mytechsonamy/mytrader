# MyTrader Product Overview

MyTrader is a full-stack crypto trading platform built with .NET backend and React/React Native frontends. The platform provides real-time market data, trading signals, strategy backtesting, and portfolio management capabilities.

## Core Features

- **Authentication System**: JWT-based auth with email verification
- **Real-time Market Data**: Live price feeds via SignalR WebSockets
- **Trading Strategies**: Technical indicators (Bollinger Bands, RSI, MACD)
- **Signal Generation**: Automated buy/sell signals based on multiple indicators
- **Portfolio Management**: Track positions and performance
- **Cross-platform**: Web app (React) and mobile app (React Native/Expo)

## Target Users

- Crypto traders seeking automated signal generation
- Portfolio managers tracking multiple assets
- Strategy developers testing trading algorithms

## Business Logic

The platform uses a multi-indicator approach for signal generation:
- **BUY Signal**: RSI < 30, price at lower Bollinger Band, MACD bullish
- **SELL Signal**: RSI > 70, price at upper Bollinger Band, MACD bearish
- **NEUTRAL**: Mixed or insufficient signals

## Data Sources

- Market data import via REST APIs
- Real-time updates through WebSocket connections
- PostgreSQL for persistent storage