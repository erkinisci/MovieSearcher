namespace MovieSearcher.YoutubeWrapper.Options;

public class YoutubeOptions
{
    public const string Youtube = "Youtube";
    public string ApiKey { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;

    public int MaxResults { get; set; }
}