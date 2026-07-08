using MudBlazor;

namespace SunyaSuite.Web.Client.Shared;

public class DetailField
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsLink { get; set; }
    public string? Href { get; set; }
    public Color Color { get; set; } = Color.Default;
}
