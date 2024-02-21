using Microsoft.Extensions.Logging;
using MovieSearcher.Core;
using MovieSearcher.Core.Exceptions;
using MovieSearcher.Core.Models;
using MovieSearcher.Core.Patterns.CoR;
using VimeoDotNet.Models;

namespace MovieSearcher.Services.MovieDetailAggregator.V2.Steps;

public class AggregatorVideoYoutubeCall(
    ILogger<AggregatorVideoYoutubeCall> logger,
    IEnumerable<IVideoUrlServiceWrapper> videoUrlServices)
    : BaseHandler
{
    public override async Task<object?> Handle(CancellationToken cancellationToken, object request, params object[] parameters)
    {
        if(parameters == null || parameters.Length == 0)
            throw new MovieAggregatorException("There is no video result!");
        
        if (parameters[0] is not VideoResponse<List<VideoData<Video, List<string>>>, int, int, int> videoResponse)
            throw new MovieAggregatorException("There is no video result!");

        logger.LogInformation("Registered Video Url service count(s):{Count}", videoUrlServices.Count());

        foreach (var urlServiceWrapper in videoUrlServices)
        {
            logger.LogInformation("Search service starting from the '{Name}' service.", urlServiceWrapper.GetType().Name);
            
            foreach (var videoItem in videoResponse.Data)
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
            
            logger.LogInformation("Search service end for the '{Name}' service.", urlServiceWrapper.GetType().Name);
        }

        return await base.Handle(cancellationToken, request, videoResponse);
    }
}