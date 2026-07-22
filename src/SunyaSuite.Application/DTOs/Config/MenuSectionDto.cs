namespace SunyaSuite.Application.DTOs.Config;

public class MenuSectionDto
{
    public string SectionTitle { get; set; } = string.Empty;
    public List<MenuItemDto> Items { get; set; } = [];
}

public class MenuItemDto
{
    public string Label { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Href { get; set; } = string.Empty;
    public bool IsExternal { get; set; }
}
