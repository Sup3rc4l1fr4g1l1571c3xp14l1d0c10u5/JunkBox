using System;
using System.IO;
using System.Text;
using System.Linq;
using Hnx8.ReadJEnc;

namespace CSCPP {
    /// <summary>
    /// 拡張クラス
    /// </summary>
    public static class Encoding {
        /// <summary>
        /// ファイルの文字コードを推測する
        /// </summary>
        /// <param name="path">ファイルパス</param>
        /// <returns>推測結果</returns>
        public static System.Text.Encoding DetectEncodingFromFile(string path) {
            // jisコード(not sjis)の解析に対応した文字コード変換ライブラリの利用に変更
            var file = new FileInfo(path);
            using (FileReader reader = new FileReader(file)) {
                reader.ReadJEnc = ReadJEnc.JP;
                CharCode c = reader.Read(file);
                return c.GetEncoding();
            }
        }



        public static StreamReader CreateStreamReaderFromFile(string filename, bool autoDetectEncoding, System.Text.Encoding defaultEncoding) {
            System.Text.Encoding enc;
            if (autoDetectEncoding) {
                // 自動認識モード
                enc = Encoding.DetectEncodingFromFile(filename);
                if (enc == null) {
                    if (defaultEncoding != null) {
                        CppContext.Warning(new Position(filename, 1, 1), $"ファイル `{filename}` の文字エンコードを自動認識できないためデフォルトの文字コード({defaultEncoding.WebName})で読み取りを行います。この警告はファイルがバイナリファイルの場合や、ファイル内に文字化けや非標準の機種依存文字が含まれている場合に出力されます。ファイルの内容を確認してください。");
                        enc = defaultEncoding;
                    } else {
                        CppContext.Error(new Position(filename, 1, 1), $"ファイル `{filename}` の文字エンコードを自動認識できません。このエラーはファイルがバイナリファイルの場合や、ファイル内に文字化けや非標準の機種依存文字が含まれている場合に出力されます。ファイルの内容を確認してください。");
                        return null;
                    }
                } else {
                    //CppContext.Notify(new Position(filename, 1, 1), $"自動認識によりファイル `{filename}` の文字エンコードを {enc.WebName} とします。");
                }
            } else {
                enc = defaultEncoding;
            }
            if (enc == null) {
                return null;
            }
            enc = System.Text.Encoding.GetEncoding(enc.CodePage, EncoderFallback.ReplacementFallback, new DecoderScalarValueFallback());
            return new StreamReader(filename, enc);
        }

        // エンコードできない文字をUnicodeスカラ値表記(U+XXXX)に置き換えるEncoderFallback
        class DecoderScalarValueFallback : System.Text.DecoderFallback {
            // 置き換えられる代替文字列の最大文字数
            public override int MaxCharCount {
                get { return 10; /* "U+XXXX".Length */ }
            }

            public override System.Text.DecoderFallbackBuffer CreateFallbackBuffer() {
                // EncoderFallbackBufferのインスタンスを作成して返す
                return new DecoderScalarValueFallbackBuffer();
            }

            // 代替文字列の生成を行うクラス
            class DecoderScalarValueFallbackBuffer : System.Text.DecoderFallbackBuffer {
                private char[] alternative;
                private int offset;

                // 代替文字列の残り文字数
                public override int Remaining {
                    get { return alternative.Length - offset; }
                }

                // エンコードできない文字が見つかった場合に呼び出されるメソッド
                public override bool Fallback(byte [] charUnknown, int index) {
                    // エンコードできない文字を0xFFFFで括った文字列を代替文字列とする。
                    // 0xFFFFは一応UTF16上は使わない（未使用）文字となっている
                    var str = charUnknown.Aggregate(new StringBuilder(), (s,x) => s.Append($"{ x:X2}")).ToString();
                    alternative = ($@"{(char)0xFFFF}{str}{(char)0xFFFF}").ToCharArray();
                    offset = 0;

                    return true;
                }


                // 代替文字列の次の文字を取得するメソッド
                public override char GetNextChar() {
                    if (alternative.Length <= offset)
                        // MaxCharCountやRemainingプロパティの値は考慮されない(?)ようなので、
                        // 代替文字列の末尾に到達したらchar.MinValueを返す必要がある
                        return char.MinValue;
                    else
                        return alternative[offset++];
                }

                public override bool MovePrevious() {
                    // 実装は省略
                    throw new System.NotImplementedException();
                }

                public override void Reset() {
                    // 実装は省略
                    throw new System.NotImplementedException();
                }
            }
        }

    }

}

