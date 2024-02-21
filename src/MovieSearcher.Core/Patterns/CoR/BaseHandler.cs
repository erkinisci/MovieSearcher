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
    
    public virtual async Task<object?> Handle(CancellationToken cancellationToken, object request, params object[] parameters)
    {
        if (_nextHandler != null)
            return await _nextHandler.Handle(cancellationToken, request, parameters);

        return (request, parameters);
    }
}

// public abstract class BaseHandler : IHandler
// {
//     private IHandler? _nextHandler;
//
//     public IHandler SetNext(IHandler handler)
//     {
//         _nextHandler = handler;
//
//         return _nextHandler;
//     }
//
//     public virtual async Task<object?> Handle(object request, CancellationToken cancellationToken)
//     {
//         var result = await Process(request, cancellationToken);
//
//         if (_nextHandler == null) 
//             return result;
//         
//         var nextResult = await _nextHandler.Handle(request, cancellationToken);
//         
//         // You may want to handle or aggregate the results here
//         // For example, you could merge the results, choose the first non-null result, etc.
//         // Here, we simply return the result from the last handler in the chain.
//         result = nextResult ?? result;
//
//         return result;
//     }
//
//     // The actual processing logic for each handler should be implemented in this method.
//     protected abstract Task<object?> Process(object request, CancellationToken cancellationToken);
// }