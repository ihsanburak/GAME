using System;
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
    private const int TileSize    = 32;
    private const int FeatureW    = 112;  // büyük özellik hücresi genişliği
    private const int FeatureH    = 100;  // büyük özellik hücresi yüksekliği
    private const int DetailTile  = 16;

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
        _width  = width;
        _height = height;
    }

    public void Update(GameTime gameTime, float speed)
    {
        var s = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _worldDistance += speed * s;
        _scroll       = (_scroll      + speed       * s) % TileSize;
        _detailScroll = (_detailScroll + speed * 1.8f * s) % DetailTile;
        CurrentZone = GetZoneAtScreenY(_height / 2);
        CurrentMora = GetMora(CurrentZone);
    }

    public void Draw(SpriteBatch sb, Texture2D px, float aircraftAltitude)
    {
        var depthScale = GetAltitudeDepthScale(aircraftAltitude);
        var objectDepthScale = depthScale * GetMoraObjectBoost(aircraftAltitude, CurrentMora);

        // Katman 1: zemin renk bloğu (32×32 grid, çok az varyasyon)
        var firstTileRow = (int)Math.Floor((-_worldDistance - TileSize) / TileSize);
        var lastTileRow = firstTileRow + (_height / TileSize) + 4;

        for (var row = firstTileRow; row <= lastTileRow; row++)
        {
            var screenY = row * TileSize + _worldDistance;
            var zone = GetZoneAtWorldY(row * TileSize);

            for (var x = 0; x < _width; x += TileSize)
            {
                var ci = (int)(((long)(x / TileSize) + row) & 1);
                sb.Draw(px, new Rectangle(x, (int)screenY, TileSize, TileSize), GetBaseColor(zone, ci));
            }
        }

        // Katman 2: büyük organik özellikler.
        // Ağaç, tarla ve tepe gibi zemin detayları dünya koordinatına bağlıdır.
        var firstFeatureRow = (int)Math.Floor((-_worldDistance - FeatureH * 2) / FeatureH);
        var lastFeatureRow = firstFeatureRow + (_height / FeatureH) + 5;

        for (var featureRow = firstFeatureRow; featureRow <= lastFeatureRow; featureRow++)
        {
            var screenCy = (int)(featureRow * FeatureH + _worldDistance + FeatureH / 2);

            // Tek/çift dünya satırı ötelemesi: şekiller yere yapışık kalır.
            var xOff = (featureRow & 1) == 0 ? 0 : FeatureW / 2;

            for (var fx = -1; fx <= _width / FeatureW + 2; fx++)
            {
                var screenCx = fx * FeatureW + xOff + FeatureW / 2;
                if (screenCx < -FeatureW * 2 || screenCx > _width + FeatureW * 2) continue;
                if (screenCy < -FeatureH * 2 || screenCy > _height + FeatureH * 2) continue;

                var seed = (int)(((long)fx * 1511 + (long)featureRow * 2333) & 0x7FFFFFFF);
                var zone = GetZoneAtWorldY(featureRow * FeatureH);
                DrawLargeFeature(sb, px, screenCx, screenCy, zone, seed, objectDepthScale);
            }
        }

        DrawSingleMountain(sb, px, objectDepthScale);

        if (IsAirportVisible)
            DrawTakeoffAirport(sb, px, depthScale);
    }

    // ── Büyük özellik çağrısı ─────────────────────────────────────────────────

    private static void DrawLargeFeature(SpriteBatch sb, Texture2D px, int cx, int cy, TerrainZone zone, int seed, float depthScale)
    {
        switch (zone)
        {
            case TerrainZone.Plains:
                // Ovada orman, tarla ve küçük yerleşim lekeleri kullanıyoruz.
                if (seed % 20 < 7)        DrawForestStand(sb, px, cx, cy, seed, depthScale);
                else if (seed % 20 < 10)  DrawCropField  (sb, px, cx, cy, seed, depthScale);
                else if (seed % 20 < 13)  DrawSettlement (sb, px, cx, cy, seed, depthScale);
                break;

            case TerrainZone.Hills:
                // Tepelerde daha çok tepe, arada küçük dağ evi/tesis grubu görünür.
                if (seed % 10 < 7)        DrawHillBump(sb, px, cx, cy, seed, depthScale);
                else if (seed % 10 == 8)  DrawSettlement(sb, px, cx, cy, seed, depthScale);
                break;

            case TerrainZone.Mountains:
                // Dağ tek bir gezgin nesne olarak ayrıca çizilir.
                break;
        }
    }

    // ── Tek gezgin dağ: tepeden tabana ~10 sn, ekranda hep sadece 1 tane ─────

    private void DrawSingleMountain(SpriteBatch sb, Texture2D px, float depthScale)
    {
        // Tek büyük dağ da dünya koordinatına bağlıdır; ekranda kendi kendine zıplamaz.
        var interval = (float)_height;
        var firstMountainRow = (int)Math.Floor((-_worldDistance - interval) / interval);
        var lastMountainRow = firstMountainRow + 4;

        for (var mIdx = firstMountainRow; mIdx <= lastMountainRow; mIdx++)
        {
            var worldY = mIdx * interval;
            var mScreenY = (int)(worldY + _worldDistance);

            if (mScreenY < -160 || mScreenY > _height + 160)
                continue;

            var seed = (int)(((long)mIdx * 73856093) & 0x7FFFFFFF);
            var mCenterX = 100 + (int)(((long)Math.Abs(mIdx) * 137 + 79) % (_width - 200));

            // Dağı sadece dünya üzerindeki dağ bölgesine denk geliyorsa gösteriyoruz.
            if (GetZoneAtWorldY(worldY) != TerrainZone.Mountains)
                continue;

            DrawMountainPeak(sb, px, mCenterX, mScreenY, seed, depthScale);
        }
    }

    // ── Ova özellikleri ───────────────────────────────────────────────────────

    private static void DrawForestStand(SpriteBatch sb, Texture2D px, int cx, int cy, int seed, float depthScale)
    {
        var count  = 6 + seed % 7;    // 6-12 küçük ağaç
        var spread = (int)((22 + seed % 16) * depthScale);  // yayılma yarıçapı (px)

        for (var i = 0; i < count; i++)
        {
            var angle  = (float)((long)seed * (i * 137 + 31) % 628L) / 100f;
            var radius = (int)  ((long)seed * (i * 97  + 13) % spread);
            var tx = cx + (int)(radius * MathF.Cos(angle)) - 4;
            var ty = cy + (int)(radius * MathF.Sin(angle)) - 4;
            DrawTree(sb, px, tx, ty, seed + i * 17, depthScale);
        }
    }

    private static void DrawCropField(SpriteBatch sb, Texture2D px, int cx, int cy, int seed, float depthScale)
    {
        var w = (int)((44 + seed % 20) * depthScale);
        var h = (int)((28 + seed % 12) * depthScale);
        var crop1 = new Color(106, 142, 70);
        var crop2 = new Color(124, 156, 76);
        sb.Draw(px, new Rectangle(cx - w / 2, cy - h / 2, w, h), crop1);
        for (var row = 0; row < 4; row++)
            sb.Draw(px, new Rectangle(cx - w / 2 + 2, cy - h / 2 + 3 + row * (h / 4), w - 4, 2), crop2);
    }

    private static void DrawSettlement(SpriteBatch sb, Texture2D px, int cx, int cy, int seed, float depthScale)
    {
        var shadow = new Color(22, 32, 28, 95);
        var road = new Color(82, 86, 74, 150);
        var wall = new Color(126, 130, 112);
        var roof = new Color(84, 78, 68);
        var light = new Color(190, 184, 126);
        var buildingCount = 2 + seed % 3;

        // Küçük bina grupları da dünya koordinatına bağlıdır; araziyle birlikte aşağı akar.
        DrawScaledRect(sb, px, new Rectangle(cx - 24, cy + 4, 48, 4), road, depthScale);

        for (var i = 0; i < buildingCount; i++)
        {
            var ox = ((seed / (i + 2)) % 36) - 18;
            var oy = ((seed / (i + 5)) % 22) - 11;
            var w = 14 + (seed + i * 7) % 12;
            var h = 10 + (seed + i * 5) % 8;
            var x = cx + ox - w / 2;
            var y = cy + oy - h / 2;

            DrawScaledRect(sb, px, new Rectangle(x + 2, y + h, w, 4), shadow, depthScale);
            DrawScaledRect(sb, px, new Rectangle(x, y, w, h), wall, depthScale);
            DrawScaledRect(sb, px, new Rectangle(x - 2, y - 4, w + 4, 5), roof, depthScale);

            if (depthScale > 0.55f)
                DrawScaledRect(sb, px, new Rectangle(x + w / 2 - 2, y + h / 2, 4, 3), light, depthScale);
        }
    }

    // ── Tepe özellikleri ──────────────────────────────────────────────────────

    private static void DrawHillBump(SpriteBatch sb, Texture2D px, int cx, int cy, int seed, float depthScale)
    {
        var hw = (int)((20 + seed % 13) * depthScale);  // yarı-genişlik
        var hh = (int)((11 + seed % 8) * depthScale);   // yarı-yükseklik

        DrawEllipse(sb, px, cx,     cy + 2,      hw + 4, hh + 3, new Color(38, 42, 24, 90));       // yumuşak gölge
        DrawEllipse(sb, px, cx,     cy,          hw,     hh,     new Color(86, 104, 55));          // zemin
        DrawEllipse(sb, px, cx - 2, cy - hh / 4, hw * 3 / 4, hh * 2 / 3, new Color(118, 116, 65)); // orta
        DrawEllipse(sb, px, cx - 1, cy - hh / 3, hw / 2, hh / 3,          new Color(150, 136, 76)); // üst

        // Tepe üzerinde seyrek ağaç
        if (seed % 3 == 0)
        {
            DrawTree(sb, px, cx - 5, cy - hh / 2 - 3, seed, depthScale);
            if (seed % 5 == 0) DrawTree(sb, px, cx + 6, cy - hh / 4, seed + 3, depthScale);
        }
    }

    // ── Dağ zirveleri ─────────────────────────────────────────────────────────

    private static void DrawMountainPeak(SpriteBatch sb, Texture2D px, int cx, int cy, int seed, float depthScale)
    {
        // Dağ şekilleri büyük engel sprite'ı değil, zemine ait yükseklik lekesidir.
        var sizeClass = seed % 3;
        var hw = (int)((38 + sizeClass * 10 + seed % 10) * depthScale);  // yarı-genişlik
        var hh = (int)((25 + sizeClass * 7 + seed % 7) * depthScale);    // yarı-yükseklik

        // Küçük rastgele konum kayması arazi desenini daha doğal yapar.
        var ox = (seed / 100) % 22 - 11;
        var oy = (seed / 900) % 16 - 8;
        cx += ox; cy += oy;

        DrawEllipse(sb, px, cx,      cy + 3,           hw + 8, hh + 5,       new Color(24, 26, 30, 100)); // yumuşak gölge
        DrawEllipse(sb, px, cx,      cy,               hw,     hh,           new Color(58, 62, 66));      // kaya tabanı
        DrawEllipse(sb, px, cx - 3,  cy - hh / 5,      hw * 3 / 4, hh * 2 / 3, new Color(82, 78, 68));   // orta kaya
        DrawEllipse(sb, px, cx - 2,  cy - hh * 2 / 5,  hw / 2, hh / 2,       new Color(110, 100, 82));    // üst kaya
        DrawEllipse(sb, px, cx,      cy - hh * 5 / 9,  hw * 5 / 12, hh / 3,  new Color(176, 196, 210));   // kar bölgesi
        DrawEllipse(sb, px, cx + 1,  cy - hh * 2 / 3,  hw / 3, hh / 4,       new Color(230, 238, 246));   // kar başlığı

        // Bazı dağlarda küçük yan zirve olur; kalabalık görünmez.
        var peakCount = seed % 2;
        for (var p = 0; p < peakCount; p++)
        {
            var sign  = (p == 0 ? 1 : -1) * (seed % 2 == 0 ? 1 : -1);
            var ox2   = sign * (hw * 3 / 5 + (seed / 300) % 10);
            var oy2   = (seed / 500) % 10 - 5;
            var hw2   = hw * 5 / 9;
            var hh2   = hh * 5 / 9;
            DrawEllipse(sb, px, cx + ox2,       cy + oy2 + 2,         hw2 + 4, hh2 + 3,    new Color(24, 26, 30, 80));
            DrawEllipse(sb, px, cx + ox2,       cy + oy2,             hw2,     hh2,         new Color(58, 62, 66));
            DrawEllipse(sb, px, cx + ox2 - 2,   cy + oy2 - hh2 / 4,   hw2 / 2, hh2 * 2 / 3, new Color(82, 78, 68));
            DrawEllipse(sb, px, cx + ox2,       cy + oy2 - hh2 / 2,   hw2 / 3, hh2 / 3,    new Color(218, 230, 244));
        }
    }

    // ── Yardımcılar ───────────────────────────────────────────────────────────

    // Dikdörtgen kesimleri üst üste koyarak elips yaklaşımı.
    private static void DrawEllipse(SpriteBatch sb, Texture2D px, int cx, int cy, int hw, int hh, Color c)
    {
        if (hw < 3 || hh < 3) return;
        sb.Draw(px, new Rectangle(cx - hw / 2,       cy - hh,        hw,          Math.Max(1, hh / 2)), c);
        sb.Draw(px, new Rectangle(cx - hw * 3 / 4,   cy - hh / 2,    hw * 3 / 2,  Math.Max(1, hh / 4)), c);
        sb.Draw(px, new Rectangle(cx - hw,            cy - hh / 4,    hw * 2,      Math.Max(1, hh / 2)), c);
        sb.Draw(px, new Rectangle(cx - hw * 3 / 4,   cy + hh / 4,    hw * 3 / 2,  Math.Max(1, hh / 4)), c);
        sb.Draw(px, new Rectangle(cx - hw / 2,        cy + hh / 2,    hw,          Math.Max(1, hh / 2)), c);
    }

    // Üstten bakış ağacı: küçük, sade ve zemini boğmayan piksel-art leke.
    private static void DrawTree(SpriteBatch sb, Texture2D px, int x, int y, int seed, float depthScale)
    {
        var size = Math.Max(4, (int)(8 * depthScale));
        var leaf = Math.Max(3, size - 2);
        var treeColor = (seed % 3) switch
        {
            0 => new Color(32, 74, 36),
            1 => new Color(38, 84, 42),
            _ => new Color(44, 92, 48),
        };

        var shadowColor = new Color(24, 50, 28, 90);
        var lightColor = new Color(72, 116, 58);

        sb.Draw(px, new Rectangle(x + 1, y + leaf, leaf, 2), shadowColor);
        sb.Draw(px, new Rectangle(x + 1, y + 1, leaf, leaf), treeColor);
        sb.Draw(px, new Rectangle(x, y + leaf / 2, size, 2), treeColor);
        sb.Draw(px, new Rectangle(x + 2, y + 2, Math.Max(2, leaf / 3), Math.Max(2, leaf / 3)), lightColor);
    }

    // ── Renk ve bölge ─────────────────────────────────────────────────────────

    private TerrainZone GetZoneAtScreenY(int screenY)
    {
        return GetZoneAtWorldY(screenY - _worldDistance);
    }

    private static TerrainZone GetZoneAtWorldY(float worldY)
    {
        var zoneIndex = ((int)(worldY / 420) % 6 + 6) % 6;
        return zoneIndex switch
        {
            3 => TerrainZone.Hills,
            4 => TerrainZone.Mountains,
            _ => TerrainZone.Plains,
        };
    }

    private static int GetMora(TerrainZone zone) => zone switch
    {
        TerrainZone.Hills     => 4200,
        TerrainZone.Mountains => 7200,
        _                     => 1800,
    };

    // Zemin için yalnızca 2 yakın renk tonu; tile çizgisi neredeyse görünmez.
    private static Color GetBaseColor(TerrainZone zone, int ci) => zone switch
    {
        TerrainZone.Hills      => ci == 0 ? new Color(78, 94, 52)  : new Color(74, 90, 50),
        TerrainZone.Mountains  => ci == 0 ? new Color(55, 59, 63)  : new Color(52, 56, 60),
        _                      => ci == 0 ? new Color(72, 110, 61) : new Color(70, 106, 59),
    };

    // ── Hareket çizgileri ─────────────────────────────────────────────────────

    private void DrawMotionLines(SpriteBatch sb, Texture2D px)
    {
        var c = CurrentZone switch
        {
            TerrainZone.Plains => new Color(50, 78, 38, 80),
            TerrainZone.Hills  => new Color(62, 58, 30, 90),
            _                  => new Color(34, 36, 42, 110),
        };
        for (var y = -DetailTile; y < _height + DetailTile; y += DetailTile * 2)
        {
            var dy = y + (int)_detailScroll;
            for (var x = 68; x < _width; x += 160)
            {
                var off = (x / 96) % 2 == 0 ? 0 : DetailTile;
                sb.Draw(px, new Rectangle(x, dy + off, 2, 8), c);
            }
        }
    }

    // ── Kalkış havalimanı ────────────────────────────────────────────────────

    private void DrawTakeoffAirport(SpriteBatch sb, Texture2D px, float depthScale)
    {
        var runwayColor   = new Color(84, 86, 82);
        var runwayDark    = new Color(56, 58, 58);
        var lineColor     = new Color(204, 196, 144);
        var terminalColor = new Color(92, 92, 86);
        var apronColor    = new Color(104, 106, 100);
        var runwayWidth   = Math.Max(30, (int)(84 * depthScale));
        var edgeWidth     = Math.Max(2, (int)(5 * depthScale));
        var lineWidth     = Math.Max(2, (int)(6 * depthScale));
        var lineHeight    = Math.Max(8, (int)(22 * depthScale));
        var lineStep      = Math.Max(24, (int)(42 * depthScale));
        var runway        = new Rectangle(_width / 2 - runwayWidth / 2, -20, runwayWidth, _height + 60);
        var airportScroll = (int)_worldDistance;

        sb.Draw(px, runway, runwayColor);
        sb.Draw(px, new Rectangle(runway.X, runway.Y, edgeWidth, runway.Height), runwayDark);
        sb.Draw(px, new Rectangle(runway.Right - edgeWidth, runway.Y, edgeWidth, runway.Height), runwayDark);

        for (var y = -8; y < _height + 40; y += lineStep)
            sb.Draw(px, new Rectangle(_width / 2 - lineWidth / 2, y + (int)(_scroll * 1.6f) % lineStep, lineWidth, lineHeight), lineColor);

        DrawScaledRect(sb, px, new Rectangle(28,  _height - 180 + airportScroll, 112, 58), apronColor, depthScale);
        DrawScaledRect(sb, px, new Rectangle(42,  _height - 168 + airportScroll, 86,  28), terminalColor, depthScale);
        DrawScaledRect(sb, px, new Rectangle(38,  _height - 190 + airportScroll, 96,  12), new Color(72, 74, 72), depthScale);
        DrawScaledRect(sb, px, new Rectangle(500, _height - 210 + airportScroll, 94,  50), apronColor, depthScale);
        DrawScaledRect(sb, px, new Rectangle(512, _height - 198 + airportScroll, 70,  24), terminalColor, depthScale);
        DrawScaledRect(sb, px, new Rectangle(508, _height - 226 + airportScroll, 78,  12), new Color(72, 74, 72), depthScale);

        for (var x = 52;  x < 126; x += 18)
            sb.Draw(px, new Rectangle(x, _height - 137 + airportScroll, 10, 4), lineColor);
        for (var x = 522; x < 586; x += 18)
            sb.Draw(px, new Rectangle(x, _height - 171 + airportScroll, 10, 4), lineColor);

        var runwayLabelScale = depthScale > 0.72f ? 2 : 1;
        PixelTextRenderer.Draw(sb, px, "35L", new Vector2(_width / 2 - 13, _height - 92 + airportScroll), lineColor, runwayLabelScale);
        PixelTextRenderer.Draw(sb, px, "17R", new Vector2(_width / 2 - 13, 24), lineColor, runwayLabelScale);
    }

    private static float GetAltitudeDepthScale(float altitude)
    {
        // İlk 1000 ft içinde yer nesneleri küçülür; böylece kalkışta yükselme hissi oluşur.
        var lowAltitudeScale = MathHelper.Lerp(1f, 0.58f, MathHelper.Clamp((altitude - 320f) / 680f, 0f, 1f));
        var cruiseScale = MathHelper.Lerp(0.58f, 0.46f, MathHelper.Clamp((altitude - 1000f) / 25000f, 0f, 1f));
        return altitude <= 1000f ? lowAltitudeScale : cruiseScale;
    }

    private static float GetMoraObjectBoost(float altitude, int mora)
    {
        // MORA'ya yaklaştıkça zemin nesneleri büyür; MORA altına inince yakın yer hissi belirginleşir.
        var aboveMora = MathHelper.Clamp((mora + 2200f - altitude) / 2200f, 0f, 1f);
        var belowMora = MathHelper.Clamp((mora - altitude) / 1800f, 0f, 1f);
        return 1f + aboveMora * 0.18f + belowMora * 0.42f;
    }

    private static void DrawScaledRect(SpriteBatch sb, Texture2D px, Rectangle rect, Color color, float scale)
    {
        var center = rect.Center;
        var width = Math.Max(1, (int)(rect.Width * scale));
        var height = Math.Max(1, (int)(rect.Height * scale));
        sb.Draw(px, new Rectangle(center.X - width / 2, center.Y - height / 2, width, height), color);
    }
}
