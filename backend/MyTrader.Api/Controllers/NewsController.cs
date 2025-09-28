using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyTrader.Core.DTOs;
using System.ComponentModel.DataAnnotations;

namespace MyTrader.Api.Controllers;

/// <summary>
/// API controller for news and market updates
/// Provides market news, stock news, crypto news, and search functionality
/// </summary>
[ApiController]
[Route("api/v1/news")]
public class NewsController : ControllerBase
{
    private readonly ILogger<NewsController> _logger;

    public NewsController(ILogger<NewsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get market news with optional filtering
    /// </summary>
    [HttpGet("market")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<NewsItem>), 200)]
    [ProducesResponseType(typeof(object), 500)]
    public ActionResult<List<NewsItem>> GetMarketNews(
        [FromQuery] string? assetClass = null,
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0)
    {
        try
        {
            _logger.LogInformation("Getting market news: AssetClass={AssetClass}, Limit={Limit}, Offset={Offset}",
                assetClass, limit, offset);

            var news = GenerateMockMarketNews(assetClass, limit, offset);
            return Ok(news);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting market news");
            return StatusCode(500, new { message = "Failed to retrieve market news" });
        }
    }

    /// <summary>
    /// Get crypto news
    /// </summary>
    [HttpGet("crypto")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<NewsItem>), 200)]
    [ProducesResponseType(typeof(object), 500)]
    public ActionResult<List<NewsItem>> GetCryptoNews([FromQuery] int limit = 20)
    {
        try
        {
            _logger.LogInformation("Getting crypto news: Limit={Limit}", limit);

            var news = GenerateMockCryptoNews(limit);
            return Ok(news);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting crypto news");
            return StatusCode(500, new { message = "Failed to retrieve crypto news" });
        }
    }

    /// <summary>
    /// Get stock news
    /// </summary>
    [HttpGet("stocks")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<NewsItem>), 200)]
    [ProducesResponseType(typeof(object), 500)]
    public ActionResult<List<NewsItem>> GetStockNews(
        [FromQuery] string market = "BIST",
        [FromQuery] int limit = 20)
    {
        try
        {
            _logger.LogInformation("Getting stock news: Market={Market}, Limit={Limit}", market, limit);

            var news = GenerateMockStockNews(market, limit);
            return Ok(news);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock news");
            return StatusCode(500, new { message = "Failed to retrieve stock news" });
        }
    }

    /// <summary>
    /// Search news
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<NewsItem>), 200)]
    [ProducesResponseType(typeof(object), 500)]
    public ActionResult<List<NewsItem>> SearchNews(
        [FromQuery, Required] string q,
        [FromQuery] int limit = 20)
    {
        try
        {
            _logger.LogInformation("Searching news: Query={Query}, Limit={Limit}", q, limit);

            var news = GenerateMockSearchResults(q, limit);
            return Ok(news);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching news");
            return StatusCode(500, new { message = "Failed to search news" });
        }
    }

    /// <summary>
    /// Get news by ID
    /// </summary>
    [HttpGet("{newsId}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(NewsItem), 200)]
    [ProducesResponseType(typeof(object), 404)]
    [ProducesResponseType(typeof(object), 500)]
    public ActionResult<NewsItem> GetNewsById(string newsId)
    {
        try
        {
            _logger.LogInformation("Getting news by ID: {NewsId}", newsId);

            var news = GenerateMockNewsById(newsId);
            if (news == null)
            {
                return NotFound(new { message = $"News item with ID '{newsId}' not found" });
            }

            return Ok(news);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting news by ID: {NewsId}", newsId);
            return StatusCode(500, new { message = "Failed to retrieve news item" });
        }
    }

    // ============================================
    // MOCK DATA GENERATORS
    // ============================================

    private List<NewsItem> GenerateMockMarketNews(string? assetClass, int limit, int offset)
    {
        var baseNews = new List<NewsItem>
        {
            new NewsItem
            {
                Id = "1",
                Title = "Piyasalarda Günün Öne Çıkan Gelişmeleri",
                Summary = "Küresel piyasalarda bugün yaşanan önemli gelişmeler ve yatırımcılar için dikkat çekici noktalar.",
                Content = "Detaylı piyasa analizi ve uzman yorumları...",
                Author = "Piyasa Analisti",
                Source = "myTrader Haber",
                Category = "Market",
                AssetClass = assetClass ?? "GENERAL",
                PublishedAt = DateTime.UtcNow.AddHours(-2),
                ImageUrl = null,
                Url = "https://example.com/news/1",
                Tags = new[] { "piyasa", "analiz", "günlük" }
            },
            new NewsItem
            {
                Id = "2",
                Title = "Merkez Bankası Faiz Kararı Açıklandı",
                Summary = "Merkez Bankası'nın bugün açıkladığı faiz kararı piyasalarda nasıl karşılandı?",
                Content = "Faiz kararının detayları ve piyasa yansımaları...",
                Author = "Ekonomi Editörü",
                Source = "myTrader Haber",
                Category = "Economics",
                AssetClass = assetClass ?? "GENERAL",
                PublishedAt = DateTime.UtcNow.AddHours(-4),
                ImageUrl = null,
                Url = "https://example.com/news/2",
                Tags = new[] { "merkez-bankası", "faiz", "ekonomi" }
            },
            new NewsItem
            {
                Id = "3",
                Title = "Teknoloji Hisselerinde Yükseliş Trendi",
                Summary = "Teknoloji sektöründe yaşanan pozitif gelişmeler yatırımcı ilgisini artırıyor.",
                Content = "Teknoloji hisselerindeki yükseliş detayları...",
                Author = "Sektör Analisti",
                Source = "myTrader Haber",
                Category = "Technology",
                AssetClass = assetClass ?? "STOCK",
                PublishedAt = DateTime.UtcNow.AddHours(-6),
                ImageUrl = null,
                Url = "https://example.com/news/3",
                Tags = new[] { "teknoloji", "hisse", "yükseliş" }
            }
        };

        // Filter by asset class if specified
        if (!string.IsNullOrEmpty(assetClass))
        {
            baseNews = baseNews.Where(n => n.AssetClass.Equals(assetClass, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return baseNews.Skip(offset).Take(limit).ToList();
    }

    private List<NewsItem> GenerateMockCryptoNews(int limit)
    {
        var cryptoNews = new List<NewsItem>
        {
            new NewsItem
            {
                Id = "crypto-1",
                Title = "Bitcoin 45.000 Dolar Seviyesini Test Ediyor",
                Summary = "Bitcoin, son günlerde yaşanan yükseliş ile kritik direnç seviyesine yaklaştı.",
                Content = "Bitcoin teknik analizi ve fiyat hedefleri...",
                Author = "Kripto Analisti",
                Source = "CryptoNews",
                Category = "Cryptocurrency",
                AssetClass = "CRYPTO",
                PublishedAt = DateTime.UtcNow.AddHours(-1),
                ImageUrl = null,
                Url = "https://example.com/crypto-news/1",
                Tags = new[] { "bitcoin", "btc", "fiyat" }
            },
            new NewsItem
            {
                Id = "crypto-2",
                Title = "Ethereum 2.0 Güncellemesi Etkilerini Gösteriyor",
                Summary = "Ethereum'un son güncellemesi ağ performansını önemli ölçüde artırdı.",
                Content = "Ethereum 2.0 teknik detayları...",
                Author = "Blockchain Uzmanı",
                Source = "CryptoNews",
                Category = "Cryptocurrency",
                AssetClass = "CRYPTO",
                PublishedAt = DateTime.UtcNow.AddHours(-3),
                ImageUrl = null,
                Url = "https://example.com/crypto-news/2",
                Tags = new[] { "ethereum", "eth", "upgrade" }
            }
        };

        return cryptoNews.Take(limit).ToList();
    }

    private List<NewsItem> GenerateMockStockNews(string market, int limit)
    {
        var stockNews = new List<NewsItem>();

        if (market.Equals("BIST", StringComparison.OrdinalIgnoreCase))
        {
            stockNews.AddRange(new[]
            {
                new NewsItem
                {
                    Id = "bist-1",
                    Title = "BIST 100 Endeksi Güçlü Performans Sergiliyor",
                    Summary = "Borsa İstanbul'da yaşanan yükseliş trendi devam ediyor.",
                    Content = "BIST 100 teknik analizi...",
                    Author = "BIST Analisti",
                    Source = "Borsa Haber",
                    Category = "Stock Market",
                    AssetClass = "STOCK",
                    PublishedAt = DateTime.UtcNow.AddHours(-2),
                    ImageUrl = null,
                    Url = "https://example.com/bist-news/1",
                    Tags = new[] { "bist", "borsa", "endeks" }
                }
            });
        }
        else if (market.Equals("NASDAQ", StringComparison.OrdinalIgnoreCase))
        {
            stockNews.AddRange(new[]
            {
                new NewsItem
                {
                    Id = "nasdaq-1",
                    Title = "NASDAQ'ta Teknoloji Hisselerinde Hareketlilik",
                    Summary = "Apple, Microsoft ve Google hisselerinde dikkat çeken gelişmeler.",
                    Content = "NASDAQ teknoloji hisseleri analizi...",
                    Author = "Wall Street Analisti",
                    Source = "US Markets",
                    Category = "Stock Market",
                    AssetClass = "STOCK",
                    PublishedAt = DateTime.UtcNow.AddHours(-1),
                    ImageUrl = null,
                    Url = "https://example.com/nasdaq-news/1",
                    Tags = new[] { "nasdaq", "tech", "stocks" }
                }
            });
        }

        return stockNews.Take(limit).ToList();
    }

    private List<NewsItem> GenerateMockSearchResults(string query, int limit)
    {
        // Simple mock search - return relevant news based on query
        var allNews = GenerateMockMarketNews(null, 50, 0)
            .Concat(GenerateMockCryptoNews(20))
            .Concat(GenerateMockStockNews("BIST", 10))
            .Concat(GenerateMockStockNews("NASDAQ", 10));

        var searchResults = allNews
            .Where(n => n.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                       n.Summary.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                       n.Tags.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase)))
            .Take(limit)
            .ToList();

        return searchResults;
    }

    private NewsItem? GenerateMockNewsById(string newsId)
    {
        // Return a specific news item based on ID
        return new NewsItem
        {
            Id = newsId,
            Title = $"Haber Detayı - ID: {newsId}",
            Summary = "Bu haber detay sayfasının özeti...",
            Content = "Bu haberin tam içeriği burada yer alacak. Detaylı analiz ve uzman yorumları...",
            Author = "Haber Editörü",
            Source = "myTrader Haber",
            Category = "General",
            AssetClass = "GENERAL",
            PublishedAt = DateTime.UtcNow.AddHours(-5),
            ImageUrl = null,
            Url = $"https://example.com/news/{newsId}",
            Tags = new[] { "haber", "detay", "analiz" }
        };
    }
}

// Define NewsItem if not already defined in DTOs
public class NewsItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string AssetClass { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public string? ImageUrl { get; set; }
    public string? Url { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
}