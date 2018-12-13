using System;
using System.Linq;
using CNN.Extensions;

namespace CNN {
    static class Program {
        static void Main() {
            int numInputNodes = 28 * 28;
            int numHiddenNodes = 100;
            int numOutputNodes = 10;
            double alpha = 0.1;

            Console.WriteLine("read data...");

            double[][] trainImageVectors = MNist.LoadImages("train-images.idx3-ubyte").Select(x => x.Resize(28, 28)).Select(x => x.ToNormalizedVector()).ToArray();
            double[][] trainLabelVectors = MNist.LoadLabels("train-labels.idx1-ubyte").Select(x => numOutputNodes.Times().Select(i => x.Value == i ? 0.99 : 0.01).ToArray()).ToArray();
            double[][] testImageVectors = MNist.LoadImages("t10k-images.idx3-ubyte").Select(x => x.Resize(28, 28)).Select(x => x.ToNormalizedVector()).ToArray();
            int[] testLabelIndex = MNist.LoadLabels("t10k-labels.idx1-ubyte").Select(x => (int)x.Value).ToArray();

            System.IO.File.WriteAllLines("t10k.csv", testImageVectors.Zip(testLabelIndex, (data, label) => data.Select(y => y.ToString()).Apply(y => String.Join(",", y)).Apply(y => y + $",{label}")).ToArray());

            Console.WriteLine("initialize...");

            NeuralNetwork nn = null;
            if (System.IO.File.Exists("weight.dat")) {
                for (; ; ) {
                    Console.Write("Load model? [yn]:");
                    switch (Console.ReadKey().KeyChar) {
                        case 'y':
                            nn = NeuralNetwork.LoadModel("weight.dat");
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
            if (nn == null) {
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
            for (var i = 0; i < trainImageVectors.Length; i++) {
                nn.CalcForward(trainImageVectors[i]).CalcError(trainLabelVectors[i]).UpdateWeight(alpha);
                if ((i % 100) == 0) {
                    Console.WriteLine($"{i}: {test(100)}");
                }
            }

            Console.WriteLine("testing...");
            Console.WriteLine($"{test(testLabelIndex.Length) * 100.0} %");


            //nn.ToForwardAdjacencyMatrices().ForEach((matrix, i) => {
            //    System.IO.File.WriteAllLines($"Layer_{i}_to_{i + 1}.csv", matrix.Cols().Select(x => x.Select(y => y.ToString()).Apply(y => String.Join(",", y))).ToArray());
            //});


            for (; ; ) {
                Console.Write("Save model? [yn]:");
                switch (Console.ReadKey().KeyChar) {
                    case 'y':
                        nn.SaveModel("weight.dat");
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
