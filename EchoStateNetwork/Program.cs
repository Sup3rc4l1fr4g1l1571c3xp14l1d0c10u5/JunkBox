﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

// http://eeip.t.u-tokyo.ac.jp/gtanaka/siryo/tanaka201902_ieice.pdf

namespace EchoStateNetwork {
    static class Program {
        [STAThread]
        static void Main() {

            Matrix.UnitTest();


            {
                if (true) {
                    var esn = new EchoStateNetwork2(
                        Ni: 1,
                        No: 1,
                        Nr: 500,
                        spectral_radius: 1.2,
                        sparsity: 0.95,
                        leaking_rate: 0.99,
                        teacher_forcing: true
                    );
                    var data = System.IO.File.ReadLines(@"MackeyGlass_t17.txt").Select(double.Parse).ToArray();
                    var y_train = Matrix.buildFromArray(1, 2000, data.Take(2000).ToArray());
                    var x_train = Matrix.buildFromArray(1, 2000, Enumerable.Repeat(1.0, 2000).ToArray());
                    var y_test = Matrix.buildFromArray(1, 2000, data.Skip(2000).Take(2000).ToArray());
                    var x_test = Matrix.buildFromArray(1, 2000, Enumerable.Repeat(1.0, 2000).ToArray());

                    var y_pred1 = esn.train(x_train, y_train, reg: 1.0e-8, discard_: 100);
                    var y_pred2 = esn.predict(x_test);

                    Console.WriteLine($"train RMSE: ${(Vector.norm(new Vector(y_train.ToArray()) - new Vector(y_pred1.ToArray())) / Math.Sqrt(2000))}");
                    Console.WriteLine($"test RMSE:  ${(Vector.norm(new Vector(y_test.ToArray()) - new Vector(y_pred1.ToArray())) / Math.Sqrt(2000))}");

                    System.IO.File.WriteAllLines("data.txt", data.Select(x => x.ToString()));
                    System.IO.File.WriteAllLines("y_train.txt", y_train.ToArray().Select(x => x.ToString()));
                    System.IO.File.WriteAllLines("y_test.txt", y_test.ToArray().Select(x => x.ToString()));
                    System.IO.File.WriteAllLines("y_pred1.txt", y_pred1.ToArray().Select(x => x.ToString()));
                    System.IO.File.WriteAllLines("y_pred2.txt", y_pred2.ToArray().Select(x => x.ToString()));
                }


                {
                    var data = System.IO.File.ReadLines(@"data.txt").Select(double.Parse).ToArray();
                    var y_train = System.IO.File.ReadLines(@"y_train.txt").Select(double.Parse).ToArray();
                    var y_test = System.IO.File.ReadLines(@"y_test.txt").Select(double.Parse).ToArray();
                    var y_pred1 = System.IO.File.ReadLines(@"y_pred1.txt").Select(double.Parse).ToArray();
                    var y_pred2 = System.IO.File.ReadLines(@"y_pred2.txt").Select(double.Parse).ToArray();

                    var form = new PlotForm();
                    form.Plots.Add(new PlotForm.PlotData() { Pen = System.Drawing.Pens.Red, OffsetX = 0, ScaleY = 400, Data = data });
//                    form.Plots.Add(new PlotForm.PlotData() { Pen = System.Drawing.Pens.Blue, OffsetX = 0, ScaleY = 400, Data = y_train });
//                    form.Plots.Add(new PlotForm.PlotData() { Pen = System.Drawing.Pens.Blue, OffsetX = 2000, ScaleY = 400, Data = y_test });
                    form.Plots.Add(new PlotForm.PlotData() { Pen = System.Drawing.Pens.Green, OffsetX = 2000, ScaleY = 400, Data = y_pred1 });
                    form.Plots.Add(new PlotForm.PlotData() { Pen = System.Drawing.Pens.Blue, OffsetX = 2000, ScaleY = 400, Data = y_pred2 });

                    form.Show();
                    Application.Run(form);
                }
            }
            return;
            {
                var data = Step(0, 16, 0.06).Select(x => Math.Sin(x * Math.PI) * 0.9).ToArray();
                var trainResult = EchoStateNetwork.train(Matrix.buildFromArray(data.Length, 1, data), 1, 300, 1);

                // 訓練の結果を取得
                var trained_data = trainResult.get_train_result();

                // 予測結果を取得
                var predicator = trainResult.CreatePredicator();
                var predicted_data = predicator.predict(200);

                var form = new Form();
                var scrollX = 0;
                form.Paint += (s, e) => {
                    if (form.ClientSize.Width <= 0 || form.ClientSize.Height <= 0) {
                        return;
                    }
                    using (var bitmap = new System.Drawing.Bitmap(form.ClientSize.Width, form.ClientSize.Height)) {
                        using (var g = System.Drawing.Graphics.FromImage(bitmap)) {

                            var offsetX = 30 - scrollX;
                            var offsetY = form.ClientSize.Height / 2;

                            g.Clear(System.Drawing.Color.White);
                            // draw x-axis
                            g.DrawLine(System.Drawing.Pens.Gray, 0, offsetY, form.ClientSize.Width, offsetY);

                            // draw y-axis
                            g.DrawLine(System.Drawing.Pens.Gray, offsetX, 0, offsetX, form.ClientSize.Height);

                            // draw trained_data
                            g.DrawLines(System.Drawing.Pens.Blue, trained_data.Select((x, i) => new System.Drawing.PointF((float)(i * 4) + offsetX, ((float)x[0] * -400) + offsetY)).ToArray());

                            // draw predicted_data
                            offsetX += trained_data.Count * 4;
                            g.DrawLines(System.Drawing.Pens.Red, predicted_data.Select((x, i) => new System.Drawing.PointF((float)(i * 4) + offsetX, ((float)x[0] * -400) + offsetY)).ToArray());

                            g.Flush();
                        }
                        e.Graphics.DrawImageUnscaled(bitmap, 0, 0);
                    }
                };
                form.Resize += (s, e) => {
                    form.Invalidate();
                };
                {
                    var isDown = false;
                    var prevX = 0;
                    form.MouseDown += (s, e) => {
                        if (isDown == false) {
                            prevX = e.X;
                            isDown = true;
                        }
                    };
                    form.MouseMove += (s, e) => {
                        if (isDown) {
                            scrollX += (prevX - e.X);
                            if (scrollX < 0) { scrollX = 0; }
                            prevX = e.X;
                            form.Invalidate();
                        }
                    };
                    form.MouseUp += (s, e) => {
                        if (isDown) {
                            isDown = false;
                            form.Invalidate();
                        }
                    };
                }

                form.Show();
                Application.Run(form);
            }
        }

        public static IEnumerable<double> Step(double start_time, double end_time, double dt) {
            var num_time_steps = (int)((end_time - start_time) / dt);
            for (var t = start_time; t < end_time; t += dt) {
                yield return t;
            }
        }
    }

    public class PlotForm : Form {
        public class PlotData {
            public System.Drawing.Pen Pen;
            public double[] Data;
            public double ScaleY;
            public int OffsetX;
        }
        private int scrollX;
        public List<PlotData> Plots { get; }

