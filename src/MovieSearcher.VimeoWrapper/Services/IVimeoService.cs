using MovieSearcher.Core.Models;
using MovieSearcher.Core.Query;
using VimeoDotNet.Models;

namespace MovieSearcher.VimeoWrapper.Services;

public interface IVimeoService
{
    Task<ServiceResult<Paginated<Video>?>> Search(QueryParameters queryParameters);
}