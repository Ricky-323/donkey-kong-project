using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrayNotify;

namespace DonkeyKongGame
{
    public class MapManager
    {
        // Dimensions
        public const int TileSize = 48;
        public const int MapCols = 40;
        public const int MapRows = 22;

        // Base dir (usually ...\bin\Debug\ or ...\bin\Release\)
        private readonly string _baseDir;

        // Paths (relative to base dir)
        private readonly string _csvDirectory;
        private readonly string _tileDirectory;

        private Dictionary<int, Image> _tileSet = new Dictionary<int, Image>();
        private List<int[,]> _mapLayers = new List<int[,]>();

        private Image _backgroundImage;

        public MapManager()
        {
            _baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // bin/Debug/outputCSV , bin/Debug/assets
            _csvDirectory = Path.Combine(_baseDir, "outputCSV");
            _tileDirectory = Path.Combine(_baseDir, "assets");
        }

        public void InitializeMap(LevelId level)
        {
            _mapLayers.Clear();

            // 先依地圖載入「要用的圖」
            LoadAssets(level);

            // 再依地圖載入「要用的 CSV layers」
            if (level == LevelId.Map1)
            {
                AddLayer("map1_floor.csv");
                AddLayer("map1_wood.csv");
                AddLayer("map1_brokeladder.csv");
                AddLayer("map1_ladder.csv");
                AddLayer("map1_vine.csv");
                AddLayer("map1_apple.csv");
            }
            else if (level == LevelId.Map2)
            {
                AddLayer("map2_floor.csv");
                AddLayer("map2_wood.csv");
                AddLayer("map2_brokeladder.csv");
                AddLayer("map2_ladder.csv");
                AddLayer("map2_catus.csv");
                AddLayer("map2_apple.csv");
            }
            else if (level == LevelId.Map3)
            {
                AddLayer("map3_floor.csv");
                AddLayer("map3_wood.csv");
                AddLayer("map3_brokeladder.csv");
                AddLayer("map3_ladder.csv");
                AddLayer("map3_ice.csv");
                AddLayer("map3_apple.csv");
            }


        }


        private void LoadAssets(LevelId level)
        {
            // 只要整體 assets/tile 路徑規則不變，這邊改檔名即可
            string bgFile;
            string floorFile;
            string ladderFile;
            string brokenLadderFile;
            string appleFile;
            string id5File;

            if (level == LevelId.Map1)
            {
                bgFile = "background.png";
                floorFile = "floor.png";
                ladderFile = "ladder.png";
                brokenLadderFile = "brokenLadder.png";
                id5File = "vine.png";
                appleFile = "apple.png";
            }
            else if (level == LevelId.Map2)
            {
                bgFile = "background2.png";
                floorFile = "floor2.png";
                ladderFile = "ladder2.png";
                brokenLadderFile = "brokenLadder2.png";
                id5File = "catus.png";
                appleFile = "apple.png";
            }
            else if (level == LevelId.Map3)
            {
                bgFile = "background3.png";
                floorFile = "floor3.png";
                ladderFile = "ladder3.png";
                brokenLadderFile = "brokenLadder3.png";
                id5File = "deco.png";
                appleFile = "apple.png";
            }
            else
            {
                bgFile = "background.png";
                floorFile = "floor.png";
                ladderFile = "ladder.png";
                brokenLadderFile = "brokenLadder.png";
                id5File = "deco.png";
                appleFile = "apple.png";
            }
            string bgPath = Path.Combine(_tileDirectory, bgFile);
            if (File.Exists(bgPath))
            {
                using (Image rawImg = Image.FromFile(bgPath))
                {
                    // 2. Create a new bitmap exactly the size of the screen (1920x1080)
                    // This does the resizing "heavy lifting" right now, instead of during the game.
                    _backgroundImage = new Bitmap(1920, 1080);
                    using (Graphics g = Graphics.FromImage(_backgroundImage))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.DrawImage(rawImg, 0, 0, 1920, 1080);
                    }
                }
            }
            else
            {
                // Fallback if file missing
                _backgroundImage = new Bitmap(1920, 1080);
            }

            // 下面維持你原本載入 Image.FromFile 的方式即可
            _backgroundImage = Image.FromFile(Path.Combine(_tileDirectory, bgFile));
            _tileSet[1] = Image.FromFile(Path.Combine(_tileDirectory, floorFile));
            // _tileSet[2] = Image.FromFile(Path.Combine(_tileDirectory, woodFile));
            _tileSet[3] = Image.FromFile(Path.Combine(_tileDirectory, brokenLadderFile));
            _tileSet[4] = Image.FromFile(Path.Combine(_tileDirectory, ladderFile));
            _tileSet[5] = Image.FromFile(Path.Combine(_tileDirectory, id5File));
            _tileSet[6] = Image.FromFile(Path.Combine(_tileDirectory, appleFile));
        }


