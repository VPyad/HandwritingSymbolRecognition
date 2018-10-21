using HandwritingSymbolRecognition.Helpers;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
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
            var inkPresenter = inkCanvas.InkPresenter;

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
                Windows.UI.Xaml.Media.CompositionTarget.Rendering -= CompositionTarget_Rendering;
                pendingDry = null;

                inkSynchronizer.EndDry();
            }
        }

        private void OnClear(object sender, RoutedEventArgs e)
        {
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
        #endregion
    }
}
