using System;
using System.Drawing;
using System.IO;

namespace DonkeyKongGame
{
    public class Bomb
    {
        // --- Dimensions ---
        public const int Width = 48;
        private const int Height = 48;

        // Physics
        private const float Gravity = 1.5f;
        private const float RollSpeed = 5.0f;
        private const float LadderSpeed = 4.0f;

        // Position
        public float X { get; private set; }
        public float Y { get; private set; }
        private float _vx;
        private float _vy;


        // State
        private enum BombState { Falling, Rolling, OnLadder, Exploding }
        private BombState _state = BombState.Falling;
        public bool IsDead { get; private set; } = false;
        public bool IsExploding => _state == BombState.Exploding;

        // Animation
        private Image[] _explosionFrames;  // Explosion Frames
        private Image[] _frames;
        private int _frameIndex = 0;
        private int _animTimer = 0;
        private const int AnimSpeed = 4;
        private static Random _rng = new Random();

        // Logic Timer
        private int _ladderTimer = 0;
        private const int LadderSafetyTime = 40; // Approx 1 second at 60FPS

        // Spawn safety: ignore floor collision for a few frames after spawn
        private int _spawnIgnoreFloorTimer = 8; // 8 frames ~ 0.13s

        private MapManager _map;

        public Bomb(float startX, float startY, MapManager map)
        {
            X = startX;
            Y = startY;
            _map = map;
            LoadImages();
        }

        private void LoadImages()
        {
            string baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Bombs", "RollingBomb");
            _frames = new Image[12];

            for (int i = 0; i < 12; i++)
            {
                int fileNum = i + 2;
                string path = Path.Combine(baseDir, $"RollingBomb{fileNum}.png");

                if (File.Exists(path))
                    _frames[i] = Image.FromFile(path);
                else
                {
                    Bitmap bmp = new Bitmap(Width, Height);
                    using (Graphics g = Graphics.FromImage(bmp)) g.Clear(Color.Black);
                    _frames[i] = bmp;
                }
            }
            string explosionDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Bombs", "Explosion");
            _explosionFrames = new Image[17]; // 18 - 2 + 1 = 17 frames

            for (int i = 0; i < 17; i++)
            {
                int fileNum = i + 2; // Starts at 2
                string path = Path.Combine(explosionDir, $"Explosion{fileNum}.png");

                if (File.Exists(path))
                    _explosionFrames[i] = Image.FromFile(path);
                else
                {
                    // Fallback Red Box
                    Bitmap bmp = new Bitmap(Width, Height);
                    using (Graphics g = Graphics.FromImage(bmp)) g.Clear(Color.OrangeRed);
                    _explosionFrames[i] = bmp;
                }
            }
        }
        public void Explode()
        {
            if (_state != BombState.Exploding)
            {
                _state = BombState.Exploding;

                // FIX: Reset animation counters so explosion starts at frame 0
                _frameIndex = 0;
                _animTimer = 0;

                // Optional: Stop movement immediately
                _vx = 0;
                _vy = 0;
            }
        }

        public void Update()
        {
            // 1. Update Animation
            _animTimer++;
            if (_animTimer >= AnimSpeed)
            {
                _animTimer = 0;
                _frameIndex++;

                // --- FIX LOGIC STARTS HERE ---
                if (_state == BombState.Exploding)
                {
                    // Check against EXPLOSION frames
                    if (_frameIndex >= _explosionFrames.Length)
                    {
                        // Animation finished -> Kill the bomb
                        IsDead = true;
                        _frameIndex = _explosionFrames.Length - 1; // Stay on last frame
                    }
                }
                else
                {
                    // Normal Loop (Rolling)
                    if (_frameIndex >= _frames.Length) _frameIndex = 0;
                }
                // --- FIX LOGIC ENDS HERE ---
            }

            // 2. Physics & State Logic
            switch (_state)
            {
                case BombState.Falling:
                    _vy += Gravity;
                    Y += _vy;

                    // PRIORITY: Check for Wood first (Layer 1)
                    // If we hit wood, we grab it and ignore the floor below
                    if (TryGetWoodColumn(out int woodCol))
                    {
                        StartClimbing(woodCol);
                    }
                    else
                    {
                        if (_spawnIgnoreFloorTimer > 0)
                        {
                            _spawnIgnoreFloorTimer--;
                        }
                        else
                        {
                            CheckFloorCollision();
                        }
                    }
                    break;

                case BombState.Rolling:
                    HandleRolling();
                    break;

                case BombState.OnLadder:
                    HandleClimbing();
                    break;

                case BombState.Exploding:
                    // Physics are disabled during explosion
                    break;
            }

            // Apply Horizontal Velocity
            X += _vx;
        }

        private void StartClimbing(int col)
        {
            _state = BombState.OnLadder;
            _vx = 0;
            _vy = LadderSpeed;
            _ladderTimer = 0; // Reset timer for the "blind fall" logic

            // Snap X to the wood tile's center (col is the wood tile col)
            X = (col * 48) + (48 - Width) / 2;
        }

        private void CheckFloorCollision()
        {
            Rectangle rect = GetBounds();

            if (IsTouchingTile(rect, 1, 0))
            {
                if (_vy > 0)
                {
                    float feetY = Y + Height;
                    float tileRow = (float)Math.Floor((feetY - 1) / 48.0);

                    // Snap exactly to top of the tile
                    Y = (tileRow * 48) - Height;

                    _vy = 0;
                    StartRolling();
                }
            }
        }

