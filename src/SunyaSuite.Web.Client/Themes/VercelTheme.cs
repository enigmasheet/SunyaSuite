using MudBlazor;

namespace SunyaSuite.Web.Client.Themes;

public static class VercelTheme
{
    public static MudTheme Instance => new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#16a34a",
            PrimaryContrastText = "#ffffff",
            Secondary = "#525252",
            Tertiary = "#0a0a0a",
            Surface = "#ffffff",
            Background = "#fafafa",
            BackgroundGray = "#f5f5f5",
            AppbarBackground = "#ffffff",
            AppbarText = "#0a0a0a",
            DrawerBackground = "#ffffff",
            DrawerText = "#0a0a0a",
            TextPrimary = "#0a0a0a",
            TextSecondary = "#525252",
            TextDisabled = "#a3a3a3",
            Dark = "#171717",
            Info = "#2563eb",
            Success = "#16a34a",
            Warning = "#d97706",
            Error = "#dc2626",
            LinesDefault = "#e5e5e5",
            LinesInputs = "#d4d4d4",
            Divider = "#e5e5e5",
            DividerLight = "#f0f0f0",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#22c55e",
            PrimaryContrastText = "#ffffff",
            Secondary = "#a3a3a3",
            Tertiary = "#fafafa",
            Surface = "#171717",
            Background = "#0a0a0a",
            BackgroundGray = "#171717",
            AppbarBackground = "#0a0a0a",
            AppbarText = "#fafafa",
            DrawerBackground = "#0a0a0a",
            DrawerText = "#fafafa",
            TextPrimary = "#fafafa",
            TextSecondary = "#a3a3a3",
            TextDisabled = "#525252",
            Dark = "#000000",
            DarkLighten = "#171717",
            Info = "#60a5fa",
            Success = "#4ade80",
            Warning = "#fbbf24",
            Error = "#f87171",
            LinesDefault = "#262626",
            LinesInputs = "#404040",
            Divider = "#262626",
            DividerLight = "#1a1a1a",
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = new[] { "Inter", "system-ui", "-apple-system", "BlinkMacSystemFont", "Segoe UI", "Roboto", "sans-serif" },
                FontSize = "0.875rem",
                FontWeight = "400",
                LineHeight = "1.5",
                LetterSpacing = ".01em"
            },
            H1 = new H1Typography { FontSize = "2rem", FontWeight = "700", LineHeight = "1.25" },
            H2 = new H2Typography { FontSize = "1.5rem", FontWeight = "600", LineHeight = "1.3" },
            H3 = new H3Typography { FontSize = "1.25rem", FontWeight = "600", LineHeight = "1.4" },
            H4 = new H4Typography { FontSize = "1rem", FontWeight = "600", LineHeight = "1.4" },
            H5 = new H5Typography { FontSize = "0.875rem", FontWeight = "600", LineHeight = "1.5" },
            H6 = new H6Typography { FontSize = "0.75rem", FontWeight = "600", LineHeight = "1.5", LetterSpacing = ".05em", TextTransform = "uppercase" },
            Button = new ButtonTypography { FontSize = "0.875rem", FontWeight = "500", LineHeight = "1.5", LetterSpacing = ".01em", TextTransform = "none" },
            Body1 = new Body1Typography { FontSize = "0.875rem", FontWeight = "400", LineHeight = "1.5" },
            Body2 = new Body2Typography { FontSize = "0.75rem", FontWeight = "400", LineHeight = "1.5" },
            Subtitle1 = new Subtitle1Typography { FontSize = "0.875rem", FontWeight = "500", LineHeight = "1.5" },
            Subtitle2 = new Subtitle2Typography { FontSize = "0.75rem", FontWeight = "500", LineHeight = "1.5" },
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "8px",
            DrawerWidthLeft = "260px",
            DrawerWidthRight = "260px",
            AppbarHeight = "56px",
        }
    };
}
