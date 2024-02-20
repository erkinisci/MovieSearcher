namespace MovieSearcher.Core.Exceptions;

/// <summary>
/// Exception type for aggregator exceptions
/// </summary>
public class MovieAggregatorException : Exception
{
    public MovieAggregatorException()
    {
    }

    public MovieAggregatorException(string message)
        : base(message)
    {
    }

    public MovieAggregatorException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}