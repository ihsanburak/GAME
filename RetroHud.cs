using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GAME;

public sealed class RetroHud
{
    // HUD ekranın altında sabit kalan bilgi panelidir.
    private readonly int _height;

    public RetroHud(int height)
    {
        _height = height;
    }

    public void Draw(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        Rectangle screen,
        int passengerComfort,
        bool seatBeltOn,
        int score,
        TurbulenceLevel turbulenceLevel,
        int altitude,
        int speed,
        float fuel,
        int mora,
        bool showTerrainWarning,
        bool showMaydayWarning,
        bool showSeatBeltWarning,
        float estimatedFuelAtDestination,
        float finalReserveFuel,
        float plannedFuelAtDestination,
        int plannedCruiseAltitude,
        bool showRotateWarning,
        bool showTcasWarning,
        string tcasCommand,
        string gameOverReason,
        bool isGameOver,
        FlightPhase flightPhase)
    {
        var hudBounds = new Rectangle(0, screen.Height - _height, screen.Width, _height);
        var textColor = new Color(224, 248, 208);
        var warningColor = new Color(248, 216, 72);

        // Panel arka planı ve üst çizgisi retro konsol hissi verir.
        spriteBatch.Draw(pixel, hudBounds, new Color(16, 32, 32));
        spriteBatch.Draw(pixel, new Rectangle(hudBounds.X, hudBounds.Y, hudBounds.Width, 3), new Color(144, 184, 88));

        // Değerler şimdilik geçicidir; sonraki adımlarda gerçek oyun verisine bağlanabilir.
        PixelTextRenderer.Draw(spriteBatch, pixel, $"ALT {altitude:00000}", new Vector2(16, hudBounds.Y + 8), textColor, 2);
        PixelTextRenderer.Draw(spriteBatch, pixel, $"SPD {speed:000}", new Vector2(144, hudBounds.Y + 8), textColor, 2);
        PixelTextRenderer.Draw(spriteBatch, pixel, $"FUEL {fuel:00}", new Vector2(248, hudBounds.Y + 8), textColor, 2);
        PixelTextRenderer.Draw(spriteBatch, pixel, $"SCORE {score:0000}", new Vector2(384, hudBounds.Y + 8), textColor, 2);

        // İkinci satır karar ve yolcu durumu içindir.
        PixelTextRenderer.Draw(spriteBatch, pixel, $"PAX COMFORT {passengerComfort:000}", new Vector2(16, hudBounds.Y + 31), textColor, 2);
        PixelTextRenderer.Draw(spriteBatch, pixel, $"SEAT BELT {(seatBeltOn ? "ON" : "OFF")}", new Vector2(232, hudBounds.Y + 31), textColor, 2);

        var turbulenceText = GetTurbulenceText(turbulenceLevel);
        var turbulenceColor = turbulenceLevel == TurbulenceLevel.None ? textColor : warningColor;

        PixelTextRenderer.Draw(spriteBatch, pixel, $"TURB LEVEL {turbulenceText}", new Vector2(408, hudBounds.Y + 31), turbulenceColor, 2);

        // Üçüncü satır arazi emniyeti ve oyun sonu uyarıları içindir.
        PixelTextRenderer.Draw(spriteBatch, pixel, $"MORA {mora:0000}", new Vector2(16, hudBounds.Y + 54), textColor, 2);
        PixelTextRenderer.Draw(spriteBatch, pixel, $"PHASE {GetPhaseText(flightPhase)}", new Vector2(132, hudBounds.Y + 54), textColor, 2);

        if (showTerrainWarning)
            PixelTextRenderer.Draw(spriteBatch, pixel, "TERRAIN", new Vector2(320, hudBounds.Y + 54), warningColor, 2);

        if (showMaydayWarning)
            PixelTextRenderer.Draw(spriteBatch, pixel, "MAYDAY", new Vector2(320, hudBounds.Y + 54), warningColor, 2);

        if (showSeatBeltWarning)
            PixelTextRenderer.Draw(spriteBatch, pixel, "BELT", new Vector2(320, hudBounds.Y + 54), warningColor, 2);

        if (showTcasWarning)
            PixelTextRenderer.Draw(spriteBatch, pixel, $"TCAS {tcasCommand}", new Vector2(320, hudBounds.Y + 54), warningColor, 2);

        if (isGameOver)
        {
            PixelTextRenderer.Draw(spriteBatch, pixel, "GAME OVER", new Vector2(448, hudBounds.Y + 54), warningColor, 2);
            if (!string.IsNullOrEmpty(gameOverReason))
                PixelTextRenderer.Draw(spriteBatch, pixel, $"CAUSE {gameOverReason}", new Vector2(320, hudBounds.Y + 55), warningColor, 1);
        }

        if (showRotateWarning)
            DrawRotateWarning(spriteBatch, pixel, screen);

        DrawFuelPlan(spriteBatch, pixel, screen, estimatedFuelAtDestination, finalReserveFuel, plannedFuelAtDestination, plannedCruiseAltitude, textColor, warningColor);
    }

