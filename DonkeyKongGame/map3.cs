using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices; // Required for Audio
using System.IO; // Required for Path

namespace DonkeyKongGame
{
    public partial class map3 : Form
    {
        List<Monster3> _monsters = new List<Monster3>();
        private void StartDeathCutscene()
        {
            if (_deathCutsceneActive) return;

            _deathCutsceneActive = true;
            _deathState = DeathCutsceneState.PreDelay;
            _deathDelayFrames = DeathPreDelayFrames;

            // 鎖輸入，避免玩家最後一刻還能動
            goLeft = goRight = goUp = goDown = goJump = false;
            p2Left = p2Right = false;

            // 清空武器，避免 cutscene 期間又扣血
            knives.Clear();
            bombs.Clear();
            fireballs.Clear();

            // zoom 從正常開始
            _deathZoom = 1.0f;
        }

        private void RunDeathCutscene()
        {
            // Zoom 慢慢拉近（以 player1 為中心）
            if (_deathZoom < DeathZoomTarget)
                _deathZoom = Math.Min(DeathZoomTarget, _deathZoom + DeathZoomSpeed);

            switch (_deathState)
            {
                case DeathCutsceneState.PreDelay:
                    {
                        // 這 1 秒先停住畫面（或你也可以讓 player2 idle）
                        player.Update(false, false, false, false, false);
                        player2.Update(false, false);

                        _deathDelayFrames--;
                        if (_deathDelayFrames <= 0)
                        {
                            // ⭐ 1 秒後才開始播 player1 death（同時會播 WoodDeathAudio）
                            player.TriggerDeath();
                            _deathState = DeathCutsceneState.PlayDeathAnim;
                        }
                        break;
                    }

                case DeathCutsceneState.PlayDeathAnim:
                    {
                        player.Update(false, false, false, false, false);
                        player2.Update(false, false);

                        // 你 Player 裡如果有「死亡動畫完成」的 flag，這裡就等它
                        // 假設你是 player.IsDeathFinished (若你沒有，我可以教你加)
                        if (player.IsDeathFinished)
                        {
                            _deathWaitFrames = DeathWaitFramesDefault;
                            _deathState = DeathCutsceneState.WaitThenGameOver;
                        }
                        break;
                    }

                case DeathCutsceneState.WaitThenGameOver:
                    {
                        player.Update(false, false, false, false, false);
                        player2.Update(false, false);

                        _deathWaitFrames--;
                        if (_deathWaitFrames <= 0)
                        {
                            EndGame(isWin: false);
                        }
                        break;
                    }
            }
        }


        // --- Death Cutscene (Player1) ---
        private bool _deathCutsceneActive = false;

        private enum DeathCutsceneState
        {
            PreDelay,        // 觸發死亡後先等 1 秒
            PlayDeathAnim,   // 播放 death1~death6
            WaitThenGameOver // 動畫播完後再等 1~2 秒
        }
        private DeathCutsceneState _deathState;

        private int _deathDelayFrames = 0;
        private int _deathWaitFrames = 0;

        private const int DeathPreDelayFrames = 60;  // 約 1 秒（60fps）
        private const int DeathWaitFramesDefault = 90; // 約 1.5 秒

        // Zoom（以 player1 為中心）
        private float _deathZoom = 1.0f;
        private const float DeathZoomTarget = 5.0f;
        private const float DeathZoomSpeed = 0.1f;


        private int _fadeAlpha = 0;
        private bool _fadeIn = false;

        // --- Win Cutscene ---
        private bool _winCutsceneActive = false;

        private enum WinCutsceneState
        {
            MoveToPlayer2,
            PlayAttackAndDeath,
            WaitThenGameOver
        }
        private WinCutsceneState _winState;
        private int _winWaitFrames = 0;

        // 1~2 秒緩衝（Timer=16ms，60fps）
        private const int WinWaitFramesDefault = 300; // 約 1.5 秒

