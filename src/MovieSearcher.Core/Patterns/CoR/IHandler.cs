namespace MovieSearcher.Core.Patterns.CoR;

public interface IHandler
{
    IHandler SetNext(IHandler handler);
    Task<object?> Handle(object request, CancellationToken cancellationToken);
}