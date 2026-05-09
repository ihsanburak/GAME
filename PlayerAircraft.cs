using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GAME;

public sealed class PlayerAircraft
{
    private const float MoveSpeed = 230f;
    private const float Acceleration = 9f;
    private readonly Point _size = new(46, 62);
    private Vector2 _velocity;
    private float _bank;
    private float _pitch;
    private float _visualScale = 1f;

    public Vector2 Position { get; private set; }

    // Çarpışma ve çizim için uçağın ekrandaki dikdörtgen alanı.
    public Rectangle Bounds => new(
        (int)(Position.X - _size.X / 2f),
        (int)(Position.Y - _size.Y / 2f),
        _size.X,
        _size.Y);

    public PlayerAircraft(Vector2 startPosition)
    {
        Position = startPosition;
    }

    public void Update(GameTime gameTime, KeyboardState keyboard, Rectangle bounds, bool isClimbing, bool isDescending, float visualScale)
    {
        _visualScale = MathHelper.Clamp(visualScale, 0.7f, 1f);

        // Ok tuşlarından gelen yön girdisini topluyoruz.
        var direction = Vector2.Zero;

        if (keyboard.IsKeyDown(Keys.Left))
            direction.X -= 1f;
        if (keyboard.IsKeyDown(Keys.Right))
            direction.X += 1f;
        if (keyboard.IsKeyDown(Keys.Up))
            direction.Y -= 1f;
        if (keyboard.IsKeyDown(Keys.Down))
            direction.Y += 1f;

        if (direction != Vector2.Zero)
            direction.Normalize();

        var seconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var targetVelocity = direction * MoveSpeed;

        // Hedef hıza yumuşak yaklaşmak uçağa hafif atalet hissi verir.
        _velocity = Vector2.Lerp(_velocity, targetVelocity, MathHelper.Clamp(Acceleration * seconds, 0f, 1f));
        Position += _velocity * seconds;

        // Görsel banka ve burun tutumu için girdiyi kısa süreli değerlerde tutuyoruz.
        _bank = MathHelper.Lerp(_bank, direction.X, MathHelper.Clamp(12f * seconds, 0f, 1f));

        var targetPitch = 0f;
        if (isClimbing || direction.Y < 0f)
            targetPitch = -1f;
        else if (isDescending || direction.Y > 0f)
            targetPitch = 1f;

        _pitch = MathHelper.Lerp(_pitch, targetPitch, MathHelper.Clamp(10f * seconds, 0f, 1f));

        var halfWidth = _size.X / 2f;
        var halfHeight = _size.Y / 2f;

        // Uçak belirlenen oynanabilir alanın dışına çıkamaz.
        Position = new Vector2(
            MathHelper.Clamp(Position.X, bounds.Left + halfWidth, bounds.Right - halfWidth),
            MathHelper.Clamp(Position.Y, bounds.Top + halfHeight, bounds.Bottom - halfHeight));
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        // Şimdilik uçak modern yolcu uçağına benzeyen piksel parçalarla çiziliyor.
        // Gerçek sprite gelene kadar burası tutum durumlarını okunur şekilde gösterir.
        var body = GetVisualBounds();
        var centerX = body.Center.X;
        var noseOffset = (int)(_pitch * 5f);
        var tailOffset = -noseOffset / 2;
        var leftBank = _bank < -0.25f;
        var rightBank = _bank > 0.25f;

        var white = new Color(236, 244, 232);
        var light = new Color(208, 224, 216);
        var shadow = new Color(128, 152, 160);
        var dark = new Color(56, 72, 88);
        var accent = new Color(248, 184, 64);
        var highWing = new Color(244, 248, 232);
        var lowWing = new Color(160, 176, 176);

        var leftWingColor = leftBank ? lowWing : highWing;
        var rightWingColor = rightBank ? lowWing : highWing;
        var leftWingY = body.Y + 28 + (leftBank ? 4 : rightBank ? -3 : 0);
        var rightWingY = body.Y + 28 + (rightBank ? 4 : leftBank ? -3 : 0);
        var leftWingLength = leftBank ? 42 : 48;
        var rightWingLength = rightBank ? 42 : 48;

        // Kanat gölgeleri uçağı zeminden ayırır.
        spriteBatch.Draw(pixel, new Rectangle(centerX - leftWingLength - 2, leftWingY + 7, leftWingLength, 5), shadow);
        spriteBatch.Draw(pixel, new Rectangle(centerX + 2, rightWingY + 7, rightWingLength, 5), shadow);

        // Ana kanatlar arkadan/üstten görülen geniş airliner hissini verir.
        spriteBatch.Draw(pixel, new Rectangle(centerX - leftWingLength, leftWingY, leftWingLength, 8), leftWingColor);
        spriteBatch.Draw(pixel, new Rectangle(centerX + 1, rightWingY, rightWingLength, 8), rightWingColor);
        spriteBatch.Draw(pixel, new Rectangle(centerX - leftWingLength + 8, leftWingY - 4, 25, 4), leftWingColor);
        spriteBatch.Draw(pixel, new Rectangle(centerX + rightWingLength - 33, rightWingY - 4, 25, 4), rightWingColor);

        // Motorlar kanat altında küçük koyu kapsüller olarak gösterilir.
        spriteBatch.Draw(pixel, new Rectangle(centerX - 28, leftWingY + 8, 10, 8), dark);
        spriteBatch.Draw(pixel, new Rectangle(centerX + 18, rightWingY + 8, 10, 8), dark);
        spriteBatch.Draw(pixel, new Rectangle(centerX - 27, leftWingY + 9, 8, 4), shadow);
        spriteBatch.Draw(pixel, new Rectangle(centerX + 19, rightWingY + 9, 8, 4), shadow);

        // Gövde birkaç parça ile çizilerek burun, kabin ve kuyruk okunur hale gelir.
        spriteBatch.Draw(pixel, new Rectangle(centerX - 12, body.Y + 7 + noseOffset, 24, 12), white);
        spriteBatch.Draw(pixel, new Rectangle(centerX - 16, body.Y + 18, 32, 33), white);
        spriteBatch.Draw(pixel, new Rectangle(centerX - 11, body.Y + 8 + noseOffset, 22, 5), light);
        spriteBatch.Draw(pixel, new Rectangle(centerX + 7, body.Y + 21, 5, 27), light);
        spriteBatch.Draw(pixel, new Rectangle(centerX - 15, body.Y + 46 + tailOffset, 30, 12), light);

        // Kokpit camı ve pencere çizgisi ölçeğe rağmen uçağın yönünü belli eder.
        spriteBatch.Draw(pixel, new Rectangle(centerX - 5, body.Y + 10 + noseOffset, 10, 3), dark);
        spriteBatch.Draw(pixel, new Rectangle(centerX - 10, body.Y + 25, 3, 3), dark);
        spriteBatch.Draw(pixel, new Rectangle(centerX - 4, body.Y + 26, 3, 3), dark);
        spriteBatch.Draw(pixel, new Rectangle(centerX + 3, body.Y + 26, 3, 3), dark);
        spriteBatch.Draw(pixel, new Rectangle(centerX + 9, body.Y + 25, 3, 3), dark);

        // Kuyruk ve yatay stabilize parçaları gövdenin arkasında okunur.
        spriteBatch.Draw(pixel, new Rectangle(centerX - 5, body.Bottom - 19 + tailOffset, 10, 18), accent);
        spriteBatch.Draw(pixel, new Rectangle(centerX - 27, body.Bottom - 11 + tailOffset, 22, 7), light);
        spriteBatch.Draw(pixel, new Rectangle(centerX + 5, body.Bottom - 11 + tailOffset, 22, 7), light);

        DrawAttitudeCue(spriteBatch, pixel, body, leftBank, rightBank, noseOffset, accent);
    }

