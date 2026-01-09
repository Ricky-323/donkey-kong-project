using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace DonkeyKongGame
{
    public class Player
    {
        // Physics Constants
        private const float Gravity = 1.0f;
        private const float WalkSpeed = 3.0f;
        private const float JumpForce = -15.0f;
        private const float ClimbSpeed = 3.0f;
        private float _knockbackX = 0;

        // Position & Velocity
        public float X { get; private set; }
        public float Y { get; private set; }
        private float _vy;

        // Dimensions (Kept 26x32 as requested)
        public const int Width = 26;
        public const int Height = 32;

        // State
        private bool _onGround;
        private bool _isClimbing;
        private bool _facingRight = true;

        // --- Death State ---
        private bool _isDead = false;

        // 0 = Not Hurt
        // 1 = Flash 1 (Hurt Sprite)
        // 2 = Gap (Normal Sprite, but input locked)
        // 3 = Flash 2 (Hurt Sprite)
        private int _hurtPhase = 0;
        private int _hurtTimer = 0;

        // Duration configuration (in frames, approx 16ms per frame)
        private const int PhaseDuration_Flash = 15;
        private const int PhaseDuration_Gap = 10;

        // Animations

        // Climb
        private Image _playerImage;
        private Image[] _climbFrames;
        private int _climbFrameIndex = 0;
        private int _animTimer = 0;
        private const int AnimSpeed = 5;

        // Run
        private Image[] _runFrames;
        private int _runFrameIndex = 0;

        // Idle
        private Image[] _idleFrames;
        private int _idleFrameIndex = 0;

        // Jump
        private Image[] _jumpFrames;
        private int _jumpFrameIndex = 0;

        // Hurt
        private Image _hurtImage;

        // Death
        private Image[] _deathFrames;
        private int _deathFrameIndex = 0;
        private int _deathTimer = 0;
        private const int DeathAnimSpeed = 6;
        private bool _deathAnimFinished = false;

        // --- Attack ---
        private Image[] _attackFrames;
        private int _attackFrameIndex = 0;
        private int _attackTimer = 0;
        private const int AttackAnimSpeed = 4;
        private bool _isAttacking = false;
        private bool _attackAnimFinished = false;
        public bool IsAttackFinished => _attackAnimFinished;

        public bool IsDeathFinished => _deathAnimFinished;

        [DllImport("winmm.dll")]
        private static extern long mciSendString(string strCommand, StringBuilder strReturn, int iReturnLength, IntPtr hwndCallback);

        private void PlayWoodDeathSound()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string sfxPath = Path.Combine(baseDir, "assets", "woodDeathAudio.mp3");

            mciSendString("close WoodDeathSFX", null, 0, IntPtr.Zero);

            string openCmd = $"open \"{sfxPath}\" type mpegvideo alias WoodDeathSFX";
            mciSendString(openCmd, null, 0, IntPtr.Zero);

            mciSendString("play WoodDeathSFX", null, 0, IntPtr.Zero);
        }
        private void PlayHurtSound()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string sfxPath = Path.Combine(baseDir, "assets", "woodHurtAudio.mp3");

            // 1. Close any previous instance so we can replay immediately if hit rapidly
            mciSendString("close WoodHurtSFX", null, 0, IntPtr.Zero);

            // 2. Open the file
            string commandOpen = $"open \"{sfxPath}\" type mpegvideo alias WoodHurtSFX";
            mciSendString(commandOpen, null, 0, IntPtr.Zero);

            // 3. Play
            string commandPlay = "play WoodHurtSFX";
            mciSendString(commandPlay, null, 0, IntPtr.Zero);
        }


        // References
        private MapManager _map;

        public Player(MapManager map)
        {
            _map = map;

            // Set Initial Position
            Point start = _map.GetStartPosition();
            X = start.X;
            Y = start.Y;

            LoadImages();
        }

        private void LoadImages()
        {
            string baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WoodCutter");

            // 1. Load Main Character (Idle/Walk)
            string mainPath = Path.Combine(baseDir, "Woodcutter.png");
            if (File.Exists(mainPath))
            {
                _playerImage = Image.FromFile(mainPath);
            }
            else
            {
                // Fallback: Green Box
                Bitmap bmp = new Bitmap(Width, Height);
                using (Graphics g = Graphics.FromImage(bmp)) g.Clear(Color.Green);
                _playerImage = bmp;
            }

            // 2. Load Climbing Animation (climb1.png to climb6.png)
            _climbFrames = new Image[6];
            for (int i = 0; i < 6; i++)
            {
                // File names: climb1.png, climb2.png ... climb6.png
                string path = Path.Combine(baseDir, $"climb{i + 1}.png");

                if (File.Exists(path))
                {
                    _climbFrames[i] = Image.FromFile(path);
                }
                else
                {
                    // Fallback: Orange Box if missing
                    Bitmap bmp = new Bitmap(Width, Height);
                    using (Graphics g = Graphics.FromImage(bmp)) g.Clear(Color.Orange);
                    _climbFrames[i] = bmp;
                }
            }

            // 3. Load Running Animation (run1.png to run6.png)
            _runFrames = new Image[6];
            for (int i = 0; i < 6; i++)
            {
                string path = Path.Combine(baseDir, $"run{i + 1}.png");
                if (File.Exists(path))
                {
                    _runFrames[i] = Image.FromFile(path);
                }
                else
                {
                    Bitmap bmp = new Bitmap(Width, Height);
                    using (Graphics g = Graphics.FromImage(bmp)) g.Clear(Color.Blue);
                    _runFrames[i] = bmp;
                }
            }

            // 4. Load Idle Animation (idle1.png to idle4.png)
            _idleFrames = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                string path = Path.Combine(baseDir, $"idle{i + 1}.png");
                if (File.Exists(path))
                {
                    _idleFrames[i] = Image.FromFile(path);
                }
                else
                {
                    // Fallback: Purple Box if missing
                    Bitmap bmp = new Bitmap(Width, Height);
                    using (Graphics g = Graphics.FromImage(bmp)) g.Clear(Color.Purple);
                    _idleFrames[i] = bmp;
                }
            }

            // 5. Load Jump Animation (jump1.png to jump6.png)
            _jumpFrames = new Image[6];
            for (int i = 0; i < 6; i++)
            {
                string path = Path.Combine(baseDir, $"jump{i + 1}.png");
                if (File.Exists(path))
                {
                    _jumpFrames[i] = Image.FromFile(path);
                }
                else
                {
                    // Fallback: Yellow Box if missing
                    Bitmap bmp = new Bitmap(Width, Height);
                    using (Graphics g = Graphics.FromImage(bmp)) g.Clear(Color.Yellow);
                    _jumpFrames[i] = bmp;
                }
            }

            // 6. Load Attack Animation (attack1.png ~ attack6.png)
            _attackFrames = new Image[6];
            for (int i = 0; i < 6; i++)
            {
                string path = Path.Combine(baseDir, $"attack{i + 1}.png");
                if (File.Exists(path)) _attackFrames[i] = Image.FromFile(path);
                else _attackFrames[i] = CreateFallback(Color.Brown);
            }


            // 7. Load Death Animation (death1.png ~ death6.png)
            _deathFrames = new Image[6];
            for (int i = 0; i < 6; i++)
            {
                string path = Path.Combine(baseDir, $"death{i + 1}.png");
                if (File.Exists(path))
                {
                    _deathFrames[i] = Image.FromFile(path);
                }
                else
                {
                    _deathFrames[i] = CreateFallback(Color.Black);
                }
            }


            // Load Hurt Image
            string hurtPath = Path.Combine(baseDir, "hurt.png");
            if (File.Exists(hurtPath)) _hurtImage = Image.FromFile(hurtPath);
            else _hurtImage = CreateFallback(Color.Red);
        }

        private Image CreateFallback(Color c)
        {
            Bitmap bmp = new Bitmap(Width, Height);
            using (Graphics g = Graphics.FromImage(bmp)) g.Clear(c);
            return bmp;
        }

        public void Update(bool left, bool right, bool up, bool down, bool jump)
        {
            float vx = 0;

            if (Math.Abs(_knockbackX) > 0.5f)
            {
                // Apply bounce force
                vx = _knockbackX;

                // Slow down the bounce (Friction)
                _knockbackX *= 0.9f;
            }
            else
            {
                // Normal Movement
                if (left) vx = -WalkSpeed;
                if (right) vx = WalkSpeed;
            }

            // Apply Velocity to Position
            X += vx;

            // Death Logic (highest priority)
            if (_isDead)
            {
                if (!_deathAnimFinished)
                {
                    _deathTimer++;
                    if (_deathTimer >= DeathAnimSpeed)
                    {
                        _deathTimer = 0;
                        _deathFrameIndex++;

                        if (_deathFrameIndex >= _deathFrames.Length)
                        {
                            _deathFrameIndex = _deathFrames.Length - 1;
                            _deathAnimFinished = true;
                        }
                    }
                }
                return;
            }

            // Attack Logic (second highest priority)
            if (_isAttacking)
            {
                _attackTimer++;
                if (_attackTimer >= AttackAnimSpeed)
                {
                    _attackTimer = 0;
                    _attackFrameIndex++;

                    if (_attackFrameIndex >= _attackFrames.Length)
                    {
                        _attackFrameIndex = _attackFrames.Length - 1;
                        _isAttacking = false;
                        _attackAnimFinished = true;
                    }
                }
                return;
            }

            bool inputLocked = false;

            if (_hurtPhase != 0)
            {
                inputLocked = true;
                _hurtTimer--;

                if (_hurtTimer <= 0)
                {
                    if (_hurtPhase == 1)
                    {
                        _hurtPhase = 2;
                        _hurtTimer = PhaseDuration_Gap;
                    }
                    else if (_hurtPhase == 2)
                    {
                        _hurtPhase = 3;
                        _hurtTimer = PhaseDuration_Flash;
                    }
                    else // _hurtPhase == 3
                    {
                        _hurtPhase = 0; // done
                    }
                }
            }

            if (inputLocked)
            {
                left = right = up = down = jump = false;
            }

            // --- Input to horizontal velocity ---
            float dx = 0;
            if (left) dx = -WalkSpeed;
            if (right) dx = WalkSpeed;

            bool isMovingHorizontally = (dx != 0);

            // Update facing direction (for flipping)
            if (left) _facingRight = false;
            if (right) _facingRight = true;

            // Remember previous ground state (for landing detection)
            bool wasOnGround = _onGround;

            // --- Horizontal Movement ---
            X += dx;
            CheckHorizontalCollision(dx);

            // --- Clamp to screen/world bounds (0 ~ 1920-Width) ---
            if (X < 0) X = 0;
            if (X > 1920 - Width) X = 1920 - Width;

            // --- Climbing Logic ---
            Rectangle playerRect = GetBounds();
            bool touchingLadder = CheckCollisionWithTile(playerRect, 4, 3); // Layer 2, ID 4

            if (touchingLadder && (up || down))
            {
                _isClimbing = true;
                _onGround = true; // treat as grounded while climbing
                _vy = 0;          // cancel gravity while climbing
            }
            else if (!touchingLadder)
            {
                _isClimbing = false;
            }

            // --- Vertical Movement + Animations ---
            if (_isClimbing)
            {
                // Move Up/Down
                if (up) Y -= ClimbSpeed;
                if (down) Y += ClimbSpeed;

                // Climb animation (only when actually moving on ladder)
                if (up || down)
                {
                    _animTimer++;
                    if (_animTimer >= AnimSpeed)
                    {
                        _animTimer = 0;
                        _climbFrameIndex++;
                        if (_climbFrameIndex >= 6) _climbFrameIndex = 0;
                    }
                }

                // While climbing, don't run/idle/jump
                _runFrameIndex = 0;
                _idleFrameIndex = 0;
                _jumpFrameIndex = 0;
            }
            else
            {
                // Jump (only if grounded)
                if (jump && _onGround)
                {
                    PlayJumpSound();
                    _vy = JumpForce;
                    _onGround = false;
                    _jumpFrameIndex = 0; // start jump animation from first frame
                    _animTimer = 0;      // start cleanly
                }

                // Gravity
                _vy += Gravity;
                Y += _vy;

                // Update _onGround here
                CheckVerticalCollision();

                // Landing detection: if we just landed, reset jump frames
                if (!wasOnGround && _onGround)
                {
                    _jumpFrameIndex = 0;
                }

                // --- JUMP ANIMATION (in air) ---
                if (!_onGround)
                {
                    _runFrameIndex = 0;
                    _idleFrameIndex = 0;

                    _animTimer++;
                    if (_animTimer >= AnimSpeed)
                    {
                        _animTimer = 0;
                        _jumpFrameIndex++;
                        if (_jumpFrameIndex >= 6) _jumpFrameIndex = 0;
                    }
                }
                else
                {
                    // --- RUN / IDLE ANIMATION (grounded) ---
                    _jumpFrameIndex = 0;

                    if (isMovingHorizontally)
                    {
                        _idleFrameIndex = 0;

                        _animTimer++;
                        if (_animTimer >= AnimSpeed)
                        {
                            _animTimer = 0;
                            _runFrameIndex++;
                            if (_runFrameIndex >= 6) _runFrameIndex = 0;
                        }
                    }
                    else
                    {
                        _runFrameIndex = 0;

                        _animTimer++;
                        if (_animTimer >= AnimSpeed)
                        {
                            _animTimer = 0;
                            _idleFrameIndex++;
                            if (_idleFrameIndex >= 4) _idleFrameIndex = 0;
                        }
                    }
                }
            }
        }

        private void CheckHorizontalCollision(float dx)
        {
            Rectangle collisionRect = new Rectangle((int)X, (int)Y + 2, Width, Height - 4);

            if (IsTouchingTile(collisionRect, 1, 0))
            {
                if (dx > 0) // Moving Right
                {
                    int wallCol = (int)Math.Floor((X + Width) / 48.0);
                    X = (wallCol * 48) - Width - 0.1f;
                }
                else if (dx < 0) // Moving Left
                {
                    int wallCol = (int)Math.Floor(X / 48.0);
                    X = (wallCol * 48) + 48 + 0.1f;
                }
            }
        }

        private void CheckVerticalCollision()
        {
            _onGround = false;
            Rectangle rect = GetBounds();

            // Check Layer 0 (Floor) for ID 1
            if (IsTouchingTile(rect, 1, 0))
            {
                // 1. FALLING DOWN
                if (_vy > 0)
                {
                    float feetY = Y + Height;

                    float tileRow = (float)Math.Floor(feetY / 48.0);

                    Y = (tileRow * 48) - Height;

                    _onGround = true;
                    _vy = 0;
                }

            }
        }

        private bool CheckCollisionWithTile(Rectangle rect, int targetId, int layerIdx)
        {
            return IsTileAt(rect.Left, rect.Top, targetId, layerIdx) ||
                   IsTileAt(rect.Right, rect.Top, targetId, layerIdx) ||
                   IsTileAt(rect.Left, rect.Bottom, targetId, layerIdx) ||
                   IsTileAt(rect.Right, rect.Bottom, targetId, layerIdx);
        }

        private bool IsTouchingTile(Rectangle rect, int targetId, int layerIdx)
        {
            int leftTile = rect.Left / 48;
            int rightTile = rect.Right / 48;
            int topTile = rect.Top / 48;
            int bottomTile = rect.Bottom / 48;

            for (int c = leftTile; c <= rightTile; c++)
            {
                for (int r = topTile; r <= bottomTile; r++)
                {
                    if (_map.GetTileID(c, r, layerIdx) == targetId) return true;
                }
            }
            return false;
        }

        private bool IsTileAt(int px, int py, int id, int layer)
        {
            int c = px / 48;
            int r = py / 48;
            return _map.GetTileID(c, r, layer) == id;
        }

        private Rectangle GetBounds()
        {
            return new Rectangle((int)X, (int)Y, Width, Height);
        }

        public Rectangle GetBoundsPublic()
        {
            return new Rectangle((int)X, (int)Y, Width, Height);
        }


        public void Draw(Graphics g)
        {
            // --- DEATH DRAW ---
            if (_isDead && _deathFrames != null)
            {
                Image img = _deathFrames[_deathFrameIndex];
                g.DrawImage(img, X, Y, Width, Height);
                return;
            }

            // --- ATTACK DRAW ---
            if (_isAttacking && _attackFrames != null && _attackFrames.Length > 0)
            {
                Image img = _attackFrames[_attackFrameIndex];
                if (_facingRight)
                    g.DrawImage(img, X, Y, Width, Height);
                else
                {
                    var state = g.Save();
                    g.TranslateTransform(X + Width, Y);
                    g.ScaleTransform(-1, 1);
                    g.DrawImage(img, 0, 0, Width, Height);
                    g.Restore(state);
                }
                return;
            }


            Image imgToDraw = null;

            bool showHurtSprite = (_hurtPhase == 1 || _hurtPhase == 3);
            if (showHurtSprite && _hurtImage != null)
            {
                imgToDraw = _hurtImage;
            }
            else
            {
                // 1) Climb
                if (_isClimbing && _climbFrames != null && _climbFrames.Length > 0)
                {
                    int idx = _climbFrameIndex;
                    if (idx < 0) idx = 0;
                    if (idx >= _climbFrames.Length) idx = _climbFrames.Length - 1;
                    imgToDraw = _climbFrames[idx];
                }
                // 2) Jump
                else if (!_onGround && _jumpFrames != null && _jumpFrames.Length > 0)
                {
                    int idx = _jumpFrameIndex;
                    if (idx < 0) idx = 0;
                    if (idx >= _jumpFrames.Length) idx = _jumpFrames.Length - 1;
                    imgToDraw = _jumpFrames[idx];
                }
                // 3) Run
                else if (_onGround && _runFrames != null && _runFrames.Length > 0 && _runFrameIndex != 0)
                {
                    int idx = _runFrameIndex;
                    if (idx < 0) idx = 0;
                    if (idx >= _runFrames.Length) idx = _runFrames.Length - 1;
                    imgToDraw = _runFrames[idx];
                }
                // 4) Idle
                else if (_onGround && _idleFrames != null && _idleFrames.Length > 0)
                {
                    int idx = _idleFrameIndex;
                    if (idx < 0) idx = 0;
                    if (idx >= _idleFrames.Length) idx = _idleFrames.Length - 1;
                    imgToDraw = _idleFrames[idx];
                }
                // 5) Fallback
                else
                {
                    imgToDraw = _playerImage;
                }
            }

            // Safety fallback
            if (imgToDraw == null)
            {
                using (var brush = new SolidBrush(Color.Red))
                    g.FillRectangle(brush, X, Y, Width, Height);
                return;
            }

            // --- DRAW WITH LEFT / RIGHT FLIP (does not affect other drawings) ---
            if (_facingRight)
            {
                g.DrawImage(imgToDraw, X, Y, Width, Height);
            }
            else
            {
                var state = g.Save();
                g.TranslateTransform(X + Width, Y);
                g.ScaleTransform(-1, 1);
                g.DrawImage(imgToDraw, 0, 0, Width, Height);
                g.Restore(state);
            }
        }
        public void TriggerHurt()
        {
            TriggerHurt(this.X); // Default: no horizontal bounce
        }
        public void TriggerHurt(float damageSourceX)
        {
            PlayHurtSound(); // Ensure this is still here from previous steps

            _hurtPhase = 1;
            _hurtTimer = PhaseDuration_Flash;

            // --- BOUNCE LOGIC ---
            _vy = -6; // 1. Vertical Hop (Bounce up)

            // 2. Horizontal Bounce
            if (Math.Abs(damageSourceX - X) < 1.0f)
            {
                _knockbackX = 0;
            }
            else
            {
                float myCenterX = X + Width / 2;
                float dir = (myCenterX < damageSourceX) ? -1 : 1;

                _knockbackX = 30 * dir; // Strength of bounce (10 is good)
            }
        }

        public void TriggerDeath()
        {
            if (_isDead) return;

            _isDead = true;
            _deathFrameIndex = 0;
            _deathTimer = 0;

            _vy = 0;

            PlayWoodDeathSound();
        }

        public void TriggerAttack()
        {
            if (_attackFrames == null || _attackFrames.Length == 0) return;
            if (_isAttacking) return;

            _isAttacking = true;
            _attackAnimFinished = false;
            _attackFrameIndex = 0;
            _attackTimer = 0;
        }
        private void PlayJumpSound()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string sfxPath = Path.Combine(baseDir, "assets", "jumpAudio.mp3");

            mciSendString("close JumpSFX", null, 0, IntPtr.Zero);

            string openCmd = $"open \"{sfxPath}\" type mpegvideo alias JumpSFX";
            mciSendString(openCmd, null, 0, IntPtr.Zero);

            mciSendString("play JumpSFX", null, 0, IntPtr.Zero);
        }
        public void PlayAppleSound()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string sfxPath = Path.Combine(baseDir, "assets", "appleAudio.mp3");

            mciSendString("close AppleSFX", null, 0, IntPtr.Zero);

            string commandOpen = $"open \"{sfxPath}\" type mpegvideo alias AppleSFX";
            mciSendString(commandOpen, null, 0, IntPtr.Zero);

            string commandPlay = "play AppleSFX";
            mciSendString(commandPlay, null, 0, IntPtr.Zero);
        }


    }
}


