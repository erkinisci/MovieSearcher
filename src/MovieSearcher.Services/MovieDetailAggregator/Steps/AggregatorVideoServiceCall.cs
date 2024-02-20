using Microsoft.Extensions.Logging;
using MovieSearcher.Core.Exceptions;
using MovieSearcher.Core.Models;
using MovieSearcher.Core.Patterns.CoR;
using MovieSearcher.Core.Query;
using MovieSearcher.VimeoWrapper.Services;
using VimeoDotNet.Models;

namespace MovieSearcher.Services.MovieDetailAggregator.Steps;

public class AggregatorVideoServiceCall(ILogger<AggregatorVideoServiceCall> logger, IVimeoService vimeoService)
    : BaseHandler
{
    public override async Task<object?> Handle(object request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Request object is checking {request}", request);

        var queryParameters = request as QueryParameters;

        logger.LogInformation("Vimeo service is calling for request: {queryParameters}", queryParameters);

        var serviceResult = await vimeoService.Search(queryParameters!);

        logger.LogInformation("Vimeo service is called for request: {queryParameters}", queryParameters);

        if (!serviceResult.IsSuccess)
            throw new MovieAggregatorException(serviceResult.Errors.FirstOrDefault().Message);

        if (serviceResult.Data == null || serviceResult.Data.Data.Count == 0)
            throw new MovieAggregatorException("There is no video result!");

        logger.LogInformation("Creating a collection from the Vimeo service result");

        var items = serviceResult.Data
            .Data
            .Select(video => new VideoData<Video, List<string>>(video, [..new[] { video.Link }]))
            .ToList();

        logger.LogInformation("Created a collection from the Vimeo service result");

        return await base.Handle(new VideoResponse<List<VideoData<Video, List<string>>>, int, int, int>(items,
            serviceResult.Data.Total,
            serviceResult.Data.PerPage, serviceResult.Data.Page), cancellationToken);
    }
}