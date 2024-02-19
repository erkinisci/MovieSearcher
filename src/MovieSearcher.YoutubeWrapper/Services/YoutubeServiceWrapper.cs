using MovieSearcher.Core;
using MovieSearcher.Core.Models;
using MovieSearcher.Core.Utility;
using Microsoft.Extensions.Logging;
using Polly;

namespace MovieSearcher.YoutubeWrapper.Services;

public partial class YoutubeServiceWrapper(
    ILogger<YoutubeServiceWrapper> logger,
    IProxyYoutubeVideoService proxyYoutubeVideoService,
    IDelayService delayService) : IVideoUrlServiceWrapper
{

    #region Retry

    private const int MaxRetries = 3;

    #endregion

    /// <summary>
    /// Search from Youtube Data API.
    /// </summary>
    /// <param name="query">Search term</param>
    /// <param name="cancellationToken">Cancel current request</param>
    /// <returns>string[]</returns>
    public async Task<ServiceResult<string[]>> Search(string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            logger.LogError($"Query can not be null");
            return new ServiceResult<string[]>(Array.Empty<string>(), "Query can not be null!");
        }

        logger.LogInformation($"Searching from Youtube. SearchTerm:{query}");

        var policyResult = await Policy
            .Handle<Exception>(ExceptionChecker)
            .RetryAsync(MaxRetries, (e, n) =>
            {
                logger.LogError(e, "Youtube Service 'Search' has an error. Retrying n={n}", n);

                return delayService.ExponentialDelaySecondsAsync(n);
            })
            .ExecuteAndCaptureAsync(async () =>
            {
                var response = await proxyYoutubeVideoService.ExecuteAsync(query, cancellationToken);

                return new ServiceResult<string[]>(response);
            });

        if (policyResult.Outcome != OutcomeType.Failure)
        {
            logger.LogInformation($"Searched from Youtube. SearchTerm:{query}");

            return policyResult.Result;
        }

        logger.LogError(policyResult.FinalException, "Youtube Service 'Search' has failed.");

        return ReturnClearServiceResult(policyResult.FinalException);
    }
}