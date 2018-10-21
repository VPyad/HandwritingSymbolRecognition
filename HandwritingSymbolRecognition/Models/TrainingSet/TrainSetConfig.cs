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
    }
}
