using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GAME;

public readonly struct TrafficInteraction
{
    public TrafficInteraction(bool inWake, bool collision, bool tcasWarning, string tcasCommand, int altitude, bool nearPass, bool farPass)
    {
        InWake = inWake;
        Collision = collision;
        TcasWarning = tcasWarning;
        TcasCommand = tcasCommand;
        Altitude = altitude;
        NearPass = nearPass;
        FarPass = farPass;
    }

    public bool InWake { get; }
    public bool Collision { get; }
    public bool TcasWarning { get; }
    public string TcasCommand { get; }
    public int Altitude { get; }
    public bool NearPass { get; }
    public bool FarPass { get; }
}

public sealed class TrafficField
{
    private const int MaxTraffic = 5;
    private readonly Random _random = new(41);
    private readonly TrafficAircraft[] _traffic = new TrafficAircraft[MaxTraffic];
    private readonly int _width;
    private readonly int _height;
    private float _singleTimer = 3f;
    private float _pairTimer = 10f;
    private float _conflictTimer = 15f;
    private float _wakeTime;
    private bool _nextOvertakingTraffic;

    public TrafficField(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public void Update(GameTime gameTime, float playerAltitude)
    {
        var seconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _wakeTime += seconds;
        UpdateTimers(seconds, playerAltitude);
        UpdateTrafficPositions(seconds);
    }

    public TrafficInteraction GetInteraction(Rectangle aircraftBounds, float playerAltitude)
    {
        var inWake = false;
        var collision = false;
        var tcasWarning = false;
        var tcasCommand = "";
        var nearestAltitude = 0;
        var nearestDistance = float.MaxValue;
        var nearPass = false;
        var farPass = false;

        foreach (var traffic in _traffic)
        {
            if (!traffic.Active)
                continue;

            var altitudeDifference = MathF.Abs(playerAltitude - traffic.Altitude);
            var distance = Vector2.Distance(GetCenter(aircraftBounds), GetCenter(traffic.Bounds));

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestAltitude = traffic.Altitude;
            }

            // Uçak geçiş sesleri için mesafe ve irtifa farkını sade bir eşikle algılıyoruz.
            if (altitudeDifference <= 900f && distance <= 92f)
                nearPass = true;
            else if (altitudeDifference <= 2200f && distance <= 180f)
                farPass = true;

            // Çarpışma kutusu görselden biraz küçüktür; böylece kanat ucu yakın geçişleri haksız game over yapmaz.
            if (altitudeDifference <= 500f && traffic.CollisionBounds.Intersects(aircraftBounds))
                collision = true;

            var tcasArea = traffic.Bounds;
            tcasArea.Inflate(42, 70);
            if (altitudeDifference <= 1100f && tcasArea.Intersects(aircraftBounds))
            {
                tcasWarning = true;
                tcasCommand = traffic.Altitude <= playerAltitude ? "CLIMB" : "DESCENT";
            }

            if (altitudeDifference <= 1400f && GetWakeBounds(traffic).Intersects(aircraftBounds))
                inWake = true;
        }

        return new TrafficInteraction(inWake, collision, tcasWarning, tcasCommand, nearestAltitude, nearPass, farPass);
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        foreach (var traffic in _traffic)
        {
            if (!traffic.Active)
                continue;

            DrawWake(spriteBatch, pixel, traffic, _wakeTime);
            DrawHeavyAircraft(spriteBatch, pixel, traffic);
        }
    }

    private void UpdateTimers(float seconds, float playerAltitude)
    {
        _singleTimer -= seconds;
        _pairTimer -= seconds;
        _conflictTimer -= seconds;

        if (_singleTimer <= 0f)
        {
            SpawnTraffic(playerAltitude, sameAltitude: false, fromBehind: _nextOvertakingTraffic);
            _nextOvertakingTraffic = !_nextOvertakingTraffic;
            _singleTimer = 5f;
        }

        if (_pairTimer <= 0f)
        {
            SpawnTraffic(playerAltitude, sameAltitude: false, fromBehind: false);
            SpawnTraffic(playerAltitude, sameAltitude: false, fromBehind: true);
            _pairTimer = 10f;
        }

        if (_conflictTimer <= 0f)
        {
            SpawnTraffic(playerAltitude, sameAltitude: true, fromBehind: false);
            _conflictTimer = 15f;
        }
    }

