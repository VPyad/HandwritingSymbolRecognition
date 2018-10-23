using HandwritingSymbolRecognition.Helpers;
using HandwritingSymbolRecognition.Models.TrainingSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml.Media.Imaging;

namespace HandwritingSymbolRecognition.Services
{
    public class ImageProcessor
    {
        private TrainSetConfig trainSetConfig;

        public ImageProcessor()
        {
            InitConfig();
        }

        public async Task Process(StorageFile file)
        {
            var imageProps = await file.Properties.GetImagePropertiesAsync();

            var resizedImageStream = await ResizeImage(await file.OpenReadAsync(), (int)imageProps.Width, (int)imageProps.Height, trainSetConfig.ImageWidth, trainSetConfig.ImageHeight);
            var wbImageStream = await ConvertToBW(resizedImageStream, trainSetConfig.ImageWidth, trainSetConfig.ImageHeight);

            //await SaveStreamToFile(wbImageStream, trainSetConfig.ImageWidth, trainSetConfig.ImageHeight);
        }

        private async Task<IRandomAccessStream> ResizeImage(IRandomAccessStream imageStream, int sourceWidth, int sourceHeight, int width, int height)
        {
            WriteableBitmap bitmap = new WriteableBitmap(sourceWidth, sourceHeight);

            bitmap.SetSource(imageStream);
            bitmap = bitmap.Resize(width, height, WriteableBitmapExtensions.Interpolation.Bilinear);

            InMemoryRandomAccessStream outputStream = new InMemoryRandomAccessStream();
            await bitmap.ToStream(outputStream, BitmapEncoder.PngEncoderId);

            return outputStream;
        }

        public async Task<IRandomAccessStream> ConvertToBW(IRandomAccessStream imageStream, int width, int height)
        {
            WriteableBitmap bitmap = new WriteableBitmap(width, height);

            bitmap.SetSource(imageStream);

            bitmap = bitmap.Gray();

            InMemoryRandomAccessStream outputStream = new InMemoryRandomAccessStream();
            await bitmap.ToStream(outputStream, BitmapEncoder.PngEncoderId);

            return outputStream;
        }

        private async void InitConfig()
        {
            trainSetConfig = await TrainSetConfigHelper.ParseConfigJson();
        }

        private async Task SaveStreamToFile(IRandomAccessStream imageStream, int width, int height)
        {
            WriteableBitmap bitmap = new WriteableBitmap(width, height);

            bitmap.SetSource(imageStream);
            
            var stFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(Guid.NewGuid().ToString() + ".png", CreationCollisionOption.GenerateUniqueName);
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
            //open file as stream
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
    }
}
