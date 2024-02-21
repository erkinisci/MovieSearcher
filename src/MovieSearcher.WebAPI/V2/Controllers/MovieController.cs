using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using MovieSearcher.Core.Models;
using MovieSearcher.Core.Query;
using MovieSearcher.Services;
using MovieSearcher.Services.MovieDetailAggregator.V2;
using VimeoDotNet.Models;

namespace MovieSearcher.WebAPI.V2.Controllers;

[ApiController]
[ApiVersion(2)]
[Route("api/v{version:apiVersion}/movie")]
public class MovieController([FromKeyedServices(nameof(MovieDetailAggregator))] IMovieDetailAggregatorService detailAggregatorService) : ControllerBase
{
    /// <summary>
    /// Searches for videos based on the provided query parameters from Vimeo,
    /// retrieves relevant URLs from YouTube, and aggregates the results for the user.
    /// </summary>
    /// <remarks>
    /// Example Usage:
    /// GET /api/movie/search?query=truman show&perPage=1&page=1
    /// GET /api/movie/search?query=truman show
    /// GET /api/movie/search?query=truman show&page=1
    /// </remarks>
    /// <param name="queryParameters">The query parameters for the video search.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The aggregated search results.
    /// Each result includes video details and associated video URLs.
    /// </returns>
    /// <example>
    /// {
    ///     "data": [
    ///         {
    ///             "video": {
    ///                 "id": 156673325,
    ///                 "uri": "/videos/156673325"
    ///                 ...
    ///             },
    ///             "videoUrls": [
    ///                 "https://vimeo.com/156673325",
    ///                 "https://www.youtube.com/watch?v=gVosTabd_9M",
    ///                 "https://www.youtube.com/watch?v=vS1UY8KcY60",
    ///                 "https://www.youtube.com/watch?v=1NNcTnADtek",
    ///                 "https://www.youtube.com/watch?v=MAX9qWiJxvU",
    ///                 "https://www.youtube.com/watch?v=6U4-KZSoe6g"
    ///             ]
    ///         },
    ///         {
    ///             "video": {
    ///                 "id": 156673345,
    ///                 "uri": "/videos/156673345"
    ///                 ...
    ///             },
    ///             "videoUrls": [
    ///                 "https://vimeo.com/156673345",
    ///                 "https://www.youtube.com/watch?v=gVosTabd_1M",
    ///                 "https://www.youtube.com/watch?v=vS1UY8KcY30",
    ///                 "https://www.youtube.com/watch?v=1NNcTnADt3k",
    ///                 "https://www.youtube.com/watch?v=MAX9qWiJx4U",
    ///                 "https://www.youtube.com/watch?v=6U4-KZSoe5g"
    ///             ]
    ///         }
    ///     ]
    /// }
    /// </example>
    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK,
        Type = typeof(VideoResponse<List<VideoData<Video, List<string>>>, int, int, int>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [OutputCache]
    public async Task<IActionResult> Search([ModelBinder] QueryParameters queryParameters,
        CancellationToken cancellationToken)
    {
        return Ok(await detailAggregatorService.Search(queryParameters, cancellationToken));
    }
}