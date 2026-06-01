using Microsoft.Xna.Framework;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GAME;

public enum FlightPhase
{
    Takeoff,
    InitialClimb,
    Cruise,
    Descent,
    Approach,
    Landing,
    MissionComplete,
    GameOver,
}

public class Game1 : Game
{
    // MonoGame'in temel çizim araçları ve prototipte kullanılan oyun nesneleri.
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private RenderTarget2D _gameRenderTarget;
    private Texture2D _pixel;
    private Texture2D _logoTexture;
    private PlayerAircraft _aircraft;
    private RetroHud _hud;
    private RouteProgressHud _routeHud;
    private ApproachView _approachView;
    private ScrollingTerrain _terrain;
    private CloudField _clouds;
    private TrafficField _traffic;
    private AudioManager _audio;
    private TurbulenceLevel _turbulenceLevel;
    private KeyboardState _previousKeyboard;
    private float _shakeTimer;
    private float _cloudDepthCue;
    private float _passengerComfort = 100f;
    private float _score = 1000f;
    private float _altitude;
    private float _speed = 35f;
    private float _fuel = 8.6f;
    private float _estimatedFuelAtDestination;
    private bool _showMaydayWarning;
    private float _totalRouteNm = 235f;
    private float _remainingNm = 235f;
    private float _topOfClimbNm = 210f;
    private float _topOfDescentNm = 45f;
    private float _approachStartNm = 20f;
    private float _plannedCruiseAltitude = 26000f;
    private float _belowMoraTimer;
    private float _rotateWindowTimer;
    private float _localizerDeviation;
    private float _glideDeviation;
    private float _verticalSpeedFpm = -700f;
    private float _selectedApproachVerticalSpeed = -700f;
    private float _approachGustTimer = 5f;
    private float _approachGustForce;
    private float _approachVerticalGust;
    private float _approachBank;
    private string _landingQuality = "";
    private bool _approachInitialized;
    private bool _seatBeltOn;
    private float _seatBeltOnTimer;
    private bool _showSeatBeltWarning;
    private bool _terrainWarning;
    private bool _showTerrainWarning;
    private bool _showRotateWarning;
    private bool _showTcasWarning;
    private string _tcasCommand = "";
    private string _gameOverReason = "";
    private float _previousScoreForAudio = 1000f;
    private bool _isHomeScreen = true;
    private bool _isGameOver;
    private FlightPhase _flightPhase = FlightPhase.Takeoff;

