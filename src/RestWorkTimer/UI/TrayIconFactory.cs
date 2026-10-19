using System.Drawing.Drawing2D;
using RestWorkTimer.Core;
using RestWorkTimer.Platform;

namespace RestWorkTimer.UI;

internal static class TrayIconFactory
{
    public static Icon Create(TimerState state)
    {
        using var bitmap = new Bitmap(32, 32);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);

            var color = state switch
            {
                TimerState.Working => Theme.Working,
                TimerState.Rest => Theme.Rest,
                _ => Theme.Idle
            };

            using var brush = new SolidBrush(color);
            graphics.FillEllipse(brush, 1, 1, 30, 30);

            using var pen = new Pen(Color.White, 2.4f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            graphics.DrawEllipse(pen, 5, 5, 22, 22);
            graphics.DrawLine(pen, 16, 16, 16, 8.5f);
            graphics.DrawLine(pen, 16, 16, 21.5f, 19.5f);
            graphics.FillEllipse(Brushes.White, 14, 14, 4, 4);
        }

        var handle = bitmap.GetHicon();
        try
        {
            using var temporary = Icon.FromHandle(handle);
            return (Icon)temporary.Clone();
        }
        finally
        {
            NativeMethods.DestroyIcon(handle);
        }
    }
}
