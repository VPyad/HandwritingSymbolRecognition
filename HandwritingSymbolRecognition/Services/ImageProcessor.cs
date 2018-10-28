using HandwritingSymbolRecognition.Helpers;
using HandwritingSymbolRecognition.Models.TrainingSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml.Media.Imaging;

namespace HandwritingSymbolRecognition.Services
{
    public class ImageProcessor
    {
        private const int MAGIC_COEFICIENT = 3;

        private TrainSetConfig trainSetConfig;

        public ImageProcessor()
        {
            InitConfig();
        }

        public async Task<IRandomAccessStream> Process(StorageFile file)
        {
            var imageProps = await file.Properties.GetImagePropertiesAsync();

            var resizedImageStream = await ResizeImageWithoutBitmap(await file.OpenReadAsync(), trainSetConfig.ImageWidth, trainSetConfig.ImageHeight);
            //await SaveStreamToFile(resizedImageStream.CloneStream(), trainSetConfig.ImageWidth, trainSetConfig.ImageHeight, "resized");

            var wbImageStream = await ConvertToBW(resizedImageStream, trainSetConfig.ImageWidth, trainSetConfig.ImageHeight);
            //await SaveStreamToFile(wbImageStream.CloneStream(), trainSetConfig.ImageWidth, trainSetConfig.ImageHeight, "grayed");

            return wbImageStream;
        }

        private async Task<IRandomAccessStream> ResizeImage(IRandomAccessStream imageStream, int sourceWidth, int sourceHeight, int width, int height)
        {
            WriteableBitmap bitmap = new WriteableBitmap(sourceWidth, sourceHeight);

            bitmap.SetSource(imageStream);

            bitmap = bitmap.Resize(width, height, WriteableBitmapExtensions.Interpolation.NearestNeighbor);

            InMemoryRandomAccessStream outputStream = new InMemoryRandomAccessStream();
            await bitmap.ToStream(outputStream, BitmapEncoder.PngEncoderId);

            return outputStream;
        }

        public async Task<IRandomAccessStream> ConvertToBW(IRandomAccessStream imageStream, int width, int height)
        {
            WriteableBitmap bitmap = await BitmapFactory.New(width, height).FromStream(imageStream);
            
            bitmap = bitmap.Gray();

            InMemoryRandomAccessStream outputStream = new InMemoryRandomAccessStream();
            await bitmap.ToStream(outputStream, BitmapEncoder.PngEncoderId);

            return outputStream;
        }

        public async Task<int[]> GetCells(IRandomAccessStream imageStream, int cellsCount)
        {
            int[] cells = new int[cellsCount];
            int[] cellValues = new int[cellsCount];

            Dictionary<Point, Color> pixels = new Dictionary<Point, Color>();

            WriteableBitmap bitmap = await BitmapFactory.New(trainSetConfig.ImageWidth, trainSetConfig.ImageHeight).FromStream(imageStream);

            for (int i = 0; i < bitmap.PixelWidth; i++)
            {
                for (int j = 0; j < bitmap.PixelHeight; j++)
                {
                    var color = bitmap.GetPixel(i, j);

                    if (color.A == 255 && color.R != 255 && color.G != 255 && color.B != 255)
                        pixels.Add(new Point(i, j), color);
                }
            }

            if (pixels.Count == 0)
                return null;

            var leftTop = new Point(pixels.Min(p => p.Key.X), pixels.Min(p => p.Key.Y));
            var rightBottom = new Point(pixels.Max(p => p.Key.X), pixels.Max(p => p.Key.Y));

            WriteableBitmap boundBitmap = new WriteableBitmap((int)(rightBottom.X - leftTop.X + 1), (int)(rightBottom.Y - leftTop.Y + 1));

            foreach (var pixel in pixels)
            {
                boundBitmap.SetPixel((int)(pixel.Key.X - leftTop.X), (int)(pixel.Key.Y - leftTop.Y), pixel.Value);
            }

            var stretchX = boundBitmap.PixelWidth / trainSetConfig.ImageWidth;
            var stretchY = boundBitmap.PixelHeight / trainSetConfig.ImageHeight;

            WriteableBitmap stretchedBitmap = new WriteableBitmap(100, 100);

            for (int i = 0; i < 100; i++)
            {
                for (int j = 0; j < 100; j++)
                {
                    var color = boundBitmap.GetPixel(i * stretchX, j * stretchY);
                    stretchedBitmap.SetPixel(i, j, color);
                }
            }

            for (int i = 0; i < stretchedBitmap.PixelWidth; i++)
            {
                for (int j = 0; j < stretchedBitmap.PixelHeight; j++)
                {
                    var color = stretchedBitmap.GetPixel(i, j);
                    if (color.A == 255 && color.R != 255 && color.G != 255 && color.B != 255)
                    {
                        cellValues[i / MAGIC_COEFICIENT * trainSetConfig.ImageWidth + j / MAGIC_COEFICIENT]++;
                    }
                }
            }

            for (int i = 0; i < cellsCount; i++)
            {
                if (cellValues[i] > MAGIC_COEFICIENT)
                {
                    cells[i] = 1; 
                }
            }

            Debug.WriteLine(cells.Count(x => x == 1), "QWERTY");
            return cells;
        }