        public PlotForm() {
            this.scrollX = 0;
            this.Plots = new List<PlotData>();
            this.DoubleBuffered = true;
            this.Paint += (s, e) => {
                if (this.ClientSize.Width <= 0 || this.ClientSize.Height <= 0) {
                    return;
                }
                var g = e.Graphics;

                var offsetX = 30 - scrollX;
                var offsetY = this.ClientSize.Height / 2;

                g.Clear(System.Drawing.Color.White);
                // draw x-axis
                g.DrawLine(System.Drawing.Pens.Gray, 0, offsetY, this.ClientSize.Width, offsetY);

                // draw y-axis
                g.DrawLine(System.Drawing.Pens.Gray, offsetX, 0, offsetX, this.ClientSize.Height);

                foreach (var plot in Plots) {
                    g.DrawLines(plot.Pen, plot.Data.Select((x, i) => new System.Drawing.PointF((float)(i + offsetX + plot.OffsetX), (float)(x * -plot.ScaleY + offsetY))).ToArray());
                }
            };
            this.Resize += (s, e) => {
                this.Invalidate();
            };
            {
                var isDown = false;
                var prevX = 0;
                this.MouseDown += (s, e) => {
                    if (isDown == false) {
                        prevX = e.X;
                        isDown = true;
                    }
                };
                this.MouseMove += (s, e) => {
                    if (isDown) {
                        scrollX += (prevX - e.X);
                        if (scrollX < 0) { scrollX = 0; }
                        prevX = e.X;
                        this.Invalidate();
                    }
                };
                this.MouseUp += (s, e) => {
                    if (isDown) {
                        isDown = false;
                        this.Invalidate();
                    }
                };
            }
        }


    }

    public partial class Matrix : ICloneable {
        /// <summary>
        /// 行の数
        /// </summary>
        public int Row { get; }

        /// <summary>
        /// 列の数
        /// </summary>
        public int Col { get; }

        public double this[int row, int col] {
            get { return this._values[this.at(row, col)]; }
            private set { this._values[this.at(row, col)] = value; }
        }

        /// <summary>
        /// 要素配列
        /// </summary>
        protected double[] _values { get; }

        /// <summary>
        /// 行を列挙
        /// </summary>
        public IEnumerable<double[]> Rows {
            get { for (var i = 0; i < this.Row; i++) { yield return this.getRow(i); } }
        }

        /// <summary>
        /// 列を列挙
        /// </summary>
        public IEnumerable<double[]> Cols {
            get { for (var i = 0; i < this.Col; i++) { yield return this.getCol(i); } }
        }

        /// <summary>
        /// col×row 行列のコンストラクタ
        /// </summary>
        /// <param name="row">行数</param>
        /// <param name="col">列数</param>
        /// <param name="values">要素数が 行数×列数 である配列</param>
        protected Matrix(int row, int col, double[] values) {
            if (row < 0 || col < 0 || values.Length != row * col) {
                throw new Exception("out of index");
            }
            this.Row = row;
            this.Col = col;
            this._values = values;
        }

        /// <summary>
        /// col×row 行列を生成し、m行n列目の要素を gen(m,n) の結果で初期化する。
        /// </summary>
        /// <param name="row">行数</param>
        /// <param name="col">列数</param>
        /// <param name="gen">要素の初期値を算出する関数</param>
        /// <returns></returns>
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

        /// <summary>
        /// col×row 行列を生成し、要素を value で初期化する。
        /// </summary>
        /// <param name="row">行数</param>
        /// <param name="col">列数</param>
        /// <param name="value">要素の初期値</param>
        /// <returns></returns>
        public static Matrix create(int row, int col, double value) {
            var ret = new double[row * col];
            for (var i = 0; i < row * col; i++) {
                ret[i] = value;
            }
            return new Matrix(row, col, ret);
        }

        /// <summary>
        /// 一次元配列 values を col×row 行列 と見なして行列を生成。
        /// </summary>
        /// <param name="row">行数</param>
        /// <param name="col">列数</param>
        /// <param name="values">一次元配列</param>
        /// <returns></returns>
        public static Matrix buildFromArray(int row, int col, double[] values) {
            return new Matrix(row, col, values);
        }

        /// <summary>
        /// 行列の複製を作る
        /// </summary>
        /// <returns></returns>
        public Matrix Clone() {
            return new Matrix(this.Row, this.Col, this._values.ToArray());
        }

        Object ICloneable.Clone() {
            return Clone();
        }
        public enum Order {
            LeftToRight,
            TopToBottom
        }

        /// <summary>
        /// 行列から一次元配列を作る
        /// </summary>
        /// <returns></returns>
        public double[] ToArray(Order order = Order.LeftToRight) {
            if (order == Order.LeftToRight) {
                return this._values.ToArray();
            } else {
                return this.Cols.SelectMany(x => x).ToArray();
            }
        }

        /// <summary>
        /// 文字列表現化する
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            var lines = new List<string>();
            var line = new double[this.Col];
            for (var j = 0; j < this.Row; j++) {
                for (var i = 0; i < this.Col; i++) {
                    line[i] = this._values[this.at(j, i)];
                }
                lines.Add(String.Join(", ", line));
            }
            return $"[{String.Join("; ", lines)}]";
        }

        /// <summary>
        /// (内部用) row行col列を示す配列のインデクス番号を求める
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        private int at(int row, int col) {
            if (row < 0 || this.Row <= row || col < 0 || this.Col <= col) {
                throw new Exception("out of index");
            }
            return this.Col * row + col;
        }

        /// <summary>
        /// 行列のrow行を配列として取得する
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public double[] getRow(int row) {
            if (row < 0 || this.Row <= row) {
                throw new Exception("out of index");
            }
            var ret = new double[this.Col];
            for (var j = 0; j < this.Col; j++) {
                ret[j] = this._values[this.at(row, j)];
            }
            return ret;
        }

        /// <summary>
        /// 行列のrow行を 配列 values で書き換える
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public Matrix setRow(int row, double[] values) {
            if (row < 0 || this.Row <= row) {
                throw new Exception("out of index");
            }
            if (this.Col != values.Length) {
                throw new Exception("column length mismatch");
            }
            for (var j = 0; j < this.Col; j++) {
                this._values[this.at(row, j)] = values[j];
            }
            return this;
        }

        /// <summary>
        /// 行列のcol列を配列として取得する
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public double[] getCol(int col) {
            if (col < 0 || this.Col <= col) {
                throw new Exception("out of index");
            }
            var ret = new double[this.Row];
            for (var j = 0; j < this.Row; j++) {
                ret[j] = this._values[this.at(j, col)];
            }
            return ret;
        }

        /// <summary>
        /// 行列のcol列を 配列 values で書き換える
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public Matrix setCol(int col, double[] values) {
            if (col < 0 || this.Col <= col) {
                throw new Exception("out of index");
            }
            if (this.Row != values.Length) {
                throw new Exception("row length mismatch");
            }
            for (var j = 0; j < this.Row; j++) {
                this._values[this.at(j, col)] = values[j];
            }
            return this;
        }

        /// <summary>
        /// 行列の各要素を関数 selector で射影した新しい行列を作成する
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>
        public Matrix Select(Func<double, double> selector) {
            return new Matrix(this.Row, this.Col, this._values.Select(selector).ToArray());
        }

