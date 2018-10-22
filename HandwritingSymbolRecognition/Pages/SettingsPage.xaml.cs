using HandwritingSymbolRecognition.Helpers;
using HandwritingSymbolRecognition.Models.TrainingSet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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

        private void OnSymbol1ButtonClicked(object sender, RoutedEventArgs e)
        {

        }

        private void OnSymbol2ButtonClicked(object sender, RoutedEventArgs e)
        {

        }

        private void OnTrainButtonClicked(object sender, RoutedEventArgs e)
        {

        }

        private void OnDeleteModelButtonClicked(object sender, RoutedEventArgs e)
        {

        }
        #endregion
    }
}
