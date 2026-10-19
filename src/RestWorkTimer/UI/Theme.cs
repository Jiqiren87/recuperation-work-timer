using System.Drawing.Drawing2D;

namespace RestWorkTimer.UI;

internal static class Theme
{
    public static readonly Color Background = Color.FromArgb(245, 247, 250);
    public static readonly Color Card = Color.White;
    public static readonly Color Text = Color.FromArgb(31, 41, 55);
    public static readonly Color Muted = Color.FromArgb(107, 114, 128);
    public static readonly Color Border = Color.FromArgb(229, 231, 235);
    public static readonly Color Accent = Color.FromArgb(47, 111, 237);
    public static readonly Color AccentHover = Color.FromArgb(37, 92, 204);
    public static readonly Color Working = Color.FromArgb(22, 163, 74);
    public static readonly Color Rest = Color.FromArgb(234, 88, 12);
    public static readonly Color Idle = Color.FromArgb(107, 114, 128);

    public static Font Ui(float size, FontStyle style = FontStyle.Regular)
        => new("Microsoft YaHei UI", size, style, GraphicsUnit.Point);

    public static Font Mono(float size, FontStyle style = FontStyle.Regular)
        => new("Consolas", size, style, GraphicsUnit.Point);

    public static void StylePrimary(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = Accent;
        button.ForeColor = Color.White;
        button.Font = Ui(10f, FontStyle.Bold);
        button.Height = 42;
        button.Cursor = Cursors.Hand;
        button.UseVisualStyleBackColor = false;
    }

    public static void StyleSecondary(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = Border;
        button.BackColor = Card;
        button.ForeColor = Text;
        button.Font = Ui(10f);
        button.Height = 42;
        button.Cursor = Cursors.Hand;
        button.UseVisualStyleBackColor = false;
    }

    public static void StyleDanger(Button button)
    {
        StyleSecondary(button);
        button.ForeColor = Rest;
    }

    public static void EnableDoubleBuffering(Control control)
    {
        typeof(Control)
            .GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.SetValue(control, true, null);
    }

    public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var diameter = Math.Max(1, radius * 2);
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
