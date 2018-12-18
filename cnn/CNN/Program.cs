using System;
using System.Linq;
using LibCNN;
using LibMNIST;
using LibPredicate;

namespace CNN
{
    static class Program
    {
        static void Main()
        {
            int numInputNodes = 28 * 28;
            int numHiddenNodes = 100;
            int numOutputNodes = 10;
            double alpha = 0.1;

            Console.WriteLine("read data...");

            double[][] testImageVectors;
            int[] testLabelIndexes;
            using (var testImages = new MNIST.ImageLoader("t10k-images.idx3-ubyte"))
            using (var testLabels = new MNIST.LabelLoader("t10k-labels.idx1-ubyte"))
            {
                testImageVectors = testImages.Select(x => x.Resize(28, 28)).Select(x => x.ToNormalizedVector()).ToArray();
                testLabelIndexes = testLabels.Select(x => (int)x.Value).ToArray();
            }

            LibCNN.CNN nn = null;
            if (System.IO.File.Exists("weight.dat"))
            {
                for (; ; )
                {
                    Console.Write("Load model? [yn]:");
                    switch (Console.ReadKey().KeyChar)
                    {
                        case 'y':
                            nn = LibCNN.CNN.Load("weight.dat");
                            break;
                        case 'n':
                            break;
                        default:
                            continue;
                    }
                    Console.WriteLine();
                    break;
                }
            }

            if (nn == null)
            {
                nn = new LibCNN.CNN(numInputNodes, numOutputNodes, numHiddenNodes);
            }

            using (var trainImages = new MNIST.ImageLoader("train-images.idx3-ubyte"))
            using (var trainLabels = new MNIST.LabelLoader("train-labels.idx1-ubyte"))
            {
                var trainImageVectors = trainImages.Select(x => x.Resize(28, 28)).Select(x => x.ToNormalizedVector());
                var trainLabelVectors = trainLabels.Select(x => numOutputNodes.Times().Select(i => x.Value == i ? 0.99 : 0.01).ToArray());
                Console.WriteLine("initialize...");
                int step = 0;
                foreach (var il in trainImageVectors.Zip(trainLabelVectors, Tuple.Create))
                {
                    var image = il.Item1;
                    var label = il.Item2;
                    nn.Train(image, label, alpha);
                    if ((++step % 100) == 0)
                    {
                        var ok = testImageVectors.Zip(testLabelIndexes, Tuple.Create).Sample(100).Count(x => nn.Predict(x.Item1).Output.IndexOfMax() == x.Item2);
                        Console.WriteLine($"{step}: {ok / 100.0}");
                    }
                }
            }
            {
                var ok = testImageVectors.Zip(testLabelIndexes, Tuple.Create).Count(x => nn.Predict(x.Item1).Output.IndexOfMax() == x.Item2);
                Console.WriteLine($"finish: {ok / (double)testLabelIndexes.Length}");
            }
            for (; ; ) {
                Console.Write("Save model? [yn]:");
                switch (Console.ReadKey().KeyChar)
                {
                    case 'y':
                        nn.Save("weight.dat");
                        break;
                    case 'n':
                        break;
                    default:
                        continue;
                }
                Console.WriteLine();
                break;
            }
        }

