namespace MovieSearcher.Core.Query;

public class QueryParameters
{
    public required string Query { get; init; }
    public int? Page { get; set; }
    public int? PerPage { get; set; }
}