namespace MovieSearcher.VimeoWrapper.Options;

public class VimeoOptions
{
    public const string Vimeo = "Vimeo";
    public string AccessToken { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public SearchOptions Search { get; init; } = new();
}

public class SearchOptions
{
    public string? Fields { get; set; } = null;
}