using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

// original code is https://github.com/yamaguchi23/adaboost
// test data is http://www.csie.ntu.edu.tw/~cjlin/libsvmtools/datasets/binary.html

namespace AdaBoost
{
    class Program
    {
        static void Main(string[] args)
        {
            Train(new[] { "-v", "test.txt" });
            Predict(new[] { "-v", "testresult.txt", "test.txt.model" });
            Console.ReadKey();
        }

        static void Train(string[] args)
        {

            AdaBoostTrainParameter parameter = AdaBoostTrainParameter.ParseCommandline(args);

            if (parameter.Verbose)
            {
                string[] boostingTypeName = new[] { "discrete", "real", "gentle" };
                Console.Error.WriteLine();
                Console.Error.WriteLine($"Traing data:  {parameter.TrainingDataFilename}");
                Console.Error.WriteLine($"Output model: {parameter.OutputModelFilename}");
                Console.Error.WriteLine($"   Type:      {boostingTypeName[parameter.BoostingType]}");
                Console.Error.WriteLine($"   #rounds:   {parameter.RoundTotal}");
                Console.Error.WriteLine();
            }

            AdaBoost adaBoost = new AdaBoost();
            adaBoost.SetBoostingType(parameter.BoostingType);
            adaBoost.SetTrainingSamples(parameter.TrainingDataFilename);
            adaBoost.Train(parameter.RoundTotal, parameter.Verbose);

            adaBoost.WriteFile(parameter.OutputModelFilename);
        }

        static void Predict(string[] args)
        {
            AdaBoostPredictParameter parameter = AdaBoostPredictParameter.ParseCommandline(args);

            if (parameter.Verbose)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine($"Test data: {parameter.TestDataFilename}");
                Console.Error.WriteLine($"Model:     {parameter.ModelFilename}");
                if (parameter.OutputScoreFile)
                {
                    Console.Error.WriteLine($"Output score: {parameter.OutputScorelFilename}");
                }
                Console.Error.WriteLine();
            }

            AdaBoost adaBoost = new AdaBoost();
            adaBoost.ReadFile(parameter.ModelFilename);

            SampleDataFile datafile = SampleDataFile.Load(parameter.TestDataFilename);
            int testSampleTotal = datafile.SampleFeatures.Count;

            StreamWriter outputScoreStream = null;
            if (parameter.OutputScoreFile)
            {
                outputScoreStream = new StreamWriter(parameter.OutputScorelFilename);
            }

            int positiveTotal = 0;
            int positiveCorrectTotal = 0;
            int negativeTotal = 0;
            int negativeCorrectTotal = 0;
            for (int sampleIndex = 0; sampleIndex < testSampleTotal; ++sampleIndex)
            {
                double score = adaBoost.Predict(datafile.SampleFeatures[sampleIndex]);

                if (datafile.SampleLabels[sampleIndex])
                {
                    ++positiveTotal;
                    if (score > 0) ++positiveCorrectTotal;
                }
                else
                {
                    ++negativeTotal;
                    if (score <= 0) ++negativeCorrectTotal;
                }

                outputScoreStream?.Write(score);
            }
            if (outputScoreStream != null)
            {
                outputScoreStream.Close();
                outputScoreStream.Dispose();
            }