        /// <summary>
        /// 二つの行列を比較する
        /// </summary>
        /// <param name="m1"></param>
        /// <param name="m2"></param>
        /// <param name="eps">十分に近いと判断するための差の絶対値</param>
        /// <returns></returns>
        public static bool Equal(Matrix m1, Matrix m2, double eps = double.Epsilon) {
            if (m1.Row != m2.Row || m1.Col != m2.Col) {
                return false;
            }
            for (var i = 0; i < m1._values.Length; i++) {
                if (Math.Abs(m1._values[i] - m2._values[i]) >= eps) {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 行列 m1 と 行列 m2 の和を求める
        /// </summary>
        /// <param name="m1"></param>
        /// <param name="m2"></param>
        /// <returns></returns>
        public static Matrix add(Matrix m1, Matrix m2) {
            if (m1.Row != m2.Row || m1.Col != m2.Col) {
                throw new Exception("dimension mismatch");
            }
            var size = m1.Row * m1.Col;
            var ret = new double[size];
            for (var i = 0; i < size; i++) {
                ret[i] = m1._values[i] + m2._values[i];
            }
            return new Matrix(m1.Row, m1.Col, ret);
        }

        /// <summary>
        /// 行列 m1 と 行列 m2 の差を求める
        /// </summary>
        /// <param name="m1"></param>
        /// <param name="m2"></param>
        /// <returns></returns>
        public static Matrix sub(Matrix m1, Matrix m2) {
            if (m1.Row != m2.Row || m1.Col != m2.Col) {
                throw new Exception("dimension mismatch");
            }
            var size = m1.Row * m1.Col;
            var ret = new double[size];
            for (var i = 0; i < size; i++) {
                ret[i] = m1._values[i] - m2._values[i];
            }
            return new Matrix(m1.Row, m1.Col, ret);
        }

        /// <summary>
        /// 行列 m の スカラー s 倍を求める
        /// </summary>
        /// <param name="m1"></param>
        /// <param name="m2"></param>
        /// <returns></returns>
        public static Matrix scalarMultiplication(Matrix m, double s) {
            return m.Select(x => x * s);
        }

        /// <summary>
        /// 行列 m1 と 行列 m2 の ドット積 を求める
        /// </summary>
        /// <param name="m1"></param>
        /// <param name="m2"></param>
        /// <returns></returns>
        public static Matrix dotProduct(Matrix m1, Matrix m2) {
            if (m1.Col != m2.Row) {
                throw new Exception("dimension mismatch");
            }
            var ret = new double[m1.Row * m2.Col];
            var n = 0;
            var dp = 1;
            var dq = m2.Col;
            for (var i = 0; i < m1.Row; i++) {
                for (var j = 0; j < m2.Col; j++) {
                    var sum = 0.0;
                    var p = m1.at(i, 0);
                    var q = m2.at(0, j);
                    for (var k = 0; k < m1.Col; k++) {
                        sum += m1._values[p] * m2._values[q];
                        p += dp;
                        q += dq;
                    }
                    ret[n++] = sum;
                }
            }
            return new Matrix(m1.Row, m2.Col, ret);
        }

        /// <summary>
        /// 行列 m1 と 行列 m2 のアダマール積を求める。
        /// （アダマール積：同じサイズの行列に対して成分ごとに積を取ることによって定まる行列の積。要素ごとの積、シューア積、点ごとの積などともいう）
        /// </summary>
        /// <param name="m1"></param>
        /// <param name="m2"></param>
        /// <returns></returns>
        public static Matrix hadamardProduct(Matrix m1, Matrix m2) {
            if (m1.Row != m2.Row || m1.Col != m2.Col) {
                throw new Exception("dimension mismatch");
            }
            var size = m1.Row * m1.Col;
            var ret = new double[size];
            for (var i = 0; i < size; i++) {
                ret[i] = m1._values[i] * m2._values[i];
            }
            return new Matrix(m1.Row, m1.Col, ret);
        }

        /// <summary>
        /// 行列 m の転置行列を求める
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public static Matrix transpose(Matrix m) {
            var size = m._values.Length;
            var ret = new double[size];
            var p = 0; var dq = m.Col;
            for (var j = 0; j < m.Col; j++) {
                var q = m.at(0, j);
                for (var i = 0; i < m.Row; i++) {
                    ret[p] = m._values[q];
                    p += 1;
                    q += dq;
                }
            }
            return new Matrix(m.Col, m.Row, ret);
        }

        #region 演算子の定義

        public static Matrix operator +(Matrix rhs, Matrix lhs) => add(rhs, lhs);
        public static Matrix operator +(double[] rhs, Matrix lhs) => add(Matrix.buildFromArray(1, rhs.Length, rhs), lhs);
        public static Matrix operator +(Matrix rhs, double[] lhs) => add(rhs, Matrix.buildFromArray(1, lhs.Length, lhs));

        public static Matrix operator -(Matrix rhs, Matrix lhs) => sub(rhs, lhs);
        public static Matrix operator -(Matrix rhs, double lhs) => rhs.Select(x => x - lhs);

        public static Matrix operator *(Matrix rhs, double lhs) => scalarMultiplication(rhs, lhs);
        public static Matrix operator *(double rhs, Matrix lhs) => scalarMultiplication(lhs, rhs);

        public static Matrix operator *(Matrix rhs, Matrix lhs) => dotProduct(rhs, lhs);
        public static Matrix operator *(double[] rhs, Matrix lhs) => dotProduct(Matrix.buildFromArray(1, rhs.Length, rhs), lhs);
        public static Matrix operator *(Matrix rhs, double[] lhs) => dotProduct(rhs, Matrix.buildFromArray(1, lhs.Length, lhs));

        public static Matrix operator /(Matrix rhs, double lhs) => rhs.Select(x => x / lhs);

        #endregion

        /// <summary>
        /// ベクトル x を単位ベクトルに正規化する
        /// </summary>
        /// <param name="x"></param>
        private static void normarize(double[] x) {
            double s = 0.0;
            for (int i = 0; i < x.Length; i++) { s += x[i] * x[i]; }
            s = Math.Sqrt(s);
            for (int i = 0; i < x.Length; i++) { x[i] /= s; }
        }

        /// <summary>
        /// 行列の絶対値最大な固有値をベキ乗法で求める
        /// max(abs(numpy.linalg.eigvals(weights)))に相当
        /// （numpy.linalg.eigvalsはQR法を用いて固有値を求める）
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        public static double AbsMaxEigenValue(Matrix matrix) {
            if (matrix.Col != matrix.Row) {
                throw new Exception("not square matrix");
            }

            // 固有ベクトルを初期値に設定
            double[] x0 = new double[matrix.Col];
            x0[0] = 1;

            // 固有値
            double lambda = 0.0;

            // 正規化 (ベクトル x0 の長さを１にする)
            normarize(x0);
            double e0 = 0.0;
            for (int i = 0; i < matrix.Col; i++) {
                e0 += x0[i];
            }

            for (int k = 1; k <= 200; k++) {

                // 行列の積 x1 = A × x0 
                double[] x1 = new double[matrix.Col];
                for (int i = 0; i < matrix.Col; i++) {
                    for (int j = 0; j < matrix.Col; j++) {
                        x1[i] += matrix._values[matrix.at(i, j)] * x0[j];
                    }
                }

                // 内積
                double p0 = 0.0;
                double p1 = 0.0;
                for (int i = 0; i < matrix.Col; i++) {
                    p0 += x1[i] * x1[i];
                    p1 += x1[i] * x0[i];
                }

                // 固有値
                lambda = p0 / p1;

                // 正規化 (ベクトル x1 の長さを１にする)
                normarize(x1);

                // 収束判定
                double e1 = 0.0;
                for (int i = 0; i < matrix.Col; i++) { e1 += x1[i]; }
                if (Math.Abs(e0 - e1) < 0.00000000001) break;

                for (int i = 0; i < matrix.Col; i++) {
                    x0[i] = x1[i];
                }
                e0 = e1;
            }
            return lambda;
        }

        /// <summary>
        /// 固有値をQR法で求める
        /// numpy.linalg.eigvals(weights)に相当
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        public static double[] EigenValues(Matrix A, int MAX = 100, double TOL = 1e-10) {
            for (var j = 0; j < MAX; j++) {
                // 下三角要素の絶対値のうち最大値とその行、列を求める
                var tmp1 = Matrix.createTriangle(A.Row, k: -1);
                var tmp2 = Matrix.hadamardProduct(A, tmp1);
                var absval1 = tmp2.Select(Math.Abs).ToArray();
                var amax1 = absval1.Max();

                // 最大値が収束条件未満なら終了
                if (amax1 < TOL) { break; }

                // Rを順次求めていく
                var Q = Matrix.createIdentity(A.Row);
                var R = A.Clone();
                for (var i = 0; i < R.Row * R.Col; i++) {
                    // Rの下三角要素の絶対値のうち最大値とその行、列を求める
                    var absval2 = Matrix.hadamardProduct(R, Matrix.createTriangle(R.Row, k: -1)).Select(Math.Abs).ToArray();
                    var amax2 = absval2.Max();
                    // 最大値が収束条件未満なら終了
                    if (amax2 < TOL) { break; }

                    var aindex = absval2.Select((x, z) => Tuple.Create(x, z)).OrderByDescending(x => x.Item1).First().Item2;

                    // 最大値の行と列
                    var r = (int)(aindex / R.Row);
                    var c = (int)(aindex % R.Row);

                    // ギブンス回転行列の角度を計算
                    var theta = Math.Atan2(R[r, c], R[c, c]);

                    // ギブンス回転行列を作って直交行列Pを更新する
                    var G = Matrix.createIdentity(R.Row);
                    G[r, r] = Math.Cos(theta);
                    G[r, c] = -Math.Sin(theta);
                    G[c, r] = Math.Sin(theta);
                    G[c, c] = Math.Cos(theta);
                    // QとRを更新する
                    R = Matrix.dotProduct(G, R);
                    Q = Matrix.dotProduct(Q, Matrix.transpose(G));
                }
                // Aを更新する
                A = Matrix.dotProduct(Matrix.dotProduct(Matrix.transpose(Q), A), Q);

            }
            return Matrix.diag(A);
        }

        /// <summary>
        /// 対角要素を取り出す
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        private static double[] diag(Matrix a) {
            var ret = new double[a.Row];
            for (var i = 0; i < a.Row; i++) {
                ret[i] = a[i, i];
            }
            return ret;
        }


        /// <summary>
        /// col×row のゼロ行列を生成
        /// </summary>
        /// <param name="row">行数</param>
        /// <param name="col">列数</param>
        /// <returns></returns>
        public static Matrix createZero(int row, int col) {
            return Matrix.create(row, col, 0.0);
        }

        /// <summary>
        /// n×n の単位行列を生成
        /// </summary>
        /// <param name="n">単位行列のサイズ</param>
        /// <returns></returns>
        public static Matrix createIdentity(int n) {
            return Matrix.create(n, n, (x, y) => x == y ? 1.0 : 0.0);
        }

        /// <summary>
        /// n×n の三角行列(下三角成分が1、上三角成分が0)を作成
        /// </summary>
        /// <param name="n">単位行列のサイズ</param>
        /// <param name="k">対角要素のオフセット位置。k>0の場合は上に、k<0の場合は下にずれる</param>
        /// <returns></returns>
        public static Matrix createTriangle(int n, int k = 0) {
            return Matrix.create(n, n, (row, col) => (row + k >= col) ? 1.0 : 0.0);
        }

        /// <summary>
        /// 行列 m の逆行列を求める。逆行列が存在しない場合は null を返す。
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public static Matrix inverse(Matrix m) {

            int col = m.Col;
            int row = m.Row;

            if (col != row) {
                throw new Exception("It is not a square matrix");
            }

            var invA = Matrix.createIdentity(col);

            for (int k = 0; k < col; k++) {
                var max = k;
                for (int j = k + 1; j < col; j++) {
                    if (Math.Abs(m._values[m.at(j, k)]) > Math.Abs(m._values[m.at(max, k)])) {
                        max = j;
                    }
                }

                if (max != k) {
                    for (int i = 0; i < col; i++) {
                        var ki = m.at(k, i);
                        var maxi = m.at(max, i);
                        // 入力行列側
                        var tmp = m._values[maxi];
                        m._values[maxi] = m._values[ki];
                        m._values[ki] = tmp;
                        // 単位行列側
                        tmp = invA._values[maxi];
                        invA._values[maxi] = invA._values[ki];
                        invA._values[ki] = tmp;
                    }
                }

                {
                    var tmp = m._values[m.at(k, k)];
                    for (int i = 0; i < col; i++) {
                        var ki = m.at(k, i);
                        m._values[ki] /= tmp;
                        invA._values[ki] /= tmp;
                    }
                }

                for (int j = 0; j < col; j++) {
                    if (j != k) {
                        var tmp = m._values[m.at(j, k)] / m._values[m.at(k, k)];
                        for (int i = 0; i < col; i++) {
                            var ji = m.at(j, i);
                            var ki = m.at(k, i);
                            m._values[ji] -= m._values[ki] * tmp;
                            invA._values[ji] -= invA._values[ki] * tmp;
                        }
                    }
                }
            }

            // 逆行列が計算できなかったかチェック
            if (invA._values.Any(double.IsNaN)) {
                return null;
            }

            return invA;
        }


        /// <summary>
        /// 行列 m を order で示される方向優先でスキャンした結果を元に row行 col列 の行列を生成
        /// </summary>
        /// <param name="m"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <param name="order">変換順序</param>
        /// <returns></returns>
        public static Matrix Reshape(Matrix m, int row, int col, Order order = Order.LeftToRight) {
            if (m.Row * m.Col != row * col) {
                throw new Exception();
            }
            return Matrix.buildFromArray(row, col, m.ToArray(order));
        }

        /// <summary>
        /// 行列 m の 半開区間 [startColumn, endColumn) で示される列を行列として切り出す
        /// </summary>
        /// <param name="m"></param>
        /// <param name="startColumn"></param>
        /// <param name="endColumn"></param>
        /// <returns></returns>
        public static Matrix SliceColumn(Matrix m, int startColumn, int endColumn) {
            var newRow = m.Row;
            var newCol = endColumn - startColumn;
            var newArray = m.Rows.SelectMany(x => x.Skip(startColumn).Take(newCol)).ToArray();
            return Matrix.buildFromArray(newRow, newCol, newArray);
        }

        /// <summary>
        /// 行列 m の 半開区間 [startRow, endRow) で示される行を行列として切り出す
        /// </summary>
        /// <param name="m"></param>
        /// <param name="startRow"></param>
        /// <param name="endRow"></param>
        /// <returns></returns>
        public static Matrix SliceRow(Matrix m, int startRow, int endRow) {
            var newRow = endRow - startRow;
            var newCol = m.Col;
            var newArray = Enumerable.Range(startRow, endRow - startRow).SelectMany(x => m.getRow(x)).ToArray();
            return Matrix.buildFromArray(newRow, newCol, newArray);
        }

        /// <summary>
        /// 行列 m に配列 vs を横方向に連結する
        /// matrix [[1,2,3],[4,5,6]] に hcat([7,8,9,10]) すると [[1,2,3,7,9],[4,5,6,8,10]]になる。
        /// </summary>
        /// <param name="m"></param>
        /// <param name="vs"></param>
        /// <returns></returns>
        public static Matrix appendColumn(Matrix m, double[] vs) {
            if ((vs.Length % m.Row) != 0) {
                throw new Exception("Length missmatch");
            }
            var incCol = vs.Length / m.Row;
            var newRow = m.Row;
            var newCol = m.Col + incCol;
            var newArray = new double[newRow * newCol];

            for (var row = 0; row < m.Row; row++) {
                for (var col = 0; col < m.Col; col++) {
                    newArray[row * newCol + col] = m._values[row * m.Col + col];
                }
                for (var i = 0; i < incCol; i++) {
                    newArray[row * newCol + m.Col + i] = vs[i * m.Row + row];
                }
            }

            return Matrix.buildFromArray(m.Row, newCol, newArray);
        }

    }

    public partial class Matrix {
        /// <summary>
        /// ユニットテスト
        /// </summary>
        /// <returns></returns>
        public static bool UnitTest() {
            Func<double[], double[], bool> arrayEqual = (x, y) => x.Length == y.Length && x.SequenceEqual(y);

            // hadamardProduct test
            {
                var m1 = Matrix.buildFromArray(3, 2, new double[] { 2, 4, 6, 8, 10, 12 });
                var m2 = Matrix.buildFromArray(3, 2, new double[] { 3, 5, 7, 9, 11, 13 });
                var m3 = Matrix.hadamardProduct(m1, m2);
                var m4 = Matrix.buildFromArray(3, 2, new double[] { 6, 20, 42, 72, 110, 156 });
                if (!Matrix.Equal(m3, m4)) { return false; }
                Console.WriteLine("hadamardProduct: ok");
            }

            // scalarMultiplication test
            {
                var m1 = Matrix.buildFromArray(3, 2, new double[] { 2, 4, 6, 8, 10, 12 });
                var m2 = Matrix.scalarMultiplication(m1, -4);
                var m3 = Matrix.buildFromArray(3, 2, new double[] { 2, 4, 6, 8, 10, 12 }.Select(x => x * -4).ToArray());
                if (!Matrix.Equal(m2, m3)) { return false; }
                Console.WriteLine("scalarMultiplication: ok");
            }

            // dotProduct test
            {
                var m1 = Matrix.buildFromArray(3, 2, new double[] { 2, 4, 6, 8, 10, 12 });
                var m2 = Matrix.buildFromArray(2, 3, new double[] { 3, 5, 7, 13, 11, 9 });
                var m3 = Matrix.dotProduct(m1, m2);
                var m4 = Matrix.buildFromArray(3, 3, new double[] { 58, 54, 50, 122, 118, 114, 186, 182, 178 });
                if (!Matrix.Equal(m3, m4)) { return false; }
                Console.WriteLine("dotProduct: ok");
            }

            // transpose test
            {
                var m1 = Matrix.buildFromArray(3, 2, new double[] { 2, 4, 6, 8, 10, 12 });
                var m2 = Matrix.transpose(m1);
                var m3 = Matrix.buildFromArray(2, 3, new double[] { 2, 6, 10, 4, 8, 12 });
                if (!Matrix.Equal(m2, m3)) { return false; }
                Console.WriteLine("transpose: ok");
            }

            // toString test
            {
                var s1 = Matrix.buildFromArray(3, 2, new double[] { 2, 4, 6, 8, 10, 12 }).ToString();
                var s2 = "[2, 4; 6, 8; 10, 12]";
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
                if (!arrayEqual(m1.ToArray(), new double[] { 7, 8, 9, 10, 11, 12 })) { return false; }
                Console.WriteLine("setRow: ok");
            }

            // setCol test
            {
                var m1 = Matrix.buildFromArray(2, 3, new double[] { 1, 2, 3, 4, 5, 6 });
                m1.setCol(0, new double[] { 7, 8 });
                m1.setCol(1, new double[] { 9, 10 });
                m1.setCol(2, new double[] { 11, 12 });
                if (!arrayEqual(m1.ToArray(), new double[] { 7, 9, 11, 8, 10, 12 })) { return false; }
                Console.WriteLine("setCol: ok");
            }

            // normarize test
            {
                var v1 = new double[] { 1, 2, 3, 2, 1 };
                var v1_a = new double[] { 0.22941573, 0.45883147, 0.6882472, 0.45883147, 0.22941573 };
                normarize(v1);
                if (v1.Zip(v1_a, (x, y) => Math.Abs(x - y)).Any(x => x >= 0.00000001)) { return false; }
                Console.WriteLine("normarize: ok");
            }

            // AbsMaxEigenValue test
            {
                var m1 = Matrix.buildFromArray(4, 4, new double[] { 5.0, 4.0, 1.0, 1.0,
                                                                    4.0, 5.0, 1.0, 1.0,
                                                                    1.0, 1.0, 4.0, 2.0,
                                                                    1.0, 1.0, 2.0, 4.0 });
                var a1 = Matrix.AbsMaxEigenValue(m1);
                if (Math.Abs(a1 - 10.0) >= 0.00000001) { return false; }

                var m2 = Matrix.buildFromArray(3, 3, new double[] { 6, 4, 1,
                                                                    1, 8, -2,
                                                                    3, 2, 0 });
                var a2 = Matrix.AbsMaxEigenValue(m2);
                if (Math.Abs(a2 - 8.13881715) >= 0.00000001) { return false; }

                Console.WriteLine("AbsMaxEigenValue: ok");
            }

            // EigenValues test
            {
                //var m1 = Matrix.buildFromArray(4, 4, new double[] { 16, -1, 1, 2, 2, 12, 1, -1, 1, 3, -24, 2, 4, -2, 1, 20 });
                //var a1 = new double[] { -24.148520575017, 21.579344996529, 14.338219152324, 12.230956426163 };
                //var a2 = Matrix.EigenValues(m1);
                var m1 = Matrix.buildFromArray(3, 3, new double[] { 6, 4, 1, 1, 8, -2, 3, 2, 0 });
                var a1 = new double[] { 8.13881715, 6.29086844, -0.42968559 };
                var a2 = Matrix.EigenValues(m1);
                if (a1.Zip(a2, (x, y) => Math.Abs(x - y)).Any(x => x >= 0.00000001)) { return false; }

                Console.WriteLine("EigenValues: ok");
            }
            // inverse
            {
                var m1 = Matrix.buildFromArray(4, 4, new double[] { 3, 1, 1, 2, 5, 1, 3, 4, 2, 0, 1, 0, 1, 3, 2, 1 });
                var m2 = Matrix.inverse(m1);
                var m3 = Matrix.buildFromArray(4, 4, new double[] { 0.5, -0.22727272727273, 0.36363636363636, -0.090909090909091, 0.5, -0.31818181818182, -0.090909090909091, 0.27272727272727, -1, 0.45454545454546, 0.27272727272727, 0.18181818181818, 0, 0.27272727272727, -0.63636363636364, -0.090909090909091 });
                if (m2.ToArray().Zip(m3.ToArray(), (x, y) => Math.Abs(x - y)).Any(x => x >= 0.00000001)) { return false; }
                Console.WriteLine("inverse: ok");

            }

            // SliceColumn
            {
                {
                    var m1 = Matrix.buildFromArray(4, 4, new double[] { 3, 1, 1, 2, 5, 1, 3, 4, 2, 0, 1, 0, 1, 3, 2, 1 });
                    var m2 = Matrix.SliceColumn(m1, 0, 2);
                    var m3 = Matrix.buildFromArray(4, 2, new double[] { 3, 1, 5, 1, 2, 0, 1, 3, });
                    if (m2.ToArray().Zip(m3.ToArray(), (x, y) => Math.Abs(x - y)).Any(x => x >= 0.00000001)) { return false; }
                }
                {
                    var m1 = Matrix.buildFromArray(4, 4, new double[] { 3, 1, 1, 2, 5, 1, 3, 4, 2, 0, 1, 0, 1, 3, 2, 1 });
                    var m2 = Matrix.SliceColumn(m1, 1, 4);
                    var m3 = Matrix.buildFromArray(4, 3, new double[] { 1, 1, 2, 1, 3, 4, 0, 1, 0, 3, 2, 1 });
                    if (m2.ToArray().Zip(m3.ToArray(), (x, y) => Math.Abs(x - y)).Any(x => x >= 0.00000001)) { return false; }
                }
                {
                    var m1 = Matrix.buildFromArray(4, 4, new double[] { 3, 1, 1, 2, 5, 1, 3, 4, 2, 0, 1, 0, 1, 3, 2, 1 });
                    var m2 = Matrix.SliceColumn(m1, 1, 3);
                    var m3 = Matrix.buildFromArray(4, 2, new double[] { 1, 1, 1, 3, 0, 1, 3, 2 });
                    if (m2.ToArray().Zip(m3.ToArray(), (x, y) => Math.Abs(x - y)).Any(x => x >= 0.00000001)) { return false; }
                }
                Console.WriteLine("SliceColumn: ok");
            }

            // hcat
            {
                {
                    var m1 = Matrix.buildFromArray(4, 2, new double[] { 3, 1, 5, 1, 2, 0, 1, 3, });
                    var a1 = new double[] { 1, 3, 1, 2 };
                    var m2 = Matrix.appendColumn(m1, a1);
                    var m3 = Matrix.buildFromArray(4, 3, new double[] { 3, 1, 1, 5, 1, 3, 2, 0, 1, 1, 3, 2, });
                    if (m2.ToArray().Zip(m3.ToArray(), (x, y) => Math.Abs(x - y)).Any(x => x >= 0.00000001)) { return false; }
                }
                {
                    var m1 = Matrix.buildFromArray(4, 2, new double[] { 3, 1, 5, 1, 2, 0, 1, 3, });
                    var a1 = new double[] { 1, 3, 1, 2, 2, 4, 0, 1 };
                    var m2 = Matrix.appendColumn(m1, a1);
                    var m3 = Matrix.buildFromArray(4, 4, new double[] { 3, 1, 1, 2, 5, 1, 3, 4, 2, 0, 1, 0, 1, 3, 2, 1 });
                    if (m2.ToArray().Zip(m3.ToArray(), (x, y) => Math.Abs(x - y)).Any(x => x >= 0.00000001)) { return false; }
                }
                {
                    var m1 = Matrix.buildFromArray(4, 4, new double[] { 3, 1, 1, 2, 5, 1, 3, 4, 2, 0, 1, 0, 1, 3, 2, 1 });
                    var a1 = new double[] { };
                    var m2 = Matrix.appendColumn(m1, a1);
                    var m3 = Matrix.buildFromArray(4, 4, new double[] { 3, 1, 1, 2, 5, 1, 3, 4, 2, 0, 1, 0, 1, 3, 2, 1 });
                    if (m2.ToArray().Zip(m3.ToArray(), (x, y) => Math.Abs(x - y)).Any(x => x >= 0.00000001)) { return false; }
                }
                Console.WriteLine("hcat: ok");
            }


            return true;
        }
    }

    // vector [1,2,3] は matrix[[1],[2],[3]]相当
    public class Vector {
        private double[] _values { get; }

        public Vector(double[] values) {
            this._values = values;
        }

        public Vector(int num) {
            this._values = new double[num];
        }

        public static Vector Concat(double[] a, Vector v) {
            return new Vector(a.Concat(v._values).ToArray());
        }
        public new Vector map(Func<double, double> func) {
            return new Vector(this._values.Select(func).ToArray());
        }

        public static Vector create(int len, Func<int, double> func) {
            return new Vector(Enumerable.Range(0, len).Select(func).ToArray());
        }

        public static Vector add(Vector lhs, Vector rhs) {
            if (lhs._values.Length != rhs._values.Length) {
                throw new Exception();
            }
            return new Vector(lhs._values.Zip(rhs._values, (l, r) => l + r).ToArray());
        }

        public static Vector sub(Vector lhs, Vector rhs) {
            if (lhs._values.Length != rhs._values.Length) {
                throw new Exception();
            }
            return new Vector(lhs._values.Zip(rhs._values, (l, r) => l - r).ToArray());
        }

        public static Vector scalerMultiply(double lhs, Vector rhs) {
            return rhs.map(x => lhs * x);
        }

        public static Vector scalerMultiply(Vector lhs, double rhs) {
            return lhs.map(x => x * rhs);
        }

        public static Vector multiply(Matrix lhs, Vector rhs) {
            if (lhs.Col != rhs._values.Length) {
                throw new Exception();
            }
            var ret = Enumerable.Range(0, lhs.Row).Select(i => lhs.getRow(i).Zip(rhs._values, (x, y) => x * y).Sum()).ToArray();
            return new Vector(ret);
        }

        public static Vector operator +(Vector lhs, Vector rhs) => add(lhs, rhs);
        public static Vector operator -(Vector lhs, Vector rhs) => sub(lhs, rhs);
        public static Vector operator *(double lhs, Vector rhs) => scalerMultiply(lhs, rhs);
        public static Vector operator *(Vector lhs, double rhs) => scalerMultiply(lhs, rhs);
        public static Vector operator *(Matrix lhs, Vector rhs) => multiply(lhs, rhs);

        public double[] ToArray() {
            return this._values.ToArray();
        }

        public static double norm(Vector v) {
            return Math.Sqrt(v._values.Select(x => x * x).Sum());
        }
    }

    public class EchoStateNetwork {
        //private Matrix inputs;
        //private Matrix log_reservoir_nodes;
        //private Matrix weights_input;
        //private Matrix weights_reservoir;
        //private Matrix weights_output;

        //private int num_input_nodes;
        //private int num_reservoir_nodes;
        //private int num_output_nodes;
        //private double leak_rate;
        //private Func<double[], double[]> activator;

        //private EchoStateNetwork(Matrix inputs, Matrix log_reservoir_nodes, int num_input_nodes, int num_reservoir_nodes, int num_output_nodes, double leak_rate = 0.1, Func<double[], double[]> activator = null) {
        //    activator = activator ?? (xs => xs.Select(Math.Tanh).ToArray());

        //    this.inputs = inputs; // u: 時刻0から時刻N-1までの入力ベクトル列(Vector<double>[N]型だと思っておけばいい)
        //    this.log_reservoir_nodes = log_reservoir_nodes; // 時刻0から時刻N-1までのreservoir層の状態

        //    // 各層の重みを初期化する
        //    this.weights_input = _generate_variational_weights(num_input_nodes, num_reservoir_nodes); // W_in: 入力層の重み
        //    this.weights_reservoir = _generate_reservoir_weights(num_reservoir_nodes); // W_res: Reservoir層の重み
        //    this.weights_output = Matrix.create(num_reservoir_nodes, num_output_nodes, (r, c) => 0.0); // W_out: 出力層の重み

        //    // それぞれの層のノードの数
        //    this.num_input_nodes = num_input_nodes; // K: 入力層のノード数
        //    this.num_reservoir_nodes = num_reservoir_nodes; // N: Reservoir層のノード数
        //    this.num_output_nodes = num_output_nodes; // K: 出力層のノード数

        //    this.leak_rate = leak_rate; // δ: 漏れ率
        //    this.activator = activator; // f: 活性化関数
        //}

        /// <summary>
        /// 時刻 t-1 の時の入力、状態、重みを元に、時刻 t の時の状態を求める
        /// （x(t) = f((1−δ) * x(t−1) + δ(W_in * u(t−1) + W_res * x(t−1)) を求めている）
        /// </summary>
        /// <param name="input">時刻 t-1 における入力値</param>
        /// <param name="current_state">x(t−1) : 時刻 t−1 におけるReservoir層の状態</param>
        /// <param name="weights_input">W_in: 入力層の重み</param>
        /// <param name="weights_reservoir">W_res: Reservoir層の重み</param>
        /// <param name="leak_rate">δ: 漏れ率</param>
        /// <param name="activator">f: 活性化関数</param>
        /// <returns>x(t): 時刻 t におけるReservoir層の状態</returns>
        private static double[] _get_next_reservoir_nodes(double[] input, double[] current_state, Matrix weights_input, Matrix weights_reservoir, double leak_rate, Func<double[], double[]> activator) {
            // this.leak_rate = δ
            // current_state = x(t−1) 
            // x(t−1): 時刻 t−1 までの関数の状態
            var temp = current_state.Select(x => (1.0 - leak_rate) * x).ToArray();

            // this.leak_rate = δ
            // np.array([input]) = u(t-1) 
            // u(t-1): 時刻 t-1 の 力入
            // this.weights_input = W_in
            // current_state = x(t−1)
            // this.weights_reservoir = W_res
            var next_state = temp + leak_rate * (input * weights_input + current_state * weights_reservoir);

            // this.activator = f 
            return activator(next_state.ToArray());
        }


        /// <summary>
        /// 出力層の重みを算出
        /// 最小二乗推定(二乗和誤差を最小にする係数βを求める)の式: β=(X.T * X)^−1 * X.T * y
        ///   -1 (逆行列導出)
        ///   X.T: 行列 X の転置行列
        /// リッジ回帰を入れる: β=(X.T * X + k * I)^−1 * X.T * y
        ///   k: リッジパラメーター(小さな正の値)
        ///   I: 単位行列
        /// より
        ///   Y_target: 教師データ
        ///   λ: リッジパラメータk
        ///   W_out = (X.T * X + λ * I)^−1 * X.T * Y_target
        /// </summary>
        /// <param name="inputs">Y_target:時刻 0 から時刻 t-1 までの教師データ</param>
        /// <param name="num_reservoir_nodes">N: Reservoir層のノード数</param>
        /// <param name="log_reservoir_nodes">X: 時刻 0 から時刻 t-1 までの reservoir層 の状態</param>
        /// <param name="lambda0">λ: リッジパラメータ k </param>
        /// <returns>W_out: 出力層の重み(最小二乗推定結果であり、二乗和誤差を最小にする係数β)</returns>
        private static Matrix _update_weights_output(Matrix inputs, int num_reservoir_nodes, Matrix log_reservoir_nodes, double lambda0) {
            // リッジ回帰部分式 k * I を求める
            // np.identity(this.num_reservoir_nodes) = I
            // lambda0 = λ
            // E_lambda0 = λ * I = k * I
            var E_lambda0 = Matrix.createIdentity(num_reservoir_nodes) * lambda0;

            // リッジ回帰を含めた (X.T * X + k * I)^−1  を求める
            // this.log_reservoir_nodes.T = X.T
            // this.log_reservoir_nodes = X
            // @ 演算子 = 行列積 (※なぜこれを注意書きしているかというと、numpyでは二項演算子 * を行列に適用するとアダマール積になるから。）
            // np.linalg.inv: ^-1 (逆行列導出)
            var inv_x = Matrix.inverse(Matrix.transpose(log_reservoir_nodes) * log_reservoir_nodes + E_lambda0);

            // 二乗和誤差を最小にする係数βを求める
            var weights_output = (inv_x * Matrix.transpose(log_reservoir_nodes)) * inputs;

            return weights_output;
        }

        public class TrainResult {
            private Func<double[], double[]> activator { get; }
            private Matrix inputs { get; }
            private double leak_rate { get; }
            private Matrix log_reservoir_nodes { get; }
            private Matrix weights_input { get; }
            private Matrix weights_output { get; }
            private Matrix weights_reservoir { get; }
            public TrainResult(
            Matrix inputs,
            Matrix log_reservoir_nodes,
            Matrix weights_input,
            Matrix weights_output,
            Matrix weights_reservoir,
            double leak_rate,
            Func<double[], double[]> activator

                ) {
                this.inputs = inputs;
                this.log_reservoir_nodes = log_reservoir_nodes;
                this.weights_input = weights_input;
                this.weights_output = weights_output;
                this.weights_reservoir = weights_reservoir;
                this.leak_rate = leak_rate;
                this.activator = activator;

            }
            // 学習で得られた重みを基に訓練データを学習できているかを出力
            public List<double[]> get_train_result() {
                var outputs = new List<double[]>();
                var num_reservoir_nodes = log_reservoir_nodes.Col;
                // ゼロ初期化されたreservoir層の内部状態 reservoir_nodesを作る
                var reservoir_nodes = Enumerable.Repeat(0.0, num_reservoir_nodes).ToArray();

                // 時刻ごとの入力一つ毎に時系列で処理する
                for (var i = 0; i < inputs.Row; i++) {
                    var input = inputs.getRow(i);
                    // 入力ベクトル input と reservoir層の内部状態 reservoir_nodes から 次の時刻における reservoir 層の内部状態 reservoir_nodes を求める
                    reservoir_nodes = _get_next_reservoir_nodes(input, reservoir_nodes, this.weights_input, this.weights_reservoir, this.leak_rate, this.activator);
                    // 求めた reservoir_nodes を元にした出力を生成して解のリストに追加
                    outputs.Add(this.get_output(reservoir_nodes));
                }
                return outputs;
            }

            // 時刻 t における reservoir層の内部状態 reservoir_nodes と 出力層の重み this.weights_output の行列積結果に
            // 活性化関数 this.activator を適用することで時刻 t における出力値を得る
            private double[] get_output(double[] reservoir_nodes) {
                return this.activator((reservoir_nodes * this.weights_output).ToArray());
            }

            public Predicator CreatePredicator() {
                return new Predicator(
                    lastInput: this.inputs.getRow(this.inputs.Row - 1),
                    lastLogReservoirNode: this.log_reservoir_nodes.getRow(this.log_reservoir_nodes.Row - 1),
                    weights_input: this.weights_input.Clone(),
                    weights_reservoir: this.weights_reservoir.Clone(),
                    weights_output: this.weights_output.Clone(),
                    leak_rate: this.leak_rate,
                    activator: this.activator
                );
            }
        }

        public class Predicator {
            private Func<double[], double[]> activator { get; }
            private double[] lastInput { get; }
            private double[] lastLogReservoirNode { get; }
            private double leak_rate { get; }
            private Matrix weights_input { get; }
            private Matrix weights_reservoir { get; }
            private Matrix weights_output { get; }

            public Predicator(double[] lastInput, double[] lastLogReservoirNode, Matrix weights_input, Matrix weights_reservoir, Matrix weights_output, double leak_rate, Func<double[], double[]> activator) {
                this.lastInput = lastInput;
                this.lastLogReservoirNode = lastLogReservoirNode;
                this.weights_input = weights_input;
                this.weights_reservoir = weights_reservoir;
                this.weights_output = weights_output;
                this.leak_rate = leak_rate;
                this.activator = activator;
            }

            // 予測する
            public List<double[]> predict(int length_of_sequence) {
                var predicted_outputs = new List<double[]>() { lastInput }; // 学習データの最後のデータを予測開始時の入力とする
                var reservoir_nodes = lastLogReservoirNode; // 訓練の結果得た最後の内部状態を取得
                                                            // 時間の進行を元に予測を進める
                for (var i = 0; i < length_of_sequence; i++) {
                    reservoir_nodes = _get_next_reservoir_nodes(predicted_outputs.Last(), reservoir_nodes, this.weights_input, this.weights_reservoir, this.leak_rate, this.activator);
                    predicted_outputs.Add(this.get_output(reservoir_nodes));
                }
                // 最初に使用した学習データの最後のデータを外して返す
                predicted_outputs.RemoveAt(0);
                return predicted_outputs;
            }

            // 時刻 t における reservoir層の内部状態 reservoir_nodes と 出力層の重み this.weights_output の行列積結果に
            // 活性化関数 this.activator を適用することで時刻 t における出力値を得る
            private double[] get_output(double[] reservoir_nodes) {
                return this.activator((reservoir_nodes * this.weights_output).ToArray());
            }
        }

        // 学習する
        public static TrainResult train(Matrix inputs, int num_input_nodes, int num_reservoir_nodes, int num_output_nodes, double leak_rate = 0.1, Func<double[], double[]> activator = null, double lambda0 = 0.1) {
            activator = activator ?? (xs => xs.Select(Math.Tanh).ToArray());

            // inputs; // u: 時刻0から時刻N-1までの入力ベクトル列(Vector<double>[N]型だと思っておけばいい)
            var log_reservoir_nodes = Matrix.create(inputs.Row + 1, num_reservoir_nodes, 0.0); // reservoir層のノードの状態を記録

            // 各層の重みを初期化する
            var weights_input = _generate_variational_weights(num_input_nodes, num_reservoir_nodes); // W_in: 入力層の重み
            var weights_reservoir = _generate_reservoir_weights(num_reservoir_nodes); // W_res: Reservoir層の重み

            // 時刻ごとの入力一つ毎に時系列で処理する
            for (var i = 0; i < inputs.Row; i++) {
                // input = u(t): 時刻 t の入力ベクトル
                var input = inputs.getRow(i);
                // current_state = 時刻 t-1 における reservoir層の内部状態 
                var current_state = log_reservoir_nodes.getRow(i);
                // 時刻 tの入力ベクトル input と 時刻 t-1における reservoir層の内部状態 current_state から 時刻 t における reservoir 層の内部状態 next_state を求める
                var next_state = _get_next_reservoir_nodes(input, current_state, weights_input, weights_reservoir, leak_rate, activator);
                // reservoir層の内部状態リストの末尾に 新しい状態 next_state を追加する
                log_reservoir_nodes.setRow(i + 1, next_state);
            }
            // reservoir層の内部状態リストの先頭（＝初期状態, 全部0初期化されてる）を削除
            log_reservoir_nodes = Matrix.buildFromArray(log_reservoir_nodes.Row - 1, log_reservoir_nodes.Col, log_reservoir_nodes.ToArray().Skip(log_reservoir_nodes.Col).ToArray());

            // W_out: 出力層の重みを算出
            var weights_output = _update_weights_output(inputs, num_reservoir_nodes, log_reservoir_nodes, lambda0);

            return new TrainResult(
                inputs: inputs,
                log_reservoir_nodes: log_reservoir_nodes,
                weights_input: weights_input,
                weights_reservoir: weights_reservoir,
                weights_output: weights_output,
                leak_rate: leak_rate,
                activator: activator
            );
        }


        // 入力層の重みの初期値を作って返す
        // -0.1 or 0.1 の 一様分布になっている必要があるらしい。
        private static Matrix _generate_variational_weights(int num_pre_nodes, int num_post_nodes) {
            var rand = new Random();
            var array = Enumerable.Range(0, num_pre_nodes * num_post_nodes).Select(x => x < num_pre_nodes * num_post_nodes / 2 ? -0.1 : +0.1).OrderBy(x => rand.Next()).ToArray();
            return Matrix.buildFromArray(num_pre_nodes, num_post_nodes, array);
        }

        // Reservoir層の重みの初期値を作って返す
        // 平均 0.0, 分散 1.0 の正規分布になっている必要があるらしい。
        private static Matrix _generate_reservoir_weights(int num_nodes) {
            var rand = new Random();
            var weightsArray = Enumerable.Range(0, num_nodes * num_nodes).Select(x => Math.Sqrt(-2 * Math.Log(rand.NextDouble(), 2)) * Math.Sin(2 * Math.PI * rand.NextDouble())).ToArray();
            var weights = Matrix.buildFromArray(num_nodes, num_nodes, weightsArray);

            var spectral_radius = Matrix.AbsMaxEigenValue(weights);
            var result = weights / spectral_radius;
            return result;
        }
    }

    public class EchoStateNetwork2 {
        int Nr; //  the number of reservoir neurons
        int Ni; //  the number of input dimension
        int No; //  the number of output dimension
        double sparsity;
        double spectral_radius;
        double noise_level;
        double leaking_rate;
        bool teacher_forcing;
        Matrix Wr; // reservoir weights;
        Matrix Wi;  //  input weights
        Matrix Wo;  // readout weights
        Matrix Wf; //  feedback weights
        Vector state;
        Vector input;
        Vector output;
        Random rng;

        public EchoStateNetwork2(
            int Ni = 1,
            int No = 1,
            int Nr = 100,
            double sparsity = 0.95,
            double spectral_radius = 0.95,
            double noise_level = 0.001,
            double leaking_rate = 1.0,
            bool teacher_forcing = true,
            Random rng = null
       ) {
            rng = rng ?? new Random();
            this.Ni = Ni;
            this.No = No;
            this.Nr = Nr;
            this.sparsity = sparsity;
            this.spectral_radius = spectral_radius;
            this.teacher_forcing = teacher_forcing;
            this.noise_level = noise_level;
            this.leaking_rate = leaking_rate;
            this.rng = rng;


            init_weights();
        }

        private void init_weights() {
            // init reservoir weight matrix
            var weight = Enumerable.Repeat(-0.5, this.Nr * this.Nr).Select(x => x + this.rng.NextDouble()).ToArray();

            // reduce connections based on `sparsity`
            var Ns = (int)Math.Round(this.Nr * this.Nr * this.sparsity);
            foreach (var i in Enumerable.Range(0, this.Nr * this.Nr).OrderBy(x => this.rng.Next()).Take(Ns)) {
                weight[i] = 0.0;
            }
            this.Wr = Matrix.buildFromArray(this.Nr, this.Nr, weight);

            // rescale the matrix to fit the `spectral radius`
            this.Wr *= this.spectral_radius / Matrix.AbsMaxEigenValue(this.Wr);

            // init input weight matrix
            this.Wi = Matrix.create(this.Nr, this.Ni + 1, (r, c) => this.rng.NextDouble() - 0.5);

            // init feedback weight matrix
            this.Wf = Matrix.create(this.Nr, this.No, (r, c) => this.rng.NextDouble() - 0.5);
        }

        private Vector update(Vector state, Vector input, Vector output, bool TeacherForcing) {
            var v = this.Wi * Vector.Concat(new[] { 1.0 }, input) + this.Wr * state;
            if (TeacherForcing) { v += this.Wf * output; }
            return v.map(Math.Tanh) + this.noise_level * Vector.create(this.Nr, (c) => this.rng.NextDouble() - 0.5);
        }

        private Matrix reservoir_states(Matrix inputs, Matrix outputs) {
            var Nd = inputs.Col;
            var states = Matrix.createZero(this.Nr, Nd);
            for (var t = 1; t < inputs.Col; t++) {
                var v = (1.0 - this.leaking_rate) * new Vector(states.getCol(t - 1)) + this.leaking_rate * update(new Vector(states.getCol(t - 1)), new Vector(inputs.getCol(t)), new Vector(outputs.getCol(t - 1)), this.teacher_forcing);
                states.setCol(t, v.ToArray());
            }
            return states;
        }

        public Matrix train(Matrix inputs, Matrix outputs, int? discard_ = null, double reg = 1e-8) {
            var discard = discard_ ?? Math.Min(inputs.Col / 10, 100);

            System.Diagnostics.Debug.Assert(inputs.Row == this.Ni);
            System.Diagnostics.Debug.Assert(outputs.Row == this.No);
            System.Diagnostics.Debug.Assert(inputs.Col == outputs.Col);

            var states = reservoir_states(inputs, outputs);

            // extended system states
            var X = Matrix.createZero(1 + this.Ni + this.Nr, inputs.Col);

            X.setRow(0, Vector.create(X.Col, (_) => 1.0).ToArray());
            for (var i = 0; i < this.Ni; i++) {
                X.setRow(1 + i, inputs.getRow(i));
            }
            for (var i = 0; i < this.Nr; i++) {
                X.setRow(1 + this.Ni + i, states.getRow(i));
            }

            // discard initial transient
            var Xe = Matrix.SliceColumn(X, discard, X.Col);
            var tXe = Matrix.transpose(Xe);
            var O = Matrix.SliceColumn(outputs, discard, outputs.Col);

            // calc output weight matrix
            this.Wo = O * tXe * Matrix.inverse(Xe * tXe + reg * Matrix.createIdentity(Xe.Row));

            // store last states
            this.state = new Vector(states.getCol(states.Col - 1));
            this.input = new Vector(inputs.getCol(inputs.Col - 1));
            this.output = new Vector(outputs.getCol(outputs.Col - 1));

            return this.Wo * X;
        }


        public Matrix predict(Matrix inputs, bool cont = true) {
            var Nd = inputs.Col;
            var outputs = Matrix.createZero(this.No, Nd);
            var state = Vector.create(this.Nr, (_) => 0);

            if (cont) {
                inputs = Matrix.appendColumn(inputs, this.input.ToArray());
                outputs = Matrix.appendColumn(outputs, this.output.ToArray());
                state = this.state;
            } else {
                inputs = Matrix.appendColumn(inputs, new Vector(this.Ni).ToArray());
                outputs = Matrix.appendColumn(outputs, new Vector(this.No).ToArray());
            }

            for (var t = 0; t < Nd; t++) {
                state = (1.0 - this.leaking_rate) * state + this.leaking_rate * update(state, new Vector(inputs.getCol(t + 1)), new Vector(outputs.getCol(t)), this.teacher_forcing);
                var ret = this.Wo * new Vector((new[] { 1.0 }).Concat(inputs.getCol(t)).Concat(state.ToArray()).ToArray());
                outputs.setCol(t + 1, ret.ToArray());
            }

            return Matrix.SliceColumn(outputs, 2, outputs.Col);
        }

    }

}