        private async void InitConfig()
        {
            trainSetConfig = await TrainSetConfigHelper.ParseConfigJson();
        }

        private async Task SaveStreamToFile(IRandomAccessStream imageStream, int width, int height, string fileName = null)
        {
            WriteableBitmap bitmap = new WriteableBitmap(width, height);

            bitmap.SetSource(imageStream);

            string name = string.IsNullOrEmpty(fileName) ? Guid.NewGuid().ToString() : fileName;

            var stFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(name + ".png", CreationCollisionOption.GenerateUniqueName);
            using (IRandomAccessStream stream = await stFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                Stream pixelStream = bitmap.PixelBuffer.AsStream();
                byte[] pixels = new byte[pixelStream.Length];
                await pixelStream.ReadAsync(pixels, 0, pixels.Length);

                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                                    (uint)bitmap.PixelWidth,
                                    (uint)bitmap.PixelHeight,
                                    96.0,
                                    96.0,
                                    pixels);
                await encoder.FlushAsync();
            }

            Debug.WriteLine(stFile.Path);
        }

        private async Task ResizeImageWithoutBitmap(StorageFile file, int width, int height)
        {
            using (IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                var decoder = await BitmapDecoder.CreateAsync(fileStream);

                var resizedStream = new InMemoryRandomAccessStream();

                BitmapEncoder encoder = await BitmapEncoder.CreateForTranscodingAsync(resizedStream, decoder);
                double widthRatio = (double)width / decoder.PixelWidth;
                double heightRatio = (double)height / decoder.PixelHeight;

                double scaleRatio = Math.Min(widthRatio, heightRatio);

                if (width == 0)
                    scaleRatio = heightRatio;

                if (height == 0)
                    scaleRatio = widthRatio;

                uint aspectHeight = (uint)Math.Floor(decoder.PixelHeight * scaleRatio);
                uint aspectWidth = (uint)Math.Floor(decoder.PixelWidth * scaleRatio);

                encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Linear;

                encoder.BitmapTransform.ScaledHeight = aspectHeight;
                encoder.BitmapTransform.ScaledWidth = aspectWidth;

                await encoder.FlushAsync();
                resizedStream.Seek(0);
                var outBuffer = new byte[resizedStream.Size];
                await resizedStream.ReadAsync(outBuffer.AsBuffer(), (uint)resizedStream.Size, InputStreamOptions.None);



                StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                Debug.WriteLine(storageFolder.Path);
                StorageFile sampleFile = await storageFolder.CreateFileAsync(Guid.NewGuid().ToString() + ".png", CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteBytesAsync(sampleFile, outBuffer);
            }
        }

        private async Task<IRandomAccessStream> ResizeImageWithoutBitmap(IRandomAccessStream imageStream, int width, int height)
        {
            //open file as stream
            using (IRandomAccessStream fileStream = imageStream)
            {
                var decoder = await BitmapDecoder.CreateAsync(fileStream);

                var resizedStream = new InMemoryRandomAccessStream();

                BitmapEncoder encoder = await BitmapEncoder.CreateForTranscodingAsync(resizedStream, decoder);
                double widthRatio = (double)width / decoder.PixelWidth;
                double heightRatio = (double)height / decoder.PixelHeight;

                double scaleRatio = Math.Min(widthRatio, heightRatio);

                if (width == 0)
                    scaleRatio = heightRatio;

                if (height == 0)
                    scaleRatio = widthRatio;

                uint aspectHeight = (uint)Math.Floor(decoder.PixelHeight * scaleRatio);
                uint aspectWidth = (uint)Math.Floor(decoder.PixelWidth * scaleRatio);

                encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Linear;

                encoder.BitmapTransform.ScaledHeight = aspectHeight;
                encoder.BitmapTransform.ScaledWidth = aspectWidth;

                await encoder.FlushAsync();

                return resizedStream;
            }
        }
    }
}
