using System.Reflection;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using MovieSearcher.Core.Extensions;
using MovieSearcher.Core.Query;
using StackExchange.Redis;

namespace MovieSearcher.Services.Tests;

public class CachedMovieDetailAggregatorServiceTests
{
    private Mock<IDistributedCache> _distributedCacheMock;
    private Mock<ILogger<CachedMovieDetailAggregatorService>> _loggerMock;
    private Mock<IMovieDetailAggregatorService> _movieDetailAggregatorServiceMock;

    private CachedMovieDetailAggregatorService Target;

    public CachedMovieDetailAggregatorServiceTests()
    {
        _loggerMock = new Mock<ILogger<CachedMovieDetailAggregatorService>>();
        _movieDetailAggregatorServiceMock = new Mock<IMovieDetailAggregatorService>();
        _distributedCacheMock = new Mock<IDistributedCache>();

        Target = new CachedMovieDetailAggregatorService(_loggerMock.Object, _movieDetailAggregatorServiceMock.Object,
            _distributedCacheMock.Object);
    }

    [Fact]
    public async Task Empty_search_should_return_an_error_with_message()
    {
        var videoResponse = await Target.Search(It.IsAny<QueryParameters>(), CancellationToken.None);

        videoResponse.IsSuccess.Should().BeFalse();
        videoResponse.Data.Should().BeNull();
        videoResponse.Messages.Length.Should().Be(1);
        videoResponse.Messages.First().Message.Should().Be("Query can not be null!");

        _distributedCacheMock.Verify(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _movieDetailAggregatorServiceMock.Verify(
            x => x.Search(It.IsAny<QueryParameters>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Given_search_should_get_from_cache_but_verify_key_and_return_result()
    {
        var queryParameters = new QueryParameters() { Query = "key-text" };
        var cacheData = await ReadFromEmbeddedResource("VideoData.SampleVideo.json");

        _distributedCacheMock.Setup(x => x.GetAsync(queryParameters.Query, CancellationToken.None))
            .ReturnsAsync(Encoding.UTF8.GetBytes(cacheData));

        var videoResponse = await Target.Search(queryParameters, CancellationToken.None);

        videoResponse.IsSuccess.Should().BeTrue();
        videoResponse.Data.Should().NotBeNull();

        videoResponse.Data.Count.Should().Be(1);
        videoResponse.Page.Should().Be(1);
        videoResponse.PerPage.Should().Be(1);
        videoResponse.TotalCount.Should().Be(801);

        var expectedKey = queryParameters.GenerateKey();

        _distributedCacheMock.Verify(x => x.GetAsync(expectedKey, It.IsAny<CancellationToken>()), Times.Once);
        _movieDetailAggregatorServiceMock.Verify(
            x => x.Search(It.IsAny<QueryParameters>(), It.IsAny<CancellationToken>()), Times.Never);
        _distributedCacheMock.Verify(
            x => x.SetAsync(expectedKey, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Given_search_should_get_from_api_when_no_cache()
    {
        var queryParameters = new QueryParameters() { Query = "key-text" };
        var expectedKey = queryParameters.GenerateKey();

        await Target.Search(queryParameters, CancellationToken.None);

        _distributedCacheMock.Verify(x => x.GetAsync(expectedKey, It.IsAny<CancellationToken>()), Times.Once);
        _movieDetailAggregatorServiceMock.Verify(x => x.Search(queryParameters, It.IsAny<CancellationToken>()),
            Times.Once);
        _distributedCacheMock.Verify(
            x => x.SetAsync(expectedKey, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task When_redis_error_occurs_return_readable_message_instead_of_fallback()
    {
        var queryParameters = new QueryParameters() { Query = "key-text" };
        var expectedKey = queryParameters.GenerateKey();

        _distributedCacheMock.Setup(x => x.GetAsync(queryParameters.Query, CancellationToken.None))
            .ThrowsAsync(new RedisException("Redis connection error"));

        var response = await Target.Search(queryParameters, CancellationToken.None);

        response.IsSuccess.Should().BeFalse();
        response.Messages.Length.Should().Be(1);
        response.Messages.First().Message.Should().Be("An error occured on Redis connection!");

        _distributedCacheMock.Verify(x => x.GetAsync(expectedKey, It.IsAny<CancellationToken>()), Times.Once);
        _movieDetailAggregatorServiceMock.Verify(x => x.Search(queryParameters, It.IsAny<CancellationToken>()),
            Times.Never);
        _distributedCacheMock.Verify(
            x => x.SetAsync(expectedKey, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }


    #region JsonHelper

    private async Task<string?> ReadFromEmbeddedResource(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();

        await using var stream = assembly.GetManifestResourceStream($"{GetType().Namespace}.{fileName}");

        if (stream == null) return null;
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();
        return content;
    }

    #endregion
}