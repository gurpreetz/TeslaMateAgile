using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Contrib.HttpClient;
using NUnit.Framework;
using TeslaMateAgile.Data.Options;
using TeslaMateAgile.Services;

namespace TeslaMateAgile.Tests.Services;

public class PGEServiceTests
{
    private PGEService _subject;
    private Mock<HttpMessageHandler> _handler;

    [SetUp]
    public void Setup()
    {
        _handler = new Mock<HttpMessageHandler>();
        var httpClient = _handler.CreateClient();
        var pgeOptions = Options.Create(new PGEOptions 
        { 
            BaseUrl = "https://pge-pe-api.gridx.com",
            Utility = "PGE",
            Market = "DAM",
            RateName = "EV2A",
            RepresentativeCircuitId = "083611114",
            Program = "CalFUSE"
        });
        httpClient.BaseAddress = new Uri(pgeOptions.Value.BaseUrl);
        
        var mockLogger = new Mock<ILogger<PGEService>>();
        _subject = new PGEService(httpClient, pgeOptions, mockLogger.Object);
    }

    [Test]
    public async Task TestAsync()
    {
        var jsonFile = "pge_test.json";
        var json = File.ReadAllText(Path.Combine("Prices", jsonFile));

        _handler.SetupAnyRequest()
            .ReturnsResponse(json, "application/json");

        var startDate = DateTimeOffset.Parse("2023-10-26T00:00:00-07:00");
        var endDate = DateTimeOffset.Parse("2023-10-26T05:00:00-07:00");
        var prices = await _subject.GetPriceData(startDate, endDate);
        var priceList = prices.ToList();

        // The service now requests multiple days (with buffer days for timezone handling)
        // so we expect 3 requests: one day before, the actual day, and one day after
        _handler.VerifyAnyRequest(Times.Exactly(3));

        // Since we're returning the same test data for all 3 API calls,
        // we'll get 15 total prices (5 from each call), all with the same dates
        Assert.That(priceList.Count, Is.EqualTo(15), "Should return all intervals from the 3 API calls");
        
        // Verify that our expected prices are in the result
        var targetPrice1 = priceList.FirstOrDefault(p => 
            p.ValidFrom == DateTimeOffset.Parse("2023-10-26T00:00:00-07:00"));
        var targetPrice2 = priceList.FirstOrDefault(p => 
            p.ValidFrom == DateTimeOffset.Parse("2023-10-26T04:00:00-07:00"));
            
        Assert.That(targetPrice1, Is.Not.Null, "Should contain price starting at 00:00");
        Assert.That(targetPrice1.ValidTo, Is.EqualTo(DateTimeOffset.Parse("2023-10-26T01:00:00-07:00")));
        Assert.That(targetPrice1.Value, Is.EqualTo(0.15234M));
        
        Assert.That(targetPrice2, Is.Not.Null, "Should contain price starting at 04:00");
        Assert.That(targetPrice2.ValidTo, Is.EqualTo(DateTimeOffset.Parse("2023-10-26T05:00:00-07:00")));
        Assert.That(targetPrice2.Value, Is.EqualTo(0.14456M));
    }

    [Test]
    public void Constructor_ShouldThrowException_WhenRateNameIsEmpty()
    {
        var httpClient = new HttpClient();
        var pgeOptions = Options.Create(new PGEOptions 
        { 
            BaseUrl = "https://pge-pe-api.gridx.com",
            Utility = "PGE",
            Market = "DAM",
            RateName = "", // Empty RateName
            RepresentativeCircuitId = "083611114",
            Program = "CalFUSE"
        });
        var mockLogger = new Mock<ILogger<PGEService>>();
        
        var ex = Assert.Throws<InvalidOperationException>(() => 
            new PGEService(httpClient, pgeOptions, mockLogger.Object));
        
        Assert.That(ex.Message, Does.Contain("PGE RateName is required"));
    }

    [Test]
    public void Constructor_ShouldThrowException_WhenRepresentativeCircuitIdIsEmpty()
    {
        var httpClient = new HttpClient();
        var pgeOptions = Options.Create(new PGEOptions 
        { 
            BaseUrl = "https://pge-pe-api.gridx.com",
            Utility = "PGE",
            Market = "DAM",
            RateName = "EV2A",
            RepresentativeCircuitId = "", // Empty RepresentativeCircuitId
            Program = "CalFUSE"
        });
        var mockLogger = new Mock<ILogger<PGEService>>();
        
        var ex = Assert.Throws<InvalidOperationException>(() => 
            new PGEService(httpClient, pgeOptions, mockLogger.Object));
        
        Assert.That(ex.Message, Does.Contain("PGE RepresentativeCircuitId is required"));
    }
}
