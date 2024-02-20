using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MovieSearcher.Core.Models;
using MovieSearcher.Core.Patterns.CoR;
using MovieSearcher.Core.Query;
using VimeoDotNet.Models;

namespace MovieSearcher.Services.MovieDetailAggregator;

public class CachedMovieDetailAggregator(
    ILogger<CachedMovieDetailAggregator> logger,
    [FromKeyedServices("AggregatorQueryParameterChecks")]
    IHandler queryParameterChecks,
    [FromKeyedServices("SearchViaCache")] IHandler searchFromCache,
    [FromKeyedServices("AggregatorVideoServiceCall")]
    IHandler aggregatorVideoServiceCall,
    [FromKeyedServices("AggregatorVideoYoutubeCall")]
    IHandler videoYoutubeCall,
    [FromKeyedServices("StoreInCache")] IHandler storeInCache
) : IMovieDetailAggregatorService
{
    #region Chains

    private IHandler SearchHandler
    {
        get
        {
            queryParameterChecks
                .SetNext(searchFromCache)
                .SetNext(aggregatorVideoServiceCall)
                .SetNext(videoYoutubeCall)
                .SetNext(storeInCache);

            return queryParameterChecks;
        }
    }

    #endregion

    public async Task<VideoResponse<List<VideoData<Video, List<string>>>, int, int, int>> Search(
        QueryParameters queryParameters, CancellationToken cancellationToken)
    {
        try
        {
            await SearchHandler.Handle(queryParameters, cancellationToken);

            return new VideoResponse<List<VideoData<Video, List<string>>>, int, int, int>();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}