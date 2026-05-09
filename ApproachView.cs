using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GAME;

public sealed class ApproachView
{
    public void Draw(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        Rectangle screen,
        int altitude,
        int speed,
        float fuel,
        float localizerDeviation,
        float glideDeviation,
        float verticalSpeedFpm,
        FlightPhase flightPhase,
        string landingQuality,
        int score,
        int passengerComfort)
    {
        DrawSky(spriteBatch, pixel, screen);
        DrawRunwayPerspective(spriteBatch, pixel, screen, localizerDeviation, glideDeviation);
        DrawAircraftReference(spriteBatch, pixel, screen);
        DrawInstrumentPanel(spriteBatch, pixel, screen, altitude, speed, fuel, localizerDeviation, glideDeviation, verticalSpeedFpm, flightPhase);

        if (flightPhase == FlightPhase.MissionComplete || flightPhase == FlightPhase.GameOver)
            DrawResult(spriteBatch, pixel, screen, landingQuality, score, passengerComfort);
    }

    private static void DrawSky(SpriteBatch spriteBatch, Texture2D pixel, Rectangle screen)
    {
        var horizonY = 250;

        // Yaklaşma ekranı normal haritadan ayrı, sade gece yaklaşması gibi çizilir.
        spriteBatch.Draw(pixel, new Rectangle(0, 0, screen.Width, horizonY), new Color(8, 12, 48));
        spriteBatch.Draw(pixel, new Rectangle(0, horizonY, screen.Width, screen.Height - horizonY), new Color(20, 32, 72));
        spriteBatch.Draw(pixel, new Rectangle(0, horizonY - 3, screen.Width, 6), new Color(104, 132, 176));

        for (var i = 0; i < 18; i++)
        {
            var x = 24 + i * 35;
            var y = 38 + (i * 19) % 150;
            spriteBatch.Draw(pixel, new Rectangle(x, y, 2, 2), new Color(216, 232, 248));
        }
    }

    private static void DrawRunwayPerspective(SpriteBatch spriteBatch, Texture2D pixel, Rectangle screen, float localizerDeviation, float glideDeviation)
    {
        var centerX = screen.Width / 2 - (int)(localizerDeviation * 150f);
        var horizonY = 260 + (int)(glideDeviation * 35f);
        var runwayColor = new Color(28, 28, 44);
        var edgeColor = new Color(232, 216, 96);
        var centerLine = new Color(248, 248, 216);

        // Pist trapezi perspektif hissi verir; localizer sapması pisti sağa sola kaydırır.
        DrawRect(spriteBatch, pixel, centerX - 40, horizonY, 80, 10, runwayColor);
        DrawRect(spriteBatch, pixel, centerX - 150, screen.Height - 112, 300, 44, runwayColor);

        spriteBatch.Draw(pixel, new Rectangle(centerX - 154, screen.Height - 112, 6, 44), edgeColor);
        spriteBatch.Draw(pixel, new Rectangle(centerX + 148, screen.Height - 112, 6, 44), edgeColor);

        for (var i = 0; i < 6; i++)
        {
            var y = horizonY + 24 + i * 36;
            var width = 4 + i * 3;
            spriteBatch.Draw(pixel, new Rectangle(centerX - width / 2, y, width, 16), centerLine);
        }

        DrawCityLights(spriteBatch, pixel, screen, centerX);
    }

    private static void DrawCityLights(SpriteBatch spriteBatch, Texture2D pixel, Rectangle screen, int centerX)
    {
        var light = new Color(232, 184, 64);

        // Basit şehir ışıkları pist çevresine yaklaşma hissi verir.
        for (var i = 0; i < 34; i++)
        {
            var side = i % 2 == 0 ? -1 : 1;
            var x = centerX + side * (120 + (i * 17) % 190);
            var y = 318 + (i * 23) % 170;
            spriteBatch.Draw(pixel, new Rectangle(x, y, 4, 3), light);
        }
    }

    private static void DrawAircraftReference(SpriteBatch spriteBatch, Texture2D pixel, Rectangle screen)
    {
        var centerX = screen.Width / 2;
        var y = screen.Height - 168;
        var body = new Color(208, 224, 216);
        var shadow = new Color(80, 96, 112);

        // Yaklaşmada uçak küçük bir referans silüeti olarak altta kalır.
        spriteBatch.Draw(pixel, new Rectangle(centerX - 58, y + 22, 116, 8), body);
        spriteBatch.Draw(pixel, new Rectangle(centerX - 18, y, 36, 42), body);
        spriteBatch.Draw(pixel, new Rectangle(centerX - 10, y + 8, 20, 8), shadow);
        spriteBatch.Draw(pixel, new Rectangle(centerX - 43, y + 28, 13, 10), shadow);
        spriteBatch.Draw(pixel, new Rectangle(centerX + 30, y + 28, 13, 10), shadow);
    }

