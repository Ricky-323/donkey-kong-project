using System;
using System.Drawing;
using System.IO;

namespace DonkeyKongGame
{
    public class HealthBar
    {
        // --- Main Bar Variables ---
        private Image[] _frames;
        private int _currentFrameIndex;
        private int _targetFrameIndex;
        private int _currentState; // 2 = Full, 1 = Half, 0 = Empty
        private bool _isAnimating;

        private int _originalWidth;
        private int _originalHeight;
        private const float BarScale = 0.5f;

        // --- NEW: Reserve Health Variables ---
        private Image _heartFull;
        private Image _heartBorder;
        private int _lives;
        public bool IsDead { get; private set; } = false;


        // *** CHANGE 1: Set MaxLives to 2 ***
        private const int MaxLives = 2;
        private const float HeartScale = 2.0f;

        public HealthBar()
        {
            LoadImages();

            // Initial State
            _currentFrameIndex = 56;
            _currentState = 2;
            _isAnimating = false;

            // *** CHANGE 2: Initialize lives to 2 ***
            _lives = 2;
        }

        private void LoadImages()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string hpDir = Path.Combine(baseDir, "HP_frames");

            // 1. Load Main Health Bar Frames
            _frames = new Image[56];
            if (Directory.Exists(hpDir))
            {
                for (int i = 1; i <= 56; i++)
                {
                    string path = Path.Combine(hpDir, i + ".png");
                    if (File.Exists(path))
                        _frames[i - 1] = Image.FromFile(path);
                    else
                    {
                        Bitmap bmp = new Bitmap(200, 30);
                        using (Graphics g = Graphics.FromImage(bmp)) g.Clear(Color.Red);
                        _frames[i - 1] = bmp;
                    }
                }
            }

            if (_frames[0] != null)
            {
                _originalWidth = _frames[0].Width;
                _originalHeight = _frames[0].Height;
            }

            // 2. Load Heart Images
            string heartPath = Path.Combine(hpDir, "heart.png");
            string borderPath = Path.Combine(hpDir, "noheart.png");

            if (File.Exists(heartPath)) _heartFull = Image.FromFile(heartPath);
            else _heartFull = CreateFallbackBitmap(Color.Red);

            if (File.Exists(borderPath)) _heartBorder = Image.FromFile(borderPath);
            else _heartBorder = CreateFallbackBitmap(Color.Gray);
        }

        private Image CreateFallbackBitmap(Color c)
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp)) g.Clear(c);
            return bmp;
        }

        public void TakeDamage()
        {
            if (_isAnimating) return;

            if (_currentState == 2) // Full -> Half
            {
                _targetFrameIndex = 26;
                _currentState = 1;
                _isAnimating = true;
            }
            else if (_currentState == 1) // Half -> Empty
            {
                _targetFrameIndex = 1;
                _currentState = 0;
                _isAnimating = true;
            }
        }

        public void Update()
        {
            if (_isAnimating)
            {
                if (_currentFrameIndex > _targetFrameIndex)
                {
                    _currentFrameIndex--;
                }
                else
                {
                    _isAnimating = false;

                    // Check if we died (Empty) and have Lives
                    if (_currentState == 0)
                    {
                        if (_lives > 0)
                        {
                            ConsumeReserve();
                        }
                        else
                        {
                            // Game Over logic
                            IsDead = true;
                        }
                    }
                }
            }
        }

        private void ConsumeReserve()
        {
            _lives--;

            // Refill the Main Bar immediately
            _currentState = 2;
            _currentFrameIndex = 56;
        }

        public void Draw(Graphics g, int clientHeight)
        {
            // --- 1. Draw Main Health Bar ---
            Image img = _frames[_currentFrameIndex - 1];

            int barW = (int)(_originalWidth * BarScale);
            int barH = (int)(_originalHeight * BarScale);
            int barX = 10;
            // Position at bottom left
            int barY = clientHeight - 150;

            if (img != null)
            {
                g.DrawImage(img, barX, barY, barW, barH);
            }

            // --- 2. Draw Reserve Hearts ---
            int heartSize = 32;
            int heartSpacing = 36;
            int startHeartX = 10;
            int startHeartY = barY + 100;

            for (int i = 0; i < MaxLives; i++)
            {
                Image heartImg = (i < _lives) ? _heartFull : _heartBorder;

                if (heartImg != null)
                {
                    g.DrawImage(heartImg, startHeartX + (i * heartSpacing), startHeartY, heartSize, heartSize);
                }
            }
        }
    }
}

