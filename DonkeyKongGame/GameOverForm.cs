using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace DonkeyKongGame
{
    public partial class GameOverForm : Form
    {
        public bool BackToMenuClicked { get; private set; } = false;

        public GameOverForm(bool isWin)
        {
            InitializeComponent();
            BuildUI(isWin);
        }

        private void GameOverForm_Load(object sender, EventArgs e)
        {
        }
        private void BuildUI(bool isWin)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.DoubleBuffered = true;

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string assetsDir = Path.Combine(baseDir, "assets");

            string bgFile = isWin ? "gameover2.png" : "gameover1.png";
            string bgPath = Path.Combine(assetsDir, bgFile);

            if (File.Exists(bgPath))
            {
                this.BackgroundImage = Image.FromFile(bgPath);
                this.BackgroundImageLayout = ImageLayout.Stretch;
            }

            // Buttons
            Button btnExit = CreateImageButton(
                "btn_exit.png",
                "btn_exit_hover.png",
                new Size(270, 90),
                new Point(1600, 720),
                (s, e) => { Application.Exit(); }
            );

            Button btnBack = CreateImageButton(
                "back_to_menu.png",
                "back_to_menu_hover.png",
                new Size(270, 90),
                new Point(30, 720),
                (s, e) =>
                {
                    BackToMenuClicked = true;
                    this.Close();
                }
            );


            this.Controls.Add(btnBack);
            this.Controls.Add(btnExit);

            btnBack.BringToFront();
            btnExit.BringToFront();
        }

        private Button CreateImageButton(
            string normalFile,
            string hoverFile,
            Size size,
            Point location,
            EventHandler onClick)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string assetsDir = Path.Combine(baseDir, "assets");

            string normalPath = Path.Combine(assetsDir, normalFile);
            string hoverPath = Path.Combine(assetsDir, hoverFile);

            Image normalImg = File.Exists(normalPath)
                ? Image.FromFile(normalPath)
                : new Bitmap(size.Width, size.Height);

            Image hoverImg = File.Exists(hoverPath)
                ? Image.FromFile(hoverPath)
                : normalImg;

            Button btn = new Button();
            btn.Size = size;
            btn.Location = location;

            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btn.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btn.UseVisualStyleBackColor = false;
            btn.BackColor = Color.Transparent;
            btn.TabStop = false;

            btn.BackgroundImage = normalImg;
            btn.BackgroundImageLayout = ImageLayout.Stretch;
            btn.Cursor = Cursors.Hand;

            btn.MouseEnter += (s, e) => btn.BackgroundImage = hoverImg;
            btn.MouseLeave += (s, e) => btn.BackgroundImage = normalImg;

            btn.Click += onClick;

            return btn;
        }

    }
}
