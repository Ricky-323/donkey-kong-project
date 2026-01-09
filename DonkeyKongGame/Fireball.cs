using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;                    

namespace DonkeyKongGame
{
    public class Fireball
    {
        // Audio setup
        [DllImport("winmm.dll")]
        private static extern long mciSendString(string strCommand, StringBuilder strReturn, int iReturnLength, IntPtr hwndCallback);

        public static void PlayFireballSound()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string sfxPath = Path.Combine(baseDir, "assets", "fireballAudio.mp3");

            // Close any previous instance
            mciSendString("close FireballSFX", null, 0, IntPtr.Zero);

            // Open the file with a unique alias
            string commandOpen = $"open \"{sfxPath}\" type mpegvideo alias FireballSFX";
            mciSendString(commandOpen, null, 0, IntPtr.Zero);

            // Play
            string commandPlay = "play FireballSFX";
            mciSendString(commandPlay, null, 0, IntPtr.Zero);
        }

        public static void StopFireballSound()
        {
            mciSendString("close FireballSFX", null, 0, IntPtr.Zero);
        }

        private const float Speed = 12.0f;
        private const int FrameCount = 4;
        private const int AnimSpeed = 4;

        public float X { get; private set; }
        public float Y { get; private set; }

        public const int Width = 32;
        public const int Height = 32;

        private float _vx;

        private static Image[] _frames;
        private int _frameIndex = 0;
        private int _animTimer = 0;

        public Fireball(float startX, float startY, bool facingRight)
        {
            X = startX;
            Y = startY;
            _vx = facingRight ? Speed : -Speed;

            EnsureImagesLoaded();
        }

        private static void EnsureImagesLoaded()
        {
            if (_frames != null) return;

            string baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
            _frames = new Image[FrameCount];

            for (int i = 0; i < FrameCount; i++)
            {
                string path = Path.Combine(baseDir, $"fireball{i + 1}.png");
                if (File.Exists(path))
                {
                    _frames[i] = Image.FromFile(path);
                }
                else
                {
                    Bitmap bmp = new Bitmap(Width, Height);
                    using (Graphics g = Graphics.FromImage(bmp)) g.Clear(Color.OrangeRed);
                    _frames[i] = bmp;
                }
            }
        }

        public void Update()
        {
            // Move
            X += _vx;

            // Animate
            _animTimer++;
            if (_animTimer >= AnimSpeed)
            {
                _animTimer = 0;
                _frameIndex++;
                if (_frameIndex >= FrameCount) _frameIndex = 0;
            }
        }

        public bool IsOffScreen(int screenWidth)
        {
            return X < -200 || X > screenWidth + 200;
        }

        public void Draw(Graphics g)
        {
            if (_frames == null) return;
            g.DrawImage(_frames[_frameIndex], X, Y, Width, Height);
        }

        public Rectangle GetBounds()
        {
            return new Rectangle((int)X, (int)Y, Width, Height);
        }
    }
}


