using Microsoft.Extensions.Logging;
using MovieSearcher.Core;
using MovieSearcher.Core.Models;
using MovieSearcher.Core.Query;
using MovieSearcher.VimeoWrapper.Services;
using VimeoDotNet.Models;

namespace MovieSearcher.Services.OLD_Aggregator;

public class MovieDetailAggregatorService(
    ILogger<MovieDetailAggregatorService> logger,
    IVimeoService vimeoService,
    IEnumerable<IVideoUrlServiceWrapper> videoUrlServices) : IMovieDetailAggregatorService
{
    public async Task<VideoResponse<List<VideoData<Video, List<string>>>, int, int, int>> Search(
        QueryParameters queryParameters, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(queryParameters?.Query))
        {
            logger.LogError($"Query can not be null");
            return new VideoResponse<List<VideoData<Video, List<string>>>, int, int, int>
                { Messages = [new MovieSearcherError("Query can not be null!")] };
        }

        var videoServiceResult = await vimeoService.Search(queryParameters);

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

        logger.LogInformation("Registered Services count for Video Url:{Count}", videoUrlServices.Count());

        foreach (var urlServiceWrapper in videoUrlServices)
        {
            logger.LogInformation(
                "Search service starting. Search Video Url from the video service. Service name:{Name}",
                urlServiceWrapper.GetType().Name);

            #region Create list of task if you have paid customer

            // NOTE: Creating an asynchronous call for a list of requests is not done due to YouTube limitations for unpaid requests.
            // To avoid unnecessary API calls, iterate through each call individually and wait for the response.

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

            logger.LogInformation(
                "Search service completed. Search Video Urls from the video service. Service name:{Name}",
                urlServiceWrapper.GetType().Name);
        }

        return videoResponse;
    }
}