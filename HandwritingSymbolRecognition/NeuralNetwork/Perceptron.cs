﻿using HandwritingSymbolRecognition.Helpers;
using HandwritingSymbolRecognition.Models.TrainingSet;
using HandwritingSymbolRecognition.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private ImageProcessor imageProcessor;

        private int[] cells;
        private double[] weights;

        private int rowCellsCount;
        private int columnCellCount;
        private int cellsCount;
        private int result;
        #endregion

        private const string MODEL_FILE_NAME = "model.json";
        private const double WEAIGHT_INIT_COEFICIENT = .5;
        private const double LEARN_SPEED = .7;

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

        public async Task<TrainConfig> Activation(IRandomAccessStream imageStream)
        {
            await ProcessImageStream(imageStream);

            double sum = 0;
            sum += weights[0];
            for (int i = 1; i < cellsCount + 1; i++)
            {
                sum += cells[i - 1] * weights[i];
            }

            Debug.WriteLine(sum, "sum in activanion function");

            result = sum >= 0 ? 1 : 0;

            if (result == 0)
                return trainSetConfig.Train1;
            else
                return trainSetConfig.Train2;
        }

        public async Task Calculate(IRandomAccessStream imageStream, TrainConfig trainConfig)
        {
            await ProcessImageStream(imageStream);

            var delta = trainConfig.Value - result;
            weights[0] = weights[0] + LEARN_SPEED * delta;

            for (int i = 1; i < cellsCount + 1; i++)
            {
                weights[i] = weights[i] + LEARN_SPEED * delta * cells[i - 1];
            }

            await SaveModel();
        }

        private async Task ProcessImageStream(IRandomAccessStream imageStream)
        {
            cells = await imageProcessor.GetCells(imageStream, cellsCount);
        }

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
            imageProcessor = new ImageProcessor();

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
