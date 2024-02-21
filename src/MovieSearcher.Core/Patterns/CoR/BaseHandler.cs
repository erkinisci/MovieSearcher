namespace MovieSearcher.Core.Patterns.CoR;

public abstract class BaseHandler : IHandler
{
    private IHandler? _nextHandler;

    public IHandler SetNext(IHandler handler)
    {
        _nextHandler = handler;

        return _nextHandler;
    }

    public virtual async Task<object?> Handle(object request, CancellationToken cancellationToken)
    {
        if (_nextHandler != null)
            return await _nextHandler.Handle(request, cancellationToken);

        return request;
    }

    public virtual async Task<object?> Handle(CancellationToken cancellationToken, object request,
        params object[] parameters)
    {
        if (_nextHandler != null)
            return await _nextHandler.Handle(cancellationToken, request, parameters);

        return (request, parameters);
    }
}