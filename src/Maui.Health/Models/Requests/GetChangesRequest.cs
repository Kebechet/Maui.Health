namespace Maui.Health.Models.Requests;

public class GetChangesRequest
{
    public List<string>? OriginFilter { get; set; }
    public object? ChangeTokenOrAnchor { get; set; }
}