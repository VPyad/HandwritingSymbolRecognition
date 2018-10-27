using HandwritingSymbolRecognition.Models.TrainingSet;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;

namespace HandwritingSymbolRecognition.Helpers
{
    public class TrainSetConfigHelper
    {
        public async static Task<TrainSetConfig> ParseConfigJson()
        {
            var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(@"ms-appx:///Configs/TrainingSetConfig.json"));

            JObject json = JObject.Parse(File.ReadAllText(file.Path));
            
            var config = new TrainSetConfig
                             {
                                 ImageHeight = json.Value<int>("imageH"),
                                 ImageWidth = json.Value<int>("imageW"),
                                 Train1 = new TrainConfig
                                 {
                                     Symbol = json["train1"].Value<string>("symbol"),
                                     Value = json["train1"].Value<int>("value")
                                 },
                                 Train2 = new TrainConfig
                                 {
                                     Symbol = json["train2"].Value<string>("symbol"),
                                     Value = json["train2"].Value<int>("value")
                                 },
                             };

            return config;
        }

        public async static Task<TrainConfig> GetOppositTrainConfig(TrainConfig trainConfig)
        {
            var config = await ParseConfigJson();

            if (config.Train1.Equals(trainConfig))
                return config.Train1;
            else
                return config.Train2;
        }
    }
}