    private static void DrawRotateWarning(SpriteBatch spriteBatch, Texture2D pixel, Rectangle screen)
    {
        var box = new Rectangle(screen.Width - 74, 122, 52, 28);
        var red = new Color(232, 64, 48);

        // VR uyarısı pilotun rotate zamanını kaçırmaması için belirgin gösterilir.
        spriteBatch.Draw(pixel, box, new Color(8, 12, 12));
        PixelTextRenderer.Draw(spriteBatch, pixel, "VR", new Vector2(box.X + 14, box.Y + 8), red, 2);
    }

    private static void DrawFuelPlan(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        Rectangle screen,
        float estimatedFuelAtDestination,
        float finalReserveFuel,
        float plannedFuelAtDestination,
        int plannedCruiseAltitude,
        Color textColor,
        Color warningColor)
    {
        var panelBounds = new Rectangle(screen.Width - 126, 58, 116, 54);
        var fuelColor = estimatedFuelAtDestination < finalReserveFuel ? warningColor : textColor;

        // Sağ üstteki küçük panel varış yakıt planını özetler.
        spriteBatch.Draw(pixel, panelBounds, new Color(16, 32, 32, 220));
        PixelTextRenderer.Draw(spriteBatch, pixel, $"DEST {estimatedFuelAtDestination:00}", new Vector2(panelBounds.X + 6, panelBounds.Y + 6), fuelColor, 2);
        PixelTextRenderer.Draw(spriteBatch, pixel, $"RES {finalReserveFuel:00}", new Vector2(panelBounds.X + 6, panelBounds.Y + 20), textColor, 2);
        PixelTextRenderer.Draw(spriteBatch, pixel, $"PLAN {plannedFuelAtDestination:00}", new Vector2(panelBounds.X + 58, panelBounds.Y + 20), textColor, 1);
        PixelTextRenderer.Draw(spriteBatch, pixel, $"CRZ {plannedCruiseAltitude / 100:000}", new Vector2(panelBounds.X + 6, panelBounds.Y + 38), textColor, 1);
    }

    private static string GetTurbulenceText(TurbulenceLevel turbulenceLevel)
    {
        return turbulenceLevel switch
        {
            TurbulenceLevel.Light => "LIGHT",
            TurbulenceLevel.Moderate => "MOD",
            TurbulenceLevel.Severe => "HVY",
            _ => "NONE",
        };
    }

    private static string GetPhaseText(FlightPhase flightPhase)
    {
        return flightPhase switch
        {
            FlightPhase.Takeoff => "TAKEOFF",
            FlightPhase.InitialClimb => "CLIMB",
            FlightPhase.Cruise => "CRUISE",
            FlightPhase.Descent => "DESCENT",
            FlightPhase.Approach => "APPROACH",
            FlightPhase.Landing => "LAND",
            FlightPhase.MissionComplete => "DONE",
            _ => "GAMEOVER",
        };
    }
}
