#define DEBUGLOGx

using System;
using System.Collections.Generic;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.CompilerServices;



#if WINDOWS
using System.Drawing;
#else
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
#endif
using System.IO;

/* 
 * based on: https://www.youtube.com/watch?v=vzabeKdW9tE
 * start mnist training dataset http://yann.lecun.com/exdb/mnist/
 * 
 */

namespace OCRFromScratch
{
    public class DataPoint
    {
        public DataImageFlat X { get; set; }
        public DataImageFlat Y { get; set; }

        DataPoint(DataImageFlat x, DataImageFlat y)
        {
            this.X = x;
            this.Y = y;
        }
    }

    public class DataImageFlat
    {
        public byte[] PixelData { get { return this.pixelData; } }
        private byte[] pixelData;

        public DataImageFlat(int width, int height)
        {
            pixelData = new byte[height * width];
        }

        public byte GetPixel(int width, int height, int x, int y)
        {
            return this.PixelData[(y * width) + x];
        }
        public byte SetPixel(int width, int height, int x, int y, byte value)
        {
            return this.PixelData[(y * width) + x] = value;
        }
    }

    public class DataImage
    {
        public byte[,] PixelData { get { return this.pixelData; } }
        private byte[,] pixelData;

        public DataImage(int width, int height)
        {
            pixelData = new byte[height, width];
        }

        public byte GetPixel(int x, int y)
        {
            return this.PixelData[y,x];
        }
        public byte SetPixel(int x, int y, byte value)
        {
            return this.PixelData[y,x] = value;
        }
    }

    class Program
    {
        const string dataDir = "data\\";
        const string trainDataFN = dataDir + "train-images.idx3-ubyte";
        const string trainLabelsFN = dataDir + "train-labels.idx1-ubyte";
        const string testDataFN = dataDir + "t10k-images.idx3-ubyte";
        const string testLabelsFN = dataDir + "t10k-labels.idx1-ubyte";
        static string[] manualTestData = {
            dataDir + "custom.png"
        };

        const int trainDataLen = 1000;
        const int testDataLen = 5;
        const int k = 3;