        private void StartWinCutscene()
        {
            if (_winCutsceneActive) return;

            _winCutsceneActive = true;
            _winState = WinCutsceneState.MoveToPlayer2;

            // 鎖輸入
            goLeft = goRight = goUp = goDown = goJump = false;
            p2Left = p2Right = false;

            // 清空武器，避免 cutscene 期間又扣血/爆炸
            knives.Clear();
            bombs.Clear();
            fireballs.Clear();

            _fadeAlpha = 0;
            _fadeIn = true;
        }

        private void RunWinCutscene()
        {
            if (_fadeIn && _fadeAlpha < 120)
            {
                _fadeAlpha += 4; // 越小越慢
                return;
            }
            else
            {
                _fadeIn = false;
            }


            // 一樣更新一下（但用 cutscene 的控制）
            switch (_winState)
            {
                case WinCutsceneState.MoveToPlayer2:
                    {
                        float targetX = player2.X; // 你說同一層，只要對 X
                        float dx = targetX - player.X;

                        bool moveLeft = dx < -4;
                        bool moveRight = dx > 4;

                        // 讓 player1 用原本 Update 的走路+碰撞邏輯去靠近
                        player.Update(moveLeft, moveRight, false, false, false);

                        // player2 不動（保持 idle）
                        player2.Update(false, false);

                        if (!moveLeft && !moveRight)
                        {
                            // 到位：開始播動畫
                            player.TriggerAttack();
                            player2.TriggerDeath();
                            _winState = WinCutsceneState.PlayAttackAndDeath;
                        }
                        break;
                    }

                case WinCutsceneState.PlayAttackAndDeath:
                    {
                        // 不要讓玩家亂動，專心播動畫
                        player.Update(false, false, false, false, false);
                        player2.Update(false, false);

                        if (player.IsAttackFinished && player2.IsDeathFinished)
                        {
                            _winWaitFrames = WinWaitFramesDefault;
                            _winState = WinCutsceneState.WaitThenGameOver;
                        }
                        break;
                    }

                case WinCutsceneState.WaitThenGameOver:
                    {
                        player.Update(false, false, false, false, false);
                        player2.Update(false, false);

                        _winWaitFrames--;
                        if (_winWaitFrames <= 0)
                        {
                            EndGame(isWin: true);
                        }
                        break;
                    }
            }
        }


        private void EndGame(bool isWin)
        {
            if (_gameEnded) return;
            _gameEnded = true;

            gameTimer.Stop();
            StopMusic(); // map1 有音樂 :contentReference[oaicite:3]{index=3}

            using (var go = new GameOverForm(isWin))
            {
                this.Hide();
                go.ShowDialog();   // 等玩家按鈕
            }

            // Back to Menu：關掉 map1，Form1 那邊會 FormClosed -> Show() + PlayMusic()
            //（你 Form1 已經寫好了 mapForm.FormClosed 回主選單）:contentReference[oaicite:4]{index=4}
            this.Close();
        }


        private bool _gameEnded = false;

        // --- AUDIO SETUP START ---
        [DllImport("winmm.dll")]
        private static extern long mciSendString(string strCommand, StringBuilder strReturn, int iReturnLength, IntPtr hwndCallback);

        private void PlayMusic()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            // Target file: map3Audio.mp3
            string musicPath = Path.Combine(baseDir, "assets", "map3Audio.mp3");

            StopMusic();

            // Use a unique alias "Map3Music"
            string commandOpen = $"open \"{musicPath}\" type mpegvideo alias Map3Music";
            mciSendString(commandOpen, null, 0, IntPtr.Zero);

            string commandPlay = "play Map3Music repeat";
            mciSendString(commandPlay, null, 0, IntPtr.Zero);
        }

        private void StopMusic()
        {
            // Close the specific alias for Map 3
            mciSendString("close Map3Music", null, 0, IntPtr.Zero);
        }

        private void PlayExplosionSound()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string sfxPath = Path.Combine(baseDir, "assets", "explosionAudio.mp3");

