using MovieSearcher.Core.Models;

namespace MovieSearcher.Core;

/// <summary>
/// This is a unified service wrapper designed for video searches across various suppliers. 
/// Any object derived from this interface and registered in Dependency Injection will be iterated to retrieve video links based on the remote service.
/// </summary>
public interface IVideoUrlServiceWrapper
{
    Task<ServiceResult<string[]>> Search(string query, CancellationToken cancellationToken);
}