using HandwritingSymbolRecognition.Helpers;
using HandwritingSymbolRecognition.Models.TrainingSet;
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

        

        public SettingsPage()
        {
            this.InitializeComponent();

            Loaded += SettingsPage_Loaded;
        }

        #region Event and overrides
        private async void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            TrainSetConfig config = await TrainSetConfigHelper.ParseConfigJson();

            symbol1TextBlock.Text = config.Train1.Symbol;
            symbol2TextBlock.Text = config.Train2.Symbol;

            imageHTextBlock.Text = $" {config.ImageHeight.ToString()}";
            imageWTextBlock.Text = $" {config.ImageWidth.ToString()}";
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

            if (files == null)
            {
                DecorateResultPickerTextBlock(symbol1ResultPickerTextBlock, false);
                return;
            }
            
            DecorateResultPickerTextBlock(symbol1ResultPickerTextBlock, true, files.Count());
            
            ImageProcessor imageProcessor = new ImageProcessor();

            foreach (var file in files)
                await imageProcessor.Process(file);
        }

        private async void OnSymbol2ButtonClicked(object sender, RoutedEventArgs e)
        {
            var files = await ShowFolderPickerAsync();
        }

        private void OnTrainButtonClicked(object sender, RoutedEventArgs e)
        {
            trainProgressRing.Visibility = Visibility.Visible;
        }

        private void OnDeleteModelButtonClicked(object sender, RoutedEventArgs e)
        {

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
        #endregion
    }
}
