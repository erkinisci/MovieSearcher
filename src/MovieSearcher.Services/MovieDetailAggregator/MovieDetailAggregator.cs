using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MovieSearcher.Core.Exceptions;
using MovieSearcher.Core.Models;
using MovieSearcher.Core.Patterns.CoR;
using MovieSearcher.Core.Query;
using VimeoDotNet.Models;

namespace MovieSearcher.Services.MovieDetailAggregator;

public class MovieDetailAggregator(
    ILogger<MovieDetailAggregator> logger,
    [FromKeyedServices("AggregatorQueryParameterChecks")]
    IHandler queryParameterChecks,
    [FromKeyedServices("AggregatorVideoServiceCall")]
    IHandler videoServiceCall,
    [FromKeyedServices("AggregatorVideoYoutubeCall")]
    IHandler videoYoutubeCall) : IMovieDetailAggregatorService
{
    #region Chains
    
    private IHandler SearchHandler
    {
        get
        {
            queryParameterChecks
                .SetNext(videoServiceCall)
                .SetNext(videoYoutubeCall);
    
            return queryParameterChecks;
        }
    }
    
    #endregion

    public async Task<VideoResponse<List<VideoData<Video, List<string>>>, int, int, int>> Search(
        QueryParameters queryParameters, CancellationToken cancellationToken)
    {
        try
        {
            var result = (ValueTuple<object, object[]>)(await SearchHandler.Handle(queryParameters, cancellationToken) ?? throw new InvalidOperationException("Unexpected service result!"));
            
            return (VideoResponse<List<VideoData<Video, List<string>>>, int, int, int>)(result.Item2[0] ?? throw new InvalidOperationException("Unexpected service result!"));
        }
        catch (MovieAggregatorException movieAggregatorException)
        {
            logger.LogError(movieAggregatorException.Message, movieAggregatorException);

            return new VideoResponse<List<VideoData<Video, List<string>>>, int, int, int>()
            {
                Messages = [new MovieSearcherError(movieAggregatorException.Message)]
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);

            return new VideoResponse<List<VideoData<Video, List<string>>>, int, int, int>()
            {
                Messages = [new MovieSearcherError("Error")]
            };
        }
    }
}