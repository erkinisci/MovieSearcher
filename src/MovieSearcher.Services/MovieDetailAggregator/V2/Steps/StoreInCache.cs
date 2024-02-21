using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using MovieSearcher.Core.Extensions;
using MovieSearcher.Core.Patterns.CoR;
using MovieSearcher.Core.Query;

namespace MovieSearcher.Services.MovieDetailAggregator.V2.Steps;

public class StoreInCache(ILogger<StoreInCache> logger, IDistributedCache distributedCache) : BaseHandler
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

    public override async Task<object?> Handle(CancellationToken cancellationToken, object request,
        params object[] parameters)
    {
        var key = (request as QueryParameters)!.GenerateKey();
        
        logger.LogInformation("Data storing into cache with key: {key}", key);
        
        var data = JsonSerializer.Serialize(request, JsonOptions);

        await distributedCache.SetStringAsync(key, data, CacheOptions, cancellationToken);

        logger.LogInformation("Data stored in cache with key: {key}", key);

        return await base.Handle(cancellationToken, request, parameters);
    }
}