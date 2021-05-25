using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Newtonsoft.Json;
using SevenZip.Compression.LZMA;
using Tibia.Protobuf.Appearances;
using System.Drawing.Imaging;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;


namespace SpriteDumper
{
    class Program
    {
        /// <summary>
        /// Rotworm outfit id is 26
        /// </summary>
        private static string _assetsPath = "C:\\Users\\klusb\\AppData\\Local\\Tibia\\packages\\Tibia\\assets\\";

        private static string _creaturesDump =
            "C:\\Users\\klusb\\RiderProjects\\ConsoleApp1\\SpriteDumper\\creatures\\";

        private static string _creaturesBlackWhiteGray =
            "C:\\Users\\klusb\\RiderProjects\\ConsoleApp1\\SpriteDumper\\creatures_randombackground\\";

        private static string _ground =
            _ground = "C:\\Users\\klusb\\RiderProjects\\ConsoleApp1\\SpriteDumper\\ground\\";

        private static List<Catalog> catalog;
        private static MemoryStream _spriteBuffer;
        private static ConcurrentDictionary<int, MemoryStream> SprLists = new ConcurrentDictionary<int, MemoryStream>();
        private static Appearances appearances;
        private static int _objectCount;
        private static List<int> dumped = new List<int>();
        private static int count = 0;

        private static List<int> groundIDS = new List<int>();

        static void Main(string[] args)
        {
            _spriteBuffer = new MemoryStream(0x100000);
            LoadCatalogJson();
            LoadAppearances();

            LoadSpritesToMemory();

            DumpGround();

            DumpOutFits();
        }


        private static Image getRandomGroundImage()
        {
            Random rand = new Random();
            int randomIndex = rand.Next(0, groundIDS.Count);
            int randomId = groundIDS[randomIndex];
            return ResizeImage(System.Drawing.Image.FromStream(SprLists[randomId]), 64, 64);
        }

        private static void DumpGround()
        {
            foreach (var obj in appearances.Object)
            {
                if (obj.Flags.Bank != null && obj.Flags.Bank.Waypoints > 0)
                {
                    groundIDS.Add((int) obj.FrameGroup[0].SpriteInfo.SpriteId[0]);
                  
                }
            }
        }

        private static void LoadSpritesToMemory()
        {
            float progress = 0;
            SprLists = new ConcurrentDictionary<int, MemoryStream>();
            var options = new ParallelOptions()
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount * 5
            };
            Parallel.ForEach(catalog, options, (spr, state) =>
            {
                if (spr.Type == "sprite")
                {
                    string _sprPath = String.Format("{0}{1}", _assetsPath, spr.File);
                    if (File.Exists(_sprPath))
                    {
                        Bitmap SheetM = LZMA.DecompressFileLZMA(_sprPath);
                        GenerateTileSetImageList(SheetM, spr);
                        SheetM.Dispose();
                    }
                }

                Console.WriteLine(progress++ * 100.0 / catalog.Count);
            });
        }

        private static void DumpOutFits()
        {
            foreach (var outfit in appearances.Outfit)
            {
                if (DumpOutFit(outfit))
                {
                    count++;
                }
            }
        }


