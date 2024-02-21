namespace MovieSearcher.Core.Patterns.CoR;

public interface IHandler
{
    IHandler SetNext(IHandler handler);
    Task<object?> Handle(object request, CancellationToken cancellationToken);
    Task<object?> Handle(CancellationToken cancellationToken, object request, params object[] parameters);
}