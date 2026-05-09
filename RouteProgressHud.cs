using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GAME;

public sealed class RouteProgressHud
{
    public void Draw(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        Rectangle screen,
        float totalRouteNm,
        float remainingNm,
        float topOfClimbNm,
        float topOfDescentNm,
        float approachStartNm)
    {
        var barBounds = new Rectangle(86, 15, screen.Width - 172, 8);
        var routeColor = new Color(32, 48, 48);
        var progressColor = new Color(144, 184, 88);
        var markerColor = new Color(248, 216, 72);
        var textColor = new Color(224, 248, 208);

        // Üst rota çubuğu şimdilik sadece LTFM -> LTCC ilerlemesini gösterir.
        spriteBatch.Draw(pixel, barBounds, routeColor);

        var completedRatio = MathHelper.Clamp((totalRouteNm - remainingNm) / totalRouteNm, 0f, 1f);
        var completedWidth = (int)(barBounds.Width * completedRatio);
        spriteBatch.Draw(pixel, new Rectangle(barBounds.X, barBounds.Y, completedWidth, barBounds.Height), progressColor);

        DrawMarker(spriteBatch, pixel, barBounds, totalRouteNm, topOfClimbNm, markerColor);
        DrawMarker(spriteBatch, pixel, barBounds, totalRouteNm, topOfDescentNm, markerColor);
        DrawMarker(spriteBatch, pixel, barBounds, totalRouteNm, approachStartNm, new Color(208, 232, 200));

        PixelTextRenderer.Draw(spriteBatch, pixel, "LTFM", new Vector2(16, 10), textColor, 2);
        PixelTextRenderer.Draw(spriteBatch, pixel, "LTCC", new Vector2(screen.Width - 64, 10), textColor, 2);
        PixelTextRenderer.Draw(spriteBatch, pixel, $"NM {remainingNm:000}", new Vector2(screen.Width / 2 - 28, 28), textColor, 2);
        PixelTextRenderer.Draw(spriteBatch, pixel, "TOC", new Vector2(GetMarkerX(barBounds, totalRouteNm, topOfClimbNm) - 12, 28), markerColor, 2);
        PixelTextRenderer.Draw(spriteBatch, pixel, "TOD", new Vector2(GetMarkerX(barBounds, totalRouteNm, topOfDescentNm) - 12, 28), markerColor, 2);
    }

    private static void DrawMarker(SpriteBatch spriteBatch, Texture2D pixel, Rectangle barBounds, float totalRouteNm, float markerNm, Color color)
    {
        var x = GetMarkerX(barBounds, totalRouteNm, markerNm);
        spriteBatch.Draw(pixel, new Rectangle(x - 2, barBounds.Y - 5, 4, barBounds.Height + 10), color);
    }

    private static int GetMarkerX(Rectangle barBounds, float totalRouteNm, float markerNm)
    {
        // NM değerleri varışa kalan mesafe olduğu için çubukta ters yönden hesaplanır.
        var ratio = MathHelper.Clamp((totalRouteNm - markerNm) / totalRouteNm, 0f, 1f);
        return barBounds.X + (int)(barBounds.Width * ratio);
    }
}
