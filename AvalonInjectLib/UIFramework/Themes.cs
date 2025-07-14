namespace AvalonInjectLib.UIFramework
{
    public static class Themes
    {
        public static Theme CurrentTheme { get; private set; } = Dark;

        public static readonly Theme Dark = new Theme
        {
            Name = "Dark",
            Primary = new Color(33, 150, 243),
            Secondary = new Color(66, 66, 66),
            Surface = new Color(30, 30, 30),
            Background = new Color(25, 25, 25),
            Text = new Color(240, 240, 240),
            TextSecondary = new Color(180, 180, 180),
            Border = new Color(55, 55, 55),
            Hover = new Color(50, 50, 50),
            Active = new Color(70, 70, 70),
            Disabled = new Color(100, 100, 100)
        };

        public static readonly Theme Light = new Theme
        {
            Name = "Light",
            Primary = new Color(30, 136, 229),
            Secondary = new Color(224, 224, 224),
            Surface = new Color(255, 255, 255),
            Background = new Color(245, 245, 245),
            Text = new Color(33, 33, 33),
            TextSecondary = new Color(97, 97, 97),
            Border = new Color(224, 224, 224),
            Hover = new Color(238, 238, 238),
            Active = new Color(224, 224, 224),
            Disabled = new Color(200, 200, 200)
        };

        public static readonly Theme Blue = new Theme
        {
            Name = "Blue",
            Primary = new Color(13, 71, 161),
            Secondary = new Color(21, 101, 192),
            Surface = new Color(25, 118, 210),
            Background = new Color(30, 136, 229),
            Text = new Color(255, 255, 255),
            TextSecondary = new Color(227, 242, 253),
            Border = new Color(66, 165, 245),
            Hover = new Color(100, 181, 246),
            Active = new Color(144, 202, 249),
            Disabled = new Color(187, 222, 251)
        };

        public static readonly Theme Red = new Theme
        {
            Name = "Red",
            Primary = new Color(198, 40, 40),
            Secondary = new Color(229, 57, 53),
            Surface = new Color(244, 67, 54),
            Background = new Color(255, 82, 82),
            Text = new Color(255, 255, 255),
            TextSecondary = new Color(255, 205, 210),
            Border = new Color(239, 83, 80),
            Hover = new Color(255, 138, 128),
            Active = new Color(255, 112, 67),
            Disabled = new Color(255, 171, 145)
        };

        public static void SetTheme(Theme theme)
        {
            CurrentTheme = theme ?? Dark;
        }

        public static void SetTheme(string themeName)
        {
            switch (themeName.ToLower())
            {
                case "light":
                    CurrentTheme = Light;
                    break;
                case "blue":
                    CurrentTheme = Blue;
                    break;
                case "red":
                    CurrentTheme = Red;
                    break;
                default:
                    CurrentTheme = Dark;
                    break;
            }
        }
    }

    public class Theme
    {
        public string Name { get; set; }
        public Color Primary { get; set; }
        public Color Secondary { get; set; }
        public Color Surface { get; set; }
        public Color Background { get; set; }
        public Color Text { get; set; }
        public Color TextSecondary { get; set; }
        public Color Border { get; set; }
        public Color Hover { get; set; }
        public Color Active { get; set; }
        public Color Disabled { get; set; }

        // Métodos útiles
        public Color GetTextColor(bool isEnabled)
        {
            return isEnabled ? Text : Disabled;
        }

        public Color GetButtonColor(bool isHovered, bool isPressed)
        {
            if (!isHovered && !isPressed) return Primary;
            if (isPressed) return Active;
            return Hover;
        }
    }
}