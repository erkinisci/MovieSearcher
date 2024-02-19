namespace MovieSearcher.Core.Models;

public record struct VideoResponse<TVideoModel, TTotalPage, TPerPage, TPage>(
    TVideoModel Data,
    TTotalPage TotalCount,
    TPerPage PerPage,
    TPage Page
)
{
    private MovieSearcherError[] _messages = Array.Empty<MovieSearcherError>();

    public MovieSearcherError[] Messages
    {
        readonly get => _messages;
        set
        {
            _messages = value;

            if (_messages is { Length : > 0 })
                IsSuccess = false;
        }
    }

    public bool IsSuccess { get; set; } = true;
}