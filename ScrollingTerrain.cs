using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GAME;

public enum TerrainZone
{
    Plains,
    Hills,
    Mountains,
}

public sealed class ScrollingTerrain
{
    // Büyük kareler arazi rengini, küçük çizgiler ise hız hissini verir.
    private const int TileSize = 32;
    private const int DetailTileSize = 16;

    private readonly int _width;
    private readonly int _height;
    private float _worldDistance;
    private float _scroll;
    private float _detailScroll;

    public TerrainZone CurrentZone { get; private set; }
    public int CurrentMora { get; private set; } = 1800;
    public bool IsAirportVisible => _worldDistance < 950f;
    public float WorldDistance => _worldDistance;

    public ScrollingTerrain(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public void Update(GameTime gameTime, float speed)
    {
        var seconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // İki ayrı kaydırma değeri kullanarak zeminde katmanlı hareket hissi oluşturuyoruz.
        _worldDistance += speed * seconds;
        _scroll = (_scroll + speed * seconds) % TileSize;
        _detailScroll = (_detailScroll + speed * 1.8f * seconds) % DetailTileSize;

        CurrentZone = GetZoneAtScreenY(_height / 2);
        CurrentMora = GetMora(CurrentZone);
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        // Ekranı tekrar eden renk bloklarıyla dolduruyoruz.
        for (var y = -TileSize; y < _height + TileSize; y += TileSize)
        {
            for (var x = 0; x < _width; x += TileSize)
            {
                var worldRow = (int)((y + _scroll) / TileSize);
                var zone = GetZoneAtScreenY(y);
                var colorIndex = System.Math.Abs((x / TileSize * 3 + worldRow * 5) % 3);
                var block = new Rectangle(x, y + (int)_scroll, TileSize, TileSize);

                spriteBatch.Draw(pixel, block, GetTerrainColor(zone, colorIndex));
                DrawTerrainPatch(spriteBatch, pixel, block, zone, x / TileSize, worldRow);
            }
        }

        if (IsAirportVisible)
            DrawTakeoffAirport(spriteBatch, pixel);

        DrawSpeedDetails(spriteBatch, pixel);
        DrawSubtleTerrainLines(spriteBatch, pixel);
    }

    private void DrawSpeedDetails(SpriteBatch spriteBatch, Texture2D pixel)
    {
        var detailColor = CurrentZone == TerrainZone.Plains
            ? new Color(120, 152, 96)
            : new Color(160, 128, 88);

        // Küçük dikey izler daha hızlı akarak uçuş hızını güçlendirir.
        for (var y = -DetailTileSize; y < _height + DetailTileSize; y += DetailTileSize * 2)
        {
            var detailY = y + (int)_detailScroll;

            for (var x = 36; x < _width; x += 96)
            {
                var offset = (x / 96) % 2 == 0 ? 0 : DetailTileSize;
                spriteBatch.Draw(pixel, new Rectangle(x, detailY + offset, 4, 18), detailColor);
            }
        }
    }

    private void DrawSubtleTerrainLines(SpriteBatch spriteBatch, Texture2D pixel)
    {
        var lineColor = CurrentZone == TerrainZone.Plains
            ? new Color(112, 136, 88)
            : new Color(128, 96, 64);

        // Kısa ve sabit arazi çizgileri hareket hissi verir, ama ekranda dikkat dağıtmaz.
        for (var y = -32; y < _height + 32; y += 72)
        {
            var lineY = y + (int)(_scroll * 1.2f) % 72;
            spriteBatch.Draw(pixel, new Rectangle(72, lineY, 28, 3), lineColor);
            spriteBatch.Draw(pixel, new Rectangle(_width - 130, lineY + 30, 34, 3), lineColor);

            if (CurrentZone != TerrainZone.Plains)
                spriteBatch.Draw(pixel, new Rectangle(_width / 2 - 18, lineY + 12, 36, 4), lineColor);
        }
    }

    private TerrainZone GetZoneAtScreenY(int screenY)
    {
        var worldY = _worldDistance + screenY;
        var zoneIndex = ((int)(worldY / 420) % 6 + 6) % 6;

        return zoneIndex switch
        {
            3 => TerrainZone.Hills,
            4 => TerrainZone.Mountains,
            _ => TerrainZone.Plains,
        };
    }

    private static int GetMora(TerrainZone zone)
    {
        return zone switch
        {
            TerrainZone.Hills => 4200,
            TerrainZone.Mountains => 7200,
            _ => 1800,
        };
    }

    private static Color GetTerrainColor(TerrainZone zone, int colorIndex)
    {
        return zone switch
        {
            TerrainZone.Hills => colorIndex switch
            {
                0 => new Color(104, 88, 64),
                1 => new Color(122, 102, 72),
                _ => new Color(88, 76, 58),
            },
            TerrainZone.Mountains => colorIndex switch
            {
                0 => new Color(78, 62, 52),
                1 => new Color(96, 74, 56),
                _ => new Color(58, 50, 44),
            },
            _ => colorIndex switch
            {
                0 => new Color(78, 108, 88),
                1 => new Color(92, 122, 92),
                _ => new Color(70, 98, 84),
            },
        };
    }

    private void DrawTakeoffAirport(SpriteBatch spriteBatch, Texture2D pixel)
    {
        var runwayColor = new Color(84, 86, 82);
        var runwayDark = new Color(56, 58, 58);
        var lineColor = new Color(204, 196, 144);
        var terminalColor = new Color(92, 92, 86);
        var apronColor = new Color(104, 106, 100);
        var runway = new Rectangle(_width / 2 - 42, -20, 84, _height + 60);
        var airportScroll = (int)_worldDistance;

        // Kalkış başlangıcında İstanbul pistini hatırlatan basit bir pist ve apron çiziyoruz.
        spriteBatch.Draw(pixel, runway, runwayColor);
        spriteBatch.Draw(pixel, new Rectangle(runway.X, runway.Y, 5, runway.Height), runwayDark);
        spriteBatch.Draw(pixel, new Rectangle(runway.Right - 5, runway.Y, 5, runway.Height), runwayDark);

        for (var y = -8; y < _height + 40; y += 42)
            spriteBatch.Draw(pixel, new Rectangle(_width / 2 - 3, y + (int)(_scroll * 1.6f) % 42, 6, 22), lineColor);

        // Terminal ve apron dünyaya bağlıdır; uçak ilerledikçe ekranda aşağı akar.
        spriteBatch.Draw(pixel, new Rectangle(28, _height - 180 + airportScroll, 112, 58), apronColor);
        spriteBatch.Draw(pixel, new Rectangle(42, _height - 168 + airportScroll, 86, 28), terminalColor);
        spriteBatch.Draw(pixel, new Rectangle(38, _height - 190 + airportScroll, 96, 12), new Color(72, 74, 72));
        spriteBatch.Draw(pixel, new Rectangle(500, _height - 210 + airportScroll, 94, 50), apronColor);
        spriteBatch.Draw(pixel, new Rectangle(512, _height - 198 + airportScroll, 70, 24), terminalColor);
        spriteBatch.Draw(pixel, new Rectangle(508, _height - 226 + airportScroll, 78, 12), new Color(72, 74, 72));

        for (var x = 52; x < 126; x += 18)
            spriteBatch.Draw(pixel, new Rectangle(x, _height - 137 + airportScroll, 10, 4), lineColor);

        for (var x = 522; x < 586; x += 18)
            spriteBatch.Draw(pixel, new Rectangle(x, _height - 171 + airportScroll, 10, 4), lineColor);

        PixelTextRenderer.Draw(spriteBatch, pixel, "35L", new Vector2(_width / 2 - 13, _height - 92 + airportScroll), lineColor, 2);
        PixelTextRenderer.Draw(spriteBatch, pixel, "17R", new Vector2(_width / 2 - 13, 24), lineColor, 2);
    }

    private static void DrawTerrainPatch(SpriteBatch spriteBatch, Texture2D pixel, Rectangle block, TerrainZone zone, int column, int row)
    {
        var pattern = System.Math.Abs(column * 17 + row * 31) % 10;

        if (zone == TerrainZone.Plains)
        {
            DrawPlainsPatch(spriteBatch, pixel, block, pattern);
            return;
        }

        if (zone == TerrainZone.Hills)
        {
            DrawHillPatch(spriteBatch, pixel, block, pattern);
            return;
        }

        DrawMountainPatch(spriteBatch, pixel, block, pattern);
    }

    private static void DrawPlainsPatch(SpriteBatch spriteBatch, Texture2D pixel, Rectangle block, int pattern)
    {
        var grassColor = new Color(112, 144, 88);

        // Küçük çimen yamaları yeşil ovalara piksel-art dokusu verir.
        if (pattern < 6)
            spriteBatch.Draw(pixel, new Rectangle(block.X + 7, block.Y + 9, 12, 5), grassColor);
        if (pattern is 2 or 7)
            spriteBatch.Draw(pixel, new Rectangle(block.X + 21, block.Y + 20, 7, 4), grassColor);
    }

    private static void DrawHillPatch(SpriteBatch spriteBatch, Texture2D pixel, Rectangle block, int pattern)
    {
        var hillLight = new Color(160, 120, 72);
        var rock = new Color(88, 64, 48);

        // Tepeler basit basamaklı şekillerle gösterilir.
        if (pattern < 7)
        {
            spriteBatch.Draw(pixel, new Rectangle(block.X + 4, block.Y + 20, 24, 5), hillLight);
            spriteBatch.Draw(pixel, new Rectangle(block.X + 9, block.Y + 15, 14, 5), hillLight);
        }

        if (pattern is 1 or 8)
            spriteBatch.Draw(pixel, new Rectangle(block.X + 20, block.Y + 8, 5, 5), rock);
    }

    private static void DrawMountainPatch(SpriteBatch spriteBatch, Texture2D pixel, Rectangle block, int pattern)
    {
        var ridge = new Color(112, 80, 56);
        var darkRock = new Color(32, 28, 28);

        // Dağ bölgelerinde koyu kaya ve sırt çizgileri daha sert görünür.
        spriteBatch.Draw(pixel, new Rectangle(block.X + 5, block.Y + 23, 22, 5), ridge);
        spriteBatch.Draw(pixel, new Rectangle(block.X + 10, block.Y + 16, 12, 5), ridge);

        if (pattern < 5)
            spriteBatch.Draw(pixel, new Rectangle(block.X + 15, block.Y + 7, 6, 6), darkRock);
    }
}
