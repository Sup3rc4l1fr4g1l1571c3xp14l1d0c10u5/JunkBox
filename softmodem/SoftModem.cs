using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftModem
{
    /// <summary>
    /// エンコーダ
    /// </summary>
    public class Encoder
    {
        /// <summary>
        /// サンプリングレート
        /// </summary>
        public int sampleRate { get; }

        /// <summary>
        /// ボーレート
        /// </summary>
        public int baud { get; }

        /// <summary>
        /// 高周波の搬送波
        /// </summary>
        public int freqHigh { get; }

        /// <summary>
        /// 低周波の搬送波
        /// </summary>
        public int freqLow { get; }

        /// <summary>
        /// softmodem互換動作の有無
        /// </summary>
        public bool softmodem { get; }

        /// <summary>
        /// 高周波の搬送波の波形サンプル
        /// </summary>
        private float[] bitBufferHigh { get; }
        
        /// <summary>
        /// 低周波の搬送波の波形サンプル
        /// </summary>
        private float[] bitBufferLow { get; }

        /// <summary>
        /// プリアンブル長
        /// </summary>
        private int preambleLength { get; }

        /// <summary>
        /// 1ビット当たりのサンプル数
        /// </summary>
        private int samplesPerBit { get; }

        /// <summary>
        /// エンコーダを初期化
        /// </summary>
        /// <param name="sampleRate"></param>
        /// <param name="baud"></param>
        /// <param name="freqLow"></param>
        /// <param name="freqHigh"></param>
        /// <param name="softModem"></param>
        public Encoder(int sampleRate = 44100, int baud = 1225, int freqLow = 4900, int freqHigh = 7350, bool softModem = true) {
            this.sampleRate = sampleRate;
            this.baud = baud;
            this.freqLow = freqLow;
            this.freqHigh = freqHigh;
            this.softmodem = softModem;

            this.samplesPerBit = (sampleRate / baud);
            this.preambleLength = (sampleRate * 40 / 1000 / samplesPerBit);
            this.bitBufferLow = new float[samplesPerBit];
            this.bitBufferHigh = new float[samplesPerBit];

            var phaseIncLow = 2.0 * Math.PI * freqLow / sampleRate;
            var phaseIncHigh = 2.0 * Math.PI * freqHigh / sampleRate;
            for (var i = 0; i < samplesPerBit; i++) {
                bitBufferLow[i] = (float)Math.Cos(phaseIncLow * i);
                bitBufferHigh[i] = (float)Math.Cos(phaseIncHigh * i);
            }
        }

        /// <summary>
        /// バイト列をシリアルデータに符号化
        /// </summary>
        /// <param name="uint8"></param>
        /// <returns></returns>
        public IEnumerable<byte> encoding(byte[] uint8) {
            /* プリアンブルの生成 */
            for (var i = 0; i < preambleLength; i++) {
                yield return 1;
            }

            /* データ部の生成 */
            for (var x = 0; x < uint8.Length; x++) {
                /* データ部パケットの0ビット目は 0 */
                yield return 0;
                for (int b = 0, c = uint8[x]; b < 8; b++, c >>= 1) {
                    /* 下位ビットから順にデータ部に格納していく */
                    yield return (byte)(c & 1);
                }
                /* データ部パケットの9ビット目は 1 */
                yield return 1;
            }

            /* ポストアンブルの生成 */
            yield return 1;
            if (!softmodem) {
                yield return 0;
            }
        }

        /// <summary>
        /// シリアルデータをFSKで変調
        /// </summary>
        /// <param name="encoded"></param>
        /// <returns></returns>
        public IEnumerable<float> modulating(IEnumerable<byte> encoded) {
            foreach (byte bit in encoded) {
                if (bit != 0) {
                    foreach (var sample in bitBufferHigh) {
                        yield return sample;
                    }
                } else {
                    foreach (var sample in bitBufferLow) {
                        yield return sample;
                    }
                }
            }
        }

    }

    /// <summary>
    /// デコーダー
    /// </summary>
    public class Decoder
    {

        /// <summary>
        /// サンプリングレート
        /// </summary>
        public int sampleRate { get; }

        /// <summary>
        /// ボーレート
        /// </summary>
        public int baud { get; }

        /// <summary>
        /// 高周波の搬送波
        /// </summary>
        public int freqHigh { get; }

        /// <summary>
        /// 低周波の搬送波
        /// </summary>
        public int freqLow { get; }

        /// <summary>
        /// softmodem互換動作の有無
        /// </summary>
        public bool softmodem { get; }


        private int samplesPerBit;
        private int preambleLength;
        private float[] cLowReal;
        private float[] cLowImag;
        private float[] cHighReal;
        private float[] cHighImag;
        private float[] sinusLow;
        private float[] sinusHigh;
        private float[] cosinusLow;
        private float[] cosinusHigh;

        public EventHandler<byte[]> onReceive;

        /// <summary>
        /// デコーダー状態
        /// </summary>
        private enum StateCode
        {
            IDLE = 0,
            PREAMBLE = 1,
            START = 2,
            DATA = 3,
            STOP = 4,
        }

        /// <summary>
        /// デコーダー状態
        /// </summary>
        private StateCode current = StateCode.IDLE;

        public int bitCounter = 0;  // counts up to 8 bits
        public byte byteBuffer = 0;  // where the 8 bits get assembled
        public List<byte> wordBuffer = new List<byte>();  // where the 8 bits get assembled

        public int lastTransition = 0;
        public int lastBitState = 0;
        public int state_t = 0;// sample counter, no reset currently -> will overflow
        public int state_c = 0;// counter for the circular correlation arrays


        public Decoder(int sampleRate, int baud = 1225, int freqLow = 4900, int freqHigh = 7350, bool softModem = true) {
            this.sampleRate = sampleRate;
            this.baud = baud;
            this.freqLow = freqLow;
            this.freqHigh = freqHigh;
            this.softmodem = softModem;

            this.samplesPerBit = (sampleRate / baud);
            this.preambleLength = (sampleRate * 40 / 1000 / samplesPerBit);

            this.cLowReal = new float[(samplesPerBit / 2)];
            this.cLowImag = new float[(samplesPerBit / 2)];
            this.cHighReal = new float[(samplesPerBit / 2)];
            this.cHighImag = new float[(samplesPerBit / 2)];

            this.sinusLow = new float[(samplesPerBit / 2)];
            this.sinusHigh = new float[(samplesPerBit / 2)];
            this.cosinusLow = new float[(samplesPerBit / 2)];
            this.cosinusHigh = new float[(samplesPerBit / 2)];

            var phaseIncLow = 2 * Math.PI * ((float)freqLow / sampleRate);
            var phaseIncHigh = 2 * Math.PI * ((float)freqHigh / sampleRate);

            for (var i = 0; i < samplesPerBit / 2; i++) {
                sinusLow[i] = (float)Math.Sin(phaseIncLow * i);
                sinusHigh[i] = (float)Math.Sin(phaseIncHigh * i);
                cosinusLow[i] = (float)Math.Cos(phaseIncLow * i);
                cosinusHigh[i] = (float)Math.Cos(phaseIncHigh * i);
            }

        }

        /// <summary>
        /// 信号の正規化
        /// </summary>
        /// <param name="samples"></param>
        /// <returns></returns>
        private IEnumerable<float> Normalize(IList<float> samples) {
            var max = samples.Max();
            return samples.Select(x => x / max);
        }

        /// <summary>
        /// 信号のスムージング
        /// </summary>
        /// <param name="samples"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public static float[] smoothing3(IEnumerable<float> samples, int n) {
            return samples.EachWindow(n).Select(x => (x.Sum() / (n * 2 + 1.0f))).ToArray();
        }

        /// <summary>
        /// 同期検波を行う
        /// </summary>
        /// <param name="samples">正規化済みの信号列</param>
        /// <returns>検波結果のタプル（高周波側、低周波側、差分）</returns>
        public IEnumerable<Tuple<float, float, float>> SynchronousDetection(IEnumerable<float> samples) {
            foreach (var sample in samples) {
                // 各周波数の信号を混合
                cLowReal[state_c] = sample * cosinusLow[state_c];   // 低周波側のQ
                cLowImag[state_c] = sample * sinusLow[state_c];     // 低周波側のI
                cHighReal[state_c] = sample * cosinusHigh[state_c]; // 高周波側のQ
                cHighImag[state_c] = sample * sinusHigh[state_c];   // 高周波側のI
                state_c++;
                if (state_c == samplesPerBit / 2) { state_c = 0; }

                // 窓区間の信号のQ,I成分の積分結果から合成ベクトルを求める
                var cLow = Math.Sqrt(Math.Pow(cLowReal.Sum(), 2) + Math.Pow(cLowImag.Sum(), 2));
                var cHigh = Math.Sqrt(Math.Pow(cHighReal.Sum(), 2) + Math.Pow(cHighImag.Sum(), 2));

                // 合成ベクトルの差が検波結果になる
                yield return  Tuple.Create((float)cHigh, (float)cLow, (float)(cHigh - cLow));
            }
        }

        /// <summary>
        /// ビット列を復号
        /// </summary>
        /// <param name="samples">検波結果（差分）</param>
        /// <returns>ビット情報のタプル（ビット値、ビット情報）</returns>
        public IEnumerable<Tuple<int, int>> DetectSymbol(IEnumerable<float> samples) {
            var level = false;
            foreach (var sample in samples) {
                // シュミットトリガでエッジを認識
                if (Math.Abs(0.5f - sample) > 0.3 && ((sample >= 0) != level)) {
                    var length = (int)Math.Round((float)(state_t - lastTransition) / samplesPerBit);
                    if (length > 0) {
                        lastTransition = state_t;
                        yield return Tuple.Create(level ? 1 : 0, length);
                        level = !level;
                    }
                }
                state_t++;
            }
            {
                var length = (int)Math.Round((float)(state_t - lastTransition) / samplesPerBit);
                if (length > 0) {
                    lastTransition = state_t;
                    yield return Tuple.Create(level ? 1 : 0, length);
                    level = !level;
                }
            }
        }

        /// <summary>
        /// FSKの復調を行う
        /// </summary>
        /// <param name="smpls"></param>
        /// <returns></returns>
        public Tuple<float[], float[], float[], float[], Tuple<int,int>[]> demodding(float[] smpls) {

            // 同期検波を行う
            var hs = new float[smpls.Length];
            var ls = new float[smpls.Length];
            var samples = new float[smpls.Length];
            smpls.Apply(Normalize).Apply(SynchronousDetection).Each((x, i) => {
                hs[i] = x.Item1;
                ls[i] = x.Item2;
                samples[i] = x.Item3;
            });

            // 復調結果をスムージングする
            var samples2 = smoothing3(samples, 1);

            // ビット値と長さを求める
            var symbols = DetectSymbol(samples2).ToArray();

            return Tuple.Create(ls, hs, samples, samples2, symbols);
        }

        /// <summary>
        /// n個のビットbitを受信してバッファに書き込む処理
        /// </summary>
        /// <param name="bit"></param>
        /// <param name="n"></param>
        private void addBitNTimes(int bit, int n) {
            if (bitCounter + n > 8) {
                throw new Exception("byteBuffer too small");
            }
            for (var b = 0; b < n; b++) {
                bitCounter++;
                byteBuffer >>= 1;
                if (bit != 0) {
                    byteBuffer |= 128;
                }
                if (bitCounter == 8) {
                    wordBuffer.Add(byteBuffer);
                    byteBuffer = 0;
                }
            }
        }

        /// <summary>
        /// シリアルデータをバイト列に復号
        /// </summary>
        /// <param name="bitlengths"></param>
        public void decode(IEnumerable<Tuple<int,int>> bitlengths) {

            var nextState = StateCode.PREAMBLE;

            foreach (var item in bitlengths) {
                var symbols = item.Item2;
                //Console.WriteLine(current);
                switch (current) {
                    case StateCode.PREAMBLE: { 
                        /* プリアンブル受信待ち状態 */
                        if (symbols >= 12 && symbols <= preambleLength + 20) {
                            /* 12個以上プリアンブル長+20個以下の連続する1ビットを受信した場合、データ受信開始状態へ遷移 */
                            nextState = StateCode.START;
                            lastBitState = 0;
                            byteBuffer = 0;
                            wordBuffer.Clear();
                        }
                        break;
                    }
                    case StateCode.START: { 
                        /* データ受信開始状態 */
                        bitCounter = 0;
                        if (symbols == 1) {
                            /* 連続0ビット数が1個（0b0を受信）の場合、データ受信モードへ */
                            nextState = StateCode.DATA;
                        } else if (symbols > 1 && symbols <= 9) {
                            /* 連続0ビット数が２～８の場合はデータ受信モードへ、９ビットの場合はデータ受信終了モード */
                            nextState = symbols == 9 ? StateCode.STOP : StateCode.DATA;
                            addBitNTimes(0, symbols - 1);
                        } else {
                            /* それ以外はプリアンブル受信モードへ */
                            nextState = StateCode.PREAMBLE;
                        }
                        break;
                    }
                    case StateCode.DATA: { 
                        // 受信ビット数
                        var bits_total = symbols + bitCounter;
                        // 最後に受け取ったビットの状態
                        var bit = lastBitState ^ 1;

                        if (bits_total > 11) {
                            // 受信ビット数が11ビット超えた場合はプリアンブル受信モードへ
                            nextState = StateCode.PREAMBLE;
                        } else if (bits_total == 11) { // all bits high, stop bit, push bit, preamble
                            // 受信ビット数が11の場合、データ部の全ビットが1で、それに続いてStopビット、pushビット、そしてプリアンブルビットを受信したことを示す。
                            addBitNTimes(1, symbols - 3);
                            nextState = StateCode.START;
                            onReceive.Invoke(this, wordBuffer.ToArray());
                            wordBuffer.Clear();
                        } else if (bits_total == 10) { // all bits high, stop bit, push bit, no new preamble
                            // 受信ビット数が10の場合、データ部の全ビットが1で、それに続いてStopビット、pushビットを受信し、プリアンブルビットは受信していないことを示す。。
                            addBitNTimes(1, symbols - 2);
                            nextState = StateCode.PREAMBLE;
                            onReceive.Invoke(this, wordBuffer.ToArray());
                        } else if (bits_total == 9) { // all bits high, stop bit, no push bit
                            // 受信ビット数が9の場合、データ部の全ビットが1で、それに続いてStopビットを受信し、pushビットとプリアンブルビットは受信していないことを示す。。
                            addBitNTimes(1, symbols - 1);
                            nextState = StateCode.START;
                        } else if (bits_total == 8) {
                            // 受信ビット数が9の場合、データ部の全ビットが1で、Stopビット、pushビットとプリアンブルビットは受信していないことを示す。。
                            addBitNTimes(bit, symbols);
                            nextState = StateCode.STOP;
                            lastBitState = bit; // 最後に受け取ったビットを記録しておく
                            } else {
                            // 受信ビット数が7以下の場合、データ部の1ビットを受信したことを示す。
                            addBitNTimes(bit, symbols);
                            nextState = StateCode.DATA;
                            lastBitState = bit; // 最後に受け取ったビットを記録しておく
                        }

                        if (symbols == 0) { // 0 always indicates a misinterpreted symbol
                            nextState = StateCode.PREAMBLE;
                        }
                        break;
                    }
                    case StateCode.STOP:
                        if (symbols == 1) {
                            nextState = StateCode.START;
                        } else if (symbols == 3) {
                            nextState = StateCode.START;
                            onReceive.Invoke(this, wordBuffer.ToArray());
                            wordBuffer.Clear();
                        } else if (symbols >= 2) {
                            nextState = StateCode.PREAMBLE;
                            onReceive.Invoke(this, wordBuffer.ToArray());
                        } else
                            nextState = StateCode.PREAMBLE;

                        break;

                    default:
                        nextState = StateCode.PREAMBLE;
                        bitCounter = 0;
                        byteBuffer = 0;
                        break;
                }
                current = nextState;
            }
        }

    }

    public class Resampler
    {
        private float[] inputBuffer { get; }
        private int fromSampleRate { get; }
        private int toSampleRate { get; }
        private delegate int ResampleFunction(int bufferLength);
        private ResampleFunction resampleFunction { get; }
        private float ratioWeight { get; }
        private float lastWeight { get; set; }
        private bool tailExists { get; set; }
        public float[] outputBuffer { get; private set; }
        private float[] lastOutput { get; }

        public Resampler(int inRate, int outRate, float[] inputBuffer) {
            this.fromSampleRate = inRate;
            this.toSampleRate = outRate;
            this.inputBuffer = inputBuffer;

            if (fromSampleRate > 0 && toSampleRate > 0) {
                if (fromSampleRate == toSampleRate) {
                    resampleFunction = bypassResampler;        //Resampler just returns what was passed through.
                    ratioWeight = 1;
                    outputBuffer = inputBuffer;
                    lastOutput = new float[0];
                } else {
                    var outputBufferSize = (int)Math.Ceiling(inputBuffer.Length * toSampleRate / fromSampleRate / 1.000000476837158203125) + 1; // 1.000000476837158203125 = 1.0 + 2^-21
                    outputBuffer = new float[outputBufferSize];
                    lastOutput = new float[1];
                    ratioWeight = (float)fromSampleRate / toSampleRate;
                    if (fromSampleRate < toSampleRate) {
                        resampleFunction = linearInterpolationFunction;
                        lastWeight = 1.0f;
                    } else {
                        resampleFunction = compileMultiTapFunction;
                        tailExists = false;
                        lastWeight = 0.0f;
                    }
                }
            } else {
                throw new Exception("Invalid settings specified for the resampler.");
            }
        }

        public int resample(int bufferLength) {
            return resampleFunction(bufferLength);
        }

        private int linearInterpolationFunction(int bufferLength) {
            var outputOffset = 0;
            if (bufferLength > 0) {
                float weight = lastWeight;
                float firstWeight = 0;
                float secondWeight = 0;
                var sourceOffset = 0;

                weight -= 1;
                for (bufferLength -= 1, sourceOffset = (int)Math.Floor(weight); sourceOffset < bufferLength;) {
                    secondWeight = weight % 1;
                    firstWeight = 1.0f - secondWeight;
                    outputBuffer[outputOffset++] = (inputBuffer[sourceOffset] * firstWeight) + (inputBuffer[sourceOffset + 1] * secondWeight);
                    weight += ratioWeight;
                    sourceOffset = (int)Math.Floor(weight);
                }
                lastOutput[0] = inputBuffer[sourceOffset++];
                lastWeight = weight % 1;
            }
            return outputOffset;
        }

        private int compileMultiTapFunction(int bufferLength) {
            var outputOffset = 0;
            if (bufferLength > 0) {
                float weight = 0;
                float output0 = 0;
                var actualPosition = 0;
                float amountToNext = 0;
                var alreadyProcessedTail = !tailExists;
                tailExists = false;
                float currentPosition = 0;
                do {
                    if (alreadyProcessedTail) {
                        weight = ratioWeight;
                        output0 = 0;
                    } else {
                        weight = lastWeight;
                        output0 = lastOutput[0];
                        alreadyProcessedTail = true;
                    }
                    while (weight > 0 && actualPosition < bufferLength) {
                        amountToNext = 1 + actualPosition - currentPosition;
                        if (weight >= amountToNext) {
                            output0 += inputBuffer[actualPosition++] * amountToNext;
                            currentPosition = actualPosition;
                            weight -= amountToNext;
                        } else {
                            output0 += inputBuffer[actualPosition] * weight;
                            currentPosition += weight;
                            weight = 0;
                            break;
                        }
                    }
                    if (weight <= 0) {
                        outputBuffer[outputOffset++] = output0 / ratioWeight;
                    } else {
                        lastWeight = weight;
                        lastOutput[0] = output0;
                        tailExists = true;
                        break;
                    }
                } while (actualPosition < bufferLength);
            }
            return outputOffset;
        }

        private int bypassResampler(int bufferLength) {
            return bufferLength;
        }
    }


    public static class Ext2 {
        public static TOutput Apply<TInput,TOutput>(this TInput self, Func<TInput,TOutput> func) {
            return func(self);
        }
        public static void Each<T>(this IEnumerable<T> self, Action<T,int> pred) {
            int i = 0;
            foreach (var item in self) {
                pred(item,i);
                i++;
            }
        }
        public static IEnumerable<T[]> EachWindow<T>(this IEnumerable<T> self, int windowWidth, T defaultValue = default(T)) {
            var seq = Enumerable.Repeat(defaultValue, windowWidth).Concat(self).Concat(Enumerable.Repeat(defaultValue, windowWidth));
            var queue = new Queue<T>(seq.Take(windowWidth*2+1));
            foreach (var value in seq) {
                yield return queue.ToArray();
                queue.Dequeue();
                queue.Enqueue(value);
            }
            yield return queue.ToArray();
        }
    }
}
