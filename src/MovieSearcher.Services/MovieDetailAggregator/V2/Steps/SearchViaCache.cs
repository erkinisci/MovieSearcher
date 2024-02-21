using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using MovieSearcher.Core.Extensions;
using MovieSearcher.Core.Models;
using MovieSearcher.Core.Patterns.CoR;
using MovieSearcher.Core.Query;
using VimeoDotNet.Models;

namespace MovieSearcher.Services.MovieDetailAggregator.V2.Steps;

public class SearchViaCache(ILogger<SearchViaCache> logger, IDistributedCache distributedCache) : BaseHandler
{
    public override async Task<object?> Handle(object request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Request object is checking {request}", request);

        var queryParameters = request as QueryParameters;

        var key = queryParameters!.GenerateKey();

        logger.LogInformation("Redis cache key: {key}", key);

        var videoResponse = await TryGetCachedResponseAsync(key, cancellationToken);

        if (videoResponse == null)
            return await base.Handle(request, cancellationToken);

        return await Task.FromResult((request, new object[] { videoResponse }));
    }

    // The following code involves setting up a JsonSerializer and related settings.
    // In a production environment or a larger codebase, such configuration might be placed in a separate layer, ensuring better organization, re-usability, and maintainability.
    // However, for the sake of simplicity in this test assignment, these settings are kept here.
    // It's worth noting that this choice is made for simplicity, not out of laziness.
    private async Task<VideoResponse<List<VideoData<Video, List<string>>>, int, int, int>?> TryGetCachedResponseAsync(
        string key, CancellationToken cancellationToken)
    {
        logger.LogInformation("Key searching from redis. Key:{Key}", key);

        var cached = await distributedCache.GetStringAsync(key, cancellationToken);

        logger.LogInformation("Key searched from redis. Key:{Key}", key);

        if (!string.IsNullOrEmpty(cached))
        {
            logger.LogInformation("Key found in redis. Key:{Key}", key);

            return JsonSerializer
                .Deserialize<VideoResponse<List<VideoData<Video, List<string>>>, int, int, int>>(cached);
        }

        logger.LogInformation("No data in redis for given key. Key:{Key}", key);

        return null;
    }
}