        private static bool AnalyzeOutfit(Appearance outfit)
        {
            uint outfitID = outfit.Id;
            var spriteIds = outfit.FrameGroup[0].SpriteInfo.SpriteId;
            foreach (var framegroup in outfit.FrameGroup)
            {
                foreach (var spriteId in framegroup.SpriteInfo.SpriteId)
                {
                    System.Drawing.Image image = System.Drawing.Image.FromStream(SprLists[(int) spriteId]);
                    if (analyzeImage(image))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        static Bitmap Transparent2Color(Image bmp1, Color target)
        {
            Bitmap bmp2 = new Bitmap(bmp1.Width, bmp1.Height);
            Rectangle rect = new Rectangle(Point.Empty, bmp1.Size);
            using (Graphics G = Graphics.FromImage(bmp2))
            {
                G.Clear(target);
                G.DrawImageUnscaledAndClipped(bmp1, rect);
            }

            return bmp2;
        }

        private static bool DumpOutFit(Appearance outfit)
        {
            uint outfitID = outfit.Id;
            var spriteIds = outfit.FrameGroup[0].SpriteInfo.SpriteId;

            int val0 = outfit.FrameGroup[0].SpriteInfo.HasLayers ? (int) outfit.FrameGroup[0].SpriteInfo.Layers : 1;
            int val = outfit.FrameGroup[0].SpriteInfo.HasPatternDepth
                ? (int) outfit.FrameGroup[0].SpriteInfo.PatternDepth
                : 1;

            if (val > 1 || val0 > 1)
            {
                return false;
            }


            if (dumped.Contains((int) outfit.FrameGroup[0].SpriteInfo.SpriteId[0]))
            {
                return false; // don't dump this outfit
            }


            foreach (var framegroup in outfit.FrameGroup)
            {
                foreach (var spriteId in framegroup.SpriteInfo.SpriteId)
                {
                    DumpImage((int) count, (int) spriteId, outfit);
                }
            }

            return true;
        }


        private static Image drawImageWithBackground(Image _target)
        {
            Image source = getRandomGroundImage();

            Bitmap target = new Bitmap(_target);
            var graphics = Graphics.FromImage(source);
            graphics.DrawImage(target, 0, 0);

            return source;
        }

        private static void DumpImage(int outfitID, int id, Appearance outfit)
        {
            System.IO.Directory.CreateDirectory(_creaturesDump + outfitID.ToString("0000") + "\\");
            System.IO.Directory.CreateDirectory(_creaturesBlackWhiteGray + outfitID.ToString("0000") + "\\");
            System.Drawing.Image image = System.Drawing.Image.FromStream(SprLists[id]);

            dumped.Add(id);
            if (image.Height != 64 || image.Width != 64)
            {
                image = ResizeImage(image, 64, 64);
            }

            for (int i = 0; i < 10; i++)
            {
               
                drawImageWithBackground(image).Save(
                    String.Format("{0}Sprites1 {1} {2}.png",
                        _creaturesBlackWhiteGray + outfitID.ToString("0000") + "\\", id,i),
                    ImageFormat.Png);
            }

/*
            //save it with transparent background
            image.Save(String.Format("{0}Sprites0 {1}.png", _creaturesDump + outfitID.ToString("0000") + "\\", id),
                ImageFormat.Png);


            System.Drawing.Image imageBlack = Transparent2Color(image, Color.Black);
            // Save it as jpg with black background.
            imageBlack.Save(
                String.Format("{0}Sprites1 {1}.png", _creaturesBlackWhiteGray + outfitID.ToString("0000") + "\\", id),
                ImageFormat.Png);

            // convert background to white and save it as jpg
            System.Drawing.Image imageWhite = Transparent2Color(image, Color.White);
            imageWhite.Save(
                String.Format("{0}Sprites2 {1}.png", _creaturesBlackWhiteGray + outfitID.ToString("0000") + "\\", id),
                ImageFormat.Png);

            */
            /* // convert background to white and save it as jpg
             System.Drawing.Image imageGray = Transparent2Color(image, Color.Gray);
             imageGray.Save(
                 String.Format("{0}Sprites3 {1}.png", _creaturesBlackWhiteGray + outfitID.ToString("0000") + "\\", id),
                 ImageFormat.Png);
                 */
        }

        /// <summary>
        /// Analyze an image, of number of colors is to low we will  not dump this.
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        static bool analyzeImage(Image image)
        {
            Bitmap b = new Bitmap(image);


            int maxColors = 0;
            for (int i = 0; i < b.Height; i++)
            {
                for (int j = 0; j < b.Width; j++)
                {
                    Color color = b.GetPixel(i, j);


                    if (color.R > 0 || color.B > 0 || color.G > 0)
                    {
                        maxColors++;
                    }
                }
            }

            Console.WriteLine(maxColors);
            if (maxColors > 200)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }


        /// <summary>
        /// Load appearances file from the catalog.
        /// </summary>
        private static void LoadAppearances()
        {
            string _datPath = String.Format("{0}{1}", _assetsPath, catalog[0].File);
            FileStream appStream;
            using (appStream = new FileStream(_datPath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            {
                appearances = Appearances.Parser.ParseFrom(appStream);
                _objectCount = (ushort) appearances.Outfit[^1].Id;
            }

            Console.WriteLine("LoadAppearances - Done");
        }

        private static void GenerateTileSetImageList(Bitmap bitmap, Catalog sheet)
        {
            Bitmap tileSetImage = new Bitmap(bitmap);
            int tileCount = sheet.LastSpriteid - sheet.FirstSpriteid;
            int sprCount = 0;
            Image tile;

            if (sheet.SpriteType >= 0)
            {
                int xCols = (sheet.SpriteType == 0 || sheet.SpriteType == 1) ? 12 : 6;
                int yCols = (sheet.SpriteType == 0 || sheet.SpriteType == 2) ? 12 : 6;
                int tWidth = (sheet.SpriteType == 0 || sheet.SpriteType == 1) ? 32 : 64;
                int tHeight = (sheet.SpriteType == 0 || sheet.SpriteType == 2) ? 32 : 64;

                System.Drawing.Size tileSize = new System.Drawing.Size(tWidth, tHeight);
                for (int x = 0; x < yCols; x++)
                {
                    for (int y = 0; y < xCols; y++)
                    {
                        if (sprCount > tileCount)
                            break;

                        tile = new Bitmap(tileSize.Width, tileSize.Height, tileSetImage.PixelFormat);
                        Graphics g = Graphics.FromImage(tile);
                        Rectangle sourceRect = new Rectangle(y * tileSize.Width, x * tileSize.Height, tileSize.Width,
                            tileSize.Height);
                        g.DrawImage(tileSetImage, new Rectangle(0, 0, tileSize.Width, tileSize.Height), sourceRect,
                            GraphicsUnit.Pixel);
                        MemoryStream ms = new MemoryStream();
                        tile.Save(ms, ImageFormat.Png);

                        tile.Dispose();
                        SprLists[sheet.FirstSpriteid + sprCount] = ms;
                        g.Dispose();
                        sprCount++;
                    }
                }
            }
        }


        private static void LoadCatalogJson()
        {
            StreamReader r = new StreamReader(_assetsPath + "catalog-content.json");
            string json = r.ReadToEnd();
            catalog = JsonConvert.DeserializeObject<List<Catalog>>(json);
        }
    }
}