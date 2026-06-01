using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GAME;

public enum CloudType
{
    WhiteCloud,
    GreyCloud,
    DarkCloud,
    StormCloud,
}

public enum TurbulenceLevel
{
    None,
    Light,
    Moderate,
    Severe,
}

public readonly struct CloudInteraction
{
    public CloudInteraction(TurbulenceLevel level, bool hasNearbyCloud, int lowerAltitude, int upperAltitude)
    {
        Level = level;
        HasNearbyCloud = hasNearbyCloud;
        LowerAltitude = lowerAltitude;
        UpperAltitude = upperAltitude;
    }

    public TurbulenceLevel Level { get; }
    public bool HasNearbyCloud { get; }
    public int LowerAltitude { get; }
    public int UpperAltitude { get; }
}

public sealed class CloudField
{
    // Bulutları birkaç piksel-art kabarcık parçasıyla çiziyoruz.
    private const int CloudCount = 9;
    private readonly Cloud[] _clouds = new Cloud[CloudCount];
    private readonly Random _random = new(17);
    private readonly int _width;
    private readonly int _height;

    public CloudField(int width, int height)
    {
        _width = width;
        _height = height;

        // Bulutları ekranın üst tarafına aralıklı şekilde yerleştiriyoruz.
        for (var i = 0; i < _clouds.Length; i++)
        {
            _clouds[i] = CreateCloud(-i * 70 - 30);
        }
    }

    public void Update(GameTime gameTime, float worldSpeed)
    {
        var seconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Her bulut kendi hız çarpanıyla aşağı doğru akar.
        for (var i = 0; i < _clouds.Length; i++)
        {
            var cloud = _clouds[i];
            cloud.Position += new Vector2(0f, worldSpeed * cloud.SpeedScale * seconds);
            cloud.LightningTimer = System.MathF.Max(0f, cloud.LightningTimer - seconds);

            // CB bulutlarında ara sıra kısa bir şimşek parlaması gösteriyoruz.
            if (cloud.Type == CloudType.StormCloud && cloud.LightningTimer <= 0f && _random.Next(1000) < 8)
                cloud.LightningTimer = 0.14f;

            _clouds[i] = cloud;

            // Ekranın altından çıkan bulut tekrar üst taraftan oyuna girer.
            if (_clouds[i].Position.Y > _height + cloud.Size.Y)
                _clouds[i] = CreateCloud(-_random.Next(60, 220));
        }
    }

    public CloudInteraction GetCloudInteraction(Rectangle aircraftBounds, float aircraftAltitude)
    {
        var strongestLevel = TurbulenceLevel.None;
        var hasNearbyCloud = false;
        var lowerAltitude = 0;
        var upperAltitude = 0;
        var closestDistance = float.MaxValue;

        // Uçağın yakınındaki bulutun irtifa bandını HUD'da göstermek için takip ediyoruz.
        foreach (var cloud in _clouds)
        {
            var nearBounds = cloud.Bounds;
            nearBounds.Inflate(20, 20);

            if (!nearBounds.Intersects(aircraftBounds))
                continue;

            hasNearbyCloud = true;

            var distance = Vector2.Distance(GetCenter(cloud.Bounds), GetCenter(aircraftBounds));
            if (distance < closestDistance)
            {
                closestDistance = distance;
                lowerAltitude = cloud.LowerAltitude;
                upperAltitude = cloud.UpperAltitude;
            }

            if (!cloud.Bounds.Intersects(aircraftBounds))
                continue;

            var level = GetAltitudeAdjustedLevel(cloud, aircraftAltitude);

            if (level > strongestLevel)
                strongestLevel = level;
        }

        return new CloudInteraction(strongestLevel, hasNearbyCloud, lowerAltitude, upperAltitude);
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, float aircraftAltitude, float verticalDepthCue)
    {
        // Her bulut tipi aynı temel şekli kullanır, renk ve boyutla farklılaşır.
        foreach (var cloud in _clouds)
        {
            var body = GetVisualBounds(cloud, aircraftAltitude, verticalDepthCue);
            var colors = GetCloudColors(cloud, aircraftAltitude);

            DrawPixelCloud(spriteBatch, pixel, body, colors, cloud.Type);

            if (cloud.LightningTimer > 0f)
                DrawLightning(spriteBatch, pixel, body);

            DrawFlightLevelLabel(spriteBatch, pixel, body, cloud);
        }
    }

    private Cloud CreateCloud(int y)
    {
        // Rastgele boyut ve hız, bulutların tekdüze görünmesini engeller.
        var type = CreateCloudType();
        var size = CreateCloudSize(type);
        var speedScale = CreateCloudSpeed(type);
        var altitudeBand = CreateAltitudeBand(type);
        var width = size.X;
        var x = _random.Next(12, Math.Max(13, _width - width - 12));

        return new Cloud(new Vector2(x, y), size, speedScale, type, altitudeBand.X, altitudeBand.Y);
    }

