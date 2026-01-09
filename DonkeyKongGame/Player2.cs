using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace DonkeyKongGame
{
    public class Player2
    {
        private const float WalkSpeed = 5.0f;

        public float X { get; private set; }
        public float Y { get; private set; }

        public const int Width = 26;
        public const int Height = 32;

        private bool _facingRight = true;
        public bool FacingRight => _facingRight;

        private Image _image;
        private readonly MapManager _map;

        // --- Weapon Cooldown ---
        private int _weaponCooldownTimer = 0;
        private const int WeaponCooldownFrames = 40; // 約 1 秒（Timer Interval = 16ms）

        public bool CanFireWeapon => _weaponCooldownTimer <= 0;

        // --- Idle Animation ---
        private Image[] _idleFrames;
        private int _idleFrameIndex = 0;
        private int _animTimer = 0;
        private const int AnimSpeed = 10;

        // --- Run Animation ---
        private Image[] _runFrames;
        private int _runFrameIndex = 0;
        private int _runTimer = 0;
        private const int RunAnimSpeed = 5;

        // --- Attack Animation ---
        private Image[] _attackFrames;
        private int _attackFrameIndex = 0;
        private bool _isAttacking = false;

        // --- Attack2 Animation ---
        private Image[] _attack2Frames;
        private int _attack2FrameIndex = 0;
        private bool _isAttacking2 = false;

        private int _attack2Timer = 0;
        private const int Attack2AnimSpeed = 4;
        private int _attackTimer = 0;
        private const int AttackAnimSpeed = 4;

        // --- Death Animation ---
        private Image[] _deathFrames;
        private int _deathFrameIndex = 0;
        private int _deathTimer = 0;
        private const int DeathAnimSpeed = 4;
        private bool _isDead = false;
        private bool _deathAnimFinished = false;

        public bool IsDeathFinished => _deathAnimFinished;

        // --- SteamMan Death Sound ---
        [DllImport("winmm.dll")]
        private static extern long mciSendString(
            string strCommand,
            StringBuilder strReturn,
            int iReturnLength,
            IntPtr hwndCallback
        );

        private void PlaySteamDeathSound()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string sfxPath = Path.Combine(baseDir, "assets", "steamDeathAudio.mp3");

            // 確保可以重播（但只播一次，下面有防呆）
            mciSendString("close SteamDeathSFX", null, 0, IntPtr.Zero);

            string openCmd = $"open \"{sfxPath}\" type mpegvideo alias SteamDeathSFX";
            mciSendString(openCmd, null, 0, IntPtr.Zero);

            mciSendString("play SteamDeathSFX", null, 0, IntPtr.Zero);
        }


        public Player2(MapManager map)
        {
            _map = map;

            // 先用跟 Player1 一樣的起點，再做一點偏移，避免重疊
            Point start = _map.GetStartPosition();  // MapManager 已有這個方法 :contentReference[oaicite:2]{index=2}
            X = start.X + 100;   // 往右偏移 100px，你想改成 200 也行
            Y = start.Y - 943;

            LoadImage();
        }

        private void LoadImage()
        {
            string baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SteamMan");

            // Load Idle Animation
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
                    // fallback：紅色方塊，方便 debug
                    Bitmap bmp = new Bitmap(Width, Height);
                    using (Graphics g = Graphics.FromImage(bmp))
                        g.Clear(Color.Red);
                    _idleFrames[i] = bmp;
                }
            }

            // Load Attack Animation (attack1.png to attack3.png)
            _attackFrames = new Image[6];
            for (int i = 0; i < 6; i++)
            {
                string path = Path.Combine(baseDir, $"attack{i + 1}.png");
                if (File.Exists(path))
                {
                    _attackFrames[i] = Image.FromFile(path);
                }
                else
                {
                    Bitmap bmp = new Bitmap(Width, Height);
                    using (Graphics g = Graphics.FromImage(bmp)) g.Clear(Color.DarkRed);
                    _attackFrames[i] = bmp;
                }
            }

            // Load Attack2 Animation (attack2_1.png to attack2_6.png)
            _attack2Frames = new Image[6];
            for (int i = 0; i < 6; i++)
            {
                string path = Path.Combine(baseDir, $"attack2_{i + 1}.png");
                if (File.Exists(path))
                {
                    _attack2Frames[i] = Image.FromFile(path);
                }
                else
                {
                    Bitmap bmp = new Bitmap(Width, Height);
                    using (Graphics g = Graphics.FromImage(bmp)) g.Clear(Color.DarkSlateBlue);
                    _attack2Frames[i] = bmp;
                }
            }


            // Load Run Animation (run1.png to run6.png)
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
                    // fallback：藍色方塊
                    Bitmap bmp = new Bitmap(Width, Height);
                    using (Graphics g = Graphics.FromImage(bmp)) g.Clear(Color.Blue);
                    _runFrames[i] = bmp;
                }
            }

            // Load Death Animation (death1.png to death6.png)
            _deathFrames = new Image[6];
            for (int i = 0; i < 6; i++)
            {
                string path = Path.Combine(baseDir, $"death{i + 1}.png");
                if (File.Exists(path)) _deathFrames[i] = Image.FromFile(path);
                else
                {
                    Bitmap bmp = new Bitmap(Width, Height);
                    using (Graphics g = Graphics.FromImage(bmp)) g.Clear(Color.Gray);
                    _deathFrames[i] = bmp;
                }
            }

        }

        public void TriggerAttack()
        {
            if (_attackFrames == null || _attackFrames.Length == 0) return;

            // 如果正在攻擊，就不要每次按鍵都重置（看你要不要）
            // 想要「連按就重新開始」：把 if 拿掉
            if (_isAttacking) return;

            _isAttacking = true;
            _attackFrameIndex = 0;
            _attackTimer = 0;
        }

        public void TriggerAttack2()
        {
            if (_attack2Frames == null || _attack2Frames.Length == 0) return;

            // 如果正在播任一攻擊，就不重置（你想可改成可打斷）
            if (_isAttacking || _isAttacking2) return;

            _isAttacking2 = true;
            _attack2FrameIndex = 0;
            _attack2Timer = 0;
        }

        public void StartWeaponCooldown()
        {
            _weaponCooldownTimer = WeaponCooldownFrames;
        }

        public void Update(bool left, bool right)
        {
            // --- Death Animation has highest priority ---
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

            // --- Movement Input ---
            float dx = 0;
            if (left) dx = -WalkSpeed;
            if (right) dx = WalkSpeed;

            bool isMoving = (dx != 0);

            if (left) _facingRight = false;
            if (right) _facingRight = true;

            // 你可以選擇：攻擊時不能移動
            // if (_isAttacking) dx = 0;

            // --- Move and Collision ---
            X += dx;

            // --- Clamp to screen/world bounds (0 ~ 1920-Width) ---
            if (X < 0) X = 0;
            if (X > 1920 - Width) X = 1920 - Width;

            // --- Attack2 has highest priority ---
            if (_isAttacking2)
            {
                _attack2Timer++;
                if (_attack2Timer >= Attack2AnimSpeed)
                {
                    _attack2Timer = 0;
                    _attack2FrameIndex++;

                    if (_attack2FrameIndex >= 6)
                    {
                        _attack2FrameIndex = 0;
                        _isAttacking2 = false;
                    }
                }

                // 攻擊時不要推進 idle/run
                _idleFrameIndex = 0;
                _runFrameIndex = 0;
                return;
            }

            // --- Attack1 next priority ---
            if (_isAttacking)
            {
                _attackTimer++;
                if (_attackTimer >= AttackAnimSpeed)
                {
                    _attackTimer = 0;
                    _attackFrameIndex++;

                    if (_attackFrameIndex >= 6)
                    {
                        _attackFrameIndex = 0;
                        _isAttacking = false;
                    }
                }

                _idleFrameIndex = 0;
                _runFrameIndex = 0;
                return;
            }

            // --- Weapon Cooldown Timer ---
            if (_weaponCooldownTimer > 0)
            {
                _weaponCooldownTimer--;
            }

            // --- Run Animation (when moving) ---
            if (isMoving)
            {
                _idleFrameIndex = 0;
                _animTimer = 0; // 可選：避免 idle timer 累積造成回到 idle 時跳帧

                _runTimer++;
                if (_runTimer >= RunAnimSpeed)
                {
                    _runTimer = 0;
                    _runFrameIndex++;
                    if (_runFrameIndex >= 6) _runFrameIndex = 0;
                }
            }
            else
            {
                _runFrameIndex = 0;
                _runTimer = 0; // 可選：停下就從 run1 開始

                // --- Idle Animation (only when not moving) ---
                _animTimer++;
                if (_animTimer >= AnimSpeed)
                {
                    _animTimer = 0;
                    _idleFrameIndex++;
                    if (_idleFrameIndex >= 4) _idleFrameIndex = 0;
                }
            }

        }

        public void Draw(Graphics g)
        {
            // --- Death Animation has highest priority ---
            if (_isDead && _deathFrames != null && _deathFrames.Length > 0)
            {
                Image img1 = _deathFrames[_deathFrameIndex];
                if (_facingRight) g.DrawImage(img1, X, Y, Width, Height);
                else
                {
                    var state = g.Save();
                    g.TranslateTransform(X + Width, Y);
                    g.ScaleTransform(-1, 1);
                    g.DrawImage(img1, 0, 0, Width, Height);
                    g.Restore(state);
                }
                return;
            }


            Image img = null;

            // 1) Attack2 first
            if (_isAttacking2 && _attack2Frames != null && _attack2Frames.Length > 0)
            {
                img = _attack2Frames[_attack2FrameIndex];
            }
            // 2) Attack1
            else if (_isAttacking && _attackFrames != null && _attackFrames.Length > 0)
            {
                img = _attackFrames[_attackFrameIndex];
            }
            // 3) Run
            else if (_runFrames != null && _runFrames.Length > 0 && _runFrameIndex != 0)
            {
                img = _runFrames[_runFrameIndex];
            }
            // 4) Idle
            else if (_idleFrames != null && _idleFrames.Length > 0)
            {
                img = _idleFrames[_idleFrameIndex];
            }

            if (img == null) return;

            if (_facingRight)
            {
                g.DrawImage(img, X, Y, Width, Height);
            }
            else
            {
                var state = g.Save();
                g.TranslateTransform(X + Width, Y);
                g.ScaleTransform(-1, 1);
                g.DrawImage(img, 0, 0, Width, Height);
                g.Restore(state);
            }
        }

        public void TriggerDeath()
        {
            if (_isDead) return;
            _isDead = true;
            _deathAnimFinished = false;
            _deathFrameIndex = 0;
            _deathTimer = 0;

            PlaySteamDeathSound();
        }

    }
}