    private static void DrawInstrumentPanel(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        Rectangle screen,
        int altitude,
        int speed,
        float fuel,
        float localizerDeviation,
        float glideDeviation,
        float verticalSpeedFpm,
        FlightPhase flightPhase)
    {
        var panel = new Rectangle(0, 0, screen.Width, 76);
        var text = new Color(224, 248, 208);
        var warn = new Color(248, 216, 72);

        // Üst panel yaklaşma sırasında temel uçuş bilgisini gösterir.
        spriteBatch.Draw(pixel, panel, new Color(28, 44, 48));
        PixelTextRenderer.Draw(spriteBatch, pixel, $"ALT {altitude:0000}", new Vector2(24, 14), text, 2);
        PixelTextRenderer.Draw(spriteBatch, pixel, $"SPD {speed:000}", new Vector2(158, 14), text, 2);
        PixelTextRenderer.Draw(spriteBatch, pixel, $"V/S {(int)verticalSpeedFpm:0000}", new Vector2(292, 14), text, 2);
        PixelTextRenderer.Draw(spriteBatch, pixel, $"FUEL {fuel:00}", new Vector2(460, 14), text, 2);
        PixelTextRenderer.Draw(spriteBatch, pixel, flightPhase == FlightPhase.Landing ? "FLARE" : "ILS", new Vector2(24, 44), warn, 2);

        DrawLocalizerBar(spriteBatch, pixel, new Rectangle(150, 47, 120, 8), localizerDeviation, text, warn);
        DrawGlidePathBar(spriteBatch, pixel, new Rectangle(508, 22, 8, 48), glideDeviation, text, warn);
        DrawHsi(spriteBatch, pixel, new Rectangle(340, 38, 72, 30), localizerDeviation, text, warn);
        PixelTextRenderer.Draw(spriteBatch, pixel, "LOC", new Vector2(112, 42), text, 1);
        PixelTextRenderer.Draw(spriteBatch, pixel, "GP", new Vector2(488, 42), text, 1);
        PixelTextRenderer.Draw(spriteBatch, pixel, "HSI", new Vector2(314, 42), text, 1);
    }

    private static void DrawLocalizerBar(SpriteBatch spriteBatch, Texture2D pixel, Rectangle bar, float deviation, Color textColor, Color warnColor)
    {
        // LOC yataydır; marker sağ-sol sapmayı gösterir.
        spriteBatch.Draw(pixel, bar, new Color(8, 16, 20));
        spriteBatch.Draw(pixel, new Rectangle(bar.Center.X - 1, bar.Y - 4, 2, bar.Height + 8), textColor);

        var markerX = bar.Center.X + (int)(MathHelper.Clamp(deviation, -1f, 1f) * (bar.Width / 2));
        spriteBatch.Draw(pixel, new Rectangle(markerX - 3, bar.Y - 5, 6, bar.Height + 10), warnColor);
    }

    private static void DrawGlidePathBar(SpriteBatch spriteBatch, Texture2D pixel, Rectangle bar, float deviation, Color textColor, Color warnColor)
    {
        // GP dikeydir; marker yukarı-aşağı sapmayı gösterir.
        spriteBatch.Draw(pixel, bar, new Color(8, 16, 20));
        spriteBatch.Draw(pixel, new Rectangle(bar.X - 4, bar.Center.Y - 1, bar.Width + 8, 2), textColor);

        var markerY = bar.Center.Y + (int)(MathHelper.Clamp(deviation, -1f, 1f) * (bar.Height / 2));
        spriteBatch.Draw(pixel, new Rectangle(bar.X - 5, markerY - 3, bar.Width + 10, 6), warnColor);
    }

    private static void DrawHsi(SpriteBatch spriteBatch, Texture2D pixel, Rectangle box, float localizerDeviation, Color textColor, Color warnColor)
    {
        var centerX = box.Center.X;
        var centerY = box.Center.Y;
        var courseX = centerX + (int)(MathHelper.Clamp(localizerDeviation, -1f, 1f) * 24f);

        // Basit HSI/gyro, pist kursunu ve localizer sapmasını küçük bir göstergede verir.
        spriteBatch.Draw(pixel, box, new Color(8, 16, 20));
        spriteBatch.Draw(pixel, new Rectangle(centerX - 1, box.Y + 4, 2, box.Height - 8), textColor);
        spriteBatch.Draw(pixel, new Rectangle(box.X + 8, centerY - 1, box.Width - 16, 2), textColor);
        spriteBatch.Draw(pixel, new Rectangle(courseX - 2, box.Y + 3, 4, box.Height - 6), warnColor);
        PixelTextRenderer.Draw(spriteBatch, pixel, "17R", new Vector2(box.X + 26, box.Y + 20), textColor, 1);
    }

    private static void DrawResult(SpriteBatch spriteBatch, Texture2D pixel, Rectangle screen, string landingQuality, int score, int passengerComfort)
    {
        var box = new Rectangle(screen.Width / 2 - 150, 145, 300, 100);
        var text = new Color(224, 248, 208);
        var warn = new Color(248, 216, 72);

        // İnişten sonra görev sonucu sade bir panelde gösterilir.
        spriteBatch.Draw(pixel, box, new Color(8, 16, 20));
        PixelTextRenderer.Draw(spriteBatch, pixel, landingQuality, new Vector2(box.X + 42, box.Y + 18), warn, 2);
        PixelTextRenderer.Draw(spriteBatch, pixel, $"SCORE {score:0000}", new Vector2(box.X + 42, box.Y + 46), text, 2);
        PixelTextRenderer.Draw(spriteBatch, pixel, $"PAX {passengerComfort:000}", new Vector2(box.X + 42, box.Y + 70), text, 2);
    }

    private static void DrawRect(SpriteBatch spriteBatch, Texture2D pixel, int x, int y, int width, int height, Color color)
    {
        spriteBatch.Draw(pixel, new Rectangle(x, y, width, height), color);
    }
}
