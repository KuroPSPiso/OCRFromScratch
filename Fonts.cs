using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp.Processing;

namespace OCRFromScratch
{
    public class Fonts
    {
        public static char[] Labels =
        {
            '0','1','2','3','4','5','6','7','8','9',
            'a','b','c','d','e','f','g','h','j','k','l','m','n','o','p','q','r','s','t','u','v','w','x','y','z',
            'A','B','C','D','E','F','G','H','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z'
        };

        public static DataImage[] ReadImagesFromFolder(string pathToDir, out DataImageFlat[] flat, out byte[] label, int maxImages = 0)
        {
            DirectoryInfo di = new DirectoryInfo(pathToDir);
            List<string> paths = new List<string>();
            foreach(FileInfo fi in di.GetFiles())
            {
                paths.Add(Path.Combine(pathToDir, fi.Name));
            }

            return ReadFontDataFromImage(paths.ToArray(), out flat, out label, maxImages);
        }

        /// <summary>
        /// Download and extract data
        /// </summary>
        /// <param name="paths">img path</param>
        /// <param name="flat">flat result</param>
        /// <param name="label">based on input name</param>
        /// <param name="maxImages">limiter</param>
        /// <returns>0</returns>
        public static DataImage[] ReadFontDataFromImage(string[] paths, out DataImageFlat[] flat, out byte[] label, int maxImages = 0)
        {
            if (maxImages == 0) maxImages = paths.Length;
            DataImage[] images = new DataImage[maxImages];
            flat = new DataImageFlat[maxImages];
            label = new byte[maxImages];

            int columns = 28; //width
            int rows = 28; //height

            for (int imgIndex = 0; imgIndex < maxImages; imgIndex++)
            {
#if WINDOWS
                using (Bitmap rawImg = new Bitmap(paths[imgIndex]))
                {

                    Bitmap scaledImg;
                    if (rawImg.Size.Width == columns && rawImg.Size.Height == rows) scaledImg = rawImg;
                    else
                    {
                        scaledImg = new Bitmap(rawImg, new Size(columns, rows));
                    }
#else
                using (Image<Rgba32> scaledImg = Image.Load<Rgba32>(paths[imgIndex]))
                {
                    scaledImg.Mutate(x => x.Resize(columns, rows));
#endif

                    DataImage di = new DataImage(columns, rows);
                    DataImageFlat dif = new DataImageFlat(columns, rows);

                    for (int y = 0; y < rows; y++)
                    {
#if DEBUGLOG
                    Console.WriteLine();
#endif
                        for (int x = 0; x < columns; x++)
                        {
#if WINDOWS
                            Color colorPixel = scaledImg.GetPixel(x, y);
#else
                            Rgba32 colorPixel = scaledImg[x, y];
#endif
                            int colorData = colorPixel.R + colorPixel.G + colorPixel.B;
                            colorData /= 3;

                            byte pixel = (byte)colorData;
                            di.SetPixel(x, y, pixel);
                            dif.SetPixel(columns, rows, x, y, pixel);
#if DEBUGLOG
                        Console.Write(pixel == 0 ? ' ' : 'x');
#endif
                        }
                    }
                    images[imgIndex] = di;
                    flat[imgIndex] = dif;
                    string? labelName = Path.GetFileNameWithoutExtension(paths[imgIndex]);
                    if (labelName != null)
                        for (int iLabel = 0; iLabel < Labels.Length; iLabel++)
                            if (Labels[iLabel] == labelName[0])
                                label[imgIndex] = Convert.ToByte(iLabel);
                }
            }

            return images;

        }
    }
}
