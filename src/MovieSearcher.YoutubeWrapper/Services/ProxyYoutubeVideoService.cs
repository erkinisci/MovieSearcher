using Google.Apis.Services;
using Google.Apis.Util;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Options;
using MovieSearcher.YoutubeWrapper.Options;

namespace MovieSearcher.YoutubeWrapper.Services;

public sealed class ProxyYoutubeVideoService : IProxyYoutubeVideoService
{
    private readonly YoutubeOptions _youtubeOptions;
    private readonly YouTubeService _youTubeService;

    public ProxyYoutubeVideoService(IOptions<YoutubeOptions> youtubeOptions)
    {
        _youtubeOptions = youtubeOptions.Value;

        _youTubeService = new YouTubeService(new BaseClientService.Initializer
        {
            ApiKey = _youtubeOptions.ApiKey,
            ApplicationName = _youtubeOptions.ApplicationName
        });
    }

    /// <summary>
    /// Sends 'Query' to Youtube Data for search
    /// </summary>
    /// <param name="query">Search Criteria</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Returns maximum result from youtube (check maximum results from app.config - default '5')</returns>
    public async Task<string[]?> ExecuteAsync(string query, CancellationToken cancellationToken)
    {
        var listRequest = _youTubeService.Search.List("snippet");
        listRequest.Type = new Repeatable<string>(Parts);
        listRequest.Q = query;

        listRequest.Order = SearchResource.ListRequest.OrderEnum.ViewCount;
        listRequest.MaxResults = _youtubeOptions.MaxResults;

        var searchResponse = await listRequest.ExecuteAsync(cancellationToken);

        return GetVideoUrls(searchResponse.Items);
    }

    /// <summary>
    /// Creates a VideoUrl for Youtube
    /// </summary>
    /// <param name="searchResults">Aggregated youtube video url(s)</param>
    /// <returns></returns>
    private static string[]? GetVideoUrls(IEnumerable<SearchResult> searchResults)
    {
        return searchResults
            .Where(searchResult => searchResult.Id.Kind == YouTubeVideoKind)
            .Select(searchResult => $"{YoutubeVideoUrlTemplate}{searchResult.Id.VideoId}")
            .ToArray();
    }

    #region Youtube settings

    private const string YouTubeVideoKind = "youtube#video";
    private const string YoutubeVideoUrlTemplate = "https://www.youtube.com/watch?v=";
    private static readonly string[] Parts = ["videos"];

    #endregion
}