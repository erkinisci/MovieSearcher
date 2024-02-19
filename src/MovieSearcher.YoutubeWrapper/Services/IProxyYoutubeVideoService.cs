namespace MovieSearcher.YoutubeWrapper.Services;

public interface IProxyYoutubeVideoService
{
    Task<string[]?> ExecuteAsync(string query, CancellationToken cancellationToken);
}