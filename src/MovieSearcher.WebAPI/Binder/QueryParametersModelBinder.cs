using MovieSearcher.Core.Query;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MovieSearcher.WebAPI.Binder;

public class QueryParametersModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var queryValue = bindingContext.ValueProvider.GetValue("Query");
        var page = bindingContext.ValueProvider.GetValue("Page");
        var perPage = bindingContext.ValueProvider.GetValue("PerPage");

        if (queryValue == ValueProviderResult.None || queryValue == ValueProviderResult.None) return Task.CompletedTask;

        var model = new QueryParameters
        {
            Query = queryValue.FirstValue
        };

        if (page.FirstValue != null)
            model.Page = Convert.ToInt32(page.FirstValue);

        if (perPage.FirstValue != null)
            model.PerPage = Convert.ToInt32(perPage.FirstValue);

        bindingContext.Result = ModelBindingResult.Success(model);
        return Task.CompletedTask;
    }
}