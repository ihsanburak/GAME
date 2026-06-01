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
        float selectedVerticalSpeedFpm,
        float remainingNm,
        int fieldElevation,
        float bankAmount,
        FlightPhase flightPhase,
        string landingQuality,
        int score,
        int passengerComfort)
    {
        DrawSky(spriteBatch, pixel, screen);
        var approachProgress = GetApproachProgress(altitude, remainingNm);
        DrawRunwayPerspective(spriteBatch, pixel, screen, altitude, localizerDeviation, glideDeviation, remainingNm, approachProgress);
        DrawAircraftReference(spriteBatch, pixel, screen, altitude, verticalSpeedFpm, glideDeviation, bankAmount, approachProgress, flightPhase);
        DrawInstrumentPanel(spriteBatch, pixel, screen, altitude, speed, fuel, localizerDeviation, glideDeviation, verticalSpeedFpm, selectedVerticalSpeedFpm, fieldElevation, flightPhase);
        DrawRadioAltimeter(spriteBatch, pixel, screen, altitude);

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

    private static float GetApproachProgress(int altitude, float remainingNm)
    {
        var approachProgress = MathHelper.Clamp(1f - altitude / 1200f, 0f, 1f);
        var distanceProgress = MathHelper.Clamp(1f - remainingNm / 20f, 0f, 1f);
        return MathHelper.Clamp((approachProgress * 0.75f) + (distanceProgress * 0.25f), 0f, 1f);
    }

    private static void DrawRunwayPerspective(SpriteBatch spriteBatch, Texture2D pixel, Rectangle screen, int altitude, float localizerDeviation, float glideDeviation, float remainingNm, float progress)
    {
        var centerX = screen.Width / 2 - (int)(localizerDeviation * MathHelper.Lerp(80f, 170f, progress));
        var horizonY = 252 + (int)(glideDeviation * 28f);
        var farWidth = (int)MathHelper.Lerp(24f, 86f, progress);
        var nearWidth = (int)MathHelper.Lerp(120f, 470f, progress);
        var farY = horizonY + (int)MathHelper.Lerp(20f, 80f, progress);
        var nearY = (int)MathHelper.Lerp(screen.Height - 86, screen.Height - 18, progress);
        var thresholdY = (int)MathHelper.Lerp(nearY - 8, screen.Height - 126, 1f - progress);
        var runwayColor = new Color(28, 28, 44);
        var runwayNear = new Color(34, 34, 48);
        var grass = new Color(40, 54, 48);
        var edgeColor = new Color(236, 232, 168);
        var centerLine = new Color(248, 248, 216);

        // Pist çevresindeki zemin ve yaklaşma ışıkları alçalma hissini güçlendirir.
        spriteBatch.Draw(pixel, new Rectangle(0, horizonY + 42, screen.Width, screen.Height - horizonY - 42), grass);
        DrawApproachLights(spriteBatch, pixel, centerX, farY, nearY, progress);

        // Pist, alçaldıkça ve yaklaştıkça genişleyen perspektif parçalarıyla çizilir.
        DrawTrapezoidApprox(spriteBatch, pixel, centerX, farY, farWidth, nearY, nearWidth, runwayColor, runwayNear);

        spriteBatch.Draw(pixel, new Rectangle(centerX - nearWidth / 2 - 5, nearY - 6, 5, screen.Height - nearY + 16), edgeColor);
        spriteBatch.Draw(pixel, new Rectangle(centerX + nearWidth / 2, nearY - 6, 5, screen.Height - nearY + 16), edgeColor);
        spriteBatch.Draw(pixel, new Rectangle(centerX - farWidth / 2 - 2, farY, 3, 28), edgeColor);
        spriteBatch.Draw(pixel, new Rectangle(centerX + farWidth / 2, farY, 3, 28), edgeColor);

        DrawThreshold(spriteBatch, pixel, centerX, thresholdY, nearWidth, progress, centerLine);
        DrawTouchdownZone(spriteBatch, pixel, centerX, thresholdY, nearWidth, progress, centerLine);
        var runwayLabelScale = progress > 0.82f ? 4 : progress > 0.62f ? 3 : progress > 0.34f ? 2 : 1;
        var labelX = centerX - runwayLabelScale * 8;
        var labelY = thresholdY + (int)MathHelper.Lerp(24f, 46f, progress);
        PixelTextRenderer.Draw(spriteBatch, pixel, "34L", new Vector2(labelX, labelY), centerLine, runwayLabelScale);

        for (var i = 0; i < 7; i++)
        {
            var t = (i + 1) / 8f;
            var y = (int)MathHelper.Lerp(farY + 18, nearY - 18, t);
            var width = (int)MathHelper.Lerp(3f, 10f, t + progress * 0.2f);
            var height = (int)MathHelper.Lerp(8f, 28f, t + progress * 0.2f);
            spriteBatch.Draw(pixel, new Rectangle(centerX - width / 2, y, width, height), centerLine);
        }

        DrawPapi(spriteBatch, pixel, centerX, thresholdY, nearWidth, glideDeviation, progress);
        DrawCityLights(spriteBatch, pixel, screen, centerX, horizonY);
    }

    private static void DrawCityLights(SpriteBatch spriteBatch, Texture2D pixel, Rectangle screen, int centerX, int horizonY)
    {
        var light = new Color(232, 184, 64);

        // Basit şehir ışıkları pist çevresine yaklaşma hissi verir.
        for (var i = 0; i < 34; i++)
        {
            var side = i % 2 == 0 ? -1 : 1;
            var x = centerX + side * (120 + (i * 17) % 190);
            var y = horizonY + 75 + (i * 23) % 190;
            spriteBatch.Draw(pixel, new Rectangle(x, y, 4, 3), light);
        }
    }

    private static void DrawAircraftReference(SpriteBatch spriteBatch, Texture2D pixel, Rectangle screen, int altitude, float verticalSpeedFpm, float glideDeviation, float bankAmount, float approachProgress, FlightPhase flightPhase)
    {
        var centerX = screen.Width / 2;
        var y = (int)MathHelper.Lerp(screen.Height - 165, screen.Height - 92, approachProgress);
        if (altitude <= 18)
            y = screen.Height - 82;
        var visualScale = MathHelper.Lerp(0.78f, 1.12f, approachProgress);
        var body = new Color(208, 224, 216);
        var light = new Color(232, 240, 224);
        var shadow = new Color(80, 96, 112);
        var gear = new Color(38, 46, 56);
        var pitch = MathHelper.Clamp((-verticalSpeedFpm - 450f) / 500f + glideDeviation * 0.35f, -0.6f, 0.8f);
        var noseY = (int)(pitch * 8f);
        var leftWingY = (int)(bankAmount * 10f);
        var rightWingY = -(int)(bankAmount * 10f);

        // Yaklaşmada uçak arkadan görünür; pitch ve iniş takımları okunur.
        DrawScaled(spriteBatch, pixel, centerX - 64, y + 26 + leftWingY, 64, 8, light, visualScale, centerX, y);
        DrawScaled(spriteBatch, pixel, centerX, y + 26 + rightWingY, 64, 8, light, visualScale, centerX, y);
        DrawScaled(spriteBatch, pixel, centerX - 58, y + 34 + leftWingY, 58, 5, shadow, visualScale, centerX, y);
        DrawScaled(spriteBatch, pixel, centerX, y + 34 + rightWingY, 58, 5, shadow, visualScale, centerX, y);
        DrawScaled(spriteBatch, pixel, centerX - 20, y + noseY, 40, 46, body, visualScale, centerX, y);
        DrawScaled(spriteBatch, pixel, centerX - 14, y + 10 + noseY, 28, 8, shadow, visualScale, centerX, y);
        DrawScaled(spriteBatch, pixel, centerX - 24, y + 37, 48, 14, light, visualScale, centerX, y);

        DrawScaled(spriteBatch, pixel, centerX - 48, y + 38, 12, 11, shadow, visualScale, centerX, y);
        DrawScaled(spriteBatch, pixel, centerX + 36, y + 38, 12, 11, shadow, visualScale, centerX, y);

        if (flightPhase is FlightPhase.Approach or FlightPhase.Landing)
        {
            DrawScaled(spriteBatch, pixel, centerX - 34, y + 54, 5, 18, gear, visualScale, centerX, y);
            DrawScaled(spriteBatch, pixel, centerX + 29, y + 54, 5, 18, gear, visualScale, centerX, y);
            DrawScaled(spriteBatch, pixel, centerX - 39, y + 70, 15, 5, gear, visualScale, centerX, y);
            DrawScaled(spriteBatch, pixel, centerX + 24, y + 70, 15, 5, gear, visualScale, centerX, y);
            DrawScaled(spriteBatch, pixel, centerX - 4, y + 56, 8, 16, gear, visualScale, centerX, y);
        }
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
        float selectedVerticalSpeedFpm,
        int fieldElevation,
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
        PixelTextRenderer.Draw(spriteBatch, pixel, "GS 1200", new Vector2(112, 58), text, 1);
        PixelTextRenderer.Draw(spriteBatch, pixel, "FLARE 50", new Vector2(182, 58), warn, 1);
        PixelTextRenderer.Draw(spriteBatch, pixel, $"ELEV {fieldElevation:0000}", new Vector2(234, 58), text, 1);
        PixelTextRenderer.Draw(spriteBatch, pixel, $"SEL {(int)selectedVerticalSpeedFpm:0000}", new Vector2(318, 58), warn, 1);

        if (altitude <= 1000)
            PixelTextRenderer.Draw(spriteBatch, pixel, $"RA {altitude:0000}", new Vector2(520, 58), warn, 1);

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
        PixelTextRenderer.Draw(spriteBatch, pixel, "34L", new Vector2(box.X + 26, box.Y + 20), textColor, 1);
    }

    private static void DrawTrapezoidApprox(SpriteBatch spriteBatch, Texture2D pixel, int centerX, int topY, int topWidth, int bottomY, int bottomWidth, Color topColor, Color bottomColor)
    {
        var slices = 18;

        for (var i = 0; i < slices; i++)
        {
            var t0 = i / (float)slices;
            var t1 = (i + 1) / (float)slices;
            var y0 = (int)MathHelper.Lerp(topY, bottomY, t0);
            var y1 = (int)MathHelper.Lerp(topY, bottomY, t1);
            var width = (int)MathHelper.Lerp(topWidth, bottomWidth, t1);
            var color = Color.Lerp(topColor, bottomColor, t1);

            spriteBatch.Draw(pixel, new Rectangle(centerX - width / 2, y0, width, System.Math.Max(1, y1 - y0 + 1)), color);
        }
    }

    private static void DrawThreshold(SpriteBatch spriteBatch, Texture2D pixel, int centerX, int y, int runwayWidth, float progress, Color color)
    {
        var stripeWidth = System.Math.Max(3, (int)(runwayWidth / MathHelper.Lerp(28f, 20f, progress)));
        var stripeHeight = System.Math.Max(12, (int)(runwayWidth / MathHelper.Lerp(14f, 9f, progress)));
        var gap = stripeWidth;
        var totalWidth = stripeWidth * 6 + gap * 5;
        var startX = centerX - totalWidth / 2;

        // Threshold çizgileri pist üstünde, iki tarafta kesik bloklar olarak görünür.
        for (var i = 0; i < 6; i++)
            spriteBatch.Draw(pixel, new Rectangle(startX + i * (stripeWidth + gap), y, stripeWidth, stripeHeight), color);
    }

    private static void DrawTouchdownZone(SpriteBatch spriteBatch, Texture2D pixel, int centerX, int thresholdY, int runwayWidth, float progress, Color color)
    {
        var markWidth = System.Math.Max(10, (int)(runwayWidth / 9f));
        var markHeight = System.Math.Max(4, (int)MathHelper.Lerp(5f, 14f, progress));
        var sideOffset = System.Math.Max(28, runwayWidth / 5);

        // Touchdown zone çizgileri pist üzerinde derinlik referansı verir.
        for (var i = 0; i < 3; i++)
        {
            var y = thresholdY + 48 + i * (markHeight + 18);
            spriteBatch.Draw(pixel, new Rectangle(centerX - sideOffset - markWidth, y, markWidth, markHeight), color);
            spriteBatch.Draw(pixel, new Rectangle(centerX + sideOffset, y, markWidth, markHeight), color);
        }
    }

    private static void DrawApproachLights(SpriteBatch spriteBatch, Texture2D pixel, int centerX, int farY, int nearY, float progress)
    {
        var white = new Color(248, 248, 216);
        var amber = new Color(248, 196, 72);

        for (var i = 0; i < 8; i++)
        {
            var t = i / 7f;
            var y = (int)MathHelper.Lerp(farY - 22, nearY - 60, t);
            var spread = (int)MathHelper.Lerp(8f, 80f, t + progress * 0.15f);
            var size = System.Math.Max(2, (int)MathHelper.Lerp(2f, 6f, t + progress * 0.2f));

            spriteBatch.Draw(pixel, new Rectangle(centerX - size / 2, y, size, size), white);
            if (i > 2)
            {
                spriteBatch.Draw(pixel, new Rectangle(centerX - spread, y, size, size), amber);
                spriteBatch.Draw(pixel, new Rectangle(centerX + spread, y, size, size), amber);
            }
        }
    }

    private static void DrawPapi(SpriteBatch spriteBatch, Texture2D pixel, int centerX, int runwayY, int runwayWidth, float glideDeviation, float progress)
    {
        var x = centerX + runwayWidth / 2 + 18;
        var y = runwayY + 18;
        var size = System.Math.Max(5, (int)MathHelper.Lerp(6f, 12f, progress));
        var spacing = size + 4;
        var box = new Color(8, 16, 12);
        var red = new Color(248, 56, 56);
        var white = new Color(248, 248, 224);

        var whiteCount = 2;
        if (glideDeviation > 0.35f)
            whiteCount = 4; // Yüksek: fazla beyaz.
        else if (glideDeviation > 0.12f)
            whiteCount = 3;
        else if (glideDeviation < -0.35f)
            whiteCount = 0; // Alçak: fazla kırmızı.
        else if (glideDeviation < -0.12f)
            whiteCount = 1;

        // PAPI: ideal süzülüşte 2 kırmızı 2 beyaz.
        spriteBatch.Draw(pixel, new Rectangle(x - 4, y - 4, spacing * 4 + 4, size + 8), box);
        for (var i = 0; i < 4; i++)
        {
            var color = i < 4 - whiteCount ? red : white;
            spriteBatch.Draw(pixel, new Rectangle(x + i * spacing, y, size, size), color);
        }
    }

    private static void DrawRadioAltimeter(SpriteBatch spriteBatch, Texture2D pixel, Rectangle screen, int altitude)
    {
        if (altitude > 1000)
            return;

        var box = new Rectangle(screen.Width / 2 - 58, screen.Height - 34, 116, 24);
        var text = new Color(248, 216, 72);

        // Radio altimeter sadece son 1000 ft içinde belirginleşir ve touchdown'da 0 olur.
        spriteBatch.Draw(pixel, box, new Color(8, 16, 20, 230));
        PixelTextRenderer.Draw(spriteBatch, pixel, $"RA {System.Math.Max(0, altitude):0000}", new Vector2(box.X + 16, box.Y + 7), text, 2);
    }

    private static void DrawScaled(SpriteBatch spriteBatch, Texture2D pixel, int x, int y, int width, int height, Color color, float scale, int originX, int originY)
    {
        var scaledX = originX + (int)((x - originX) * scale);
        var scaledY = originY + (int)((y - originY) * scale);
        var scaledWidth = System.Math.Max(1, (int)(width * scale));
        var scaledHeight = System.Math.Max(1, (int)(height * scale));

        spriteBatch.Draw(pixel, new Rectangle(scaledX, scaledY, scaledWidth, scaledHeight), color);
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