    private CloudType CreateCloudType()
    {
        var roll = _random.Next(100);

        if (roll < 45)
            return CloudType.WhiteCloud;
        if (roll < 72)
            return CloudType.GreyCloud;
        if (roll < 92)
            return CloudType.DarkCloud;

        return CloudType.StormCloud;
    }

    private Point CreateCloudSize(CloudType type)
    {
        return type switch
        {
            CloudType.WhiteCloud => new Point(_random.Next(44, 82), _random.Next(22, 38)),
            CloudType.GreyCloud => new Point(_random.Next(58, 98), _random.Next(28, 46)),
            CloudType.DarkCloud => new Point(_random.Next(76, 122), _random.Next(36, 58)),
            CloudType.StormCloud => new Point(_random.Next(96, 148), _random.Next(48, 72)),
            _ => new Point(56, 32),
        };
    }

    private float CreateCloudSpeed(CloudType type)
    {
        return type switch
        {
            CloudType.WhiteCloud => _random.Next(90, 125) / 100f,
            CloudType.GreyCloud => _random.Next(110, 145) / 100f,
            CloudType.DarkCloud => _random.Next(125, 165) / 100f,
            CloudType.StormCloud => _random.Next(140, 190) / 100f,
            _ => 1f,
        };
    }

    private Point CreateAltitudeBand(CloudType type)
    {
        var lowerFlightLevel = type switch
        {
            CloudType.WhiteCloud => _random.Next(30, 110),
            CloudType.GreyCloud => _random.Next(50, 170),
            CloudType.DarkCloud => _random.Next(80, 230),
            CloudType.StormCloud => _random.Next(100, 280),
            _ => 50,
        };

        var depthFlightLevel = type switch
        {
            CloudType.WhiteCloud => _random.Next(10, 30),
            CloudType.GreyCloud => _random.Next(20, 50),
            CloudType.DarkCloud => _random.Next(40, 90),
            CloudType.StormCloud => _random.Next(80, 180),
            _ => 20,
        };

        var upperFlightLevel = Math.Min(lowerFlightLevel + depthFlightLevel, 300);

        return new Point(lowerFlightLevel * 100, upperFlightLevel * 100);
    }

    private TurbulenceLevel GetAltitudeAdjustedLevel(Cloud cloud, float aircraftAltitude)
    {
        var baseLevel = GetTurbulenceLevel(cloud.Type);

        if (cloud.Type is CloudType.DarkCloud or CloudType.StormCloud)
            return GetConvectiveCloudLevel(cloud, aircraftAltitude);

        if (aircraftAltitude >= cloud.LowerAltitude && aircraftAltitude <= cloud.UpperAltitude)
            return baseLevel;

        // Bulutun irtifa bandı dışında kalınca etki arcade tempo için hafifletilir.
        return baseLevel switch
        {
            TurbulenceLevel.Severe => TurbulenceLevel.Light,
            TurbulenceLevel.Moderate => TurbulenceLevel.Light,
            _ => TurbulenceLevel.None,
        };
    }

    private static TurbulenceLevel GetConvectiveCloudLevel(Cloud cloud, float aircraftAltitude)
    {
        var clearanceAboveTop = aircraftAltitude - cloud.UpperAltitude;

        // Kara bulut ve CB için bulut tepesinin en az 5000 ft üstü güvenli kabul edilir.
        if (clearanceAboveTop >= 5000f)
            return TurbulenceLevel.None;

        // 4000-5000 ft arası hafif, 3000-4000 ft arası orta türbülans verir.
        if (clearanceAboveTop >= 4000f)
            return TurbulenceLevel.Light;
        if (clearanceAboveTop >= 3000f)
            return TurbulenceLevel.Moderate;

        // 3000 ft altı veya bulutun içi/altı ağır türbülans gibi davranır.
        return TurbulenceLevel.Severe;
    }

    private TurbulenceLevel GetTurbulenceLevel(CloudType type)
    {
        return type switch
        {
            CloudType.GreyCloud => TurbulenceLevel.Light,
            CloudType.DarkCloud => TurbulenceLevel.Moderate,
            CloudType.StormCloud => TurbulenceLevel.Severe,
            _ => TurbulenceLevel.None,
        };
    }

