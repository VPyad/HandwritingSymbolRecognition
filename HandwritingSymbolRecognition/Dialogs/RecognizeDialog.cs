using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace HandwritingSymbolRecognition.Dialogs
{
    public class RecognizeDialog
    {
        public async static Task<RecognitionResult> ShowDialogAsync(string symbol)
        {
            ContentDialog contentDialog = new ContentDialog
            {
                Title = "Recognition result",
                Content = "Recognized: " + symbol,
                PrimaryButtonText = "Right",
                SecondaryButtonText = "Wrong"
            };

            var result = await contentDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
                return RecognitionResult.Right;
            else
                return RecognitionResult.Wrong;
        }
    }

    public enum RecognitionResult
    {
        Right,
        Wrong
    }
}
