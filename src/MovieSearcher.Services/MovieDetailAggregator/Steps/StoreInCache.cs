using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using MovieSearcher.Core.Models;
using MovieSearcher.Core.Patterns.CoR;
using MovieSearcher.Core.Query;
using VimeoDotNet.Models;

namespace MovieSearcher.Services.MovieDetailAggregator.Steps;

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

    // TODO: handle here!!! will written a new handler in between for gluing key - response for save Handler
    public override async Task<object?> Handle(object request, CancellationToken cancellationToken)
    {
        var data = JsonSerializer.Serialize(request, JsonOptions);

        await distributedCache.SetStringAsync("key", data, CacheOptions, cancellationToken);

        return base.Handle(request, cancellationToken);
    }
}