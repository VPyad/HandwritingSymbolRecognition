using HandwritingSymbolRecognition.Models.TrainingSet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace HandwritingSymbolRecognition.NeuralNetwork
{
    public class Perceptron
    {
        /// <summary>
        /// Default concturctor, load model from cache if exists
        /// </summary>
        public Perceptron()
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="loadModel">if true → load cached model if exists</param>
        public Perceptron(bool loadModel)
        { }

        public TrainConfig Activation(IRandomAccessStream imageStream)
        {
            throw new NotImplementedException();
        }

        public void Calculate(IRandomAccessStream imageStream, TrainConfig trainConfig)
        { }

        private void SaveModel()
        { }

        private void LoadModel()
        { }
    }
}