        private void StartRolling()
        {
            _state = BombState.Rolling;
            int dir = _rng.Next(0, 2) == 0 ? -1 : 1;
            _vx = dir * RollSpeed;
        }

        public bool IsOffScreen(int screenHeight)
        {
            return Y > screenHeight + 200;
        }

        private void HandleRolling()
        {
            // 1. Check for Wood (Layer 1) overlapping the body
            if (TryGetWoodColumn(out int woodCol))
            {
                // 50% chance to climb down
                if (_rng.NextDouble() > 0.5)
                {
                    StartClimbing(woodCol);
                    return;
                }
            }

            // 2. Check if ground ends (Layer 0)
            Rectangle feetRect = new Rectangle((int)X, (int)Y + Height + 2, Width, 4);
            if (!IsTouchingTile(feetRect, 1, 0))
            {
                _state = BombState.Falling;
                _vx = 0;
                return;
            }

            // 3. Check Walls (Layer 0)
            int lookAhead = (_vx > 0) ? 5 : -5;
            Rectangle wallCheck = new Rectangle((int)X + lookAhead, (int)Y + 2, Width, Height - 4);

            if (IsTouchingTile(wallCheck, 1, 0))
            {
                _vx = -_vx; // Bounce
            }
        }

        private void HandleClimbing()
        {
            Y += _vy;
            _ladderTimer++;

            // --- THE 1-SECOND LOGIC ---
            // If we have been on the ladder for less than ~1 second (60 frames),
            // we IGNORE floor collisions. This allows us to pass through the 
            // floor tile that the wood is sitting on top of.
            if (_ladderTimer < LadderSafetyTime)
            {
                return;
            }

            // --- Normal Ladder Logic (After 1 Second) ---

            // 1. Check if we are still touching Wood/Ladder
            Rectangle centerRect = new Rectangle((int)X + Width / 2 - 2, (int)Y, 4, Height);
            bool stillOnWood = IsTouchingTile(centerRect, 2, 1);

            // Note: You might also want to check Layer 3 (Ladder) here if your ladder continues below the wood
            // bool onLadderTile = IsTouchingTile(centerRect, 4, 3);

            if (!stillOnWood)
            {
                // If we ran out of wood, start falling
                _state = BombState.Falling;
                // _vy will be increased by gravity next frame
            }

            // 2. Check if hit ground (ID 1, Layer 0)
            Rectangle feetRect = new Rectangle((int)X, (int)Y + Height, Width, 4);
            if (IsTouchingTile(feetRect, 1, 0))
            {
                float tileRow = (float)Math.Floor((Y + Height) / 48.0);
                Y = (tileRow * 48) - Height;
                StartRolling();
            }
        }

        private bool TryGetWoodColumn(out int woodCol)
        {
            woodCol = -1;

            // 用窄直條（靠 bomb 中心），降低擦邊 miss
            Rectangle probe = new Rectangle(
                (int)X + Width / 2 - 2,
                (int)Y + 4,
                4,
                Height - 8
            );

            int leftTile = probe.Left / 48;
            int rightTile = probe.Right / 48;
            int topTile = probe.Top / 48;
            int bottomTile = probe.Bottom / 48;

            if (leftTile < 0) leftTile = 0;
            if (topTile < 0) topTile = 0;

            for (int c = leftTile; c <= rightTile; c++)
            {
                for (int r = topTile; r <= bottomTile; r++)
                {
                    if (_map.GetTileID(c, r, 1) == 2) // wood id=2 on layer 1
                    {
                        woodCol = c;
                        return true;
                    }
                }
            }

            return false;
        }

        // *** FIX 2: Added layerIdx parameter to match Player.cs logic ***
        private bool IsTouchingTile(Rectangle rect, int targetId, int layerIdx)
        {
            int leftTile = rect.Left / 48;
            int rightTile = rect.Right / 48;
            int topTile = rect.Top / 48;
            int bottomTile = rect.Bottom / 48;

            // Bounds safety
            if (leftTile < 0) leftTile = 0;
            if (topTile < 0) topTile = 0;
            // You can add max width/height checks here if you have map dimensions

            for (int c = leftTile; c <= rightTile; c++)
            {
                for (int r = topTile; r <= bottomTile; r++)
                {
                    if (_map.GetTileID(c, r, layerIdx) == targetId) return true;
                }
            }
            return false;
        }

        public Rectangle GetBounds()
        {
            return new Rectangle((int)X, (int)Y, Width, Height);
        }

        public void Draw(Graphics g)
        {
            if (_state == BombState.Exploding)
            {
                if (_frameIndex < _explosionFrames.Length && _explosionFrames[_frameIndex] != null)
                {
                    float scale = 8f;
                    float expWidth = Width * scale;
                    float expHeight = Height * scale;

                    // 1. Center the animation horizontally
                    float drawX = X + (Width - expWidth) / 2;

                    // 2. Define how many pixels UP you want to shift it
                    float moveUpAmount = 160; // Change this number to go higher/lower

                    // 3. Calculate Y: Center it, then subtract to move UP
                    float drawY = Y + (Height - expHeight) / 2 - moveUpAmount;

                    g.DrawImage(_explosionFrames[_frameIndex], drawX, drawY, expWidth, expHeight);
                }
            }
            else
            {
                if (_frames[_frameIndex] != null)
                {
                    g.DrawImage(_frames[_frameIndex], X, Y, Width, Height);
                }
            }
        }
    }
}

