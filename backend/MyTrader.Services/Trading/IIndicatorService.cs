using MyTrader.Core.Models.Indicators;

namespace MyTrader.Services.Trading;

public interface IIndicatorService
{
    BollingerBands CalculateBollingerBands(List<decimal> prices, BollingerBandSettings settings);
    RSI CalculateRSI(List<decimal> prices, RSISettings settings);
    MACD CalculateMACD(List<decimal> prices, MACDSettings settings);
}