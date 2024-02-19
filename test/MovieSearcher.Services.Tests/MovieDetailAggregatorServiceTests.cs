
using MovieSearcher.Core;
using MovieSearcher.Core.Models;
using MovieSearcher.Core.Query;
using MovieSearcher.VimeoWrapper.Services;

namespace MovieSearcher.Services.Tests;

public class MovieDetailAggregatorServiceTests
{
    private readonly Mock<ILogger<MovieDetailAggregatorService>> _loggerMock;
    private readonly Mock<IVimeoService> _vimeoServiceMock;

    private readonly Mock<IVideoUrlServiceWrapper> youtubeServiceWrapperMock;

    private MovieDetailAggregatorService Target;

    public MovieDetailAggregatorServiceTests()
    {
        _loggerMock = new Mock<ILogger<MovieDetailAggregatorService>>();
        _vimeoServiceMock = new Mock<IVimeoService>();

        youtubeServiceWrapperMock = new Mock<IVideoUrlServiceWrapper>();

        var videoUrlServiceWrappersEnumerable = new[] { youtubeServiceWrapperMock.Object };

        Target = new MovieDetailAggregatorService(_loggerMock.Object, _vimeoServiceMock.Object,
            videoUrlServiceWrappersEnumerable);
    }

    [Fact]
    public async Task Empty_search_should_return_an_error_with_message()
    {
        var videoResponse = await Target.Search(It.IsAny<QueryParameters>(), CancellationToken.None);

        videoResponse.IsSuccess.Should().BeFalse();
        videoResponse.Data.Should().BeNull();
        videoResponse.Messages.Length.Should().Be(1);
        videoResponse.Messages.First().Message.Should().Be("Query can not be null!");

        _vimeoServiceMock.Verify(x => x.Search(It.IsAny<QueryParameters>()), Times.Never);
        youtubeServiceWrapperMock.Verify(x => x.Search(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_return_no_result_message_when_there_is_no_result_from_api()
    {
        var queryParameters = new QueryParameters { Query = "i-am-not-exist" };

        _vimeoServiceMock.Setup(x => x.Search(queryParameters)).ReturnsAsync(new ServiceResult<Paginated<Video>?>());

        var videoResponse = await Target.Search(queryParameters, CancellationToken.None);

        videoResponse.IsSuccess.Should().BeFalse();
        videoResponse.Data.Should().BeNull();
        videoResponse.Messages.Length.Should().Be(1);
        videoResponse.Messages.First().Message.Should().Be("There is no video result!");

        _vimeoServiceMock.Verify(x => x.Search(queryParameters), Times.Once);

        youtubeServiceWrapperMock.Verify(x => x.Search(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_return_result_but_needs_to_be_only_one_url_dueTo_no_response_from_youtube()
    {
        var queryParameters = new QueryParameters { Query = "i-am-not-exist" };

        var videos = CreateFakeVideos(100);

        _vimeoServiceMock.Setup(x => x.Search(queryParameters)).ReturnsAsync(new ServiceResult<Paginated<Video>?>(
            new Paginated<Video>()
                { Data = [..videos], Page = videos.Count, PerPage = videos.Count, Total = videos.Count }));

        var videoResponse = await Target.Search(queryParameters, CancellationToken.None);

        videoResponse.IsSuccess.Should().BeTrue();
        videoResponse.Data.Should().NotBeNull();
        videoResponse.Data.Should().NotBeEmpty();
        videoResponse.Messages.Length.Should().Be(0);

        videoResponse.Data.Select(x => x.Video).SequenceEqual(videos).Should().BeTrue();
        videoResponse.Data.All(videoData => videoData.VideoUrls.Count == 1).Should().BeTrue();

        _vimeoServiceMock.Verify(x => x.Search(queryParameters), Times.Once);
        youtubeServiceWrapperMock.Verify(x => x.Search(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(videos.Count));
    }

    [Fact]
    public async Task Should_return_result_but_needs_to_be_four_url_because_there_is_response_from_youtube()
    {
        var queryParameters = new QueryParameters { Query = "i-am-not-exist" };

        var videos = CreateFakeVideos(100);

        _vimeoServiceMock.Setup(x => x.Search(queryParameters)).ReturnsAsync(new ServiceResult<Paginated<Video>?>(
            new Paginated<Video>()
                { Data = [..videos], Page = videos.Count, PerPage = videos.Count, Total = videos.Count }));

        youtubeServiceWrapperMock.Setup(x => x.Search(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ServiceResult<string[]>([
                "https://www.youtube.com/watch?v=gVosTabd_9M",
                "https://www.youtube.com/watch?v=gVosTabd_10M",
                "https://www.youtube.com/watch?v=gVosTabd_11M"
            ]));

        var videoResponse = await Target.Search(queryParameters, CancellationToken.None);

        videoResponse.IsSuccess.Should().BeTrue();
        videoResponse.Data.Should().NotBeNull();
        videoResponse.Data.Should().NotBeEmpty();
        videoResponse.Messages.Length.Should().Be(0);

        videoResponse.Data.Select(x => x.Video).SequenceEqual(videos).Should().BeTrue();
        videoResponse.Data.All(videoData => videoData.VideoUrls.Count == 4).Should().BeTrue();

        _vimeoServiceMock.Verify(x => x.Search(queryParameters), Times.Once);
        youtubeServiceWrapperMock.Verify(x => x.Search(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(videos.Count));
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