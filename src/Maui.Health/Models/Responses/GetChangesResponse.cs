namespace Maui.Health.Models.Responses;

public class GetChangesResponse<TDto>
{
    public List<TDto> Records { get; set; } = new();
    public object? ChangeTokenOrAnchor { get; set; }
    public bool IsError { get; set; }
    public string? ErrorMessage { get; set; }
    public Exception? ErrorException { get; set; }
}