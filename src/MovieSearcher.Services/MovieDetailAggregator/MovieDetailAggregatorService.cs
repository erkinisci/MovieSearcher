using MovieSearcher.Core;
using MovieSearcher.Core.Models;
using MovieSearcher.Core.Query;
using MovieSearcher.VimeoWrapper.Services;
using Microsoft.Extensions.Logging;
using VimeoDotNet.Models;

namespace MovieSearcher.Services.MovieDetailAggregator;

public class MovieDetailAggregatorService : IMovieDetailAggregatorService
{
    private readonly ILogger<MovieDetailAggregatorService> _logger;
    private readonly IEnumerable<IVideoUrlServiceWrapper> _videoUrlServices;
    private readonly IVimeoService _vimeoService;

    public MovieDetailAggregatorService(ILogger<MovieDetailAggregatorService> logger, IVimeoService vimeoService,
        IEnumerable<IVideoUrlServiceWrapper> videoUrlServices)
    {
        _logger = logger;
        _vimeoService = vimeoService;
        _videoUrlServices = videoUrlServices;
    }

    public async Task<VideoResponse<List<VideoData<Video, List<string>>>, int, int, int>> Search(
        QueryParameters queryParameters, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(queryParameters?.Query))
        {
            _logger.LogError($"Query can not be null");
            return new VideoResponse<List<VideoData<Video, List<string>>>, int, int, int>
                { Messages = [new MovieSearcherError("Query can not be null!")] };
        }

        var videoServiceResult = await _vimeoService.Search(queryParameters);

        if (!videoServiceResult.IsSuccess)
            return new VideoResponse<List<VideoData<Video, List<string>>>, int, int, int>
                { Messages = videoServiceResult.Errors };

        if (videoServiceResult.Data == null || videoServiceResult.Data.Data.Count == 0)
            return new VideoResponse<List<VideoData<Video, List<string>>>, int, int, int>
                { Messages = [new MovieSearcherError("There is no video result!")] };

        var items = videoServiceResult.Data
            .Data
            .Select(video => new VideoData<Video, List<string>>(video, [..new[] { video.Link }]))
            .ToList();

        var videoResponse = new VideoResponse<List<VideoData<Video, List<string>>>, int, int, int>(items,
            videoServiceResult.Data.Total,
            videoServiceResult.Data.PerPage, videoServiceResult.Data.Page);

        _logger.LogInformation("Registered Services count for Video Url:{Count}", _videoUrlServices.Count());

        foreach (var urlServiceWrapper in _videoUrlServices)
        {
            _logger.LogInformation("Search service starting. Search Video Url from the video service. Service name:{Name}", urlServiceWrapper.GetType().Name);

            // NOTE: Creating an asynchronous call for a list of requests is not done due to YouTube limitations for unpaid requests.
            // To avoid unnecessary API calls, iterate through each call individually and wait for the response.

            #region create task list if you have paid customer

            // var tasks = items.Select(async videoItem =>
            // {
            //     var searchVideos = await urlServiceWrapper.Search(videoItem.Video.Name, cancellationToken);
            //
            //     if (!searchVideos.IsSuccess)
            //     {
            //         videoResponse.Messages = searchVideos.Errors;
            //         return;
            //     }
            //
            //     if (searchVideos.Data != null)
            //         videoItem.VideoUrls.AddRange(searchVideos.Data);
            // });
            //
            // await Task.WhenAll(tasks);

            #endregion

            foreach (var videoItem in items)
            {
                var searchVideos = await urlServiceWrapper.Search(videoItem.Video.Name, cancellationToken);

                if (!searchVideos.IsSuccess)
                {
                    videoResponse.Messages = searchVideos.Errors;
                    break;
                }

                if (searchVideos.Data != null)
                    videoItem.VideoUrls.AddRange(searchVideos.Data);
            }

            _logger.LogInformation("Search service completed. Search Video Urls from the video service. Service name:{Name}", urlServiceWrapper.GetType().Name);
        }

        return videoResponse;
    }
}