using HandwritingSymbolRecognition.Helpers;
using HandwritingSymbolRecognition.Models.TrainingSet;
using HandwritingSymbolRecognition.NeuralNetwork;
using HandwritingSymbolRecognition.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace HandwritingSymbolRecognition.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        private const string FILES_RESULT_PICKER_SUCCESS_TEXT = "Files selected: ";
        private const string FILES_RESULT_PICKER_FAILED_TEXT = "Containing folder is empty or has no images";

        private List<StorageFile> symbol1Images;
        private List<StorageFile> symbol2Images;

        private ImageProcessor imageProcessor;
        private Perceptron perceptron;

        TrainSetConfig trainSetConfig;

        public SettingsPage()
        {
            this.InitializeComponent();

            Loaded += SettingsPage_Loaded;
        }

        #region Event and overrides
        private async void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            trainSetConfig  = await TrainSetConfigHelper.ParseConfigJson();

            imageProcessor = new ImageProcessor();
            perceptron = new Perceptron();

            symbol1TextBlock.Text = trainSetConfig.Train1.Symbol;
            symbol2TextBlock.Text = trainSetConfig.Train2.Symbol;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            SystemNavigationManager.GetForCurrentView().BackRequested += SettingsPage_BackRequested;

            base.OnNavigatedTo(e);
        }

        private void SettingsPage_BackRequested(object sender, BackRequestedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            if (rootFrame.CanGoBack)
            {
                e.Handled = true;
                rootFrame.GoBack();
            }
            else
                Frame.Navigate(typeof(MainPage));
        }

        private async void OnSymbol1ButtonClicked(object sender, RoutedEventArgs e)
        {
            var files = await ShowFolderPickerAsync();

            if (files == null || files.Count() == 0)
            {
                DecorateResultPickerTextBlock(symbol1ResultPickerTextBlock, false);
                return;
            }

            symbol1Images = new List<StorageFile>(files);

            DecorateResultPickerTextBlock(symbol1ResultPickerTextBlock, true, files.Count());
        }

        private async void OnSymbol2ButtonClicked(object sender, RoutedEventArgs e)
        {
            var files = await ShowFolderPickerAsync();

            if (files == null || files.Count() == 0)
            {
                DecorateResultPickerTextBlock(symbol1ResultPickerTextBlock, false);
                return;
            }

            symbol2Images = new List<StorageFile>(files);

            DecorateResultPickerTextBlock(symbol2ResultPickerTextBlock, true, files.Count());
        }

        private async void OnTrainButtonClicked(object sender, RoutedEventArgs e)
        {
            trainProgressRing.Visibility = Visibility.Visible;

            if (symbol1Images != null)
            {
                int count = symbol1Images.Count;
                for (int i = 0; i < count; i++)
                {
                    DecorateResultPickerTextBlockOnFileProcess(symbol1ResultPickerTextBlock, i, count);
                    var imageStream = await imageProcessor.Process(symbol1Images[i]);
                    await perceptron.Calculate(imageStream, trainSetConfig.Train1);
                }
            }

            if (symbol2Images != null)
            {
                int count = symbol2Images.Count;
                for (int i = 0; i < count; i++)
                {
                    DecorateResultPickerTextBlockOnFileProcess(symbol2ResultPickerTextBlock, i, count);
                    var imageStream = await imageProcessor.Process(symbol2Images[i]);
                    await perceptron.Calculate(imageStream, trainSetConfig.Train2);
                }
            }

            trainProgressRing.Visibility = Visibility.Collapsed;
        }

        private async void OnDeleteModelButtonClicked(object sender, RoutedEventArgs e)
        {
            await Perceptron.DeleteModel();
        }
        #endregion

        #region Helpers
        private async Task<IEnumerable<StorageFile>> ShowFolderPickerAsync()
        {
            FolderPicker picker = new FolderPicker();

            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.ViewMode = PickerViewMode.Thumbnail;

            var folder = await picker.PickSingleFolderAsync();

            if (folder == null)
                return null;

            var files = await folder.GetFilesAsync();
            var images = files.Where(x => x.FileType == ".jpg" || x.FileType == ".png");

            if (images == null || images.Count() == 0)
                return null;
            else
                return images;
        }

        private void DecorateResultPickerTextBlock(TextBlock targetTextBlock, bool success, int? filesCount = null)
        {
            if (success)
            {
                string count = filesCount.HasValue ? filesCount.Value.ToString() : "null";
                targetTextBlock.Text = $"{FILES_RESULT_PICKER_SUCCESS_TEXT} {count}";
                targetTextBlock.Foreground = new SolidColorBrush(Colors.Green);
            }
            else
            {
                targetTextBlock.Text = FILES_RESULT_PICKER_FAILED_TEXT;
                targetTextBlock.Foreground = new SolidColorBrush(Colors.Red);
            }

            targetTextBlock.Visibility = Visibility.Visible;
        }

        private void DecorateResultPickerTextBlockOnFileProcess(TextBlock targetTextBlock, int currentIndex, int filesCount)
        {
            targetTextBlock.Text = $"Pressings: {currentIndex + 1} of {filesCount}";
            targetTextBlock.Foreground = new SolidColorBrush(Colors.Green);
        }
        #endregion
    }
}
