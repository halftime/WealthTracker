using Microsoft.Maui.Graphics;
using WealthTracker.Models;

namespace WealthTracker.Charts;

public sealed class PortfolioPieDrawable : IDrawable
{
    public IReadOnlyList<HoldingSnapshot> Holdings { get; set; } = Array.Empty<HoldingSnapshot>();

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FillColor = Color.FromArgb("#161616");
        canvas.FillRectangle(dirtyRect);

        var holdings = Holdings.Where(holding => holding.CurrentValue > 0).ToArray();
        if (holdings.Length == 0)
        {
            DrawEmpty(canvas, dirtyRect, "Add investments to see allocation");
            return;
        }

        var centerX = dirtyRect.Center.X;
        var centerY = dirtyRect.Center.Y;
        var radius = MathF.Min(dirtyRect.Width, dirtyRect.Height) * 0.36f;
        var startAngle = -90f;

        foreach (var holding in holdings)
        {
            var sweep = MathF.Max(2f, (float)(holding.Allocation * 360m));
            canvas.FillColor = Color.FromArgb(holding.ColorHex);
            canvas.FillPath(BuildSector(centerX, centerY, radius, startAngle, sweep));
            startAngle += sweep;
        }

        canvas.FillColor = Color.FromArgb("#1B1B1F");
        canvas.FillCircle(centerX, centerY, radius * 0.48f);
        canvas.FontColor = Colors.White;
        canvas.FontSize = 15;
        canvas.DrawString("Portfolio", centerX - radius, centerY - 12, radius * 2, 18, HorizontalAlignment.Center, VerticalAlignment.Center);
        canvas.FontSize = 11;
        canvas.FontColor = Color.FromArgb("#CBD5E1");
        canvas.DrawString($"{holdings.Length} assets", centerX - radius, centerY + 8, radius * 2, 18, HorizontalAlignment.Center, VerticalAlignment.Center);
    }

    private static PathF BuildSector(float centerX, float centerY, float radius, float startAngle, float sweep)
    {
        const int steps = 42;
        var path = new PathF();
        path.MoveTo(centerX, centerY);

        for (var index = 0; index <= steps; index++)
        {
            var angle = (startAngle + sweep * index / steps) * MathF.PI / 180f;
            path.LineTo(centerX + radius * MathF.Cos(angle), centerY + radius * MathF.Sin(angle));
        }

        path.Close();
        return path;
    }

    private static void DrawEmpty(ICanvas canvas, RectF dirtyRect, string text)
    {
        canvas.FontColor = Color.FromArgb("#94A3B8");
        canvas.FontSize = 14;
        canvas.DrawString(text, dirtyRect, HorizontalAlignment.Center, VerticalAlignment.Center);
    }
}
