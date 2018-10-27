using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandwritingSymbolRecognition.Models.TrainingSet
{
    public class TrainSetConfig
    {
        public int ImageHeight { get; set; }

        public int ImageWidth { get; set; }

        public TrainConfig Train1 { get; set; }

        public TrainConfig Train2 { get; set; }

        public override bool Equals(object obj)
        {
            var config = obj as TrainSetConfig;
            return config != null &&
                   ImageHeight == config.ImageHeight &&
                   ImageWidth == config.ImageWidth &&
                   EqualityComparer<TrainConfig>.Default.Equals(Train1, config.Train1) &&
                   EqualityComparer<TrainConfig>.Default.Equals(Train2, config.Train2);
        }
    }
}
