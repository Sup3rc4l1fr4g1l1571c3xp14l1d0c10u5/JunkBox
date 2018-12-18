using MathNet.Numerics.LinearAlgebra;

namespace LibCNN.ActivationFunctions {
    /// <summary>
    /// 活性化関数インタフェース
    /// </summary>
    public interface IActivationFunction {
        /// <summary>
        /// 関数
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        double[] Activate(double[] val);

        /// <summary>
        /// 微分済み関数
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        double[] Derivate(double[] val);
    }
}