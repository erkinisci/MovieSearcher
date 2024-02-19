using MovieSearcher.Core.Query;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace MovieSearcher.WebAPI.Binder;

public class QueryParametersModelBinderProvider : IModelBinderProvider
{
    public IModelBinder GetBinder(ModelBinderProviderContext context)
    {
        return (context.Metadata.ModelType == typeof(QueryParameters)
            ? new BinderTypeModelBinder(typeof(QueryParametersModelBinder))
            : null)!;
    }
}