        static void Main2()
        {
            int numInputNodes = 28 * 28;
            int numHiddenNodes = 100;
            int numOutputNodes = 10;
            double alpha = 0.1;

            Console.WriteLine("read data...");

            double[][] trainImageVectors = new MNIST.ImageLoader("train-images.idx3-ubyte").Using(o => o.Select(x => x.Resize(28, 28)).Select(x => x.ToNormalizedVector()).ToArray());
            double[][] trainLabelVectors = new MNIST.LabelLoader("train-labels.idx1-ubyte").Using(o => o.Select(x => numOutputNodes.Times().Select(i => x.Value == i ? 0.99 : 0.01).ToArray()).ToArray());
            int[] trainLabelIndex = new MNIST.LabelLoader("train-labels.idx1-ubyte").Using(o => o.Select(x => (int)x.Value).ToArray());
            //System.IO.File.WriteAllLines("train.csv", trainImageVectors.Zip(trainLabelIndex, (data, label) => data.Select(y => y.ToString()).Apply(y => String.Join(",", y)).Apply(y => y + $",{label}")).ToArray());

            double[][] testImageVectors = new MNIST.ImageLoader("t10k-images.idx3-ubyte").Using(o => o.Select(x => x.Resize(28, 28)).Select(x => x.ToNormalizedVector()).ToArray());
            int[] testLabelIndex = new MNIST.LabelLoader("t10k-labels.idx1-ubyte").Using(o => o.Select(x => (int)x.Value).ToArray());
            //System.IO.File.WriteAllLines("t10k.csv", testImageVectors.Zip(testLabelIndex, (data, label) => data.Select(y => y.ToString()).Apply(y => String.Join(",", y)).Apply(y => y + $",{label}")).ToArray());

            Console.WriteLine("initialize...");

            NeuralNetwork nn = null;
            if (System.IO.File.Exists("weight.dat"))
            {
                for (; ; )
                {
                    Console.Write("Load model? [yn]:");
                    switch (Console.ReadKey().KeyChar)
                    {
                        case 'y':
                            nn = NeuralNetwork.LoadWeight("weight.dat");
                            break;
                        case 'n':
                            break;
                        default:
                            continue;
                    }
                    Console.WriteLine();
                    break;
                }
            }
            if (nn == null)
            {
                nn = new NeuralNetwork(
                    inputLayerSize: numInputNodes,
                    hiddenLayerSizes: new[] { numHiddenNodes },
                    outputLayerSize: numOutputNodes
                );
            }

            var rand = new Random();
            Func<int, double> test = (sampling) =>
                 Enumerable.Range(0, testImageVectors.Length)
                           .Sample(rand, sampling)
                           .Count(i => nn.CalcForward(testImageVectors[i]).GetMaxOutput() == testLabelIndex[i]) / (double)sampling;

            Console.WriteLine("training...");
            for (var i = 0; i < trainImageVectors.Length; i++)
            {
                nn.CalcForward(trainImageVectors[i]);
                nn.CalcError(trainLabelVectors[i]);
                for (var j = 0; j < nn.Layers.Count; j++)
                {
                    nn.Layers[j].Nodes
                        .Select(x => x.OutputValue.ToString())
                        .Apply(x => String.Join(",", x))
                        .Tap(x => System.IO.File.WriteAllText($"Step_{i}_Layer_{j}_OutputValue.csv", x));
                    nn.Layers[j].Nodes
                        .Select(x => x.Error.ToString())
                        .Apply(x => String.Join(",", x))
                        .Tap(x => System.IO.File.WriteAllText($"Step_{i}_Layer_{j}_ErrorValue.csv", x));
                    nn.Layers[j].Nodes
                        .Select(x => x.OutputEdges.Select(y => y.Weight.ToString()).Apply(y => String.Join(",", y)))
                        .Tap(x => System.IO.File.WriteAllLines($"Step_{i}_Layer_{j}_To_{j + 1}_WeightValue.csv", x));
                }
                nn.UpdateWeight(alpha);
                if ((i % 100) == 0)
                {
                    Console.WriteLine($"{i}: {test(100)}");
                }
            }

            Console.WriteLine("testing...");
            Console.WriteLine($"{test(testLabelIndex.Length) * 100.0} %");


            nn.ToForwardAdjacencyMatrices().ForEach((matrix, i) =>
            {
                System.IO.File.WriteAllLines($"Layer_{i}_to_{i + 1}.csv", matrix.Cols().Select(x => x.Select(y => y.ToString()).Apply(y => String.Join(",", y))).ToArray());
            });


            for (; ; )
            {
                Console.Write("Save model? [yn]:");
                switch (Console.ReadKey().KeyChar)
                {
                    case 'y':
                        nn.SaveWeight("weight.dat");
                        break;
                    case 'n':
                        break;
                    default:
                        continue;
                }
                Console.WriteLine();
                break;
            }

        }

    }
}
