using System;

namespace CNN.ActivationFunctions {
    /// <summary>
    /// シグモイド関数
    /// </summary>
    sealed public class SigmoidFunction : IActivationFunction {
        /// <summary>
        /// 関数
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public double Activate(double val) {
            return 1.0 / (1.0 + Math.Exp(-val));
        }

        /// <summary>
        /// 微分済み関数
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public double Derivate(double val) {
            return (1.0 - val) * val;
        }
    }
}