            double accuracyAll = (double)(positiveCorrectTotal + negativeCorrectTotal) / (positiveTotal + negativeTotal);
            Console.Write($"Accuracy = {accuracyAll}");
            Console.Write($" ({positiveCorrectTotal + negativeCorrectTotal} / { positiveTotal + negativeTotal})");
            Console.Write($"  positive: {(double)(positiveCorrectTotal) / positiveTotal}");
            Console.Write($" ({positiveCorrectTotal} / {positiveTotal}), ");
            Console.Write($"negative: {(double)(negativeCorrectTotal) / negativeTotal}");
            Console.Write($" ({negativeCorrectTotal} / {negativeTotal})");
        }
    }

    public struct AdaBoostTrainParameter
    {
        public bool Verbose { get; }
        public string TrainingDataFilename { get; }
        public string OutputModelFilename { get; }
        public int BoostingType { get; }
        public int RoundTotal { get; }

        public AdaBoostTrainParameter(
            bool verbose,
            string trainingDataFilename,
            string outputModelFilename,
            int boostingType,
            int roundTotal
        )
        {
            Verbose = verbose;
            TrainingDataFilename = trainingDataFilename;
            OutputModelFilename = outputModelFilename;
            BoostingType = boostingType;
            RoundTotal = roundTotal;
        }

        private static void Exit()
        {
            Console.Error.WriteLine("usage: abtrain [options] training_set_file [model_file]");
            Console.Error.WriteLine("options:");
            Console.Error.WriteLine("   -t: type of boosting (0:discrete, 1:real, 2:gentle) [default:2]");
            Console.Error.WriteLine("   -r: the number of rounds [default:100]");
            Console.Error.WriteLine("   -v: verbose");

            Environment.Exit(1);
        }

        public static AdaBoostTrainParameter ParseCommandline(string[] args)
        {
            var verbose = false;
            var boostingType = 2;
            var roundTotal = 1000;

            // Options
            int argIndex;
            for (argIndex = 0; argIndex < args.Length; ++argIndex)
            {
                if (args[argIndex].Length == 0 || args[argIndex][0] != '-') break;

                switch (args[argIndex][1])
                {
                    case 'v':
                        verbose = true;
                        break;
                    case 't':
                        {
                            ++argIndex;
                            if (argIndex >= args.Length) { Exit(); throw new Exception(); }
                            if (int.TryParse(args[argIndex], out boostingType) == false || boostingType < 0 || boostingType > 2)
                            {
                                Console.Error.WriteLine("error: invalid type of boosting");
                                Exit();
                                throw new Exception();
                            }
                            break;
                        }
                    case 'r':
                        {
                            ++argIndex;
                            if (argIndex >= args.Length) { Exit(); Environment.Exit(0); }
                            if (int.TryParse(args[argIndex], out roundTotal) == false || roundTotal < 0)
                            {
                                Console.Error.WriteLine("error: negative number of rounds");
                                Exit();
                                throw new Exception();
                            }
                            break;
                        }
                    default:
                        {
                            Console.Error.WriteLine("error: undefined option");
                            Exit();
                            throw new Exception();
                        }
                }
            }

            // Training data file
            if (argIndex >= args.Length) { Exit(); Environment.Exit(0); }
            var trainingDataFilename = args[argIndex];

            // Model file
            ++argIndex;
            var outputModelFilename = argIndex >= args.Length ? trainingDataFilename + ".model" : args[argIndex];

            return new AdaBoostTrainParameter(
                verbose,
                trainingDataFilename,
                outputModelFilename,
                boostingType,
                roundTotal
            );
        }
    };

    public struct AdaBoostPredictParameter
    {
        public bool Verbose { get; }
        public string TestDataFilename { get; }
        public string ModelFilename { get; }
        public bool OutputScoreFile { get; }
        public string OutputScorelFilename { get; }

        private AdaBoostPredictParameter(
        bool verbose,
        string testDataFilename,
        string modelFilename,
        bool outputScoreFile,
        string outputScorelFilename
        )
        {
            Verbose = verbose;
            TestDataFilename = testDataFilename;
            ModelFilename = modelFilename;
            OutputScoreFile = outputScoreFile;
            OutputScorelFilename = outputScorelFilename;
        }

        private static void Exit()
        {
            Console.Error.WriteLine("usage: abtrain [options] test_set_file model_file");
            Console.Error.WriteLine("options:");
            Console.Error.WriteLine("   -o: output score file");
            Console.Error.WriteLine("   -v: verbose");

            Environment.Exit(1);
        }

        public static AdaBoostPredictParameter ParseCommandline(string[] args)
        {
            var verbose = false;
            var outputScoreFile = false;
            var outputScorelFilename = "";

            // Options
            int argIndex;
            for (argIndex = 1; argIndex < args.Length; ++argIndex)
            {
                if (args[argIndex].Length == 0 || args[argIndex][0] != '-') break;

                switch (args[argIndex][1])
                {
                    case 'v':
                        verbose = true;
                        break;
                    case 'o':
                        {
                            ++argIndex;
                            if (argIndex >= args.Length) { Exit(); throw new Exception(); }
                            outputScoreFile = true;
                            outputScorelFilename = args[argIndex];
                            break;
                        }
                    default:
                        {
                            Console.Error.WriteLine("error: undefined option");
                            Exit();
                            throw new Exception();

                        }
                }
            }

            // Test data file
            if (argIndex >= args.Length) { Exit(); throw new Exception(); }
            var testDataFilename = args[argIndex];

            // Model file
            ++argIndex;
            if (argIndex >= args.Length) { Exit(); throw new Exception(); }
            var modelFilename = args[argIndex];

            return new AdaBoostPredictParameter(
            verbose,
            testDataFilename,
            modelFilename,
            outputScoreFile,
            outputScorelFilename
            );
        }
    };

    public class SampleDataFile
    {
        private struct FeatureElement
        {
            public int Index { get; }
            public double Value { get; }

            public FeatureElement(int index, double value)
            {
                Index = index;
                Value = value;
            }
        }

        public List<List<double>> SampleFeatures { get; }
        public List<bool> SampleLabels { get; }

        private SampleDataFile(List<List<double>> sampleFeatures, List<bool> sampleLabels)
        {
            SampleFeatures = sampleFeatures;
            SampleLabels = sampleLabels;
        }

        public static SampleDataFile Load(string sampleDataFilename)
        {
            var dataFile = new StreamReader(sampleDataFilename);
            var featureElementList = new List<List<FeatureElement>>();
            var labelList = new List<int>();

            int featureDimension = 0;

            string lineBuffer;
            while ((lineBuffer = dataFile.ReadLine()) != null)
            {
                var tokens = Regex.Split(lineBuffer, @"\s+");

                if (int.TryParse(tokens[0], out int label) == false)
                {
                    Console.Error.WriteLine($"error: bad format in data file ({sampleDataFilename})");
                    return null;
                }
                labelList.Add(label);

                List<FeatureElement> featureElements = new List<FeatureElement>();
                foreach (var tok in tokens.Skip(1))
                {
                    var iv = tok.Split(":".ToCharArray());

                    if (iv.Length < 2)
                    {
                        break;
                    }
                    var indexChar = iv[0];
                    var valueChar = iv[1];
                    if (String.IsNullOrEmpty(valueChar))
                    {
                        break;
                    }

                    if (int.TryParse(indexChar, out int index) == false ||
                        double.TryParse(valueChar, out double value) == false)
                    {
                        Console.Error.WriteLine($"error: bad format in data file ({sampleDataFilename})");
                        return null;
                    }
                    FeatureElement newElement = new FeatureElement(index, value);
                    featureElements.Add(newElement);
                    if (newElement.Index > featureDimension)
                    {
                        featureDimension = newElement.Index;
                    }
                }
                featureElementList.Add(featureElements);

            }
            dataFile.Dispose();

            int sampleTotal = featureElementList.Count;

            var sampleFeatures = Enumerable.Repeat(0, sampleTotal).Select(x => new List<double>()).ToList();
            var sampleLabels = Enumerable.Repeat(0, sampleTotal).Select(x => false).ToList();
            for (int sampleIndex = 0; sampleIndex < sampleTotal; ++sampleIndex)
            {
                sampleLabels[sampleIndex] = labelList[sampleIndex] > 0;

                sampleFeatures[sampleIndex] = Enumerable.Repeat(0.0, featureDimension).ToList();
                for (int elementIndex = 0; elementIndex < featureElementList[sampleIndex].Count; ++elementIndex)
                {
                    var n = featureElementList[sampleIndex][elementIndex].Index - 1;
                    sampleFeatures[sampleIndex][n] = featureElementList[sampleIndex][elementIndex].Value;
                }
            }
            return new SampleDataFile(sampleFeatures, sampleLabels);
        }
    }

    public class AdaBoost
    {
        public struct SampleElement : IComparable<SampleElement>
        {
            public int SampleIndex { get; }
            public double SampleValue { get; }

            public SampleElement(int sampleIndex, double sampleValue)
            {
                SampleIndex = sampleIndex;
                SampleValue = sampleValue;
            }

            public int CompareTo(SampleElement other)
            {
                if (Math.Abs(SampleValue - other.SampleValue) < double.Epsilon)
                {
                    return SampleIndex.CompareTo(other.SampleIndex);
                }
                else
                {
                    return SampleValue.CompareTo(other.SampleValue);
                }
            }
        };

        public AdaBoost(int boostingType = 2)
        {
            _boostingType = boostingType;
            _featureTotal = 0;
            _sampleTotal = 0;

        }

        public void SetBoostingType(int boostingType)
        {
            if (boostingType < 0 || boostingType > 2)
            {
                throw new Exception("error: invalid type of boosting");
            }

            _boostingType = boostingType;
        }

        public void SetTrainingSamples(string trainingDataFilename)
        {
            SampleDataFile datafile = SampleDataFile.Load(trainingDataFilename);
            _samples.Clear();
            _samples.AddRange(datafile.SampleFeatures);
            _labels.Clear();
            _labels.AddRange(datafile.SampleLabels);
            _sampleTotal = _samples.Count;
            if (_sampleTotal == 0)
            {
                throw new Exception("error: no training sample");
            }
            _featureTotal = _samples[0].Count;
            InitializeWeights();
            SortSampleIndices();

            _weakClassifiers.Clear();
        }

        public void Train(int roundTotal, bool verbose = false)
        {
            for (int roundCount = 0; roundCount < roundTotal; ++roundCount)
            {
                TrainRound();

                if (verbose)
                {
                    Console.WriteLine($"Round {roundCount}: ");
                    Console.Write($"feature = {_weakClassifiers[roundCount].FeatureIndex}, ");
                    Console.Write($"threshold = {_weakClassifiers[roundCount].Threshold}, ");
                    Console.Write($"output = [ {_weakClassifiers[roundCount].OutputLarger}, {_weakClassifiers[roundCount].OutputSmaller} ], ");
                    Console.Write($"error = {_weakClassifiers[roundCount].Error}");
                    Console.WriteLine();
                }
            }

            // Prediction test
            if (verbose)
            {
                int positiveTotal = 0;
                int positiveCorrectTotal = 0;
                int negativeTotal = 0;
                int negativeCorrectTotal = 0;
                for (int sampleIndex = 0; sampleIndex < _sampleTotal; ++sampleIndex)
                {
                    double score = Predict(_samples[sampleIndex]);

                    if (_labels[sampleIndex])
                    {
                        ++positiveTotal;
                        if (score > 0) ++positiveCorrectTotal;
                    }
                    else
                    {
                        ++negativeTotal;
                        if (score <= 0) ++negativeCorrectTotal;
                    }
                }

                Console.WriteLine();
                Console.WriteLine("Training set");
                Console.Write($"positive: {(double)(positiveCorrectTotal) / positiveTotal} ({positiveCorrectTotal} / {positiveTotal})");
                Console.Write(", ");
                Console.Write($"negative: {(double)(negativeCorrectTotal) / negativeTotal} ({negativeCorrectTotal} / {negativeTotal})");
                Console.WriteLine();
            }
        }

        public double Predict(List<double> featureVector)
        {
            double score = 0.0;
            foreach (var classifier in _weakClassifiers)
            {
                score += classifier.Evaluate(featureVector);
            }

            return score;
        }

        public void WriteFile(string filename)
        {
            using (StreamWriter outputModelStream = new StreamWriter(filename))
            {

                int roundTotal = _weakClassifiers.Count;
                outputModelStream.WriteLine(roundTotal);
                for (int roundIndex = 0; roundIndex < roundTotal; ++roundIndex)
                {
                    outputModelStream.Write($"{_weakClassifiers[roundIndex].FeatureIndex} ");
                    outputModelStream.Write($"{_weakClassifiers[roundIndex].Threshold} ");
                    outputModelStream.Write($"{_weakClassifiers[roundIndex].OutputLarger} ");
                    outputModelStream.WriteLine($"{_weakClassifiers[roundIndex].OutputSmaller}");
                }

            }
        }

        public bool ReadFile(string filename)
        {
            using (StreamReader inputModelStream = new StreamReader(filename))
            {
                var line = inputModelStream.ReadLine();

                if (line == null || int.TryParse(line, out int roundTotal) == false)
                {
                    return false;
                }
                _weakClassifiers.Clear();

                for (int roundIndex = 0; roundIndex < roundTotal; ++roundIndex)
                {
                    line = inputModelStream.ReadLine();
                    if (line == null)
                    {
                        return false;
                    }
                    var cells = Regex.Split(line, @"\s+");
                    if (cells.Length < 4)
                    {
                        return false;
                    }
                    var featureIndex = int.Parse(cells[0]);
                    var threshold = double.Parse(cells[1]);
                    var outputLarger = double.Parse(cells[2]);
                    var outputSmaller = double.Parse(cells[3]);

                    var ds = new DecisionStump(featureIndex, threshold, outputLarger, outputSmaller);
                    _weakClassifiers.Add(ds);
                }
                return true;
            }
        }

        private class DecisionStump
        {
            public DecisionStump()
            {
                FeatureIndex = -1;
                Error = -1;
            }

            public DecisionStump(
                int featureIndex,
                 double threshold,
                 double outputLarger,
                 double outputSmaller,
                 double error = -1)
            {
                FeatureIndex = featureIndex;
                Threshold = threshold;
                OutputLarger = outputLarger;
                OutputSmaller = outputSmaller;
                Error = error;
            }

            public double Evaluate(double featureValue)
            {
                return featureValue > Threshold ? OutputLarger : OutputSmaller;
            }
            public double Evaluate(List<double> featureVector)
            {
                return featureVector[FeatureIndex] > Threshold ? OutputLarger : OutputSmaller;
            }

            public int FeatureIndex { get; }
            public double Threshold { get; }
            public double OutputLarger { get; }
            public double OutputSmaller { get; }
            public double Error { get; }

        };

        private void InitializeWeights()
        {
            double initialWeight = 1.0 / _sampleTotal;

            _weights.Clear();
            _weights.AddRange(Enumerable.Repeat(initialWeight, _sampleTotal));
        }

        private void SortSampleIndices()
        {
            _sortedSampleIndices.Clear();
            for (var d = 0; d < _featureTotal; ++d)
            {
                //List<SampleElement> featureElements = Enumerable.Range(0, _sampleTotal).Select(x => new SampleElement(sampleIndex: x, sampleValue: _samples[x][d])).ToList();
                List<SampleElement> featureElements = _samples.Select((x,i) => new SampleElement(sampleIndex: i, sampleValue: x[d])).ToList();
                featureElements.Sort();
                //_sortedSampleIndices.Add(Enumerable.Range(0, _sampleTotal).Select(x => featureElements[x].SampleIndex).ToList());
                _sortedSampleIndices.Add(featureElements.Select(x => x.SampleIndex).ToList());
            }
        }

        private void TrainRound()
        {
            CalcWeightSum();

            DecisionStump bestClassifier = new DecisionStump();
            for (int featureIndex = 0; featureIndex < _featureTotal; ++featureIndex)
            {
                DecisionStump optimalClassifier = LearnOptimalClassifier(featureIndex);
                if (optimalClassifier.FeatureIndex < 0) continue;

                if (bestClassifier.Error < 0 || optimalClassifier.Error < bestClassifier.Error)
                {
                    bestClassifier = optimalClassifier;
                }
            }

            UpdateWeight(bestClassifier);

            _weakClassifiers.Add(bestClassifier);
        }

        private void CalcWeightSum()
        {
            _weightSum = 0;
            _weightLabelSum = 0;
            _positiveWeightSum = 0;
            _negativeWeightSum = 0;

            for (int sampleIndex = 0; sampleIndex < _sampleTotal; ++sampleIndex)
            {
                _weightSum += _weights[sampleIndex];
                if (_labels[sampleIndex])
                {
                    _weightLabelSum += _weights[sampleIndex];
                    _positiveWeightSum += _weights[sampleIndex];
                }
                else
                {
                    _weightLabelSum -= _weights[sampleIndex];
                    _negativeWeightSum += _weights[sampleIndex];
                }
            }
        }

        private DecisionStump LearnOptimalClassifier(int featureIndex)
        {
            const double epsilonValue = 1e-6;

            double weightSumLarger = _weightSum;
            double weightLabelSumLarger = _weightLabelSum;
            double positiveWeightSumLarger = _positiveWeightSum;
            double negativeWeightSumLarger = _negativeWeightSum;

            DecisionStump optimalClassifier = new DecisionStump();
            for (int sortIndex = 0; sortIndex < _sampleTotal - 1; ++sortIndex)
            {
                int sampleIndex = _sortedSampleIndices[featureIndex][sortIndex];
                double threshold = _samples[sampleIndex][featureIndex];

                double sampleWeight = _weights[sampleIndex];
                weightSumLarger -= sampleWeight;
                if (_labels[sampleIndex])
                {
                    weightLabelSumLarger -= sampleWeight;
                    positiveWeightSumLarger -= sampleWeight;
                }
                else
                {
                    weightLabelSumLarger += sampleWeight;
                    negativeWeightSumLarger -= sampleWeight;
                }

                while (sortIndex < _sampleTotal - 1
                       && Math.Abs(_samples[sampleIndex][featureIndex] - _samples[_sortedSampleIndices[featureIndex][sortIndex + 1]][featureIndex]) < double.Epsilon)
                {
                    ++sortIndex;
                    sampleIndex = _sortedSampleIndices[featureIndex][sortIndex];
                    sampleWeight = _weights[sampleIndex];
                    weightSumLarger -= sampleWeight;
                    if (_labels[sampleIndex])
                    {
                        weightLabelSumLarger -= sampleWeight;
                        positiveWeightSumLarger -= sampleWeight;
                    }
                    else
                    {
                        weightLabelSumLarger += sampleWeight;
                        negativeWeightSumLarger -= sampleWeight;
                    }
                }
                if (sortIndex >= _sampleTotal - 1) break;

                if (Math.Abs(weightSumLarger) < epsilonValue || Math.Abs(_weightSum - weightSumLarger) < epsilonValue) continue;

                ComputeClassifierOutputs(weightSumLarger, weightLabelSumLarger, positiveWeightSumLarger, negativeWeightSumLarger,
                    out double outputLarger, out double outputSmaller);

                double error = ComputeError(positiveWeightSumLarger, negativeWeightSumLarger, outputLarger, outputSmaller);

                if (optimalClassifier.Error < 0 || error < optimalClassifier.Error)
                {
                    double classifierThreshold = (threshold + _samples[_sortedSampleIndices[featureIndex][sortIndex + 1]][featureIndex]) / 2.0;

                    if (_boostingType == 0)
                    {
                        double classifierWeight = Math.Log((1.0 - error) / error) / 2.0;
                        outputLarger *= classifierWeight;
                        outputSmaller *= classifierWeight;
                    }

                    optimalClassifier = new DecisionStump(featureIndex, classifierThreshold, outputLarger, outputSmaller, error);
                }
            }

            return optimalClassifier;
        }

        private void ComputeClassifierOutputs(
                double weightSumLarger,
                double weightLabelSumLarger,
                double positiveWeightSumLarger,
                double negativeWeightSumLarger,
            out double outputLarger,
            out double outputSmaller
        )
        {
            if (_boostingType == 0)
            {
                // Discrete AdaBoost
                if (weightLabelSumLarger > 0)
                {
                    outputLarger = 1.0;
                    outputSmaller = -1.0;
                }
                else
                {
                    outputLarger = -1.0;
                    outputSmaller = 1.0;
                }
            }
            else if (_boostingType == 1)
            {
                // Real AdaBoost
                const double epsilonReal = 0.0001;
                outputLarger = Math.Log((positiveWeightSumLarger + epsilonReal) / (negativeWeightSumLarger + epsilonReal)) / 2.0;
                outputSmaller = Math.Log((_positiveWeightSum - positiveWeightSumLarger + epsilonReal)
                                    / (_negativeWeightSum - negativeWeightSumLarger + epsilonReal)) / 2.0;
            }
            else
            {
                // Gentle AdaBoost
                outputLarger = weightLabelSumLarger / weightSumLarger;
                outputSmaller = (_weightLabelSum - weightLabelSumLarger) / (_weightSum - weightSumLarger);
            }
        }

        private double ComputeError(
            double positiveWeightSumLarger,
            double negativeWeightSumLarger,
            double outputLarger,
            double outputSmaller
        )
        {
            double error;
            if (_boostingType == 0)
            {
                // Discrete AdaBoost
                error = positiveWeightSumLarger * (1.0 - outputLarger) / 2.0
                        + (_positiveWeightSum - positiveWeightSumLarger) * (1.0 - outputSmaller) / 2.0
                        + negativeWeightSumLarger * (-1.0 - outputLarger) / -2.0
                        + (_negativeWeightSum - negativeWeightSumLarger) * (-1.0 - outputSmaller) / -2.0;
            }
            else if (_boostingType == 1)
            {
                // Real AdaBoost
                error = positiveWeightSumLarger * Math.Exp(-outputLarger)
                        + (_positiveWeightSum - positiveWeightSumLarger) * Math.Exp(-outputSmaller)
                        + negativeWeightSumLarger * Math.Exp(outputLarger)
                        + (_negativeWeightSum - negativeWeightSumLarger) * Math.Exp(outputSmaller);
            }
            else
            {
                // Gentle AdaBoost
                error = positiveWeightSumLarger * (1.0 - outputLarger) * (1.0 - outputLarger)
                        + (_positiveWeightSum - positiveWeightSumLarger) * (1.0 - outputSmaller) * (1.0 - outputSmaller)
                        + negativeWeightSumLarger * (-1.0 - outputLarger) * (-1.0 - outputLarger)
                        + (_negativeWeightSum - negativeWeightSumLarger) * (-1.0 - outputSmaller) * (-1.0 - outputSmaller);
            }

            return error;
        }

        private void UpdateWeight(DecisionStump bestClassifier)
        {
            double updatedWeightSum = 0.0;
            for (int sampleIndex = 0; sampleIndex < _sampleTotal; ++sampleIndex)
            {
                int labelInteger;
                if (_labels[sampleIndex]) labelInteger = 1;
                else labelInteger = -1;
                _weights[sampleIndex] *= Math.Exp(-1.0 * labelInteger * bestClassifier.Evaluate(_samples[sampleIndex]));
                updatedWeightSum += _weights[sampleIndex];
            }

            for (int sampleIndex = 0; sampleIndex < _sampleTotal; ++sampleIndex)
            {
                _weights[sampleIndex] /= updatedWeightSum;
            }
        }

        private int _boostingType;
        private int _featureTotal;
        private readonly List<DecisionStump> _weakClassifiers = new List<DecisionStump>();

        // Training samples
        private int _sampleTotal;
        private readonly List<List<double>> _samples = new List<List<double>>();
        private readonly List<bool> _labels = new List<bool>();
        private readonly List<double> _weights = new List<double>();

        // Data for training
        private readonly List<List<int>> _sortedSampleIndices = new List<List<int>>();
        private double _weightSum;
        private double _weightLabelSum;
        private double _positiveWeightSum;
        private double _negativeWeightSum;
    }
}
