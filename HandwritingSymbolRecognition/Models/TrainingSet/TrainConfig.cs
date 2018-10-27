using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandwritingSymbolRecognition.Models.TrainingSet
{
    public class TrainConfig
    {
        public string Symbol { get; set; }

        public int Value { get; set; }

        public override bool Equals(object obj)
        {
            var config = obj as TrainConfig;
            
            return config != null && Symbol == config.Symbol && Value == config.Value;
        }
    }
}