        public static void InspectImages(DataImageFlat[] xTrainFlat, byte[] yTrain, int index, int length)
        {
            for (int range = 0; range < length; range++)
            {
                int charSelect = index + range;
                byte[] xTrainFlatImage0 = new byte[28 * 28];
                for (int iTrainFlatImage0Pixel = 0; iTrainFlatImage0Pixel < 28 * 28; iTrainFlatImage0Pixel++)
                {
                    xTrainFlatImage0[iTrainFlatImage0Pixel] = xTrainFlat[charSelect].GetPixel(28, 28, iTrainFlatImage0Pixel % 28, iTrainFlatImage0Pixel / 28);
                }
                Console.WriteLine("label: {0} for img:", yTrain[charSelect]);
                for (int yXTrainFlatImage0Row = 0; yXTrainFlatImage0Row < 28; yXTrainFlatImage0Row++)
                {
                    byte[] charImgRow = new byte[28];
                    for (int xXTrainFlatImage0Row = 0; xXTrainFlatImage0Row < 28; xXTrainFlatImage0Row++)
                    {
                        charImgRow[xXTrainFlatImage0Row] = (xTrainFlatImage0[xXTrainFlatImage0Row + yXTrainFlatImage0Row * 28] != 0x00) ? (byte)0x04 : (byte)0x08;
                    }

                    Console.WriteLine("[{0}]", string.Join("", charImgRow));
                }
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine(Directory.GetCurrentDirectory());

            #region test_train
            DataImageFlat[] xTrainFlat;
            DataImage[] xTrain = ReadImages(trainDataFN, out xTrainFlat, trainDataLen, true);
            byte[] yTrain = ReadLabels(trainLabelsFN, trainDataLen);
#if DEBUGLOG
            Console.WriteLine(xTrain.Length);
            Console.WriteLine(xTrainFlat.Length);
            InspectImages(xTrainFlat, yTrain, 0, 5);
#endif
            DataImageFlat[] xTestFlat;
            DataImage[] xTest = ReadImages(testDataFN, out xTestFlat, testDataLen);
            byte[] yTest = ReadLabels(testLabelsFN, testDataLen);

            byte[] yPredictions = knn(xTrainFlat, yTrain, xTestFlat, k);
            Console.WriteLine("predictions: [{0}]", string.Join(", ", yPredictions));

            //test result check
            double testSum = 0;
            for(int iTestRes = 0; iTestRes < yPredictions.Length; iTestRes++)
            {
                if (yPredictions[iTestRes] == yTest[iTestRes]) testSum++;
            }
            Console.WriteLine("accuracy: {0}%", testSum / (double)yPredictions.Length * 100);
            #endregion test_train

            #region custom_train
            //Custom Dataset
            xTrain = Fonts.ReadImagesFromFolder("fonts\\regular", out xTrainFlat, out yTrain);

            //InspectImages(xTrainFlat, yTrain, 0, 5);
        #   endregion custom_train

            //test custom data
            ZEROPOINT:
            DataImageFlat[] xTestCustomFlat;
            DataImage[] xTestCustom = ReadImagesFromFile(manualTestData, out xTestCustomFlat);
            InspectImages(xTestCustomFlat, new []{ (byte)'?' }, 0, 1);
            yPredictions = knn(xTrainFlat, yTrain, xTestCustomFlat, k);
            char[] charPredictions = new char[yPredictions.Length];
            for(int yPredIndex = 0; yPredIndex < charPredictions.Length; yPredIndex++)
            {
                charPredictions[yPredIndex] = Fonts.Labels[(int)yPredictions[yPredIndex]];
            }
            Console.WriteLine("predictions: [{0}]", string.Join(", ", yPredictions));
            Console.WriteLine("predictions: [{0}]", string.Join(", ", charPredictions));

            Console.ReadLine();
            goto ZEROPOINT;
        }

        static byte[] ReadLabels(string path, int maxLabels = 0, bool skipMagicNumber = true)
        {
            byte[] data = File.ReadAllBytes(path);

            long offset = 0;
            if (skipMagicNumber) offset = 4;

            byte[] labels;

            int labelCount =
                    (data[offset] << 24) +
                    (data[offset + 1] << 16) +
                    (data[offset + 2] << 8) +
                    (data[offset + 3]);
            offset += 4;

            if (maxLabels > 0) labelCount = (maxLabels > labelCount) ? labelCount : maxLabels;

            labels = new byte[labelCount];

            for (int labelIndex = 0; labelIndex < labelCount; labelIndex++)
            {
                byte label = data[offset++];
                labels[labelIndex] = label;
            }

            return labels;
        }
        static DataImage[] ReadImagesFromFile(string[] paths, out DataImageFlat[] flat, int maxImages = 0)
        {
            if (maxImages == 0) maxImages = paths.Length;
            DataImage[] images = new DataImage[maxImages];
            flat = new DataImageFlat[maxImages];

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
                }
            }

            return images;
        }

        static DataImage[] ReadImages(string path, out DataImageFlat[] flat, int maxImages = 0, bool skipMagicNumber = true)
        {
            byte[] data = File.ReadAllBytes(path);

            long offset = 0;
            if (skipMagicNumber) offset = 4;

            DataImage[] images;

            int imageCount =
                    (data[offset] << 24) +
                    (data[offset + 1] << 16) +
                    (data[offset + 2] << 8) +
                    (data[offset + 3]);
            offset += 4;

            if (maxImages > 0) imageCount = (maxImages > imageCount) ? imageCount : maxImages;

            int rows =
                    (data[offset] << 24) +
                    (data[offset + 1] << 16) +
                    (data[offset + 2] << 8) +
                    (data[offset + 3]);
            offset += 4;

            int columns =
                    (data[offset] << 24) +
                    (data[offset + 1] << 16) +
                    (data[offset + 2] << 8) +
                    (data[offset + 3]);
            offset += 4;

            images = new DataImage[imageCount];
            flat = new DataImageFlat[imageCount];

            for(int imgIndex = 0; imgIndex < imageCount; imgIndex++)
            {
                DataImage di = new DataImage(columns, rows);
                DataImageFlat dif = new DataImageFlat(columns, rows);
#if DEBUGLOG
                Console.WriteLine();
#endif

                for (int y = 0; y < rows; y++)
                {
#if DEBUGLOG
                    Console.WriteLine();
#endif
                    for (int x = 0; x < columns; x++)
                    {
                        byte pixel = data[offset++];
                        di.SetPixel(x, y, pixel);
                        dif.SetPixel(columns, rows, x, y, pixel);
#if DEBUGLOG
                        Console.Write(pixel == 0 ? ' ' : 'x');
#endif
                    }
                }
                images[imgIndex] = di;
                flat[imgIndex] = dif;
            }

            return images;
        }

