using HandwritingSymbolRecognition.Helpers;
using HandwritingSymbolRecognition.Models.TrainingSet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace HandwritingSymbolRecognition.Dialogs
{
    public class TrainDialog
    {
        public async static Task<TrainConfig> ShowDialogAsync()
        {
            var config = await TrainSetConfigHelper.ParseConfigJson();

            ContentDialog contentDialog = new ContentDialog
            {
                Title = "Set value",
                Content = "Choose entered value",
                PrimaryButtonText = config.Train1.Symbol,
                SecondaryButtonText = config.Train2.Symbol
            };

            var result = await contentDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
                return config.Train1;
            else
                return config.Train2;
        }
    }
}
