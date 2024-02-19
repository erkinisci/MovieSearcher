using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MovieSearcher.WebAPI.Tests.Integration.Fixture;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using MovieSearcher.Core.Extensions;
using MovieSearcher.Core.Models;
using MovieSearcher.Core.Query;
using VimeoDotNet.Models;

namespace MovieSearcher.WebAPI.Tests.Integration.MovieController;

public class SearchTests : IClassFixture<MovieSearcherApiFactory>
{
    private readonly HttpClient _client;
    private readonly MovieSearcherApiFactory _factory;

    public SearchTests(MovieSearcherApiFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Search_should_return_from_api_and_save_to_redis_cache()
    {
        var searchParameters = new QueryParameters()
        {
            Query = "truman show",
            PerPage = 1
        };

        var response = await _client.GetAsync($"{HttpHelper.Search}{searchParameters.CreateQueryString()}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var videoResponse = await response.Content
            .ReadFromJsonAsync<VideoResponse<List<VideoData<Video, List<string>>>, int, int, int>>();
        videoResponse.Should().NotBeNull();
        videoResponse.Data.Count.Should().Be(1);
        videoResponse.IsSuccess.Should().BeTrue();
        videoResponse.PerPage.Should().Be(searchParameters.PerPage);

        // verify from redis container
        using var scope = _factory.Services.CreateScope();
        var scopeServiceProvider = scope.ServiceProvider;
        var distributedCache = scopeServiceProvider.GetRequiredService<IDistributedCache>();

        var stringAsync = await distributedCache.GetStringAsync(searchParameters.GenerateKey());
        stringAsync.Should().NotBeNull();
    }

    [Fact]
    public async Task Search_should_return_no_result_from_api_and_save_to_redis_cache()
    {
        var searchParameters = new QueryParameters()
        {
            Query = "ascascascascascasc",
            PerPage = 1
        };

        var response = await _client.GetAsync($"{HttpHelper.Search}{searchParameters.CreateQueryString()}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var videoResponse = await response.Content
            .ReadFromJsonAsync<VideoResponse<List<VideoData<Video, List<string>>>, int, int, int>>();
        videoResponse.Should().NotBeNull();
        videoResponse.Data.Should().BeNull();
        videoResponse.IsSuccess.Should().BeFalse();
        videoResponse.PerPage.Should().Be(0);
        videoResponse.Messages.First().Message.Should().Be("There is no video result!");

        // verify from cached value
        using var scope = _factory.Services.CreateScope();
        var scopeServiceProvider = scope.ServiceProvider;
        var distributedCache = scopeServiceProvider.GetRequiredService<IDistributedCache>();

        var cachedString = await distributedCache.GetStringAsync(searchParameters.GenerateKey());
        var cached =
            JsonSerializer
                .Deserialize<VideoResponse<List<VideoData<Video, List<string>>>, int, int, int>>(cachedString);

        const string expectedJson =
            """{ "TotalCount": 0, "PerPage": 0, "Page": 0, "Messages": [{ "Message": "There is no video result!" }], "IsSuccess": false }""";
        var expected =
            JsonSerializer
                .Deserialize<VideoResponse<List<VideoData<Video, List<string>>>, int, int, int>>(expectedJson);

        cached.Should().BeEquivalentTo(expected);
    }
}