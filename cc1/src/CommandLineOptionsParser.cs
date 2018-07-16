using System;
using System.Collections.Generic;

namespace AnsiCParser {

    /// <summary>
    /// コマンドライン引数を解析する
    /// </summary>
    public class CommandLineOptionsParser<T> {

        /// <summary>
        /// コマンドライン引数に応じた処理を行うイベントハンドラ
        /// </summary>
        /// <param name="args">コマンドラインに渡された引数列</param>
        /// <returns>解析成功ならtrue, 解析失敗ならfalse</returns>
        public delegate bool OptionHandler(T t, String[] args);

        /// <summary>
        /// 引数についての定義
        /// </summary>
        private class OptionDefinition {

            public OptionDefinition(string name, int argc, OptionHandler handler) {
                Name = name;
                Argc = argc;
                Handler = handler;
            }

            public string Name {
                get;
            }

            public int Argc {
                get;
            }

            public OptionHandler Handler {
                get;
            }

            public override int GetHashCode() {
                return Name.GetHashCode();
            }
        }

        /// <summary>
        /// 引数定義のテーブル
        /// </summary>
        private readonly Dictionary<String, OptionDefinition> _options = new Dictionary<String, OptionDefinition>();

        /// <summary>
        /// デフォルト処理
        /// </summary>
        private OptionHandler _default;

        /// <summary>
        /// 引数の定義を登録する
        /// </summary>
        /// <param name="name">引数文字列</param>
        /// <param name="argc">受け取る引数の数</param>
        /// <param name="handler">処理用のハンドラ</param>
        public CommandLineOptionsParser<T> Entry(string name, int argc, OptionHandler handler) {
            _options.Add(name, new OptionDefinition(name, argc, handler));
            return this;
        }

        /// <summary>
        /// どれにも一致しなかった場合の処理
        /// </summary>
        /// <param name="handler">処理用のハンドラ</param>
        public CommandLineOptionsParser<T> Default(OptionHandler handler) {
            _default = handler;
            return this;
        }

        /// <summary>
        /// 引数の数が少なかった場合に発生させる例外
        /// </summary>
        public class TooFewArgumentException : ArgumentException {
            public string Name {
                get;
            }
            public int ArgCount {
                get;
            }
            public int ParamCount {
                get;
            }
            public TooFewArgumentException(string name, int argc, int paramc) :
                base($"コマンドライン引数 {name} には {argc}個の引数が必要ですが、指定されているのは{paramc}個です。") {
                Name = name;
                ArgCount = argc;
                ParamCount = paramc;
            }
        }

        /// <summary>
        /// 引数の解析失敗時に発生させる例外
        /// </summary>
        public class ArgumentFormatException : ArgumentException {
            public string Name {
                get;
            }
            public string[] Params {
                get;
            }
            public ArgumentFormatException(string name, string[] paramv) :
                base($"コマンドライン引数 {name} {string.Join(" ", paramv)} の書式に誤りがあります。") {
                Name = name;
                Params = paramv;
            }
        }

        /// <summary>
        /// 引数の解析を行う
        /// </summary>
        /// <param name="t"></param>
        /// <param name="args">引数列</param>
        /// <returns>余りの引数列</returns>
        public T Parse(T t, string[] args) {
            using (IEnumerator<string> it = new List<string>(args).GetEnumerator()) {

                while (it.MoveNext()) {
                    var key = it.Current;
                    OptionDefinition info;
                    if (key == null) { continue; }
                    if (_options.TryGetValue(key, out info)) {
                        var tmp = new List<string>();
                        for (int i = 0; i < info.Argc; i++) {
                            if (it.MoveNext() == false) {
                                throw new TooFewArgumentException(info.Name, info.Argc, tmp.Count);
                            } else {
                                tmp.Add(it.Current);
                            }
                        }
                        var ary = tmp.ToArray();
                        if (info.Handler(t, ary) == false) {
                            throw new ArgumentFormatException(info.Name, ary);
                        }
                    } else {
                        var s = new List<string>();
                        if (_default != null) {
                            do {
                                s.Add(it.Current);
                            } while (it.MoveNext());
                            _default(t, s.ToArray());
                        }
                    }
                }
                return t;
            }
        }
    }
}