    private Rectangle GetVisualBounds()
    {
        var width = (int)(_size.X * _visualScale);
        var height = (int)(_size.Y * _visualScale);

        // Kalkışta görsel boyut değişir, ama oyun çarpışma alanı sade kalır.
        return new Rectangle(
            (int)(Position.X - width / 2f),
            (int)(Position.Y - height / 2f),
            width,
            height);
    }

    private static void DrawAttitudeCue(SpriteBatch spriteBatch, Texture2D pixel, Rectangle body, bool leftBank, bool rightBank, int noseOffset, Color accent)
    {
        // Küçük renk vurguları oyuncuya aktif tutumu hızlıca hissettirir.
        if (leftBank)
            spriteBatch.Draw(pixel, new Rectangle(body.X + 31, body.Y + 20, 6, 30), accent);
        else if (rightBank)
            spriteBatch.Draw(pixel, new Rectangle(body.X + 9, body.Y + 20, 6, 30), accent);

        if (noseOffset < -1)
            spriteBatch.Draw(pixel, new Rectangle(body.Center.X - 7, body.Y + 2, 14, 4), accent);
        else if (noseOffset > 1)
            spriteBatch.Draw(pixel, new Rectangle(body.Center.X - 9, body.Bottom - 5, 18, 4), accent);
    }
}
