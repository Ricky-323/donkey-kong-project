using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace DonkeyKongGame
{
    public partial class Form1 : Form
    {
        private readonly GameSelection selection = new GameSelection();

        private Button btnStart;
        private Button btnExit;
        private Button btnSetting;
        private PictureBox pbChooseMap;

        private Image menuBackground;
        private Button _selectedMapButton = null;
        private readonly List<Button> _mapButtons = new List<Button>();

        // Music playback
        [DllImport("winmm.dll")]
        private static extern long mciSendString(string strCommand, StringBuilder strReturn, int iReturnLength, IntPtr hwndCallback);

        public Form1()
        {
            InitializeComponent();
            BuildMenuUI();

            PlayMusic();
        }

        private void PlayMusic()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string musicPath = System.IO.Path.Combine(baseDir, "assets", "menuAudio.mp3");

            StopMusic();

            string commandOpen = $"open \"{musicPath}\" type mpegvideo alias MenuMusic";
            mciSendString(commandOpen, null, 0, IntPtr.Zero);

            string commandPlay = "play MenuMusic repeat";
            mciSendString(commandPlay, null, 0, IntPtr.Zero);
        }

        private void StopMusic()
        {
            string commandStop = "close MenuMusic";
            mciSendString(commandStop, null, 0, IntPtr.Zero);
        }

        // -----------------------------------------------------------------

        private void Form1_Load(object sender, EventArgs e)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState= FormWindowState.Maximized;
        }

        private void BuildMenuUI()
        {
            this.Text = "Main Menu";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ClientSize = new Size(1920, 1080);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string imgPath = System.IO.Path.Combine(baseDir, "assets", "choose_your_map.png");

            // label: Choose Your Map
            pbChooseMap = new PictureBox();
            pbChooseMap.Image = Image.FromFile(imgPath);
            pbChooseMap.SizeMode = PictureBoxSizeMode.AutoSize;
            pbChooseMap.BackColor = Color.Transparent;
            pbChooseMap.Location = new Point(
                (this.ClientSize.Width - pbChooseMap.Width) / 2,
                150
            );
            this.Controls.Add(pbChooseMap);

            // Map selection buttons
            Button btnMap1 = CreateMapSelectButton("map1.png", "map1_hover.png", new Point(530, 470), LevelId.Map1);
            Button btnMap2 = CreateMapSelectButton("map2.png", "map2_hover.png", new Point(800, 465), LevelId.Map2);
            Button btnMap3 = CreateMapSelectButton("map3.png", "map3_hover.png", new Point(1070, 470), LevelId.Map3);

            this.Controls.Add(btnMap1); btnMap1.BringToFront();
            this.Controls.Add(btnMap2); btnMap2.BringToFront();
            this.Controls.Add(btnMap3); btnMap3.BringToFront();

            _mapButtons.Add(btnMap1);
            _mapButtons.Add(btnMap2);
            _mapButtons.Add(btnMap3);

            SelectMapButton(btnMap1);
            selection.Level = LevelId.Map1;

            // Start button
            btnStart = CreateImageButton(
                "btn_start.png",
                "btn_start_hover.png",
                new Point(800, 650),
                (s, e) => StartGame()
            );
            this.Controls.Add(btnStart);

            // Exit button
            btnExit = CreateImageButton(
                "btn_exit.png",
                "btn_exit_hover.png",
                new Point(800, 770),
                (s, e) => Application.Exit()
            );
            btnExit.Click += (s, e) => Application.Exit();
            this.Controls.Add(btnExit);

            // Setting button
            btnSetting = CreateImageButton(
                "setting.png",
                "setting_hover.png",
                new Point(550, 750),
                (s, e) => OpenSetting()
            );
            btnSetting.Size = new Size(150, 150);
            this.Controls.Add(btnSetting);
            btnSetting.BringToFront();

            // Manu Background
            string bgPath = System.IO.Path.Combine(baseDir, "assets", "menu_background.png");
            menuBackground = Image.FromFile(bgPath);

            this.DoubleBuffered = true;
        }

        private void OpenSetting()
        {
            MessageBox.Show("Setting page 尚未建立");
        }

        private Button CreateMapSelectButton(string imgNormal, string imgHover, Point location, LevelId level)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string imgPath = System.IO.Path.Combine(baseDir, "assets");

            Image normalImg = Image.FromFile(System.IO.Path.Combine(imgPath, imgNormal));
            Image hoverImg = Image.FromFile(System.IO.Path.Combine(imgPath, imgHover));

            Button btn = new Button();
            btn.Size = new Size(270, 90);
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

            btn.MouseEnter += (s, e) =>
            {
                btn.BackgroundImage = hoverImg;
                btn.Cursor = Cursors.Hand;
            };

            btn.MouseLeave += (s, e) =>
            {
                if (_selectedMapButton == btn)
                    btn.BackgroundImage = hoverImg;
                else
                    btn.BackgroundImage = normalImg;

                btn.Cursor = Cursors.Default;
            };

            btn.Click += (s, e) =>
            {
                SelectMapButton(btn);
                selection.Level = level;
            };

            // Store images in Tag for easy access
            btn.Tag = new Tuple<Image, Image>(normalImg, hoverImg);

            return btn;
        }

        private void SelectMapButton(Button selected)
        {
            _selectedMapButton = selected;

            foreach (var b in _mapButtons)
            {
                var imgs = (Tuple<Image, Image>)b.Tag; // Item1 normal, Item2 hover
                b.BackgroundImage = (b == selected) ? imgs.Item2 : imgs.Item1;
            }
        }


        private void StartGame()
        {
            StopMusic();

            Form mapForm;

            // Open selected map form
            switch (selection.Level)
            {
                case LevelId.Map1:
                    mapForm = new map1();
                    break;

                case LevelId.Map2:
                    mapForm = new map2();
                    break;

                case LevelId.Map3:
                    mapForm = new map3();
                    break;

                default:
                    mapForm = new map1();
                    break;
            }


            this.Hide();

            mapForm.FormClosed += (s, e) =>
            {
                if (this.IsDisposed) return;    
                if (!this.IsHandleCreated) return;

                this.Show();
                PlayMusic();
            };
            mapForm.Show();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (menuBackground != null)
            {
                e.Graphics.DrawImage(
                    menuBackground,
                    0, 0,
                    this.ClientSize.Width,
                    this.ClientSize.Height
                );
            }
        }

        private Button CreateImageButton(
        string imgNormal,
        string imgHover,
        Point location,
        EventHandler onClick)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string imgPath = System.IO.Path.Combine(baseDir, "assets");

            Button btn = new Button();
            btn.Size = new Size(270, 90);
            btn.Location = location;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = Color.Transparent;
            btn.TabStop = false;

            btn.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btn.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btn.UseVisualStyleBackColor = false;
            btn.BackColor = Color.Transparent;
            btn.TabStop = false;

            Image normalImg = Image.FromFile(System.IO.Path.Combine(imgPath, imgNormal));
            Image hoverImg = Image.FromFile(System.IO.Path.Combine(imgPath, imgHover));

            btn.BackgroundImage = normalImg;
            btn.BackgroundImageLayout = ImageLayout.Stretch;

            btn.MouseEnter += (s, e) =>
            {
                btn.BackgroundImage = hoverImg;
                btn.Cursor = Cursors.Hand;
            };

            btn.MouseLeave += (s, e) =>
            {
                btn.BackgroundImage = normalImg;
                btn.Cursor = Cursors.Default;
            };

            btn.Click += onClick;

            return btn;
        }

    }
}
