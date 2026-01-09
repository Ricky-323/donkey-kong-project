using System;
using System.Drawing;
using System.IO;

namespace DonkeyKongGame
{
    public class Monster3
    {
        public const int Width = 48;
        public const int Height = 52;

        // Movement
        public float X { get; private set; }
        public float Y { get; private set; }
        private float _speed = 2.0f;
        private int _direction = 1; // 1 = Right, -1 = Left

        // Patrol Boundaries
        private float _minX;
        private float _maxX;

        // Animation Arrays
        private Image[] _runFrames;
        private Image[] _attackFrames; // New Array for Attack

        // Animation State
        private int _frameIndex;
        private int _animTimer;
        private const int RunAnimSpeed = 5;
        private const int AttackAnimSpeed = 3; // Attack might be faster
        private bool _facingRight = true;

        // Logic State
        public bool IsAttacking { get; private set; } = false;

        public Monster3(float startX, float startY, float minX, float maxX)
        {
            X = startX;
            Y = startY;
            _minX = minX;
            _maxX = maxX;
            LoadImages();
        }

        private void LoadImages()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string monsterDir = Path.Combine(baseDir, "monster");

            // 1. Load Run Frames (8 frames)
            _runFrames = new Image[8];
            for (int i = 0; i < 8; i++)
            {
                string path = Path.Combine(monsterDir, $"Big{i + 1}.png");
                if (File.Exists(path)) _runFrames[i] = Image.FromFile(path);
                else _runFrames[i] = CreateFallbackBitmap(Color.Red);
            }

            // 2. Load Attack Frames (10 frames)
            _attackFrames = new Image[6];
            for (int i = 0; i < 6; i++)
            {
                string path = Path.Combine(monsterDir, $"BigAttack{i + 1}.png");
                if (File.Exists(path)) _attackFrames[i] = Image.FromFile(path);
                else _attackFrames[i] = CreateFallbackBitmap(Color.Orange);
            }
        }

        private Image CreateFallbackBitmap(Color c)
        {
            Bitmap bmp = new Bitmap(Width, Height);
            using (Graphics g = Graphics.FromImage(bmp)) g.Clear(c);
            return bmp;
        }

        public void TriggerAttack()
        {
            if (IsAttacking) return; // Don't restart if already attacking

            IsAttacking = true;
            _frameIndex = 0; // Start animation from frame 0
            _animTimer = 0;
        }

        public void Update()
        {
            if (IsAttacking)
            {
                UpdateAttack();
            }
            else
            {
                UpdatePatrol();
            }
        }

        private void UpdateAttack()
        {
            // Don't move X while attacking (optional)

            _animTimer++;
            if (_animTimer >= AttackAnimSpeed)
            {
                _animTimer = 0;
                _frameIndex++;

                // If animation finishes, go back to Patrol
                if (_frameIndex >= _attackFrames.Length)
                {
                    IsAttacking = false;
                    _frameIndex = 0;
                }
            }
        }

        private void UpdatePatrol()
        {
            // 1. Move
            X += _speed * _direction;

            // 2. Check Boundaries
            if (X >= _maxX)
            {
                X = _maxX;
                _direction = -1;
                _facingRight = false;
            }
            else if (X <= _minX)
            {
                X = _minX;
                _direction = 1;
                _facingRight = true;
            }

            // 3. Animate Walk
            _animTimer++;
            if (_animTimer >= RunAnimSpeed)
            {
                _animTimer = 0;
                _frameIndex++;
                if (_frameIndex >= _runFrames.Length) _frameIndex = 0;
            }
        }

        public void Draw(Graphics g)
        {
            // Choose which array to draw from
            Image[] currentSet = IsAttacking ? _attackFrames : _runFrames;

            // Safety check
            if (_frameIndex >= currentSet.Length) _frameIndex = 0;

            Image img = currentSet[_frameIndex];

            if (img != null)
            {
                if (!_facingRight)
                {
                    g.DrawImage(img, X, Y, Width, Height);
                }
                else
                {
                    // Flip for Left
                    img.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    g.DrawImage(img, X, Y, Width, Height);
                    img.RotateFlip(RotateFlipType.RotateNoneFlipX); // Restore
                }
            }
        }

        public Rectangle GetBounds()
        {
            return new Rectangle((int)X, (int)Y, Width, Height);
        }
    }
}