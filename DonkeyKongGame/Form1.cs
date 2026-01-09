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

        // --- 1. Music Player Setup ---
        [DllImport("winmm.dll")]
        private static extern long mciSendString(string strCommand, StringBuilder strReturn, int iReturnLength, IntPtr hwndCallback);

        public Form1()
        {
            InitializeComponent();
            BuildMenuUI();

            // --- 2. Start Music immediately ---
            PlayMusic();
        }

        private void PlayMusic()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string musicPath = System.IO.Path.Combine(baseDir, "assets", "menuAudio.mp3");

            // Ensure we close any previous instance before opening a new one
            StopMusic();

            // "type mpegvideo" is the command driver for mp3
            // We use quotes around the path in case there are spaces
            string commandOpen = $"open \"{musicPath}\" type mpegvideo alias MenuMusic";
            mciSendString(commandOpen, null, 0, IntPtr.Zero);

            // "repeat" ensures it loops continuously
            string commandPlay = "play MenuMusic repeat";
            mciSendString(commandPlay, null, 0, IntPtr.Zero);
        }

        private void StopMusic()
        {
            // Close the alias "MenuMusic" to stop playback completely
            string commandStop = "close MenuMusic";
            mciSendString(commandStop, null, 0, IntPtr.Zero);
        }

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

            // Chose your map label
            pbChooseMap = new PictureBox();
            pbChooseMap.Image = Image.FromFile(imgPath);
            pbChooseMap.SizeMode = PictureBoxSizeMode.AutoSize; // 用圖片原尺寸
            pbChooseMap.BackColor = Color.Transparent;
            pbChooseMap.Location = new Point(
                (this.ClientSize.Width - pbChooseMap.Width) / 2,
                150
            );
            this.Controls.Add(pbChooseMap);

            // --- Map buttons ---
            Button btnMap1 = CreateMapSelectButton("map1.png", "map1_hover.png", new Point(530, 470), LevelId.Map1);
            Button btnMap2 = CreateMapSelectButton("map2.png", "map2_hover.png", new Point(800, 465), LevelId.Map2);
            Button btnMap3 = CreateMapSelectButton("map3.png", "map3_hover.png", new Point(1070, 470), LevelId.Map3);

            this.Controls.Add(btnMap1); btnMap1.BringToFront();
            this.Controls.Add(btnMap2); btnMap2.BringToFront();
            this.Controls.Add(btnMap3); btnMap3.BringToFront();

            _mapButtons.Add(btnMap1);
            _mapButtons.Add(btnMap2);
            _mapButtons.Add(btnMap3);

            //（可選）預設選 Map1
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

            //var f = new SettingForm();
            //f.ShowDialog(); // 設定通常用 ShowDialog 比較合理
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
                // hover 顯示 hover 圖（就算已選中也一樣是 hover 圖）
                btn.BackgroundImage = hoverImg;
                btn.Cursor = Cursors.Hand;
            };

            btn.MouseLeave += (s, e) =>
            {
                // ✅ 關鍵：如果是「已選中」，離開也要維持 hover 圖
                if (_selectedMapButton == btn)
                    btn.BackgroundImage = hoverImg;
                else
                    btn.BackgroundImage = normalImg;

                btn.Cursor = Cursors.Default;
            };

            btn.Click += (s, e) =>
            {
                // 設定選中狀態
                SelectMapButton(btn);

                // 記錄選到哪一關
                selection.Level = level;
            };

            // 讓 SelectMapButton 能重設圖用：把圖片暫存到 Tag
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
            // --- 3. Stop Music when game starts ---
            StopMusic();

            Form mapForm;

            // 依關卡選擇建立對應 Form
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

            // 切換畫面：隱藏主選單 -> 關卡關閉後再回來
            this.Hide();
            // --- 4. Resume Music when returning to menu ---
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

            // Make button background transparent
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
