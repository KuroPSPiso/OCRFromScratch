#define DEBUGLOGx

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

/* 
 * based on: https://www.youtube.com/watch?v=vzabeKdW9tE
 * start mnist training dataset
 * 
 */

namespace OCRFromScratch_TestDataOnly
{
    class DataPoint
    {
        public DataImageFlat X { get; set; }
        public DataImageFlat Y { get; set; }

        DataPoint(DataImageFlat x, DataImageFlat y)
        {
            this.X = x;
            this.Y = y;
        }
    }

    class DataImageFlat
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

    class DataImage
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

        static void MainTestData(string[] args)
        {
            Console.WriteLine(Directory.GetCurrentDirectory());

            DataImageFlat[] xTrainFlat;
            DataImage[] xTrain = ReadImages(trainDataFN, out xTrainFlat, 100, true);
            byte[] yTrain = ReadLabels(trainLabelsFN, 100);
#if DEBUGLOG
            Console.WriteLine(xTrain.Length);
            Console.WriteLine(xTrainFlat.Length);
#endif

            DataImageFlat[] xTestFlat;
            DataImage[] xTest = ReadImages(testDataFN, out xTestFlat, 5);
            byte[] yTest = ReadLabels(testLabelsFN, 5);

            byte[] yPredictions = knn(xTrainFlat, yTrain, xTestFlat);
            Console.WriteLine("predictions: [{0}]", string.Join(", ", yPredictions));

            //test result check
            double testSum = 0;
            for(int iTestRes = 0; iTestRes < yPredictions.Length; iTestRes++)
            {
                if (yPredictions[iTestRes] == yTest[iTestRes]) testSum++;
            }
            Console.WriteLine("accuracy: {0}%", testSum / (double)yPredictions.Length * 100);
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
                DataImage di = new DataImage(rows, columns);
                DataImageFlat dif = new DataImageFlat(rows, columns);
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
