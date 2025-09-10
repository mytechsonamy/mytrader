using System;
using System.Collections.Generic;

namespace MyTrader.Core.DTOs.Analytics;

public class ChartDataResponse
{
    public string ChartType { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Period { get; set; } = default!;
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
    public ChartConfiguration Configuration { get; set; } = new();
    public List<ChartSeries> Series { get; set; } = new();
    public ChartMetadata Metadata { get; set; } = new();
}

public class ChartConfiguration
{
    public string XAxisType { get; set; } = "datetime"; // datetime, category, linear
    public string YAxisType { get; set; } = "linear"; // linear, logarithmic
    public string XAxisLabel { get; set; } = "Time";
    public string YAxisLabel { get; set; } = "Value";
    public string Currency { get; set; } = "USD";
    public int DecimalPlaces { get; set; } = 2;
    public bool ShowGrid { get; set; } = true;
    public bool ShowLegend { get; set; } = true;
    public bool IsZoomEnabled { get; set; } = true;
}

public class ChartSeries
{
    public string Name { get; set; } = default!;
    public string Type { get; set; } = default!; // line, area, column, candlestick, scatter
    public string Color { get; set; } = "#007bff";
    public List<ChartDataPoint> Data { get; set; } = new();
    public Dictionary<string, object> Options { get; set; } = new();
}

public class ChartDataPoint
{
    public DateTimeOffset Timestamp { get; set; }
    public decimal Value { get; set; }
    public decimal? Volume { get; set; }
    public decimal? High { get; set; }
    public decimal? Low { get; set; }
    public decimal? Open { get; set; }
    public decimal? Close { get; set; }
    public string? Label { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class ChartMetadata
{
    public decimal MinValue { get; set; }
    public decimal MaxValue { get; set; }
    public decimal AverageValue { get; set; }
    public int DataPointCount { get; set; }
    public DateTimeOffset PeriodStart { get; set; }
    public DateTimeOffset PeriodEnd { get; set; }
    public string DataSource { get; set; } = default!;
    public Dictionary<string, object> AdditionalInfo { get; set; } = new();
}

// Specific chart types
public class EquityCurveData : ChartDataResponse
{
    public decimal StartingBalance { get; set; }
    public decimal EndingBalance { get; set; }
    public decimal PeakBalance { get; set; }
    public decimal LowestBalance { get; set; }
    public List<DrawdownPeriod> DrawdownPeriods { get; set; } = new();
}

public class DrawdownPeriod
{
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal MaxDrawdownPercentage { get; set; }
    public int DurationDays { get; set; }
}

public class PnLDistributionData : ChartDataResponse
{
    public List<PnLBucket> Distribution { get; set; } = new();
    public decimal MeanPnL { get; set; }
    public decimal MedianPnL { get; set; }
    public decimal StandardDeviation { get; set; }
    public decimal Skewness { get; set; }
    public decimal Kurtosis { get; set; }
}

public class PnLBucket
{
    public decimal RangeStart { get; set; }
    public decimal RangeEnd { get; set; }
    public int Count { get; set; }
    public decimal Frequency { get; set; }
    public decimal CumulativeFrequency { get; set; }
}

public class HeatmapData : ChartDataResponse
{
    public List<HeatmapDataPoint> HeatmapPoints { get; set; } = new();
    public HeatmapScale Scale { get; set; } = new();
}

public class HeatmapDataPoint
{
    public string XCategory { get; set; } = default!;
    public string YCategory { get; set; } = default!;
    public decimal Value { get; set; }
    public string DisplayValue { get; set; } = default!;
    public string Color { get; set; } = default!;
}

public class HeatmapScale
{
    public decimal MinValue { get; set; }
    public decimal MaxValue { get; set; }
    public List<string> ColorScale { get; set; } = new();
}