        static int[] SortDistIndices(double[] trainingDistances)
        {
            List<int> sortedDistIndices = new List<int>();

            int key = 0;
            List<KeyValuePair<int, double>> enumerate = new List<KeyValuePair<int, double>>();
            foreach(double traindingDist in trainingDistances)
            {
                enumerate.Add(new KeyValuePair<int, double>(key, traindingDist));
                key++;
            }
            enumerate.Sort((first, next) =>
            {
                return first.Value.CompareTo(next.Value);
            });
            foreach(KeyValuePair<int, double> pair in enumerate)
            {
                sortedDistIndices.Add(pair.Key);
            }

            return sortedDistIndices.ToArray();
        }

        static byte GetFirstOrMostFreq(byte[] candidates)
        {
            byte result = candidates[0];
            int count = 0;

            Dictionary<byte, int> freq = new Dictionary<byte, int>();
            foreach(byte candidate in candidates)
            {
                if (freq.ContainsKey(candidate)) freq[candidate]++;
                else freq.Add(candidate, 1);
            }

            foreach(KeyValuePair<byte, int> pair in freq)
            {
                if (count < pair.Value)
                {
                    result = pair.Key;
                    count = pair.Value;
                }
            }

            return result;
        }
        
        static byte[] knn(DataImageFlat[] xTrain, byte[] yTrain, DataImageFlat[] xInput, int k = 3)
        {
            List<byte> yPrediction = new List<byte>();
            foreach(DataImageFlat sample in xInput)
            {


                double[] trainingDistances = GetTrainingDistances(xTrain, sample);
                int[] sortedDistanceIndices = SortDistIndices(trainingDistances);

#if DEBUGLOG
                Console.WriteLine("training distances: [{0}]", string.Join(", ", trainingDistances));
                Console.WriteLine("sorted dist indices: [{0}]", string.Join(", ", sortedDistanceIndices));
#endif
                //candidates
                int newK = ((sortedDistanceIndices.Length > k) ? k : sortedDistanceIndices.Length);
                byte[] candidates = new byte[newK];
                for(int idx = 0; idx < newK; idx++)
                {
                    candidates[idx] = yTrain[sortedDistanceIndices[idx]];
                }
                byte bestCandidate = GetFirstOrMostFreq(candidates);
#if DEBUGLOG
                Console.WriteLine("candidates: [{0}]", string.Join(", ", candidates));
                Console.WriteLine("best candidate: {0}", bestCandidate);
#endif

                yPrediction.Add(bestCandidate);
            }
            return yPrediction.ToArray();
        }

        static Tuple<double, double>[] Zip(byte[] x, byte[] y)
        {
            List<Tuple<double, double>> tZip = new List<Tuple<double, double>>();

            for(int i = 0; i < x.Length && i < y.Length; i++)
            {
                tZip.Add(new Tuple<double, double>(x[i], y[i]));
            }

            return tZip.ToArray();
        }

        static double Sum(double[] values)
        {
            int size = values.Length;
            double n = 0;

            foreach(double val in values)
            {
                n += val;
            }

            n /= size;
            return n;
        }

        static double Dist(DataImageFlat x, DataImageFlat y)
        {
            List<double> dData = new List<double>();

            foreach(Tuple<double, double> xy in Zip(x.PixelData, y.PixelData))
            {
                dData.Add(Math.Pow(xy.Item1 - xy.Item2, 2));
            }

            return (int)Math.Pow(Sum(dData.ToArray()), 0.5);
        }

        static double[] GetTrainingDistances(DataImageFlat[] xTrain, DataImageFlat inputSample)
        {
            List<double> dataPoints = new List<double>();

            foreach(DataImageFlat trainSample in xTrain)
            {
                dataPoints.Add(Dist(trainSample, inputSample));
            }

            return dataPoints.ToArray();
        }

    }
}
