using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using MovieSearcher.Core;
using MovieSearcher.Core.Patterns.CoR;
using MovieSearcher.Core.Utility;
using MovieSearcher.Services;
using MovieSearcher.Services.MovieDetailAggregator;
using MovieSearcher.Services.MovieDetailAggregator.V1;
using MovieSearcher.Services.MovieDetailAggregator.V2;
using MovieSearcher.Services.MovieDetailAggregator.V2.Steps;
using MovieSearcher.VimeoWrapper.Options;
using MovieSearcher.VimeoWrapper.Services;
using MovieSearcher.YoutubeWrapper.Options;
using MovieSearcher.YoutubeWrapper.Services;
using VimeoDotNet;

#if DEBUG
#else
using VimeoDotNet.Authorization;
#endif

namespace MovieSearcher.WebAPI.Extensions;

public static class Extensions
{
    public static void AddApplicationOptions(this WebApplicationBuilder builder)
    {
        // Options for API
        builder.Services.Configure<YoutubeOptions>(builder.Configuration.GetSection(YoutubeOptions.Youtube));
        builder.Services.Configure<VimeoOptions>(builder.Configuration.GetSection(VimeoOptions.Vimeo));
    }

    public static void AddApplicationServices(this WebApplicationBuilder builder)
    {
#if DEBUG
// Usage - Public Token
        builder.Services.AddSingleton<IVimeoClient>(provider =>
        {
            var vimeoOptions = provider.GetRequiredService<IOptions<VimeoOptions>>().Value;
            return new VimeoClient(vimeoOptions.AccessToken);
        });

#else
// The nuget package I'm using lacks the capability to establish a connection to Vimeo with ClientId & ClientSecret directly.
// It seems to only support OAuth2, or I couldn't find a straightforward way to achieve it :/

// Although using Task.Run() here is not the ideal solution, it's a workaround for now.
// The synchronous call within Task.Run() is used to simplify the integration with the existing DI infrastructure.

// In an ideal scenario, the Vimeo client instantiation would be asynchronous, but due to the constraints of the current library
// and the synchronous nature of the service registration, Task.Run() is employed to perform the operation asynchronously
// without blocking the application's main thread.

builder.Services.AddSingleton<IAuthorizationClient>(provider =>
{
    var vimeoOptions = provider.GetRequiredService<IOptions<VimeoOptions>>().Value;
    var authorizationClient = new AuthorizationClient(vimeoOptions.ClientId, vimeoOptions.ClientSecret);
    return authorizationClient;
});

builder.Services.AddSingleton<IVimeoClient>(provider =>
{
    var authorizationClient = provider.GetRequiredService<IAuthorizationClient>();

    // Task.Run is used here to perform the Vimeo client instantiation asynchronously within the synchronous service registration.
    // This allows for a smoother integration with the existing DI infrastructure.

    return Task.Run(async () =>
    {
        var accessToken = await authorizationClient.GetUnauthenticatedTokenAsync();
        return new VimeoClient(accessToken.AccessToken);
    }).GetAwaiter().GetResult();
});

#endif

// Vimeo nuget package registration into DI
        builder.Services.AddScoped<IVimeoService, VimeoService>();

// Generic Video URL Service registration.
// The implementation of this interface, which is registered into DI, will be iterated in the registration order to fetch video URLs from the given provider.

// Currently, the implementation for YouTube is provided (YoutubeServiceWrapper).
// To add support for additional providers, derive your object from this interface and register it as follows:
        builder.Services.AddScoped<IVideoUrlServiceWrapper, YoutubeServiceWrapper>();
// Add more registrations for other providers if needed, for example:
// builder.Services.AddSingleton<IVideoUrlServiceWrapper, DailymotionServiceWrapper>();
// builder.Services.AddSingleton<IVideoUrlServiceWrapper, AnotherProviderServiceWrapper>();

// proxy for youtube instance. since, creating and each object per request would very expensive and not handy to test (due to Google Object), it is in proxy object that can be mocked 
        builder.Services.AddScoped<IProxyYoutubeVideoService, ProxyYoutubeVideoService>();
        builder.Services.AddSingleton<IDelayService, DelayService>();
    }

    public static void AddApplicationServicesV1(this WebApplicationBuilder builder)
    {
        // Movie aggregator service registration with the implementation of the cache decorator pattern.
        builder.Services.AddScoped<MovieDetailAggregatorService>();

// The CachedMovieDetailAggregatorService decorates the original MovieDetailAggregator with caching functionality using IDistributedCache.
        builder.Services.AddScoped<IMovieDetailAggregatorService>(provider =>
            new CachedMovieDetailAggregatorService(
                provider.GetRequiredService<ILogger<CachedMovieDetailAggregatorService>>(),
                provider.GetRequiredService<MovieDetailAggregatorService>(),
                provider.GetRequiredService<IDistributedCache>()
            ));
    }

    public static void AddApplicationServicesV2(this WebApplicationBuilder builder)
    {
        builder.Services.AddKeyedScoped<IMovieDetailAggregatorService, MovieDetailAggregator>(
            nameof(MovieDetailAggregator));
        builder.Services.AddKeyedScoped<IHandler, AggregatorQueryParameterChecks>(
            nameof(AggregatorQueryParameterChecks));
        builder.Services.AddKeyedScoped<IHandler, AggregatorVideoServiceCall>(nameof(AggregatorVideoServiceCall));
        builder.Services.AddKeyedScoped<IHandler, AggregatorVideoYoutubeCall>(nameof(AggregatorVideoYoutubeCall));

        builder.Services.AddKeyedScoped<IHandler, SearchViaCache>(nameof(SearchViaCache));
        builder.Services.AddKeyedScoped<IHandler, StoreInCache>(nameof(StoreInCache));
    }
}