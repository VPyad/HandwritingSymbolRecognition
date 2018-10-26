using HandwritingSymbolRecognition.Helpers;
using HandwritingSymbolRecognition.Models.TrainingSet;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace HandwritingSymbolRecognition.NeuralNetwork
{
    public class Perceptron
    {
        #region Fields
        private TrainSetConfig trainSetConfig;

        private int[] cells;
        private double[] weights;

        private int rowCellsCount;
        private int columnCellCount;
        private int cellsCount;
        private int result;
        #endregion

        private const string MODEL_FILE_NAME = "model.json";
        private const double WEAIGHT_INIT_COEFICIENT = .3;

        /// <summary>
        /// Default concturctor, load model from cache if exists
        /// </summary>
        public Perceptron()
        {
            InitFields(true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="loadModel">if true → load cached model if exists</param>
        public Perceptron(bool loadModel)
        {
            InitFields(loadModel);
        }

        public TrainConfig Activation(IRandomAccessStream imageStream)
        {
            throw new NotImplementedException();
        }

        public void Calculate(IRandomAccessStream imageStream, TrainConfig trainConfig)
        { }

        private async Task SaveModel()
        {
            string json = JsonConvert.SerializeObject(weights, Formatting.Indented);
            var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(MODEL_FILE_NAME, CreationCollisionOption.ReplaceExisting);

            await FileIO.WriteTextAsync(file, json);
        }

        private async Task LoadModel()
        {
            var file = await ApplicationData.Current.LocalFolder.TryGetItemAsync(MODEL_FILE_NAME);

            if (file != null)
            {
                string json = await FileIO.ReadTextAsync(file as StorageFile);
                weights = JsonConvert.DeserializeObject<double[]>(json);
            }
            else
                GenerateInitWeightVector();
        }

        public static async Task DeleteModel()
        {
            var file = await ApplicationData.Current.LocalFolder.TryGetItemAsync(MODEL_FILE_NAME);
            
            if (file != null)
                await file.DeleteAsync();
        }

        #region Helpers
        private async void InitFields(bool loadModel)
        {
            trainSetConfig = await TrainSetConfigHelper.ParseConfigJson();

            rowCellsCount = trainSetConfig.ImageWidth;
            columnCellCount = trainSetConfig.ImageHeight;

            cellsCount = rowCellsCount * columnCellCount;

            cells = new int[cellsCount];
            weights = new double[cellsCount + 1];

            if (loadModel)
                await LoadModel();
            else
                GenerateInitWeightVector();
        }

        private void GenerateInitWeightVector()
        {
            Random random = new Random();
            for (int i = 0; i < cellsCount; i++)
            {
                weights[i] = random.NextDouble() * (WEAIGHT_INIT_COEFICIENT - (-WEAIGHT_INIT_COEFICIENT)) + (-WEAIGHT_INIT_COEFICIENT);
            }
        }

        #endregion
    }
}
