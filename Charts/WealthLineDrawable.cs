using Microsoft.Maui.Graphics;
using WealthTracker.Models;

namespace WealthTracker.Charts;

public sealed class WealthLineDrawable : IDrawable
{
    public IReadOnlyList<WealthPoint> Points { get; set; } = Array.Empty<WealthPoint>();

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FillColor = Color.FromArgb("#161616");
        canvas.FillRectangle(dirtyRect);

        if (Points.Count < 2)
        {
            canvas.FontColor = Color.FromArgb("#94A3B8");
            canvas.FontSize = 14;
            canvas.DrawString("Add investments to plot wealth over time", dirtyRect, HorizontalAlignment.Center, VerticalAlignment.Center);
            return;
        }

        var plot = new RectF(dirtyRect.X + 46, dirtyRect.Y + 18, dirtyRect.Width - 62, dirtyRect.Height - 48);
        var min = Points.Min(point => point.Value);
        var max = Points.Max(point => point.Value);
        if (max <= min)
        {
            max = min + 1;
        }

        canvas.StrokeColor = Color.FromArgb("#3D3530");
        canvas.StrokeSize = 1;
        for (var index = 0; index <= 4; index++)
        {
            var y = plot.Top + plot.Height * index / 4f;
            canvas.DrawLine(plot.Left, y, plot.Right, y);
        }

        var path = new PathF();
        for (var index = 0; index < Points.Count; index++)
        {
            var point = Points[index];
            var x = plot.Left + plot.Width * index / Math.Max(1, Points.Count - 1);
            var normalized = (float)((point.Value - min) / (max - min));
            var y = plot.Bottom - plot.Height * normalized;

            if (index == 0)
            {
                path.MoveTo(x, y);
            }
            else
            {
                path.LineTo(x, y);
            }
        }

        canvas.StrokeColor = Color.FromArgb("#38BDF8");
        canvas.StrokeSize = 3;
        canvas.DrawPath(path);

        canvas.FontSize = 11;
        canvas.FontColor = Color.FromArgb("#CBD5E1");
        canvas.DrawString(max.ToString("N0"), dirtyRect.X + 4, plot.Top - 6, 40, 16, HorizontalAlignment.Right, VerticalAlignment.Center);
        canvas.DrawString(min.ToString("N0"), dirtyRect.X + 4, plot.Bottom - 8, 40, 16, HorizontalAlignment.Right, VerticalAlignment.Center);
        canvas.DrawString(Points[0].Date.ToString("yyyy-MM-dd"), plot.Left, plot.Bottom + 8, 100, 16, HorizontalAlignment.Left, VerticalAlignment.Center);
        canvas.DrawString(Points[^1].Date.ToString("yyyy-MM-dd"), plot.Right - 100, plot.Bottom + 8, 100, 16, HorizontalAlignment.Right, VerticalAlignment.Center);
    }
}
