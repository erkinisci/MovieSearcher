using Bogus;
using MovieSearcher.VimeoWrapper.Options;
using MovieSearcher.VimeoWrapper.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MovieSearcher.Core.Query;
using MovieSearcher.Core.Utility;
using VimeoDotNet;
using VimeoDotNet.Models;

namespace MovieSearcher.VimeoWrapper.Tests;

public class VimeoServiceTests
{
    private readonly Mock<IDelayService> _delayServiceMock;
    private readonly Mock<ILogger<VimeoService>> _loggerMock;
    private readonly IOptions<VimeoOptions> _options;
    private readonly Mock<IVimeoClient> _vimeoClientMock;

    /// <summary>
    /// Main Test Object
    /// </summary>
    private VimeoService Target;

    public VimeoServiceTests()
    {
        _loggerMock = new Mock<ILogger<VimeoService>>();
        _vimeoClientMock = new Mock<IVimeoClient>();
        _delayServiceMock = new Mock<IDelayService>();

        _options = new OptionsWrapper<VimeoOptions>(new VimeoOptions()
        {
            Url = "https://api.vimeo.com"
        });

        Target = new VimeoService(_loggerMock.Object, _vimeoClientMock.Object, _options, _delayServiceMock.Object);
    }

    [Fact]
    public async Task Null_queryParameter_should_return_an_error_with_message()
    {
        var result = await Target.Search(It.IsAny<QueryParameters>());

        result.IsSuccess.Should().BeFalse();
        result.Data.Should().BeNull();

        result.Errors.Length.Should().Be(1);
        result.Errors.First().Message.Should().Be("Query can not be null!");

        _vimeoClientMock.Verify(
            x => x.GetVideosAsync(null, It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string[]>()),
            Times.Never);
        _delayServiceMock.Verify(x => x.DelayWithExponentialBackoff(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Empty_search_should_return_an_error_with_message()
    {
        var result = await Target.Search(new QueryParameters { Query = "" });

        result.IsSuccess.Should().BeFalse();
        result.Data.Should().BeNull();

        result.Errors.Length.Should().Be(1);
        result.Errors.First().Message.Should().Be("Query can not be null!");

        _vimeoClientMock.Verify(
            x => x.GetVideosAsync(null, It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string[]>()),
            Times.Never);
        _delayServiceMock.Verify(x => x.DelayWithExponentialBackoff(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Search_should_return_video_for_given_query()
    {
        var count = 100;
        var videos = CreateFakeVideos(count);

        _vimeoClientMock.Setup(x =>
                x.GetVideosAsync(null, It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string[]>()))
            .ReturnsAsync(new Paginated<Video>()
                { Data = [..videos], Page = videos.Count, PerPage = videos.Count, Total = videos.Count });

        var queryParameters = new QueryParameters { Query = "search-for-me" };

        var result = await Target.Search(queryParameters);

        result.IsSuccess.Should().BeTrue();
        result.Errors.Length.Should().Be(0);

        result.Data.Should().NotBeNull();
        result.Data.Should().BeOfType<Paginated<Video>>();
        result.Data?.Data.SequenceEqual(videos).Should().BeTrue();

        _vimeoClientMock.Verify(
            x => x.GetVideosAsync(null, It.IsAny<int?>(), It.IsAny<int?>(), queryParameters.Query,
                It.IsAny<string[]>()),
            Times.Once);

        _delayServiceMock.Verify(x => x.DelayWithExponentialBackoff(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Search_should_execute_request_1_time_and_retry_3_times_because_of_gateway_error()
    {
        _vimeoClientMock.Setup(x =>
                x.GetVideosAsync(null, It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string[]>()))
            .ThrowsAsync(new Exception("Internal Server Error"));

        var queryParameters = new QueryParameters { Query = "search-for-me" };

        var result = await Target.Search(queryParameters);

        result.Data.Should().BeNull();

        result.IsSuccess.Should().BeFalse();

        result.Errors.Length.Should().Be(1);
        result.Errors.First().Message.Should().Be("An error occurred while trying to access the 'Vimeo Service'.");

        _delayServiceMock.Verify(x => x.DelayWithExponentialBackoff(It.IsAny<int>(), It.IsAny<int>()),
            Times.Exactly(3));
        _vimeoClientMock.Verify(
            x => x.GetVideosAsync(null, It.IsAny<int?>(), It.IsAny<int?>(), queryParameters.Query,
                It.IsAny<string[]>()),
            Times.Exactly(4));
    }

    [Fact]
    public async Task Search_should_return_request_error_message_when_api_fail()
    {
        _vimeoClientMock.Setup(x =>
                x.GetVideosAsync(null, It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string[]>()))
            .ThrowsAsync(new Exception("Not known error"));

        var queryParameters = new QueryParameters { Query = "search-for-me" };

        var result = await Target.Search(queryParameters);

        result.Data.Should().BeNull();

        result.IsSuccess.Should().BeFalse();

        result.Errors.Length.Should().Be(1);
        result.Errors.First().Message.Should().Be("An error occurred while trying to access the 'Vimeo Service'.");

        _delayServiceMock.Verify(x => x.DelayWithExponentialBackoff(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _vimeoClientMock.Verify(
            x => x.GetVideosAsync(null, It.IsAny<int?>(), It.IsAny<int?>(), queryParameters.Query,
                It.IsAny<string[]>()),
            Times.Once);
    }

    [Fact]
    public async Task Search_should_return_results_with_only_the_specified_fields_set_in_options()
    {
        _options.Value.Search.Fields = "name,uri,description,link";

        Target = new VimeoService(_loggerMock.Object, _vimeoClientMock.Object, _options, _delayServiceMock.Object);

        var queryParameters = new QueryParameters { Query = "search-for-me" };

        var result = await Target.Search(queryParameters);

        result.IsSuccess.Should().BeTrue();
        result.Errors.Length.Should().Be(0);
        result.Data.Should().BeNull();

        var expectedFields = _options.Value.Search.Fields.Split(',');

        _vimeoClientMock.Verify(
            x => x.GetVideosAsync(null, It.IsAny<int?>(), It.IsAny<int?>(), queryParameters.Query, expectedFields),
            Times.Once);

        _delayServiceMock.Verify(x => x.DelayWithExponentialBackoff(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Search_should_do_request_with_given_optionsQuery()
    {
        _options.Value.Search.Fields = "name,uri,description,link";

        Target = new VimeoService(_loggerMock.Object, _vimeoClientMock.Object, _options, _delayServiceMock.Object);

        var queryParameter = new Faker<QueryParameters>()
            .RuleFor(v => v.Query, f => f.Lorem.Sentence())
            .RuleFor(v => v.Page, f => f.Random.Number(1, 500))
            .RuleFor(v => v.PerPage, f => f.Random.Number(1, 100)).Generate();

        var result = await Target.Search(queryParameter);

        result.IsSuccess.Should().BeTrue();
        result.Errors.Length.Should().Be(0);
        result.Data.Should().BeNull();

        var expectedFields = _options.Value.Search.Fields.Split(',');

        _vimeoClientMock.Verify(
            x => x.GetVideosAsync(null, queryParameter.Page, queryParameter.PerPage, queryParameter.Query,
                expectedFields), Times.Once);

        _delayServiceMock.Verify(x => x.DelayWithExponentialBackoff(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }


    #region Create fake videos

    private List<Video> CreateFakeVideos(int count)
    {
        return new Faker<Video>()
            .RuleFor(v => v.Uri, f => f.Internet.Url())
            .RuleFor(v => v.User, f => new User { })
            .RuleFor(v => v.Name, f => f.Random.Word())
            .RuleFor(v => v.Description, f => f.Lorem.Sentence())
            .RuleFor(v => v.Link, f => f.Internet.Url())
            .RuleFor(v => v.Player_Embed_Url, f => f.Internet.Url())
            .RuleFor(v => v.ReviewLink, f => f.Internet.Url())
            .RuleFor(v => v.Status, f => f.PickRandom("uploading_error", "other_status"))
            .RuleFor(v => v.Type, f => f.PickRandom("video", "other_type"))
            .RuleFor(v => v.Duration, f => f.Random.Number(60, 600))
            .RuleFor(v => v.Width, f => f.Random.Number(320, 1920))
            .RuleFor(v => v.Height, f => f.Random.Number(240, 1080))
            .RuleFor(v => v.CreatedTime, f => f.Date.Past())
            .RuleFor(v => v.ModifiedTime, f => f.Date.Recent())
            .RuleFor(v => v.Privacy, f => new Privacy { })
            .RuleFor(v => v.Pictures, f => new Pictures { })
            .RuleFor(v => v.Download, f => [])
            .RuleFor(v => v.Tags, f => [])
            .RuleFor(v => v.Stats, f => new VideoStats { })
            .RuleFor(v => v.Metadata, f => new VideoMetadata { })
            .RuleFor(v => v.Embed, f => new Embed { })
            .RuleFor(v => v.Spatial, f => new Spatial { })
            .Generate(count);
    }

    #endregion
}