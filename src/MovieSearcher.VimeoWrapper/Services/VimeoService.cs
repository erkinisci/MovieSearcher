using MovieSearcher.Core.Models;
using MovieSearcher.Core.Query;
using MovieSearcher.Core.Utility;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MovieSearcher.VimeoWrapper.Options;
using Polly;
using VimeoDotNet;
using VimeoDotNet.Models;

namespace MovieSearcher.VimeoWrapper.Services;

public partial class VimeoService : IVimeoService
{
    #region Retry

    private const int MaxRetries = 3;

    #endregion

    private readonly IDelayService _delayService;
    private readonly ILogger<VimeoService> _logger;
    private readonly VimeoOptions _options;
    private readonly IVimeoClient _vimeoClient;

    // ReSharper disable once ConvertToPrimaryConstructor
    public VimeoService(ILogger<VimeoService> logger, IVimeoClient vimeoClient,
        IOptions<VimeoOptions> options, IDelayService delayService)
    {
        _logger = logger;
        _vimeoClient = vimeoClient;
        _options = options.Value;
        _delayService = delayService;
    }

    /// <summary>
    /// Search from Vimeo API
    /// It allows user to search by Query Parameters
    /// </summary>
    /// <param name="queryParameters">Refer -> QueryParameters</param>
    /// <returns>ServiceResult<Paginated<Video>?></returns>
    public async Task<ServiceResult<Paginated<Video>?>> Search(QueryParameters queryParameters)
    {
        if (string.IsNullOrWhiteSpace(queryParameters?.Query))
        {
            _logger.LogError("Query can not be null");
            return new ServiceResult<Paginated<Video>?>("Query can not be null!");
        }

        var policyResult = await Policy
            .Handle<Exception>(ExceptionChecker)
            .RetryAsync(MaxRetries, (e, n) =>
            {
                _logger.LogError(e, "Vimeo Service 'Search' has an error. Retrying n={n}", n);

                return _delayService.ExponentialDelaySecondsAsync(n);
            })
            .ExecuteAndCaptureAsync(async () =>
            {
                var fields = _options.Search.Fields is { Length: > 0 } ? _options.Search.Fields.Split(',') : null;

                _logger.LogInformation("Starting vimeo api call for parameters: {queryParameters}", queryParameters);

                Paginated<Video>? data = await _vimeoClient.GetVideosAsync(null,
                    queryParameters.Page,
                    queryParameters.PerPage,
                    queryParameters.Query,
                    fields);

                _logger.LogInformation("Finished vimeo api call for parameters: {queryParameters}", queryParameters);

                return new ServiceResult<Paginated<Video>?>(data);
            });

        if (policyResult.Outcome != OutcomeType.Failure)
            return policyResult.Result;

        _logger.LogError(policyResult.FinalException, "Vimeo Service 'Search' has failed.");

        return new ServiceResult<Paginated<Video>?>("An error occurred while trying to access the 'Vimeo Service'.");
    }
}