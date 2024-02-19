using System.Net;
using MovieSearcher.YoutubeWrapper.Services;
using FluentAssertions;
using Google;
using Microsoft.Extensions.Logging;
using Moq;
using MovieSearcher.Core.Utility;

namespace MovieSearcher.YoutubeWrapper.Tests;

public class YoutubeServiceWrapperTests
{
    private readonly Mock<IDelayService> _delayServiceMock;
    private readonly Mock<ILogger<YoutubeServiceWrapper>> _loggerMock;
    private readonly Mock<IProxyYoutubeVideoService> _proxyYoutubeVideoServiceMock;

    /// <summary>
    /// Main Test Object
    /// </summary>
    private YoutubeServiceWrapper Target;

    public YoutubeServiceWrapperTests()
    {
        _loggerMock = new Mock<ILogger<YoutubeServiceWrapper>>();
        _delayServiceMock = new Mock<IDelayService>();
        _proxyYoutubeVideoServiceMock = new Mock<IProxyYoutubeVideoService>();

        Target = new YoutubeServiceWrapper(_loggerMock.Object, _proxyYoutubeVideoServiceMock.Object,
            _delayServiceMock.Object);
    }

    [Fact]
    public async Task Empty_search_should_return_an_error_with_message()
    {
        var result = await Target.Search(It.IsAny<string>(), CancellationToken.None);

        result.Data.Should().NotBeNull();
        result.Data.Should().BeOfType<string[]>();

        result.IsSuccess.Should().BeFalse();

        result.Errors.Length.Should().Be(1);
        result.Errors.First().Message.Should().Be("Query can not be null!");

        _delayServiceMock.Verify(x => x.ExponentialDelaySecondsAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _proxyYoutubeVideoServiceMock.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Search_should_return_data_for_given_query()
    {
        var query = "i-am-search-text";

        _proxyYoutubeVideoServiceMock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                "https://www.youtube.com/watch?v=gVosTabd_9M",
                "https://www.youtube.com/watch?v=gVosTabd_MM"
            ]);

        var result = await Target.Search(query, CancellationToken.None);

        result.Data.Should().NotBeNull();
        result.Data.Should().BeOfType<string[]>();
        result.Data.Length.Should().Be(2);

        result.IsSuccess.Should().BeTrue();

        result.Errors.Length.Should().Be(0);

        _delayServiceMock.Verify(x => x.ExponentialDelaySecondsAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _proxyYoutubeVideoServiceMock.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Search_should_return_request_exceed_error_message_when_api_fail()
    {
        var query = "i-am-search-text";

        _proxyYoutubeVideoServiceMock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new GoogleApiException("youtubeservice") { HttpStatusCode = HttpStatusCode.Forbidden });

        var result = await Target.Search(query, CancellationToken.None);

        result.Data.Should().NotBeNull();
        result.Data.Should().BeOfType<string[]>();

        result.IsSuccess.Should().BeFalse();

        result.Errors.Length.Should().Be(1);
        result.Errors.First().Message.Should().Be("Youtube Service has an error. Request exceeded!");

        _delayServiceMock.Verify(x => x.ExponentialDelaySecondsAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _proxyYoutubeVideoServiceMock.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Search_should_return_request_unauthorized_error_message_when_api_fail()
    {
        var query = "i-am-search-text";

        _proxyYoutubeVideoServiceMock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new GoogleApiException("youtubeservice") { HttpStatusCode = HttpStatusCode.Unauthorized });

        var result = await Target.Search(query, CancellationToken.None);

        result.Data.Should().NotBeNull();
        result.Data.Should().BeOfType<string[]>();

        result.IsSuccess.Should().BeFalse();

        result.Errors.Length.Should().Be(1);
        result.Errors.First().Message.Should().Be("Youtube Service has an error. Client Unauthorized!");

        _delayServiceMock.Verify(x => x.ExponentialDelaySecondsAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _proxyYoutubeVideoServiceMock.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Search_should_return_request_unexpected_error_message_when_api_fail()
    {
        var query = "i-am-search-text";

        _proxyYoutubeVideoServiceMock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new GoogleApiException("youtubeservice") { HttpStatusCode = HttpStatusCode.Gone });

        var result = await Target.Search(query, CancellationToken.None);

        result.Data.Should().NotBeNull();
        result.Data.Should().BeOfType<string[]>();

        result.IsSuccess.Should().BeFalse();

        result.Errors.Length.Should().Be(1);
        result.Errors.First().Message.Should().Be("Youtube Service has an unexpected error!");

        _delayServiceMock.Verify(x => x.ExponentialDelaySecondsAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _proxyYoutubeVideoServiceMock.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Search_should_execute_request_1_time_and_retry_3_times_because_of_gateway_error()
    {
        var query = "i-am-search-text";

        _proxyYoutubeVideoServiceMock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new GoogleApiException("youtubeservice") { HttpStatusCode = HttpStatusCode.BadGateway });

        var result = await Target.Search(query, CancellationToken.None);

        result.Data.Should().NotBeNull();
        result.Data.Should().BeOfType<string[]>();

        result.IsSuccess.Should().BeFalse();

        result.Errors.Length.Should().Be(1);
        result.Errors.First().Message.Should().Be("Youtube Service has an unexpected error!");

        _delayServiceMock.Verify(x => x.ExponentialDelaySecondsAsync(It.IsAny<int>(), It.IsAny<int>()),
            Times.Exactly(3));

        // first access fails (1 time)
        // then try 3 times more because there is a policy +(3 times)
        _proxyYoutubeVideoServiceMock.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(4));
    }
}