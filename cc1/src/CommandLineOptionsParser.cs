using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnsiCParser {

    /// <summary>
    /// コマンドライン引数を解析する
    /// </summary>
    public class CommandLineOptionsParser {
        /// <summary>
        /// コマンドライン引数に応じた処理を行うイベントハンドラ
        /// </summary>
        /// <param name="args">コマンドラインに渡された引数列</param>
        /// <returns>解析成功ならtrue, 解析失敗ならfalse</returns>
        public delegate bool OptionHandler(String[] args);

        /// <summary>
        /// 引数についての定義
        /// </summary>
        private class OptionDefinition {

            public OptionDefinition(string name, int argc, OptionHandler handler) {
                this.Name = name;
                this.Argc = argc;
                this.Handler = handler;
            }

            public string Name {
                get; private set;
            }

            public int Argc {
                get; private set;
            }

            public OptionHandler Handler {
                get; private set;
            }

            public override int GetHashCode() {
                return Name.GetHashCode();
            }
        }

        /// <summary>
        /// 引数定義のテーブル
        /// </summary>
        private Dictionary<String, OptionDefinition> Options = new Dictionary<String, OptionDefinition>();

        /// <summary>
        /// 引数の定義を登録する
        /// </summary>
        /// <param name="name">引数文字列</param>
        /// <param name="argc">受け取る引数の数</param>
        /// <param name="handler">処理用のハンドラ</param>
        public void Entry(string name, int argc, OptionHandler handler) {
            Options.Add(name, new OptionDefinition(name, argc, handler));
        }

        /// <summary>
        /// 引数の数が少なかった場合に発生させる例外
        /// </summary>
        public class TooFewArgumentException : System.ArgumentException {
            public string Name {
                get; private set;
            }
            public int ArgCount {
                get; private set;
            }
            public int ParamCount {
                get; private set;
            }
            public TooFewArgumentException(string name, int argc, int paramc) :
                base(String.Format("コマンドライン引数 {0} には {1}個の引数が必要ですが、指定されているのは{2}個です。", name, argc, paramc)) {
                Name = name;
                ArgCount = argc;
                ParamCount = paramc;
            }
        }

        /// <summary>
        /// 引数の解析失敗時に発生させる例外
        /// </summary>
        public class ArgumentFormatException : System.ArgumentException {
            public string Name {
                get; private set;
            }
            public string[] Params {
                get; private set;
            }
            public ArgumentFormatException(string name, string[] paramv) :
                base(String.Format("コマンドライン引数 {0} {1} の書式に誤りがあります。", name, string.Join(" ", paramv))) {
                Name = name;
                Params = paramv;
            }
        }

        /// <summary>
        /// 引数の解析を行う
        /// </summary>
        /// <param name="args">引数列</param>
        /// <returns>余りの引数列</returns>
        public string[] Parse(string[] args) {
            IEnumerator<string> it = new List<string>(args).GetEnumerator();
            List<String> s = new List<string>();
            while (it.MoveNext()) {
                var key = it.Current;
                OptionDefinition info;
                if (Options.TryGetValue(key, out info)) {
                    List<string> tmp = new List<string>();
                    for (int i = 0; i < info.Argc; i++) {
                        if (it.MoveNext() == false) {
                            throw new TooFewArgumentException(info.Name, info.Argc, tmp.Count);
                        } else {
                            tmp.Add(it.Current);
                        }
                    }
                    var ary = tmp.ToArray();
                    if (info.Handler(ary) == false) {
                        throw new ArgumentFormatException(info.Name, ary);
                    }
                } else {
                    do {
                        s.Add(it.Current);
                    } while (it.MoveNext());
                }
            }
            return s.ToArray();
        }
    }
}
