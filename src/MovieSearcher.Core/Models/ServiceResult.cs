namespace MovieSearcher.Core.Models;

public readonly record struct ServiceResult<T>()
{
    public ServiceResult(string error) : this(default, error)
    {
    }

    public ServiceResult(T? Data) : this(Data, Array.Empty<MovieSearcherError>())
    {
    }

    public ServiceResult(T? Data, string error) : this(Data, [new MovieSearcherError(error)])
    {
    }

    private ServiceResult(T? Data, MovieSearcherError[]? errors = null) : this()
    {
        this.Data = Data;

        if (errors is { Length: > 0 })
            Errors = errors;
    }

    public T? Data { get; }
    public MovieSearcherError[] Errors { get; } = Array.Empty<MovieSearcherError>();

    public bool IsSuccess => Errors.Length == 0;
}