    private CloudColors GetCloudColors(Cloud cloud, float aircraftAltitude)
    {
        var colors = cloud.Type switch
        {
            CloudType.GreyCloud => new CloudColors(new Color(196, 204, 196), new Color(156, 168, 164), new Color(108, 128, 128), new Color(228, 236, 224)),
            CloudType.DarkCloud => new CloudColors(new Color(132, 144, 152), new Color(88, 100, 116), new Color(48, 58, 76), new Color(164, 174, 180)),
            CloudType.StormCloud => new CloudColors(new Color(98, 108, 136), new Color(58, 68, 104), new Color(24, 32, 62), new Color(130, 142, 166)),
            _ => new CloudColors(new Color(236, 248, 232), new Color(204, 230, 216), new Color(154, 196, 204), new Color(252, 252, 236)),
        };

        var altitudeCenter = (cloud.LowerAltitude + cloud.UpperAltitude) / 2f;
        var brightness = MathHelper.Clamp(1f + (altitudeCenter - aircraftAltitude) / 12000f, 0.68f, 1.2f);

        // Alttaki bulutlar daha soluk, üstteki bulutlar biraz daha parlak görünür.
        return new CloudColors(
            ScaleColor(colors.Light, brightness),
            ScaleColor(colors.Mid, brightness),
            ScaleColor(colors.Shade, brightness),
            ScaleColor(colors.Highlight, brightness));
    }

    private static Vector2 GetCenter(Rectangle rectangle)
    {
        return new Vector2(rectangle.Center.X, rectangle.Center.Y);
    }

    private static Rectangle GetVisualBounds(Cloud cloud, float aircraftAltitude, float verticalDepthCue)
    {
        var altitudeCenter = (cloud.LowerAltitude + cloud.UpperAltitude) / 2f;
        var altitudeDifference = altitudeCenter - aircraftAltitude;
        var altitudeLayerScale = MathHelper.Lerp(1f, 0.78f, MathHelper.Clamp(aircraftAltitude / 30000f, 0f, 1f));
        var relativeScale = 1f + altitudeDifference / 14000f + verticalDepthCue * 0.06f;
        var scale = MathHelper.Clamp(relativeScale * altitudeLayerScale, 0.58f, 1.18f);
        var width = (int)(cloud.Size.X * scale);
        var height = (int)(cloud.Size.Y * scale);
        var center = cloud.Bounds.Center;

        // Uçağın irtifası ve tırmanış/alçalış hissi bulutu hafifçe büyütüp küçültür.
        return new Rectangle(center.X - width / 2, center.Y - height / 2, width, height);
    }

    private static void DrawCloudShadow(SpriteBatch spriteBatch, Texture2D pixel, Rectangle body)
    {
        var shadowColor = new Color(36, 48, 52, 90);

        // Büyük bulutların altındaki yumuşak taban, bulutu zeminden ayırır.
        spriteBatch.Draw(pixel, new Rectangle(body.X + body.Width / 8, body.Bottom - 4, body.Width * 3 / 4, 5), shadowColor);
    }

    private static void DrawPixelCloud(SpriteBatch spriteBatch, Texture2D pixel, Rectangle body, CloudColors colors, CloudType type)
    {
        var isLarge = body.Width > 84;
        var isStorm = type is CloudType.DarkCloud or CloudType.StormCloud;

        if (isLarge || isStorm)
            DrawCloudShadow(spriteBatch, pixel, body);

        var baseY = body.Y + body.Height * 5 / 8;
        var left = body.X + body.Width / 10;
        var right = body.Right - body.Width / 10;

        // Alt taban, buluta yatay ve pofuduk bir siluet verir.
        DrawPixelEllipse(spriteBatch, pixel, body.X + body.Width / 2, baseY, body.Width / 2, body.Height / 4, colors.Mid);
        DrawPixelEllipse(spriteBatch, pixel, body.X + body.Width / 3, baseY + body.Height / 10, body.Width / 3, body.Height / 5, colors.Shade);
        DrawPixelEllipse(spriteBatch, pixel, body.X + body.Width * 2 / 3, baseY + body.Height / 12, body.Width / 3, body.Height / 5, colors.Shade);

        // Üst kabarcıklar, kare bulut hissini kırar.
        DrawPixelEllipse(spriteBatch, pixel, left, body.Y + body.Height / 2, body.Width / 4, body.Height / 3, colors.Light);
        DrawPixelEllipse(spriteBatch, pixel, body.X + body.Width / 3, body.Y + body.Height / 3, body.Width / 4, body.Height / 2, colors.Light);
        DrawPixelEllipse(spriteBatch, pixel, body.X + body.Width / 2, body.Y + body.Height / 4, body.Width / 3, body.Height / 2, colors.Light);
        DrawPixelEllipse(spriteBatch, pixel, body.X + body.Width * 2 / 3, body.Y + body.Height / 3, body.Width / 4, body.Height / 2, colors.Light);
        DrawPixelEllipse(spriteBatch, pixel, right, body.Y + body.Height / 2, body.Width / 5, body.Height / 3, colors.Light);

        // Küçük parlaklık parçaları, retro hacim hissi verir.
        spriteBatch.Draw(pixel, new Rectangle(body.X + body.Width / 3, body.Y + body.Height / 4, body.Width / 5, 3), colors.Highlight);
        spriteBatch.Draw(pixel, new Rectangle(body.X + body.Width / 2, body.Y + body.Height / 5, body.Width / 6, 3), colors.Highlight);

        if (isStorm)
            spriteBatch.Draw(pixel, new Rectangle(body.X + body.Width / 5, body.Bottom - body.Height / 4, body.Width * 3 / 5, body.Height / 6), colors.Shade);
    }