            mciSendString("close ExplosionSFX", null, 0, IntPtr.Zero);
            string commandOpen = $"open \"{sfxPath}\" type mpegvideo alias ExplosionSFX";
            mciSendString(commandOpen, null, 0, IntPtr.Zero);
            string commandPlay = "play ExplosionSFX";
            mciSendString(commandPlay, null, 0, IntPtr.Zero);
        }
        // --- AUDIO SETUP END ---

        // --- PAUSE MENU VARIABLES ---
        private bool _isPaused = false;
        private Image _exitBtnImg;
        private Image _exitBtnHoverImg;
        private Rectangle _exitBtnRect;
        private bool _isHoveringExit = false;

        MapManager mapManager = new MapManager();
        Player player;
        Player2 player2;

        Timer gameTimer;
        HealthBar healthBar;

        private Rectangle? _appleRect = null;
        private bool _gameWon = false;

        // Input State
        bool goLeft, goRight, goUp, goDown, goJump;
        bool p2Left, p2Right;

        List<Knife> knives = new List<Knife>();
        List<Bomb> bombs = new List<Bomb>();
        List<Fireball> fireballs = new List<Fireball>();

        public map3()
        {
            InitializeComponent();

            // 1. Full Screen Borderless
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;

            // 2. Fix Flickering
            this.DoubleBuffered = true;
            _monsters.Add(new Monster3(400, 242 - 51, 250, 1000));
            _monsters.Add(new Monster3(1200, 1008 - 51, 300, 1700));

            // Monster 3: Middle Platform Left (Floor Y is 720)
            _monsters.Add(new Monster3(300, 817 - 51, 250, 530));

            // Monster 4: Middle Platform Right (Floor Y is 672)
            _monsters.Add(new Monster3(1000, 820 - 51, 1000, 1400));
            _monsters.Add(new Monster3(1100, 434 - 51, 1100, 1600));

            // Monster 5: High Platform (Floor Y is 480)
            _monsters.Add(new Monster3(400, 625 - 51, 250, 1000));
            //this.AutoScrollMinSize = new Size(1920, 1080);

            // --- LOAD PAUSE ASSETS ---
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string exitPath = Path.Combine(baseDir, "assets", "btn_exit.png");
            string exitHoverPath = Path.Combine(baseDir, "assets", "btn_exit_hover.png");

            if (File.Exists(exitPath)) _exitBtnImg = Image.FromFile(exitPath);
            else _exitBtnImg = new Bitmap(270, 90); // Fallback size

            if (File.Exists(exitHoverPath)) _exitBtnHoverImg = Image.FromFile(exitHoverPath);
            else _exitBtnHoverImg = _exitBtnImg;

            // 3. Initialize Map (LevelId.Map3)
            mapManager.InitializeMap(LevelId.Map3);
            _appleRect = mapManager.GetAppleBounds();

            // 4. Initialize Player
            player = new Player(mapManager);
            player2 = new Player2(mapManager);
            healthBar = new HealthBar();

            // 5. Setup Game Loop
            gameTimer = new Timer();
            gameTimer.Interval = 16;
            gameTimer.Tick += GameLoop;
            gameTimer.Start();

            // 6. Start Music
            PlayMusic();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // Adjust the drawing based on the Scroll Position
            // Matrix originalTransform = e.Graphics.Transform;
            // e.Graphics.TranslateTransform(this.AutoScrollPosition.X, this.AutoScrollPosition.Y);

            Matrix originalTransform = e.Graphics.Transform;

            if (_deathCutsceneActive)
            {
                // 螢幕中心（畫面中央）
                float centerX = this.ClientSize.Width / 2f;
                float centerY = this.ClientSize.Height / 2f;

                // player1 的中心（世界座標）
                float px = player.X + Player.Width / 2f;
                float py = player.Y + Player.Height / 2f;

                // 1) 把「螢幕原點」搬到螢幕中心
                e.Graphics.TranslateTransform(centerX, centerY);

                // 2) 以螢幕中心做縮放（鏡頭拉近）
                e.Graphics.ScaleTransform(_deathZoom, _deathZoom);

                // 3) 把世界平移，讓 player1 的中心點剛好對準螢幕中心
                e.Graphics.TranslateTransform(-px, -py);
            }
            else
            {
                // 平常狀態：沿用你的卷軸世界
                e.Graphics.TranslateTransform(this.AutoScrollPosition.X, this.AutoScrollPosition.Y);
            }



            mapManager.DrawMap(e.Graphics);

            // Draw knives (在玩家前/後都可以，你想「在玩家前」就放這裡)
            foreach (var k in knives)
                k.Draw(e.Graphics);

            // Draw Bombs
            foreach (Bomb b in bombs)
            {
                b.Draw(e.Graphics);
            }

            // Draw Fireballs
            foreach (var f in fireballs)
                f.Draw(e.Graphics);

            foreach (var m in _monsters)
            {
                m.Draw(e.Graphics);
            }
            // Draw Players
            player.Draw(e.Graphics);
            player2.Draw(e.Graphics);

            e.Graphics.Transform = originalTransform;
            healthBar.Draw(e.Graphics, this.ClientSize.Height);

            if (_isPaused)
            {
                // Semi-transparent background
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(128, 0, 0, 0)))
                {
                    e.Graphics.FillRectangle(brush, this.ClientRectangle);
                }

                // Center the button
                int btnW = 270;
                int btnH = 90;
                int centerX = (this.ClientSize.Width - btnW) / 2;
                int centerY = (this.ClientSize.Height - btnH) / 2;

                _exitBtnRect = new Rectangle(centerX, centerY, btnW, btnH);

                Image imgToDraw = _isHoveringExit ? _exitBtnHoverImg : _exitBtnImg;
                if (imgToDraw != null)
                {
                    e.Graphics.DrawImage(imgToDraw, _exitBtnRect);
                }
            }

            if (_fadeIn && _fadeAlpha > 0)
            {
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(_fadeAlpha, 0, 0, 0)))
                {
                    e.Graphics.FillRectangle(brush, this.ClientRectangle);
                }
            }

        }

        private void GameLoop(object sender, EventArgs e)
        {
            // Stop logic if paused
            if (_isPaused) return;

            if (_deathCutsceneActive)
            {
                RunDeathCutscene();
                this.Invalidate();
                return;
            }

            if (_winCutsceneActive)
            {
                RunWinCutscene();
                this.Invalidate();
                return;
            }


            Rectangle playerRect = player.GetBoundsPublic();

            player.Update(goLeft, goRight, goUp, goDown, goJump);
            player2.Update(p2Left, p2Right);
            healthBar.Update();
            // --- Death Check ---
            if (healthBar.IsDead && !_deathCutsceneActive)
            {
                StartDeathCutscene();
                return;
            }


            // --- Win Check ---
            if (!_gameWon && _appleRect.HasValue)
            {
                if (playerRect.IntersectsWith(_appleRect.Value))
                {
                    _gameWon = true;
                    player.PlayAppleSound();
                    StartWinCutscene();
                    return;
                }
            }
            foreach (var m in _monsters)
            {
                m.Update();

                // Optional: Simple Collision Check (Player hits Monster)
                if (m.GetBounds().IntersectsWith(player.GetBoundsPublic()))
                {
                    m.TriggerAttack();
                    // Trigger hurt logic if they touch
                    healthBar.TakeDamage();
                    player.TriggerHurt(m.X + Monster.Width / 2);
                }
            }
            // Update Projectiles
            for (int i = knives.Count - 1; i >= 0; i--)
            {
                knives[i].Update();
                if (knives[i].IsOffScreen(1080)) knives.RemoveAt(i);
            }

            for (int i = bombs.Count - 1; i >= 0; i--)
            {
                bombs[i].Update();
                if (bombs[i].IsOffScreen(1920) || bombs[i].IsDead)
                {
                    bombs.RemoveAt(i);
                }
            }

            for (int i = fireballs.Count - 1; i >= 0; i--)
            {
                fireballs[i].Update();
                if (fireballs[i].IsOffScreen(1920)) fireballs.RemoveAt(i);
            }

            // Collisions
            for (int i = knives.Count - 1; i >= 0; i--)
            {
                if (knives[i].GetBounds().IntersectsWith(playerRect))
                {
                    healthBar.TakeDamage();
                    player.TriggerHurt();
                    knives.RemoveAt(i);
                    break;
                }
            }

            for (int i = bombs.Count - 1; i >= 0; i--)
            {
                if (!bombs[i].IsExploding && bombs[i].GetBounds().IntersectsWith(playerRect))
                {
                    healthBar.TakeDamage();
                    player.TriggerHurt();

                    // Trigger Explosion & Sound
                    bombs[i].Explode();
                    PlayExplosionSound();
                }
            }

            for (int i = fireballs.Count - 1; i >= 0; i--)
            {
                if (fireballs[i].GetBounds().IntersectsWith(playerRect))
                {
                    healthBar.TakeDamage();
                    player.TriggerHurt();
                    fireballs.RemoveAt(i);
                    break;
                }
            }

            this.Invalidate();
        }

        // --- Handle Mouse Move (Hover) ---
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_isPaused)
            {
                bool currentlyHovering = _exitBtnRect.Contains(e.Location);
                if (currentlyHovering != _isHoveringExit)
                {
                    _isHoveringExit = currentlyHovering;
                    this.Invalidate();
                }
            }
        }

        // --- Handle Mouse Click (Exit) ---
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (_isPaused)
            {
                if (_exitBtnRect.Contains(e.Location))
                {
                    this.Close(); // Return to Menu
                }
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // --- PAUSE TOGGLE ---
            if (e.KeyCode == Keys.Escape)
            {
                _isPaused = !_isPaused;

                if (_isPaused) gameTimer.Stop();
                else gameTimer.Start();

                this.Invalidate();
            }

            if (_isPaused) return;

            // Player1 Controls
            if (e.KeyCode == Keys.A) goLeft = true;
            if (e.KeyCode == Keys.D) goRight = true;
            if (e.KeyCode == Keys.W) goUp = true;
            if (e.KeyCode == Keys.S) goDown = true;
            if (e.KeyCode == Keys.Space) goJump = true;

            // Player2 Controls
            if (e.KeyCode == Keys.Left) p2Left = true;
            if (e.KeyCode == Keys.Right) p2Right = true;

            // Attacks
            if (e.KeyCode == Keys.NumPad1 && player2.CanFireWeapon)
            {
                player2.TriggerAttack();
                float spawnX = player2.X + (Player2.Width / 2f) - (Bomb.Width / 2f);
                float spawnY = player2.Y + Player2.Height;
                bombs.Add(new Bomb(spawnX, spawnY, mapManager));
                player2.StartWeaponCooldown();
            }

            if (e.KeyCode == Keys.NumPad2 && player2.CanFireWeapon)
            {
                float spawnX = player2.X + (Player2.Width / 2f) - (Knife.Width / 2f);
                float spawnY = player2.Y + Player2.Height;

                knives.Add(new Knife(spawnX, spawnY));

                // Trigger Knife Sound
                Knife.PlayKnifeSound();

                player2.StartWeaponCooldown();
            }

            if (e.KeyCode == Keys.NumPad3 && player2.CanFireWeapon)
            {
                player2.TriggerAttack2();
                float spawnX = player2.FacingRight
                    ? player2.X + Player2.Width + 5
                    : player2.X - Fireball.Width - 5;
                float spawnY = player2.Y + (Player2.Height / 2f) - (Fireball.Height / 2f);

                fireballs.Add(new Fireball(spawnX, spawnY, player2.FacingRight));

                // Trigger Fireball Sound
                Fireball.PlayFireballSound();

                player2.StartWeaponCooldown();
            }

            if (e.KeyCode == Keys.D1)
            {
                healthBar.TakeDamage();
                player.TriggerHurt();
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.KeyCode == Keys.A) goLeft = false;
            if (e.KeyCode == Keys.D) goRight = false;
            if (e.KeyCode == Keys.W) goUp = false;
            if (e.KeyCode == Keys.S) goDown = false;
            if (e.KeyCode == Keys.Space) goJump = false;

            if (e.KeyCode == Keys.Left) p2Left = false;
            if (e.KeyCode == Keys.Right) p2Right = false;
        }

        // --- CLEANUP ---
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            StopMusic();
            mciSendString("close ExplosionSFX", null, 0, IntPtr.Zero);

            // Clean up Weapon SFX
            Knife.StopKnifeSound();
            Fireball.StopFireballSound();

            gameTimer.Stop();
            gameTimer.Dispose();
            base.OnFormClosed(e);
        }
        private void map3_Load(object sender, EventArgs e)
        {

        }
    }
}


