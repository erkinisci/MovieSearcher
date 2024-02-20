using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using MovieSearcher.Core.Extensions;
using MovieSearcher.Core.Models;
using MovieSearcher.Core.Query;
using VimeoDotNet.Models;

namespace MovieSearcher.Services.OLD_Aggregator;

public class CachedMovieDetailAggregatorService : IMovieDetailAggregatorService
{
    /// <summary>
    /// Cache Options - this can go into appSettings.json
    /// </summary>
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        SlidingExpiration = TimeSpan.FromHours(2)
    };

    /// <summary>
    /// Json Serializer Options
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<CachedMovieDetailAggregatorService> _logger;
    private readonly IMovieDetailAggregatorService _movieDetailAggregatorService;

    // ReSharper disable once ConvertToPrimaryConstructor
    public CachedMovieDetailAggregatorService(ILogger<CachedMovieDetailAggregatorService> logger,
        IMovieDetailAggregatorService movieDetailAggregatorService,
        IDistributedCache distributedCache)
    {
        _logger = logger;
        _movieDetailAggregatorService = movieDetailAggregatorService;
        _distributedCache = distributedCache;
    }

    /// <summary>
    /// Attempt to retrieve data from the Cache service first; if not found, fetch the data from the actual service via API and cache the result.
    /// If a Redis error occurs, an API call can be made as a fallback.
    /// This implementation returns an exception immediately, but a secondary call can be implemented as needed.
    /// Note: The task requirements do not specify the fallback behavior.
    /// </summary>
    /// <param name="queryParameters">Search term</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    public async Task<VideoResponse<List<VideoData<Video, List<string>>>, int, int, int>> Search(
        QueryParameters queryParameters, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(queryParameters?.Query))
            {
                _logger.LogError("Query can not be null");

                return new VideoResponse<List<VideoData<Video, List<string>>>, int, int, int>
                    { Messages = [new MovieSearcherError("Query can not be null!")] };
            }

            var key = queryParameters.GenerateKey();

            _logger.LogInformation($"Redis cache key generated. Key:{key}");

            var cachedResponse = await TryGetCachedResponseAsync(key, cancellationToken);

            if (cachedResponse != null)
                return cachedResponse.Value;

            return await FetchAndCacheResponseAsync(queryParameters, key, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occured on Redis connection!");

            return new VideoResponse<List<VideoData<Video, List<string>>>, int, int, int>
            {
                Messages = [new MovieSearcherError("An error occured on Redis connection!")]
            };
        }
    }

    #region private

    // The following code involves setting up a JsonSerializer and related settings.
    // In a production environment or a larger codebase, such configuration might be placed in a separate layer, ensuring better organization, re-usability, and maintainability.
    // However, for the sake of simplicity in this test assignment, these settings are kept here.
    // It's worth noting that this choice is made for simplicity, not out of laziness.

    private async Task<VideoResponse<List<VideoData<Video, List<string>>>, int, int, int>?> TryGetCachedResponseAsync(
        string key, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Key searching from redis. Key:{Key}", key);

        var cached = await _distributedCache.GetStringAsync(key, cancellationToken);

        _logger.LogInformation("Key searched from redis. Key:{Key}", key);

        if (!string.IsNullOrEmpty(cached))
        {
            _logger.LogInformation("Key found in redis. Key:{Key}", key);

            return JsonSerializer
                .Deserialize<VideoResponse<List<VideoData<Video, List<string>>>, int, int, int>>(cached);
        }

        _logger.LogInformation("No data in redis for given key. Key:{Key}", key);

        return null;
    }

    private async Task<VideoResponse<List<VideoData<Video, List<string>>>, int, int, int>> FetchAndCacheResponseAsync(
        QueryParameters queryParameters, string key, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Due to no cached data in cache. Creating an API query with given parameters");

        var response = await _movieDetailAggregatorService.Search(queryParameters, cancellationToken);

        await CacheResponseAsync(key, response, cancellationToken);

        return response;
    }

    private async Task CacheResponseAsync(
        string key,
        VideoResponse<List<VideoData<Video, List<string>>>, int, int, int> response,
        CancellationToken cancellationToken)
    {
        var data = JsonSerializer.Serialize(response, JsonOptions);

        await _distributedCache.SetStringAsync(key, data, CacheOptions, cancellationToken);
    }

    #endregion
}