    private static void DrawPixelEllipse(SpriteBatch spriteBatch, Texture2D pixel, int centerX, int centerY, int halfWidth, int halfHeight, Color color)
    {
        if (halfWidth < 4 || halfHeight < 3)
            return;

        // Gerçek daire yerine basamaklı dikdörtgenler kullanarak piksel-art oval çiziyoruz.
        spriteBatch.Draw(pixel, new Rectangle(centerX - halfWidth / 2, centerY - halfHeight, halfWidth, Math.Max(2, halfHeight / 3)), color);
        spriteBatch.Draw(pixel, new Rectangle(centerX - halfWidth * 3 / 4, centerY - halfHeight * 2 / 3, halfWidth * 3 / 2, Math.Max(2, halfHeight / 3)), color);
        spriteBatch.Draw(pixel, new Rectangle(centerX - halfWidth, centerY - halfHeight / 3, halfWidth * 2, Math.Max(3, halfHeight * 2 / 3)), color);
        spriteBatch.Draw(pixel, new Rectangle(centerX - halfWidth * 3 / 4, centerY + halfHeight / 3, halfWidth * 3 / 2, Math.Max(2, halfHeight / 3)), color);
        spriteBatch.Draw(pixel, new Rectangle(centerX - halfWidth / 2, centerY + halfHeight * 2 / 3, halfWidth, Math.Max(2, halfHeight / 3)), color);
    }

    private static void DrawLightning(SpriteBatch spriteBatch, Texture2D pixel, Rectangle body)
    {
        var flashColor = new Color(248, 232, 88);
        var x = body.X + body.Width / 2;
        var y = body.Y + body.Height / 2;

        // Şimşek yalnızca birkaç küçük dikdörtgenden oluşan geçici bir efekttir.
        spriteBatch.Draw(pixel, new Rectangle(x, y, 4, 12), flashColor);
        spriteBatch.Draw(pixel, new Rectangle(x - 6, y + 10, 10, 4), flashColor);
        spriteBatch.Draw(pixel, new Rectangle(x - 8, y + 14, 4, 12), flashColor);
    }

    private static void DrawFlightLevelLabel(SpriteBatch spriteBatch, Texture2D pixel, Rectangle body, Cloud cloud)
    {
        var labelColor = new Color(248, 232, 120);
        var lowerFlightLevel = cloud.LowerAltitude / 100;
        var upperFlightLevel = cloud.UpperAltitude / 100;
        var label = $"FL{lowerFlightLevel}/{upperFlightLevel}";
        var labelX = body.X + body.Width / 2 - label.Length * 4;
        var labelY = body.Y - 12;

        // Bulutun irtifa bandı artık HUD yerine bulutun üstünde küçük yazı olarak görünür.
        PixelTextRenderer.Draw(spriteBatch, pixel, label, new Vector2(labelX, labelY), labelColor, 1);
    }

    private static Color ScaleColor(Color color, float scale)
    {
        return new Color(
            (byte)MathHelper.Clamp(color.R * scale, 0f, 255f),
            (byte)MathHelper.Clamp(color.G * scale, 0f, 255f),
            (byte)MathHelper.Clamp(color.B * scale, 0f, 255f),
            color.A);
    }

    private struct Cloud
    {
        public Cloud(Vector2 position, Point size, float speedScale, CloudType type, int lowerAltitude, int upperAltitude)
        {
            Position = position;
            Size = size;
            SpeedScale = speedScale;
            Type = type;
            LowerAltitude = lowerAltitude;
            UpperAltitude = upperAltitude;
            LightningTimer = 0f;
        }

        public Vector2 Position { get; set; }
        public Point Size { get; }
        public float SpeedScale { get; }
        public CloudType Type { get; }
        public int LowerAltitude { get; }
        public int UpperAltitude { get; }
        public float LightningTimer { get; set; }

        public Rectangle Bounds => new((int)Position.X, (int)Position.Y, Size.X, Size.Y);
    }

    private readonly struct CloudColors
    {
        public CloudColors(Color light, Color mid, Color shade, Color highlight)
        {
            Light = light;
            Mid = mid;
            Shade = shade;
            Highlight = highlight;
        }

        public Color Light { get; }
        public Color Mid { get; }
        public Color Shade { get; }
        public Color Highlight { get; }
    }
}