        private Image LoadImage(string fileName)
        {
            string fullPath = Path.Combine(_tileDirectory, fileName);
            if (File.Exists(fullPath))
            {
                return new Bitmap(Image.FromFile(fullPath), new Size(TileSize, TileSize));
            }
            else
            {
                // ERROR FIX: Return a bright PINK box if image is missing.
                // If you see Pink boxes, your floor.png is named wrong or missing!
                Bitmap errorBmp = new Bitmap(TileSize, TileSize);
                using (Graphics g = Graphics.FromImage(errorBmp))
                {
                    g.Clear(Color.Magenta); // Bright Pink
                    g.DrawRectangle(Pens.Black, 0, 0, TileSize - 1, TileSize - 1);
                }
                return errorBmp;
            }
        }

        private void AddLayer(string csvFileName)
        {
            string fullPath = Path.Combine(_csvDirectory, csvFileName);

            // ROBUST CHECK: Try both with and without .csv extension
            if (!File.Exists(fullPath))
            {
                if (File.Exists(fullPath.Replace(".csv", "")))
                    fullPath = fullPath.Replace(".csv", ""); // Found it without extension
                else
                    return; // File really doesn't exist
            }

            int[,] layerGrid = new int[MapRows, MapCols];
            string[] lines = File.ReadAllLines(fullPath);

            int rowsToRead = Math.Min(lines.Length, MapRows);

            for (int y = 0; y < rowsToRead; y++)
            {
                // Use Split(',') to get values. 
                string[] values = lines[y].Split(',');
                int colsToRead = Math.Min(values.Length, MapCols);

                for (int x = 0; x < colsToRead; x++)
                {
                    // If parse fails (empty string), it becomes 0 (empty tile)
                    int.TryParse(values[x], out int id);
                    layerGrid[y, x] = id;
                }
            }
            _mapLayers.Add(layerGrid);
        }

        public void DrawMap(Graphics g)
        {
            if (_backgroundImage != null)
            {
                g.DrawImage(_backgroundImage, 0, 0);
            }
            foreach (var layer in _mapLayers)
            {
                for (int y = 0; y < MapRows; y++)
                {
                    for (int x = 0; x < MapCols; x++)
                    {
                        int tileID = layer[y, x];
                        if (tileID != 0 && _tileSet.ContainsKey(tileID))
                        {
                            g.DrawImage(_tileSet[tileID], x * TileSize, y * TileSize, TileSize, TileSize);
                        }
                    }
                }
            }

        }

        // --- NEW HELPER METHODS FOR PLAYER ---

        // Returns the Tile ID at a specific grid coordinate on a specific layer
        public int GetTileID(int col, int row, int layerIndex)
        {
            if (layerIndex < 0 || layerIndex >= _mapLayers.Count) return 0;
            if (col < 0 || col >= MapCols || row < 0 || row >= MapRows) return 0;
            return _mapLayers[layerIndex][row, col];
        }

        // Find the lowest floor (Max Y) and start at its left side (Min X)
        public Point GetStartPosition()
        {
            // Check Layer 0 (Floor)
            if (_mapLayers.Count == 0) return new Point(100, 100);

            int[,] floorLayer = _mapLayers[0];
            int lowestY = -1;
            int startX = -1;

            for (int y = 0; y < MapRows; y++)
            {
                for (int x = 0; x < MapCols; x++)
                {
                    if (floorLayer[y, x] == 1) // 1 is Floor
                    {
                        if (y > lowestY)
                        {
                            lowestY = y;
                            startX = x; // Reset to the first one found in this new lowest row
                        }
                        else if (y == lowestY)
                        {
                            // If same row, keep the leftmost one (which we find first anyway iterating x 0->40)
                            if (startX == -1 || x < startX) startX = x;
                        }
                    }
                }
            }

            if (lowestY != -1)
            {
                // Place player ABOVE the floor tile
                return new Point(startX * TileSize, (lowestY - 1) * TileSize);
            }

            return new Point(100, 100);
        }

        public Rectangle? GetAppleBounds()
        {
            // apple layer 在 InitializeMap 最後 AddLayer("map1_apple.csv");
            int appleLayerIndex = 5; // 0:floor 1:wood 2:brokeladder 3:ladder 4:vine 5:apple

            for (int r = 0; r < MapRows; r++)
            {
                for (int c = 0; c < MapCols; c++)
                {
                    if (GetTileID(c, r, appleLayerIndex) == 6) // 6 = apple
                    {
                        return new Rectangle(c * TileSize, r * TileSize, TileSize, TileSize);
                    }
                }
            }
            return null;
        }

    }
}