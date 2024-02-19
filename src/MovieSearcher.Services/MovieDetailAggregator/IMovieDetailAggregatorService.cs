using MovieSearcher.Core.Models;
using MovieSearcher.Core.Query;
using VimeoDotNet.Models;

namespace MovieSearcher.Services.MovieDetailAggregator;

public interface IMovieDetailAggregatorService
{
    Task<VideoResponse<List<VideoData<Video, List<string>>>, int, int, int>> Search(QueryParameters queryParameters,
        CancellationToken cancellationToken);
}