    private void UpdateTrafficPositions(float seconds)
    {
        for (var i = 0; i < _traffic.Length; i++)
        {
            var traffic = _traffic[i];
            if (!traffic.Active)
                continue;

            // Karşıdan gelen uçak hızlı akar; arkadan gelen aynı yöndeki uçak yavaşça yaklaşır.
            traffic.Position += new Vector2(0f, traffic.Direction * traffic.ScreenSpeed * seconds);

            if (traffic.Position.Y > _height + 140 || traffic.Position.Y < -160)
                traffic.Active = false;

            _traffic[i] = traffic;
        }
    }

    private void SpawnTraffic(float playerAltitude, bool sameAltitude, bool fromBehind)
    {
        var slot = FindFreeSlot();
        if (slot < 0)
            return;

        // Karşıdan gelen trafik ekranın üstünden aşağı hızlı gelir.
        // Arkadan gelen trafik ekranın altından yukarı yavaş geçer.
        var direction = fromBehind ? -1 : 1;
        var startY = direction > 0 ? -90 : _height + 95;
        var screenSpeed = fromBehind ? 34f : 260f;
        var x = _random.Next(70, Math.Max(71, _width - 70));
        var altitudeOffset = sameAltitude ? _random.Next(-4, 5) * 100 : _random.Next(-70, 71) * 100;
        var altitude = (int)MathHelper.Clamp(playerAltitude + altitudeOffset, 3000f, 30000f);

        if (sameAltitude)
            x = _width / 2 + _random.Next(-70, 71);

        _traffic[slot] = new TrafficAircraft(
            active: true,
            position: new Vector2(x, startY),
            altitude: altitude,
            screenSpeed: screenSpeed,
            direction: direction);
    }

    private int FindFreeSlot()
    {
        for (var i = 0; i < _traffic.Length; i++)
        {
            if (!_traffic[i].Active)
                return i;
        }

        return -1;
    }

    private static Rectangle GetWakeBounds(TrafficAircraft traffic)
    {
        var x = (int)traffic.Position.X;
        var y = (int)traffic.Position.Y;

        // Wake her zaman uçağın arkasındadır; aşağı gidenin üstünde, yukarı gidenin altında kalır.
        var wakeY = traffic.Direction > 0 ? y - 112 : y + 76;
        return new Rectangle(x - 58, wakeY, 116, 116);
    }

    private static Vector2 GetCenter(Rectangle rectangle)
    {
        return new Vector2(rectangle.Center.X, rectangle.Center.Y);
    }

    private static void DrawWake(SpriteBatch spriteBatch, Texture2D pixel, TrafficAircraft traffic, float wakeTime)
    {
        var y = (int)traffic.Position.Y;
        var centerX = (int)traffic.Position.X;
        var movingDown = traffic.Direction > 0;
        var wingY = movingDown ? y + 42 : y + 36;
        var startY = movingDown ? wingY - 6 : wingY + 10;
        var step = movingDown ? -10 : 10;
        var color = new Color(180, 210, 220, 19);
        var curlColor = new Color(200, 226, 232, 24);

        // Wake çizgileri kanat uçlarından çıkar; ince, silik ve dikey vortex gibi görünür.
        DrawWakeColumn(spriteBatch, pixel, centerX - 52, startY, step, wakeTime, 0.2f, color, curlColor);
        DrawWakeColumn(spriteBatch, pixel, centerX + 52, startY, step, wakeTime, 1.7f, color, curlColor);
    }

