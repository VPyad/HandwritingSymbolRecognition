using HandwritingSymbolRecognition.Dialogs;
using HandwritingSymbolRecognition.Helpers;
using HandwritingSymbolRecognition.Models.TrainingSet;
using HandwritingSymbolRecognition.NeuralNetwork;
using HandwritingSymbolRecognition.Pages;
using HandwritingSymbolRecognition.Services;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Input.Inking;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace HandwritingSymbolRecognition
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        #region Fields
        private readonly List<InkStrokeContainer> strokes;
        private InkSynchronizer inkSynchronizer;
        private IReadOnlyList<InkStroke> pendingDry;
        private InkPresenter inkPresenter;

        ImageProcessor imageProcessor;
        Perceptron perceptron;

        private int deferredDryDelay;
        #endregion

        public MainPage()
        {
            InitializeComponent();

            strokes = new List<InkStrokeContainer>();

            Loaded += OnLoaded;
        }

        #region Events
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ApplicationView.GetForCurrentView().TryResizeView(new Size(500, 355)); // for larger view size image recognition accuracy may drop

            imageProcessor = new ImageProcessor();
            perceptron = new Perceptron();

            inkPresenter = inkCanvas.InkPresenter;

            inkSynchronizer = inkPresenter.ActivateCustomDrying();
            inkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse | Windows.UI.Core.CoreInputDeviceTypes.Pen | Windows.UI.Core.CoreInputDeviceTypes.Touch;

            inkPresenter.StrokesCollected += InkPresenter_StrokesCollected;
        }

        private void InkPresenter_StrokesCollected(InkPresenter sender, InkStrokesCollectedEventArgs args)
        {
            pendingDry = inkSynchronizer.BeginDry();

            var container = new InkStrokeContainer();

            foreach (var stroke in pendingDry)
            {
                container.AddStroke(stroke.Clone());
            }

            strokes.Add(container);

            drawingCanvas.Invalidate();
        }

        private void DrawCanvas(CanvasControl sender, CanvasDrawEventArgs args)
        {
            DrawInk(args.DrawingSession);

            if (pendingDry != null && deferredDryDelay == 0)
            {
                args.DrawingSession.DrawInk(pendingDry);

                deferredDryDelay = 1;

                CompositionTarget.Rendering += CompositionTarget_Rendering;
            }
        }

        private void CompositionTarget_Rendering(object sender, object e)
        {
            if (deferredDryDelay > 0)
            {
                deferredDryDelay--;
            }
            else
            {
                CompositionTarget.Rendering -= CompositionTarget_Rendering;
                pendingDry = null;

                inkSynchronizer.EndDry();
            }
        }

        private void OnClear(object sender, RoutedEventArgs e)
        {
            ClearCanvas();
        }

        private void OnSettingsButtonClicked(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SettingsPage));
        }

        private async void OnRecognizedButtonClicked(object sender, RoutedEventArgs e)
        {
            progressRing.Visibility = Visibility.Visible;

            InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream();

            var file = await SaveDrawing();
            var imageStream = await imageProcessor.Process(file);

            TrainConfig result = await perceptron.Activation(imageStream);

            progressRing.Visibility = Visibility.Collapsed;

            var recognitionResult = await RecognizeDialog.ShowDialogAsync(result.Symbol);

            progressRing.Visibility = Visibility.Visible;

            if (recognitionResult == RecognitionResult.Right)
                await perceptron.Calculate(imageStream.CloneStream(), result);
            else
            {
                var rightConfig = await TrainSetConfigHelper.GetOppositTrainConfig(result);
                await perceptron.Calculate(imageStream.CloneStream(), rightConfig);
            }

            progressRing.Visibility = Visibility.Collapsed;

            ClearCanvas();
        }

        private async void OnTrainButtonClicked(object sender, RoutedEventArgs e)
        {
            progressRing.Visibility = Visibility.Visible;

            InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream();

            var file = await SaveDrawing();
            var imageStream = await imageProcessor.Process(file);

            progressRing.Visibility = Visibility.Collapsed;

            var config = await TrainDialog.ShowDialogAsync();

            progressRing.Visibility = Visibility.Visible;

            await perceptron.Calculate(imageStream, config);

            progressRing.Visibility = Visibility.Collapsed;

            ClearCanvas();
        }

        #endregion

        #region Methods
        private void DrawInk(CanvasDrawingSession session)
        {
            foreach (var item in strokes)
            {
                var strokes = item.GetStrokes();

                using (var list = new CanvasCommandList(session))
                {
                    using (var listSession = list.CreateDrawingSession())
                    {
                        listSession.DrawInk(strokes);
                    }
                }

                session.DrawInk(strokes);
            }
        }

        private void ClearCanvas()
        {
            strokes.Clear();
            drawingCanvas.Invalidate();
        }

        private async Task<StorageFile> SaveDrawing(string fileName = null)
        {
            fileName = string.IsNullOrEmpty(fileName) ? "user-input" : fileName;

            var displayInformation = DisplayInformation.GetForCurrentView();
            var imageSize = drawingCanvas.RenderSize;

            drawingCanvas.Measure(imageSize);
            drawingCanvas.UpdateLayout();
            drawingCanvas.Arrange(new Rect(0, 0, imageSize.Width, imageSize.Height));

            var renderTargetBitmap = new RenderTargetBitmap();
            await renderTargetBitmap.RenderAsync(drawingCanvas, Convert.ToInt32(imageSize.Width), Convert.ToInt32(imageSize.Height));

            var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();
            var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(fileName + ".png", CreationCollisionOption.ReplaceExisting);

            using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, fileStream);

                encoder.SetPixelData(
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Ignore,
                        (uint)renderTargetBitmap.PixelWidth,
                        (uint)renderTargetBitmap.PixelHeight,
                        displayInformation.LogicalDpi,
                        displayInformation.LogicalDpi,
                        pixelBuffer.ToArray());

                await encoder.FlushAsync();
            }

            return file;
        }
        #endregion

        private async void OnMagicButtonClicked(object sender, RoutedEventArgs e)
        {
            await SaveDrawing(Guid.NewGuid().ToString());
            ClearCanvas();
        }
    }
}