    private const int ScreenWidth = 640;
    private const int ScreenHeight = 600;
    private const int WindowWidth = 1280;
    private const int WindowHeight = 1200;
    private const int HudHeight = 76;
    private const int ForwardViewMargin = 168;
    private const int AircraftBottomMargin = 2;
    private const float V1Speed = 140f;
    private const float RotateSpeed = 150f;
    private const float V2Speed = 160f;
    private const float RunwayEndDistance = 820f;
    private const float CruiseSpeed = 155f;
    private const float ClimbRate = 720f;
    private const float DescendRate = 820f;
    private const float CrashTimeBelowMora = 5f;
    private const float FinalReserveFuel = 1.0f;
    private const float PlannedFuelAtDestination = 2.4f;
    private const float SeatBeltComfortLimit = 60f;
    private const int DestinationFieldElevation = 2250;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = WindowWidth;
        _graphics.PreferredBackBufferHeight = WindowHeight;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = false;
    }

    protected override void Initialize()
    {
        // Uçak ekranın oynanabilir kısmının ortasında başlar.
        // HUD yüksekliği çıkarıldığı için uçak panelin arkasına girmez.
        _aircraft = new PlayerAircraft(new Vector2(ScreenWidth / 2f, ScreenHeight - HudHeight - 24));
        _hud = new RetroHud(HudHeight);
        _routeHud = new RouteProgressHud();
        _approachView = new ApproachView();
        _terrain = new ScrollingTerrain(ScreenWidth, ScreenHeight - HudHeight);
        _clouds = new CloudField(ScreenWidth, ScreenHeight - HudHeight);
        _traffic = new TrafficField(ScreenWidth, ScreenHeight - HudHeight);
        _audio = new AudioManager();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Oyun önce düşük çözünürlüklü hedefe çizilir, sonra pencereye net şekilde büyütülür.
        _gameRenderTarget = new RenderTarget2D(GraphicsDevice, ScreenWidth, ScreenHeight);

        // Tüm geçici görseller 1x1 beyaz pikselin büyütülmesiyle çizilir.
        // Bu yöntem erken prototip için hızlı ve anlaşılırdır.
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        // Ana ekranda oyuncunun logosunu kullanıyoruz; dosya yoksa ekran logosuz çalışır.
        if (File.Exists("logo.png"))
        {
            using var logoStream = File.OpenRead("logo.png");
            _logoTexture = Texture2D.FromStream(GraphicsDevice, logoStream);
        }

        _audio.Load();
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        var keyboard = Keyboard.GetState();

        if (_isHomeScreen)
        {
            // Enter tuşu oyunu pist başından başlatır.
            if (keyboard.IsKeyDown(Keys.Enter) && !_previousKeyboard.IsKeyDown(Keys.Enter))
            {
                RestartGame();
                _isHomeScreen = false;
            }

            _previousKeyboard = keyboard;
            base.Update(gameTime);
            return;
        }

        if (_isGameOver || _flightPhase == FlightPhase.MissionComplete)
        {
            _audio.Update(gameTime, _flightPhase);

            if (keyboard.IsKeyDown(Keys.R) && !_previousKeyboard.IsKeyDown(Keys.R))
                RestartGame();

            if (_isGameOver)
                _flightPhase = FlightPhase.GameOver;

            _previousKeyboard = keyboard;
            base.Update(gameTime);
            return;
        }

        // S tuşuna her basışta kemer durumunu açıp kapatıyoruz.
        if (keyboard.IsKeyDown(Keys.S) && !_previousKeyboard.IsKeyDown(Keys.S))
        {
            _seatBeltOn = !_seatBeltOn;
            _audio.PlaySfx(SoundKey.SeatbeltDing);
        }

        if (IsApproachMode())
        {
            _audio.Update(gameTime, _flightPhase);
            UpdateApproach(gameTime, keyboard);
            UpdateFuelPlanning(gameTime);
            UpdateAudioEvents();
            _previousKeyboard = keyboard;
            base.Update(gameTime);
            return;
        }

        _audio.Update(gameTime, _flightPhase);

        var isClimbing = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);
        var isDescending = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
        UpdateCloudDepthCue(gameTime, isClimbing, isDescending);
        UpdateFlightPhase(gameTime, isClimbing, isDescending);

        // Kalkışta uçak pist merkez hattına yakın başlar, sonra tüm oynanabilir alanda dolaşabilir.
        var playArea = GetPlayableArea();
        _aircraft.Update(gameTime, keyboard, playArea, isClimbing, isDescending, GetTakeoffVisualScale());
        _terrain.Update(gameTime, GetWorldSpeed());
        if (ShouldShowClouds())
            _clouds.Update(gameTime, GetWorldSpeed());
        if (ShouldShowTraffic())
            _traffic.Update(gameTime, _altitude);

        UpdateRouteProgress(gameTime);
        UpdateAltitudeAndFuel(gameTime, isClimbing, isDescending);
        UpdateFuelPlanning(gameTime);
        UpdateRunwaySafety();
        UpdateTerrainRisk(gameTime);
        UpdateTrafficRisk();

        // Kalkışta henüz bulut etkileşimi yok; havalanınca bulut ve irtifa bandı birlikte değerlendirilir.
        _turbulenceLevel = GetCurrentTurbulenceLevel();

        ApplyTurbulencePenalty(gameTime);
        UpdateSeatBeltComfort(gameTime);
        UpdateAudioEvents();

        _shakeTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        _previousKeyboard = keyboard;

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.SetRenderTarget(_gameRenderTarget);
        GraphicsDevice.Clear(new Color(72, 104, 88));

        if (_isHomeScreen)
        {
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            DrawHomeScreen();
            _spriteBatch.End();

            DrawScaledRenderTarget();
            base.Draw(gameTime);
            return;
        }

        if (IsApproachMode() || _flightPhase == FlightPhase.MissionComplete)
        {
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _approachView.Draw(
                _spriteBatch,
                _pixel,
                GraphicsDevice.Viewport.Bounds,
                (int)_altitude,
                (int)_speed,
                _fuel,
                _localizerDeviation,
                _glideDeviation,
                _verticalSpeedFpm,
                _selectedApproachVerticalSpeed,
                _remainingNm,
                DestinationFieldElevation,
                _approachBank,
                _flightPhase,
                _landingQuality,
                (int)_score,
                (int)_passengerComfort);
            _spriteBatch.End();

            DrawScaledRenderTarget();
            base.Draw(gameTime);
            return;
        }

        var shakeOffset = GetShakeOffset();

        // Türbülans sadece oyun dünyasını sallar; HUD sabit kalır.
        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            transformMatrix: Matrix.CreateTranslation(shakeOffset.X, shakeOffset.Y, 0f));

        _terrain.Draw(_spriteBatch, _pixel, _altitude);

        if (ShouldShowClouds())
            _clouds.Draw(_spriteBatch, _pixel, _altitude, _cloudDepthCue);
        if (ShouldShowTraffic())
            _traffic.Draw(_spriteBatch, _pixel);

        _aircraft.Draw(_spriteBatch, _pixel);
        _spriteBatch.End();

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _routeHud.Draw(
            _spriteBatch,
            _pixel,
            GraphicsDevice.Viewport.Bounds,
            _totalRouteNm,
            _remainingNm,
            _topOfClimbNm,
            _topOfDescentNm,
            _approachStartNm);

        _hud.Draw(
            _spriteBatch,
            _pixel,
            GraphicsDevice.Viewport.Bounds,
            (int)_passengerComfort,
            _seatBeltOn,
            (int)_score,
            _turbulenceLevel,
            (int)_altitude,
            (int)_speed,
            _fuel,
            _terrain.CurrentMora,
            _showTerrainWarning,
            _showMaydayWarning,
            _showSeatBeltWarning,
            _estimatedFuelAtDestination,
            FinalReserveFuel,
            PlannedFuelAtDestination,
            (int)_plannedCruiseAltitude,
            _showRotateWarning,
            _showTcasWarning,
            _tcasCommand,
            _gameOverReason,
            _isGameOver,
            _flightPhase);
        _spriteBatch.End();

        DrawScaledRenderTarget();

        base.Draw(gameTime);
    }

    private Vector2 GetShakeOffset()
    {
        var shakePower = GetShakePower();

        if (shakePower <= 0f)
            return Vector2.Zero;

        // Küçük ve hızlı bir titreşim, bulut içinde uçma hissi verir.
        var x = System.MathF.Sin(_shakeTimer * 72f) * shakePower;
        var y = System.MathF.Cos(_shakeTimer * 53f) * shakePower;

        return new Vector2((int)x, (int)y);
    }

    private void ApplyTurbulencePenalty(GameTime gameTime)
    {
        var seconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var comfortLoss = GetComfortLossPerSecond() * seconds;
        var scoreLoss = GetScoreLossPerSecond() * seconds;

        // Kemer açıksa türbülans yolcuları daha az rahatsız eder.
        if (_seatBeltOn)
            comfortLoss *= 0.4f;

        _passengerComfort = MathHelper.Clamp(_passengerComfort - comfortLoss, 0f, 100f);
        _score = System.MathF.Max(0f, _score - scoreLoss);
    }

    private void UpdateSeatBeltComfort(GameTime gameTime)
    {
        var seconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (!_seatBeltOn)
        {
            _seatBeltOnTimer = 0f;
            _showSeatBeltWarning = false;
            return;
        }

        _seatBeltOnTimer += seconds;

        // Türbülans yokken kemer çok uzun süre açık kalırsa yolcular rahatsız olur.
        if (_seatBeltOnTimer > SeatBeltComfortLimit && _turbulenceLevel == TurbulenceLevel.None)
        {
            _passengerComfort = MathHelper.Clamp(_passengerComfort - 1.4f * seconds, 0f, 100f);
            _showSeatBeltWarning = ((int)(_shakeTimer * 5f) % 2) == 0;
            return;
        }

        _showSeatBeltWarning = false;
    }

    private void UpdateFlightPhase(GameTime gameTime, bool isClimbing, bool isDescending)
    {
        var seconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Kalkışta hız otomatik artar; VR civarında tırmanış komutu beklenir.
        if (_flightPhase == FlightPhase.Takeoff)
        {
            _speed = MathHelper.Clamp(_speed + 30f * seconds, 35f, V2Speed);

            if (_speed >= RotateSpeed)
            {
                _rotateWindowTimer += seconds;
                _showRotateWarning = ((int)(_shakeTimer * 6f) % 2) == 0;
            }

            if (isClimbing && _speed < V1Speed)
            {
                TriggerGameOver("TAKEOFF");
                return;
            }

            if (isClimbing && _speed >= V1Speed && _speed <= V2Speed && _rotateWindowTimer <= 5f)
            {
                _flightPhase = FlightPhase.InitialClimb;
                _altitude = 320f;
                _showRotateWarning = false;
                return;
            }

            if (_rotateWindowTimer > 5f || _terrain.WorldDistance >= RunwayEndDistance)
            {
                TriggerGameOver("TAKEOFF");
            }
        }
        else if (_flightPhase == FlightPhase.InitialClimb)
        {
            _speed = MathHelper.Clamp(_speed + 12f * seconds, V2Speed, CruiseSpeed);

            if (_altitude >= 10000f)
                _flightPhase = FlightPhase.Cruise;
        }
        else
        {
            _speed = MathHelper.Lerp(_speed, CruiseSpeed, MathHelper.Clamp(1.8f * seconds, 0f, 1f));

            if (_remainingNm <= _approachStartNm)
                StartApproach();
        }
    }

    private void StartApproach()
    {
        if (_approachInitialized)
            return;

        // Yaklaşma modu prototipte 20 NM kala sade bir ILS ekranına geçer.
        _approachInitialized = true;
        _flightPhase = FlightPhase.Approach;
        _altitude = 1200f;
        _speed = 140f;
        _localizerDeviation = 0.45f;
        _glideDeviation = 0f;
        _verticalSpeedFpm = -700f;
        _selectedApproachVerticalSpeed = -700f;
        _approachGustTimer = 5f;
        _approachGustForce = 0f;
        _approachVerticalGust = 0f;
        _approachBank = 0f;
        _landingQuality = "";
    }

    private void UpdateApproach(GameTime gameTime, KeyboardState keyboard)
    {
        var seconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _speed = MathHelper.Lerp(_speed, _altitude <= 50f ? 125f : 135f, MathHelper.Clamp(1.2f * seconds, 0f, 1f));

        UpdateApproachGust(gameTime);

        if (keyboard.IsKeyDown(Keys.Left))
            _localizerDeviation -= 0.75f * seconds;
        if (keyboard.IsKeyDown(Keys.Right))
            _localizerDeviation += 0.75f * seconds;

        _localizerDeviation += _approachGustForce * seconds;
        _localizerDeviation = MathHelper.Clamp(_localizerDeviation, -1f, 1f);

        // Shift ve Ctrl seçili V/S değerini değiştirir; tuş bırakılınca değer orada kalır.
        if (keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift))
            _selectedApproachVerticalSpeed += 420f * seconds;
        if (keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl))
            _selectedApproachVerticalSpeed -= 420f * seconds;

        _selectedApproachVerticalSpeed = MathHelper.Clamp(_selectedApproachVerticalSpeed, -1100f, -100f);

        var targetVerticalSpeed = _selectedApproachVerticalSpeed + _approachVerticalGust;

        if (_altitude <= 50f)
        {
            _flightPhase = FlightPhase.Landing;
            targetVerticalSpeed = MathHelper.Clamp(targetVerticalSpeed + 220f, -650f, -180f);
        }

        _verticalSpeedFpm = MathHelper.Lerp(_verticalSpeedFpm, targetVerticalSpeed, MathHelper.Clamp(2.4f * seconds, 0f, 1f));
        _altitude = MathHelper.Clamp(_altitude + _verticalSpeedFpm / 60f * seconds, 0f, 30000f);

        var idealAltitude = MathHelper.Clamp(_remainingNm * 60f, 0f, 1200f);
        _glideDeviation = MathHelper.Clamp((_altitude - idealAltitude) / 300f, -1f, 1f);
        _remainingNm = MathHelper.Clamp(_remainingNm - _speed / 3600f * 8f * seconds, 0f, _totalRouteNm);
        _fuel = MathHelper.Clamp(_fuel - GetBaseFuelBurnPerSecond() * 0.7f * seconds, 0f, 99f);

        var bankTarget = 0f;
        if (keyboard.IsKeyDown(Keys.Left))
            bankTarget = -1f;
        if (keyboard.IsKeyDown(Keys.Right))
            bankTarget = 1f;
        bankTarget += _approachGustForce * 1.4f;
        _approachBank = MathHelper.Lerp(_approachBank, MathHelper.Clamp(bankTarget, -1.2f, 1.2f), MathHelper.Clamp(5f * seconds, 0f, 1f));

        if (System.MathF.Abs(_localizerDeviation) > 0.75f)
            _score = System.MathF.Max(0f, _score - 4f * seconds);

        if (_altitude <= 0f || _remainingNm <= 0.05f)
            EvaluateTouchdown();
    }

    private void UpdateApproachGust(GameTime gameTime)
    {
        var seconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _approachGustTimer -= seconds;

        if (_approachGustTimer <= 0f)
        {
            // Yaklaşmada kısa gust darbeleri LOC ve V/S'i bozarak oyuncuya tekrar düzeltme görevi verir.
            var side = System.MathF.Sin(_shakeTimer * 11.7f) >= 0f ? 1f : -1f;
            _approachGustForce = side * 0.26f;
            _approachVerticalGust = side * 260f;
            _approachGustTimer = 5f;
        }

        _approachGustForce = MathHelper.Lerp(_approachGustForce, 0f, MathHelper.Clamp(1.8f * seconds, 0f, 1f));
        _approachVerticalGust = MathHelper.Lerp(_approachVerticalGust, 0f, MathHelper.Clamp(1.3f * seconds, 0f, 1f));
    }

    private void EvaluateTouchdown()
    {
        var touchdownRate = System.MathF.Abs(_verticalSpeedFpm);

        if (touchdownRate <= 300f)
        {
            _landingQuality = "SOFT LAND";
            _score += 450f;
            _flightPhase = FlightPhase.MissionComplete;
            return;
        }

        if (touchdownRate <= 600f)
        {
            _landingQuality = "FIRM LAND";
            _score = System.MathF.Max(0f, _score - 150f);
            _passengerComfort = MathHelper.Clamp(_passengerComfort - 14f, 0f, 100f);
            _flightPhase = FlightPhase.MissionComplete;
            return;
        }

        _landingQuality = "HARD LAND";
        TriggerGameOver("HARD");
    }

    private void UpdateCloudDepthCue(GameTime gameTime, bool isClimbing, bool isDescending)
    {
        var seconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var targetCue = 0f;

        if (isClimbing)
            targetCue = -1f;
        else if (isDescending)
            targetCue = 1f;

        // Tırmanış ve alçalışta bulutlar hafif ölçek değiştirerek dikey derinlik hissi verir.
        _cloudDepthCue = MathHelper.Lerp(_cloudDepthCue, targetCue, MathHelper.Clamp(4f * seconds, 0f, 1f));
    }

    private void UpdateRouteProgress(GameTime gameTime)
    {
        var seconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Prototipte rota mesafesi hıza göre otomatik azalır.
        // Katsayı arcade tempo için gerçek havacılıktan daha hızlı tutuldu.
        var nmPerSecond = _speed / 3600f * 18f;
        _remainingNm = MathHelper.Clamp(_remainingNm - nmPerSecond * seconds, 0f, _totalRouteNm);
    }

    private void UpdateAltitudeAndFuel(GameTime gameTime, bool isClimbing, bool isDescending)
    {
        var seconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var fuelBurn = GetBaseFuelBurnPerSecond();

        // Shift tırmanış, Ctrl alçalma kontrolüdür.
        if (_flightPhase == FlightPhase.Takeoff)
        {
            _altitude = 320f;
            fuelBurn += 0.003f;
        }
        else if (_flightPhase == FlightPhase.InitialClimb)
        {
            _altitude += (ClimbRate * 0.85f + (isClimbing ? ClimbRate * 0.45f : 0f)) * seconds;
            fuelBurn += 0.0032f;
        }
        else if (isClimbing)
        {
            _altitude += ClimbRate * seconds;
            fuelBurn += 0.0042f;
        }

        if (_flightPhase != FlightPhase.Takeoff && isDescending)
        {
            _altitude -= DescendRate * seconds;
            fuelBurn += 0.0012f;
        }

        // İrtifa ve yakıt değerlerini basit sınırlar içinde tutuyoruz.
        _altitude = MathHelper.Clamp(_altitude, 0f, 30000f);
        _fuel = MathHelper.Clamp(_fuel - fuelBurn * seconds, 0f, 99f);
    }

    private void UpdateFuelPlanning(GameTime gameTime)
    {
        var seconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var averageCruiseBurnPerNm = GetEstimatedFuelBurnPerNm();

        // Varıştaki tahmini yakıt, kalan mesafe ve basit ortalama tüketimle hesaplanır.
        _estimatedFuelAtDestination = MathHelper.Clamp(_fuel - _remainingNm * averageCruiseBurnPerNm, 0f, 99f);
        _showMaydayWarning = _fuel <= FinalReserveFuel || _estimatedFuelAtDestination < FinalReserveFuel;

        if (_estimatedFuelAtDestination > PlannedFuelAtDestination)
        {
            var extraFuel = _estimatedFuelAtDestination - PlannedFuelAtDestination;
            _score += extraFuel * 0.35f * seconds;
        }

        if (_showMaydayWarning)
        {
            _score = System.MathF.Max(0f, _score - 8f * seconds);

            if (_fuel <= FinalReserveFuel)
                TriggerGameOver("FUEL");
        }
    }

    private float GetBaseFuelBurnPerSecond()
    {
        var altitudeEfficiency = GetAltitudeFuelEfficiency();

        // Hız ve irtifa birlikte basit yakıt tüketimini belirler.
        return (0.0016f + _speed * 0.00001f) * altitudeEfficiency;
    }

    private float GetAltitudeFuelEfficiency()
    {
        if (_flightPhase == FlightPhase.Takeoff)
            return 1.25f;

        if (_flightPhase == FlightPhase.InitialClimb)
            return 1.45f;

        var altitudeDifference = _altitude - _plannedCruiseAltitude;

        // Planlanan cruise irtifasından uzaklaştıkça yakıt cezası artar.
        if (altitudeDifference < -16000f)
            return 2.35f;
        if (altitudeDifference < -10000f)
            return 1.85f;
        if (altitudeDifference < -5000f)
            return 1.35f;
        if (altitudeDifference < 4000f)
            return 0.82f;
        if (altitudeDifference < 9000f)
            return 1.05f;

        return 1.28f;
    }

    private float GetEstimatedFuelBurnPerNm()
    {
        // Varış yakıt tahmini planlanan cruise irtifasına göre daha sakin hesaplanır.
        if (_flightPhase is FlightPhase.Takeoff or FlightPhase.InitialClimb)
            return 0.0105f;

        return 0.0105f * GetAltitudeFuelEfficiency();
    }

    private void UpdateTerrainRisk(GameTime gameTime)
    {
        var seconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _terrainWarning = _altitude < _terrain.CurrentMora;

        // Kalkış ve ilk tırmanışta uçak henüz rota irtifasına yerleşmediği için MORA cezasını başlatmıyoruz.
        if (!_terrainWarning || _flightPhase is FlightPhase.Takeoff or FlightPhase.InitialClimb)
        {
            _belowMoraTimer = 0f;
            _showTerrainWarning = false;
            return;
        }

        _belowMoraTimer += seconds;
        _score = System.MathF.Max(0f, _score - 18f * seconds);

        // Uyarı yazısını yanıp sönen hale getiriyoruz.
        _showTerrainWarning = ((int)(_shakeTimer * 6f) % 2) == 0;

        if (_belowMoraTimer >= CrashTimeBelowMora)
            TriggerGameOver("TERRAIN");
    }

    private void UpdateRunwaySafety()
    {
        if (_flightPhase != FlightPhase.Takeoff)
            return;

        var runwayLeft = ScreenWidth / 2 - 42;
        var runwayRight = ScreenWidth / 2 + 42;

        // Kalkışta hafif sağ-sol serbest, ama pistten tamamen çıkmak kazadır.
        if (_aircraft.Position.X < runwayLeft - 12 || _aircraft.Position.X > runwayRight + 12)
            TriggerGameOver("PIST");
    }

    private float GetComfortLossPerSecond()
    {
        return _turbulenceLevel switch
        {
            TurbulenceLevel.Light => 0.8f,
            TurbulenceLevel.Moderate => 2.4f,
            TurbulenceLevel.Severe => 6.2f,
            _ => 0f,
        };
    }

    private float GetScoreLossPerSecond()
    {
        return _turbulenceLevel switch
        {
            TurbulenceLevel.Light => 2f,
            TurbulenceLevel.Moderate => 8f,
            TurbulenceLevel.Severe => 20f,
            _ => 0f,
        };
    }

    private float GetShakePower()
    {
        return _turbulenceLevel switch
        {
            TurbulenceLevel.Light => 1.2f,
            TurbulenceLevel.Moderate => 2.6f,
            TurbulenceLevel.Severe => 4.5f,
            _ => 0f,
        };
    }

    private TurbulenceLevel GetCurrentTurbulenceLevel()
    {
        var level = !ShouldShowClouds()
            ? TurbulenceLevel.None
            : _clouds.GetCloudInteraction(_aircraft.Bounds, _altitude).Level;

        var trafficInteraction = ShouldShowTraffic()
            ? _traffic.GetInteraction(_aircraft.Bounds, _altitude)
            : new TrafficInteraction(false, false, false, "", 0, false, false);

        // Ağır uçağın wake bölgesine aynı irtifa civarında girersek ağır türbülans verir.
        if (trafficInteraction.InWake)
            return TurbulenceLevel.Severe;

        return level;
    }

    private void UpdateTrafficRisk()
    {
        _showTcasWarning = false;
        _tcasCommand = "";

        if (!ShouldShowTraffic())
            return;

        var interaction = _traffic.GetInteraction(_aircraft.Bounds, _altitude);

        if (interaction.Collision)
        {
            TriggerGameOver("TRAFFIC");
            return;
        }

        if (interaction.TcasWarning)
        {
            _showTcasWarning = ((int)(_shakeTimer * 5f) % 2) == 0;
            _tcasCommand = interaction.TcasCommand;
        }

        if (interaction.NearPass)
            _audio.PlaySfx(SoundKey.PlanePassNear);
        else if (interaction.FarPass)
            _audio.PlaySfx(SoundKey.PlanePassFar);
    }

    private void UpdateAudioEvents()
    {
        // Uyarı ve skor sesleri burada toplanır; AudioManager cooldown ile spam'i engeller.
        if (_showTcasWarning)
            _audio.PlaySfx(SoundKey.TcasBeep);

        if (_showTerrainWarning)
            _audio.PlaySfx(SoundKey.TerrainDoot);

        if (_showMaydayWarning || _showRotateWarning || _showSeatBeltWarning)
            _audio.PlaySfx(SoundKey.WarningBlip);

        if (_score > _previousScoreForAudio + 0.5f)
            _audio.PlaySfx(SoundKey.ScorePling);

        _previousScoreForAudio = _score;
    }

    private Rectangle GetPlayableArea()
    {
        // Uçağı ekranın alt tarafına yakın tutarak öndeki hava ve araziyi daha görünür yapıyoruz.
        return new Rectangle(
            0,
            ForwardViewMargin,
            ScreenWidth,
            ScreenHeight - HudHeight - ForwardViewMargin - AircraftBottomMargin);
    }

    private float GetWorldSpeed()
    {
        return MathHelper.Clamp(_speed * 1.05f, 55f, 175f);
    }

    private bool ShouldShowClouds()
    {
        // Havalimanı/pist görünürken bulut üretmeyerek kalkış ekranını temiz tutuyoruz.
        return _flightPhase != FlightPhase.Takeoff && !_terrain.IsAirportVisible;
    }

    private bool ShouldShowTraffic()
    {
        // Kalkış ve yaklaşmada trafik sistemi şimdilik kapalı; cruise prototipine odaklanıyoruz.
        return _flightPhase is FlightPhase.InitialClimb or FlightPhase.Cruise;
    }

    private bool IsApproachMode()
    {
        return _flightPhase is FlightPhase.Approach or FlightPhase.Landing;
    }

    private void TriggerGameOver(string reason)
    {
        // Game over sebebini HUD'da göstererek oyuncuya ne olduğunu açık anlatıyoruz.
        _gameOverReason = reason;
        _isGameOver = true;
        _flightPhase = FlightPhase.GameOver;
    }

    private void DrawHomeScreen()
    {
        var bgTop = new Color(16, 28, 38);
        var bgMid = new Color(26, 58, 66);
        var bgGround = new Color(40, 72, 50);
        var textColor = new Color(224, 248, 208);
        var accentColor = new Color(248, 216, 72);

        // Ana ekran düşük çözünürlükte çizilir; pencereye büyüyünce piksel görünümü korunur.
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, ScreenWidth, 210), bgTop);
        _spriteBatch.Draw(_pixel, new Rectangle(0, 210, ScreenWidth, 190), bgMid);
        _spriteBatch.Draw(_pixel, new Rectangle(0, 400, ScreenWidth, ScreenHeight - 400), bgGround);

        for (var i = 0; i < 18; i++)
        {
            var x = 28 + i * 36;
            var y = 44 + (i * 37) % 120;
            _spriteBatch.Draw(_pixel, new Rectangle(x, y, 2, 2), new Color(210, 230, 230));
        }

        PixelTextRenderer.Draw(_spriteBatch, _pixel, "LTFM TO LTCC", new Vector2(184, 96), accentColor, 4);
        PixelTextRenderer.Draw(_spriteBatch, _pixel, "RETRO FLIGHT", new Vector2(176, 142), textColor, 4);
        PixelTextRenderer.Draw(_spriteBatch, _pixel, "PRESS ENTER TO START", new Vector2(174, 246), accentColor, 2);

        DrawHomeScreenAircraft(320, 332);
        DrawHomeScreenLogo();
    }

    private void DrawHomeScreenAircraft(int centerX, int y)
    {
        var body = new Color(218, 232, 224);
        var shade = new Color(152, 172, 178);
        var dark = new Color(42, 54, 70);
        var accent = new Color(248, 184, 48);

        // Ana ekrandaki küçük uçak, oyundaki uçakla aynı dili konuşan basit bir piksel şeklidir.
        _spriteBatch.Draw(_pixel, new Rectangle(centerX - 14, y - 28, 28, 56), body);
        _spriteBatch.Draw(_pixel, new Rectangle(centerX - 64, y - 2, 128, 10), body);
        _spriteBatch.Draw(_pixel, new Rectangle(centerX - 54, y + 8, 108, 5), shade);
        _spriteBatch.Draw(_pixel, new Rectangle(centerX - 10, y - 24, 20, 5), dark);
        _spriteBatch.Draw(_pixel, new Rectangle(centerX - 7, y + 6, 4, 4), dark);
        _spriteBatch.Draw(_pixel, new Rectangle(centerX + 3, y + 6, 4, 4), dark);
        _spriteBatch.Draw(_pixel, new Rectangle(centerX - 24, y + 30, 18, 7), body);
        _spriteBatch.Draw(_pixel, new Rectangle(centerX + 6, y + 30, 18, 7), body);
        _spriteBatch.Draw(_pixel, new Rectangle(centerX - 7, y + 28, 14, 28), accent);
    }

    private void DrawHomeScreenLogo()
    {
        var logoSize = 108;
        var logoBox = new Rectangle(ScreenWidth / 2 - logoSize / 2, ScreenHeight - 168, logoSize, logoSize);

        if (_logoTexture != null)
            _spriteBatch.Draw(_logoTexture, logoBox, Color.White);
        else
            _spriteBatch.Draw(_pixel, logoBox, new Color(248, 216, 72));

        // İmza yazısı logonun altında ve okunaklı olacak şekilde merkeze alınır.
        PixelTextRenderer.Draw(_spriteBatch, _pixel, "DESIGN BY CAPTAIN21", new Vector2(244, ScreenHeight - 46), new Color(224, 248, 208), 2);
    }

    private void DrawScaledRenderTarget()
    {
        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);

        // PointClamp, iç görüntüyü pencereye bulanıklaştırmadan büyütür.
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _spriteBatch.Draw(_gameRenderTarget, new Rectangle(0, 0, WindowWidth, WindowHeight), Color.White);
        _spriteBatch.End();
    }

    private float GetTakeoffVisualScale()
    {
        if (_flightPhase != FlightPhase.Takeoff)
            return 1f;

        // Kalkış koşusunda uçak kameraya yaklaşıyormuş gibi hafif büyür.
        var rotateRatio = MathHelper.Clamp(_speed / RotateSpeed, 0f, 1f);
        return MathHelper.Lerp(0.72f, 1f, rotateRatio);
    }

    private void RestartGame()
    {
        // Restart prototipte tüm temel görev değerlerini başlangıç durumuna döndürür.
        _aircraft = new PlayerAircraft(new Vector2(ScreenWidth / 2f, ScreenHeight - HudHeight - 24));
        _terrain = new ScrollingTerrain(ScreenWidth, ScreenHeight - HudHeight);
        _clouds = new CloudField(ScreenWidth, ScreenHeight - HudHeight);
        _traffic = new TrafficField(ScreenWidth, ScreenHeight - HudHeight);
        _turbulenceLevel = TurbulenceLevel.None;
        _shakeTimer = 0f;
        _cloudDepthCue = 0f;
        _passengerComfort = 100f;
        _score = 1000f;
        _altitude = 0f;
        _speed = 35f;
        _fuel = 8.6f;
        _estimatedFuelAtDestination = 0f;
        _showMaydayWarning = false;
        _remainingNm = _totalRouteNm;
        _belowMoraTimer = 0f;
        _rotateWindowTimer = 0f;
        _localizerDeviation = 0f;
        _glideDeviation = 0f;
        _verticalSpeedFpm = -700f;
        _selectedApproachVerticalSpeed = -700f;
        _approachGustTimer = 5f;
        _approachGustForce = 0f;
        _approachVerticalGust = 0f;
        _approachBank = 0f;
        _landingQuality = "";
        _approachInitialized = false;
        _seatBeltOn = false;
        _seatBeltOnTimer = 0f;
        _showSeatBeltWarning = false;
        _terrainWarning = false;
        _showTerrainWarning = false;
        _showRotateWarning = false;
        _showTcasWarning = false;
        _tcasCommand = "";
        _gameOverReason = "";
        _previousScoreForAudio = _score;
        _isGameOver = false;
        _flightPhase = FlightPhase.Takeoff;
    }

    protected override void UnloadContent()
    {
        _audio.Dispose();
        base.UnloadContent();
    }
}
