using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices; // Required for Audio
using System.Text;                    // Required for StringBuilder

namespace DonkeyKongGame
{
    public class Knife
    {
        // --- AUDIO SETUP START ---
        [DllImport("winmm.dll")]
        private static extern long mciSendString(string strCommand, StringBuilder strReturn, int iReturnLength, IntPtr hwndCallback);

        public static void PlayKnifeSound()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string sfxPath = Path.Combine(baseDir, "assets", "knifeAudio.mp3");

            // 1. Close any previous instance (allows rapid fire)
            mciSendString("close KnifeSFX", null, 0, IntPtr.Zero);

            // 2. Open the file
            string commandOpen = $"open \"{sfxPath}\" type mpegvideo alias KnifeSFX";
            mciSendString(commandOpen, null, 0, IntPtr.Zero);

            // 3. Play
            string commandPlay = "play KnifeSFX";
            mciSendString(commandPlay, null, 0, IntPtr.Zero);
        }

        public static void StopKnifeSound()
        {
            mciSendString("close KnifeSFX", null, 0, IntPtr.Zero);
        }
        // --- AUDIO SETUP END ---

        private const float Gravity = 0.5f;
        private float _vy = 0;

        public float X { get; private set; }
        public float Y { get; private set; }

        public const int Width = 26;
        public const int Height = 32;

        private static Image _knifeImage;

        public Knife(float startX, float startY)
        {
            X = startX;
            Y = startY;

            EnsureImageLoaded();
        }

        private static void EnsureImageLoaded()
        {
            if (_knifeImage != null) return;

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string imgPath = Path.Combine(baseDir, "assets", "knife.png");

            if (File.Exists(imgPath))
            {
                _knifeImage = Image.FromFile(imgPath);
            }
            else
            {
                Bitmap bmp = new Bitmap(Width, Height);
                using (Graphics g = Graphics.FromImage(bmp)) g.Clear(Color.Yellow);
                _knifeImage = bmp;
            }
        }

        public void Update()
        {
            _vy += Gravity;
            Y += _vy;
        }

        public bool IsOffScreen(int screenHeight)
        {
            return Y > screenHeight + 200;
        }

        public void Draw(Graphics g)
        {
            if (_knifeImage == null) return;
            g.DrawImage(_knifeImage, X, Y, Width, Height);
        }

        public Rectangle GetBounds()
        {
            return new Rectangle((int)X, (int)Y, Width, Height);
        }
    }
}


