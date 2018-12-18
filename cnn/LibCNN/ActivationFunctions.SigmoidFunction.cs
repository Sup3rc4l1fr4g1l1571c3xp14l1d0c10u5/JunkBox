using System;
using System.Linq;

namespace LibCNN.ActivationFunctions {
    /// <summary>
    /// シグモイド関数
    /// </summary>
    public sealed class SigmoidFunction : IActivationFunction {
        /// <summary>
        /// 関数
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public double[] Activate(double[] val) {
            return val.Select(x => 1.0 / (1.0 + Math.Exp(-x))).ToArray();
        }

        /// <summary>
        /// 微分済み関数
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public double[] Derivate(double[] val) {
            return val.Select(x => (1.0 - x) * x).ToArray() ;
        }
    }
}