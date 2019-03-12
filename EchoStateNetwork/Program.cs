using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EchoStateNetwork {
    static class Program {
        [STAThread]
        static void Main() {

            // 入力データを準備

        }
    }

    public class Matrix {
        private int _row { get; }
        private int _col { get; }
        private double[] _values { get; }


        private Matrix(int row, int col, double[] values) {
            if (row < 0 || col < 0 || values.Length != row * col) {
                throw new Exception("out of index");
            }
            this._row = row;
            this._col = col;
            this._values = values;
        }

        public static Matrix create(int row, int col, Func<int/*row*/, int/*col*/, double> gen) {
            var ret = new double[row * col];
            var n = 0;
            for (var i = 0; i < row; i++) {
                for (var j = 0; j < col; j++) {
                    ret[n++] = gen(i, j);
                }
            }
            return new Matrix(row, col, ret);
        }
        public static Matrix buildFromArray(int row, int col, double[] values) {
            return new Matrix(row, col, values);
        }

        public Matrix clone() {
            return new Matrix(this._row, this._col, this._values.ToArray());
        }

        public double[] toArray() {
            return this._values.ToArray();
        }

        public override string ToString() {
            var lines = new List<string>();
            var line = new double[this._col];
            for (var j = 0; j < this._row; j++) {
                for (var i = 0; i < this._col; i++) {
                    line[i] = this._values[this.at(j, i)];
                }
                lines.Add($"[{String.Join(", ", line)}]");
            }
            return $"[{String.Join(", ", lines)}]";
        }

        private int at(int row, int col) {
            if (row < 0 || this._row <= row || col < 0 || this._col <= col) {
                throw new Exception("out of index");
            }
            return this._col * row + col;
        }

        public double[] getRow(int row) {
            if (row < 0 || this._row <= row) {
                throw new Exception("out of index");
            }
            var ret = new double[this._col];
            for (var j = 0; j < this._col; j++) {
                ret[j] = this._values[this.at(row, j)];
            }
            return ret;
        }

        public Matrix setRow(int row, double[] values) {
            if (row < 0 || this._row <= row) {
                throw new Exception("out of index");
            }
            if (this._col != values.Length) {
                throw new Exception("column length mismatch");
            }
            for (var j = 0; j < this._col; j++) {
                this._values[this.at(row, j)] = values[j];
            }
            return this;
        }

        public double[] getCol(int col) {
            if (col < 0 || this._col <= col) {
                throw new Exception("out of index");
            }
            var ret = new double[this._row];
            for (var j = 0; j < this._row; j++) {
                ret[j] = this._values[this.at(j, col)];
            }
            return ret;
        }

        public Matrix setCol(int col, double[] values) {
            if (col < 0 || this._col <= col) {
                throw new Exception("out of index");
            }
            if (this._row != values.Length) {
                throw new Exception("row length mismatch");
            }
            for (var j = 0; j < this._row; j++) {
                this._values[this.at(j, col)] = values[j];
            }
            return this;
        }

        public Matrix map(Func<double, double> func) {
            return new Matrix(this._row, this._col, this._values.Select(func).ToArray());
        }

        public static bool equal(Matrix m1, Matrix m2, double eps = double.Epsilon) {
            if (m1._row != m2._row || m1._col != m2._col) {
                return false;
            }
            for (var i = 0; i < m1._values.Length; i++) {
                if (Math.Abs(m1._values[i] - m2._values[i]) >= eps) {
                    return false;
                }
            }
            return true;
        }

        public static Matrix add(Matrix m1, Matrix m2) {
            if (m1._row != m2._row || m1._col != m2._col) {
                throw new Exception("dimension mismatch");
            }
            var size = m1._row * m1._col;
            var ret = new double[size];
            for (var i = 0; i < size; i++) {
                ret[i] = m1._values[i] + m2._values[i];
            }
            return new Matrix(m1._row, m1._col, ret);
        }

        public static Matrix sub(Matrix m1, Matrix m2) {
            if (m1._row != m2._row || m1._col != m2._col) {
                throw new Exception("dimension mismatch");
            }
            var size = m1._row * m1._col;
            var ret = new double[size];
            for (var i = 0; i < size; i++) {
                ret[i] = m1._values[i] - m2._values[i];
            }
            return new Matrix(m1._row, m1._col, ret);
        }

        public static Matrix scalarMultiplication(Matrix m, double s) {
            return m.map(x => x * s);
        }

        public static Matrix dotProduct(Matrix m1, Matrix m2) {
            if (m1._col != m2._row) {
                throw new Exception("dimension mismatch");
            }
            var ret = new double[m1._row * m2._col];
            var n = 0;
            var dp = 1;
            var dq = m2._col;
            for (var i = 0; i < m1._row; i++) {
                for (var j = 0; j < m2._col; j++) {
                    var sum = 0.0;
                    var p = m1.at(i, 0);
                    var q = m2.at(0, j);
                    for (var k = 0; k < m1._col; k++) {
                        sum += m1._values[p] * m2._values[q];
                        p += dp;
                        q += dq;
                    }
                    ret[n++] = sum;
                }
            }
            return new Matrix(m1._row, m2._col, ret);
        }

        public static Matrix hadamardProduct(Matrix m1, Matrix m2) {
            if (m1._row != m2._row || m1._col != m2._col) {
                throw new Exception("dimension mismatch");
            }
            var size = m1._row * m1._col;
            var ret = new double[size];
            for (var i = 0; i < size; i++) {
                ret[i] = m1._values[i] * m2._values[i];
            }
            return new Matrix(m1._row, m1._col, ret);
        }

        public static Matrix transpose(Matrix m) {
            var size = m._values.Length;
            var ret = new double[size];
            var p = 0; var dq = m._col;
            for (var j = 0; j < m._col; j++) {
                var q = m.at(0, j);
                for (var i = 0; i < m._row; i++) {
                    ret[p] = m._values[q];
                    p += 1;
                    q += dq;
                }
            }
            return new Matrix(m._col, m._row, ret);
        }

        public static bool test() {
            Func<double[], double[], bool> arrayEqual = (x, y) => x.Length == y.Length && x.SequenceEqual(y);

            // hadamardProduct test
            {
                var m1 = Matrix.buildFromArray(3, 2, new double[] { 2, 4, 6, 8, 10, 12 });
                var m2 = Matrix.buildFromArray(3, 2, new double[] { 3, 5, 7, 9, 11, 13 });
                var m3 = Matrix.hadamardProduct(m1, m2);
                var m4 = Matrix.buildFromArray(3, 2, new double[] { 6, 20, 42, 72, 110, 156 });
                if (!Matrix.equal(m3, m4)) { return false; }
                Console.WriteLine("hadamardProduct: ok");
            }

            // scalarMultiplication test
            {
                var m1 = Matrix.buildFromArray(3, 2, new double[] { 2, 4, 6, 8, 10, 12 });
                var m2 = Matrix.scalarMultiplication(m1, -4);
                var m3 = Matrix.buildFromArray(3, 2, new double[] { 2, 4, 6, 8, 10, 12 }.Select(x => x * -4).ToArray());
                if (!Matrix.equal(m2, m3)) { return false; }
                Console.WriteLine("scalarMultiplication: ok");
            }

            // dotProduct test
            {
                var m1 = Matrix.buildFromArray(3, 2, new double[] { 2, 4, 6, 8, 10, 12 });
                var m2 = Matrix.buildFromArray(2, 3, new double[] { 3, 5, 7, 13, 11, 9 });
                var m3 = Matrix.dotProduct(m1, m2);
                var m4 = Matrix.buildFromArray(3, 3, new double[] { 58, 54, 50, 122, 118, 114, 186, 182, 178 });
                if (!Matrix.equal(m3, m4)) { return false; }
                Console.WriteLine("dotProduct: ok");
            }

            // transpose test
            {
                var m1 = Matrix.buildFromArray(3, 2, new double[] { 2, 4, 6, 8, 10, 12 });
                var m2 = Matrix.transpose(m1);
                var m3 = Matrix.buildFromArray(2, 3, new double[] { 2, 6, 10, 4, 8, 12 });
                if (!Matrix.equal(m2, m3)) { return false; }
                Console.WriteLine("transpose: ok");
            }

            // toString test
            {
                var s1 = Matrix.buildFromArray(3, 2, new double[] { 2, 4, 6, 8, 10, 12 }).ToString();
                var s2 = "[[2, 4], [6, 8], [10, 12]]";
                if (s1 != s2) { return false; }
                Console.WriteLine("toString: ok");
            }

            // getRow test
            {
                var m1 = Matrix.buildFromArray(2, 3, new double[] { 3, 5, 7, 13, 11, 9 });
                var r0 = m1.getRow(0);
                var r1 = m1.getRow(1);
                if (!arrayEqual(r0, new double[] { 3, 5, 7 })) { return false; }
                if (!arrayEqual(r1, new double[] { 13, 11, 9 })) { return false; }
                Console.WriteLine("getRow: ok");
            }

            // getCol test
            {
                var m1 = Matrix.buildFromArray(2, 3, new double[] { 3, 5, 7, 13, 11, 9 });
                var c0 = m1.getCol(0);
                var c1 = m1.getCol(1);
                var c2 = m1.getCol(2);
                if (!arrayEqual(c0, new double[] { 3, 13 })) { return false; }
                if (!arrayEqual(c1, new double[] { 5, 11 })) { return false; }
                if (!arrayEqual(c2, new double[] { 7, 9 })) { return false; }
                Console.WriteLine("getCol: ok");
            }

            // setRow test
            {
                var m1 = Matrix.buildFromArray(2, 3, new double[] { 1, 2, 3, 4, 5, 6 });
                m1.setRow(0, new double[] { 7, 8, 9 });
                m1.setRow(1, new double[] { 10, 11, 12 });
                if (!arrayEqual(m1.toArray(), new double[] { 7, 8, 9, 10, 11, 12 })) { return false; }
                Console.WriteLine("setRow: ok");
            }

            // setCol test
            {
                var m1 = Matrix.buildFromArray(2, 3, new double[] { 1, 2, 3, 4, 5, 6 });
                m1.setCol(0, new double[] { 7, 8 });
                m1.setCol(1, new double[] { 9, 10 });
                m1.setCol(2, new double[] { 11, 12 });
                if (!arrayEqual(m1.toArray(), new double[] { 7, 9, 11, 8, 10, 12 })) { return false; }
                Console.WriteLine("setCol: ok");
            }

            return true;
        }
    }

    public class EchoStateNetwork {
        private IList<double[]> inputs;
        private List<double[]> log_reservoir_nodes;


        private Matrix weights_output;
        private int num_input_nodes;
        private int num_reservoir_nodes;
        private int num_output_nodes;
        private double leak_rate;
        private Func<double, double> activator;

        public EchoStateNetwork(IList<double[]> inputs, int num_input_nodes, int num_reservoir_nodes, int num_output_nodes, double leak_rate = 0.1, Func<double, double> activator = null) {
            activator = activator ?? Math.Tanh;

            this.inputs = inputs; // u: 時刻0から時刻N-1までの入力ベクトル列(Vector<double>[N]型だと思っておけばいい)
            this.log_reservoir_nodes = new List<double[]>() { Enumerable.Repeat(0.0, num_reservoir_nodes).ToArray() }; // reservoir層のノードの状態を記録

            // 各層の重みを初期化する
            this.weights_input = this._generate_variational_weights(num_input_nodes, num_reservoir_nodes); // W_in: 入力層の重み
            this.weights_reservoir = this._generate_reservoir_weights(num_reservoir_nodes); // W_res: Reservoir層の重み
            this.weights_output = new double[num_reservoir_nodes, num_output_nodes]; // W_out: 出力層の重み

            // それぞれの層のノードの数
            this.num_input_nodes = num_input_nodes; // K: 入力層のノード数
            this.num_reservoir_nodes = num_reservoir_nodes; // N: Reservoir層のノード数
            this.num_output_nodes = num_output_nodes; // K: 出力層のノード数

            this.leak_rate = leak_rate; // δ: 漏れ率
            this.activator = activator; // f: 活性化関数
        }

        // reservoir層のノードの次の状態を取得
        // 時刻 t-1 までの関数の状態と、時刻 t-1 での入力から、時刻 t における状態を求める
        // reservoir層の内部状態 x(t) = f((1−δ) * x(t−1) + δ(W_in * u(t−1) + W_res * x(t−1)) 
        private double[] _get_next_reservoir_nodes(double[] input, double[] current_state) {
            // this.leak_rate = δ
            // current_state = x(t−1) 
            // x(t−1): 時刻 t−1 までの関数の状態
            var next_state = current_state.Mul(1.0 - this.leak_rate);

            // this.leak_rate = δ
            // np.array([input]) = u(t-1) 
            // u(t-1): 時刻 t-1 の 入力
            // this.weights_input = W_in
            // current_state = x(t−1)
            // this.weights_reservoir = W_res
            next_state += this.leak_rate * (np.array([input]) @ this.weights_input + current_state @ this.weights_reservoir);

            // this.activator = f 
            return this.activator(next_state);
        }
    // 出力層の重みを更新
    // 最小二乗推定(二乗和誤差を最小にする係数βを求める): β=(X.T * X)^−1 * X.T * y
    //   -1 (逆行列導出)
    //   X.T: 計画行列 X の転置行列
    // リッジ回帰を入れる: β=(X.T * X + k * I)^−1 * X.T * y
    //   k: リッジパラメーター(小さな正の値)
    //   I: 単位行列
    // より
    //   Y_target: 教師データ
    //   λ: リッジパラメータk
    //   W_out = (X.T * X + λ * I)^−1 * X.T * Y_target
    def _update_weights_output(self, lambda0):
        // リッジ回帰部分式 k * I を求める
        // np.identity(this.num_reservoir_nodes) = I
        // lambda0 = λ
        // E_lambda0 = λ * I = k * I
        E_lambda0 = np.identity(this.num_reservoir_nodes) * lambda0 // lambda0
        
        // リッジ回帰を含めた (X.T * X + k * I)^−1  を求める
        // this.log_reservoir_nodes.T = X.T
        // this.log_reservoir_nodes = X
        // @ 演算子 = 行列積 (※なぜこれを注意書きしているかというと、numpyでは二項演算子 * を行列に適用するとアダマール積になるから。）
        // np.linalg.inv: ^-1 (逆行列導出)
        inv_x = np.linalg.inv(this.log_reservoir_nodes.T @ this.log_reservoir_nodes + E_lambda0)
        
        // 二乗和誤差を最小にする係数βを求める
        this.weights_output = (inv_x @ this.log_reservoir_nodes.T) @ this.inputs

    // 学習する
    def train(self, lambda0=0.1):
        // 時刻ごとの入力一つ毎に時系列で処理する
        for input in this.inputs:
            // input = u(t): 時刻 t の入力ベクトル
            // current_state = 時刻 t-1 における reservoir層の内部状態 
            current_state = np.array(this.log_reservoir_nodes[-1])
            // 時刻 tの入力ベクトル input と 時刻 t-1における reservoir層の内部状態 current_state から 時刻 t における reservoir 層の内部状態 next_state を求める
            next_state = this._get_next_reservoir_nodes(input, current_state)
            // reservoir層の内部状態リストの末尾に 新しい状態 next_state を追加する
            this.log_reservoir_nodes = np.append(this.log_reservoir_nodes, [next_state], axis=0)
        // reservoir層の内部状態リストの先頭（＝初期状態, 全部0初期化されてる）を削除
        this.log_reservoir_nodes = this.log_reservoir_nodes[1:]
        // 出力層の重みを更新
        this._update_weights_output(lambda0)

    // 学習で得られた重みを基に訓練データを学習できているかを出力
    def get_train_result(self):
        outputs = []
        // ゼロ初期化されたreservoir層の内部状態 reservoir_nodesを作る
        reservoir_nodes = np.zeros(this.num_reservoir_nodes)
        // 時刻ごとの入力一つ毎に時系列で処理する
        for input in this.inputs:
            // 入力ベクトル input と reservoir層の内部状態 reservoir_nodes から 次の時刻における reservoir 層の内部状態 reservoir_nodes を求める
            reservoir_nodes = this._get_next_reservoir_nodes(input, reservoir_nodes)
            // 求めた reservoir_nodes を元にした出力を生成して解のリストに追加
            outputs.append(this.get_output(reservoir_nodes))
        return outputs

    // 予測する
    def predict(self, length_of_sequence, lambda0=0.01):
        predicted_outputs = [this.inputs[-1]] // 学習データの最後のデータを予測開始時の入力とする
        reservoir_nodes = this.log_reservoir_nodes[-1] // 訓練の結果得た最後の内部状態を取得
        // 時間の進行を元に予測を進める
        for _ in range(length_of_sequence):
            reservoir_nodes = this._get_next_reservoir_nodes(predicted_outputs[-1], reservoir_nodes)
            predicted_outputs.append(this.get_output(reservoir_nodes))
        // 最初に使用した学習データの最後のデータを外して返す
        return predicted_outputs[1:] 

    // 時刻 t における reservoir層の内部状態 reservoir_nodes と 出力層の重み this.weights_output の行列積結果に
    // 活性化関数 this.activator を適用することで時刻 t における出力値を得る
    def get_output(self, reservoir_nodes):
        return this.activator(reservoir_nodes @ this.weights_output)

    //////////////////////////////////////////////////////////
    ////////// private method ////////////////
    //////////////////////////////////////////////////////////

        // 入力層の重みの初期値を作って返す
        // -0.1 or 0.1の一様分布
        public Matrix _generate_variational_weights(int num_pre_nodes, int num_post_nodes) {
            var rand = new Random();
            return Matrix.buildFromArray(num_pre_nodes, num_post_nodes, Enumerable.Range(0, num_pre_nodes * num_post_nodes).Select(x => x < num_pre_nodes * num_post_nodes / 2 ? -0.1 : +0.1).OrderBy(x => rand.Next()).ToArray());
            //return (np.random.randint(0, 2, num_pre_nodes * num_post_nodes).reshape([num_pre_nodes, num_post_nodes]) * 2 - 1) * 0.1;
        }

    // Reservoir層の重みの初期値を作って返す
    // 平均0, 分散1の正規分布
    def _generate_reservoir_weights(self, num_nodes):
        weights = np.random.normal(0, 1, num_nodes * num_nodes).reshape([num_nodes, num_nodes])
        spectral_radius = max(abs(linalg.eigvals(weights)))
        return weights / spectral_radius

    }


}