    private static void DrawWakeColumn(SpriteBatch spriteBatch, Texture2D pixel, int x, int startY, int step, float wakeTime, float phase, Color color, Color curlColor)
    {
        var lastX = x;
        var lastY = startY;

        for (var i = 0; i < 7; i++)
        {
            // Vortex çizgisi küçük bir sinüsle salınır; wake canlı ama hâlâ okunaklı kalır.
            var wave = (int)(MathF.Sin(wakeTime * 5.2f + phase + i * 0.9f) * 7f);
            var y = startY + i * step;
            var nextWave = (int)(MathF.Sin(wakeTime * 5.2f + phase + (i + 1) * 0.9f) * 7f);
            var segmentHeight = Math.Abs(step) - 3;
            var segmentY = step > 0 ? y : y - segmentHeight;

            spriteBatch.Draw(pixel, new Rectangle(x + wave, segmentY, 2, segmentHeight), color);

            // Segmentler arasında kısa yatay piksel köprüleri S kıvrımını daha okunur yapar.
            var bridgeY = step > 0 ? y + segmentHeight : y - segmentHeight;
            var bridgeX = x + Math.Min(wave, nextWave);
            var bridgeWidth = Math.Abs(nextWave - wave) + 2;
            spriteBatch.Draw(pixel, new Rectangle(bridgeX, bridgeY, bridgeWidth, 2), color);

            lastX = x + wave;
            lastY = y;
        }

        var curlY = lastY + step;
        spriteBatch.Draw(pixel, new Rectangle(lastX - 5, curlY - 3, 10, 2), curlColor);
        spriteBatch.Draw(pixel, new Rectangle(lastX - 7, curlY - 1, 2, 6), curlColor);
        spriteBatch.Draw(pixel, new Rectangle(lastX + 5, curlY - 1, 2, 6), curlColor);
        spriteBatch.Draw(pixel, new Rectangle(lastX - 3, curlY + 5, 6, 2), curlColor);
    }

    private static void DrawHeavyAircraft(SpriteBatch spriteBatch, Texture2D pixel, TrafficAircraft traffic)
    {
        var centerX = (int)traffic.Position.X;
        var y = (int)traffic.Position.Y;

        var white = new Color(224, 232, 224);
        var light = new Color(188, 204, 208);
        var shade = new Color(108, 126, 142);
        var dark = new Color(42, 54, 70);
        var engine = new Color(58, 66, 78);
        var labelColor = new Color(248, 232, 120);

        var movingDown = traffic.Direction > 0;
        var noseY = movingDown ? y + 72 : y + 10;
        var tailY = movingDown ? y + 10 : y + 62;
        var wingY = movingDown ? y + 42 : y + 36;

        // Dört motorlu ağır trafik uçağı, oyuncudan ayrı okunması için geniş çizilir.
        spriteBatch.Draw(pixel, new Rectangle(centerX - 58, wingY, 116, 9), light);
        spriteBatch.Draw(pixel, new Rectangle(centerX - 52, wingY + (movingDown ? 8 : -5), 104, 5), shade);
        spriteBatch.Draw(pixel, new Rectangle(centerX - 15, y + 10, 30, 58), white);
        spriteBatch.Draw(pixel, new Rectangle(centerX - 19, y + 28, 38, 28), light);
        spriteBatch.Draw(pixel, new Rectangle(centerX - 10, noseY, 20, 6), dark);
        spriteBatch.Draw(pixel, new Rectangle(centerX - 12, tailY, 24, 14), shade);

        var engineY = wingY + (movingDown ? 11 : -9);
        spriteBatch.Draw(pixel, new Rectangle(centerX - 45, engineY, 9, 8), engine);
        spriteBatch.Draw(pixel, new Rectangle(centerX - 26, engineY, 9, 8), engine);
        spriteBatch.Draw(pixel, new Rectangle(centerX + 17, engineY, 9, 8), engine);
        spriteBatch.Draw(pixel, new Rectangle(centerX + 36, engineY, 9, 8), engine);

        var stabY = movingDown ? y + 6 : y + 72;
        spriteBatch.Draw(pixel, new Rectangle(centerX - 32, stabY, 24, 7), light);
        spriteBatch.Draw(pixel, new Rectangle(centerX + 8, stabY, 24, 7), light);

        var label = $"FL{traffic.Altitude / 100}";
        PixelTextRenderer.Draw(spriteBatch, pixel, label, new Vector2(centerX - label.Length * 3, movingDown ? y - 8 : y + 84), labelColor, 1);
    }

    private struct TrafficAircraft
    {
        public TrafficAircraft(bool active, Vector2 position, int altitude, float screenSpeed, int direction)
        {
            Active = active;
            Position = position;
            Altitude = altitude;
            ScreenSpeed = screenSpeed;
            Direction = direction;
        }

        public bool Active { get; set; }
        public Vector2 Position { get; set; }
        public int Altitude { get; }
        public float ScreenSpeed { get; }
        public int Direction { get; }
        public Rectangle Bounds => new((int)Position.X - 58, (int)Position.Y + 6, 116, 78);
        public Rectangle CollisionBounds => new((int)Position.X - 34, (int)Position.Y + 16, 68, 54);
    }
}
