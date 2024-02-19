namespace MovieSearcher.Core.Models;

public record struct VideoData<TVideoData, TVideoUrls>(
    TVideoData Video,
    TVideoUrls VideoUrls
);