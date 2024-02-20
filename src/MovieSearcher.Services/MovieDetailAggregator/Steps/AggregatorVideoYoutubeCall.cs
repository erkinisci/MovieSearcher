﻿using Microsoft.Extensions.Logging;
using MovieSearcher.Core;
using MovieSearcher.Core.Exceptions;
using MovieSearcher.Core.Models;
using MovieSearcher.Core.Patterns.CoR;
using VimeoDotNet.Models;

namespace MovieSearcher.Services.MovieDetailAggregator.Steps;

public class AggregatorVideoYoutubeCall(
    ILogger<AggregatorVideoYoutubeCall> logger,
    IEnumerable<IVideoUrlServiceWrapper> videoUrlServices)
    : BaseHandler
{
    public override async Task<object?> Handle(object request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Request object type '{request.GetType()}'", request.GetType());
        
        if (request is not VideoResponse<List<VideoData<Video, List<string>>>, int, int, int> videoResponse)
            throw new MovieAggregatorException("There is no video result!");

        logger.LogInformation("Registered Video Url service count(s):{Count}", videoUrlServices.Count());

        foreach (var urlServiceWrapper in videoUrlServices)
        {
            //logger.LogInformation("Search service starting from the '{Name}' service.", urlServiceWrapper.GetType().Name);
            
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

        return base.Handle(videoResponse, cancellationToken);
    }
}