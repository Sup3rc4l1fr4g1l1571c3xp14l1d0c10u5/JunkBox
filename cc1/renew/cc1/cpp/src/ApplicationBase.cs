using System;
using System.Reflection;

namespace CSCPP
{
    /// <summary>
    /// アプリケーション基底クラス
    /// </summary>
    public abstract class ApplicationBase
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
	    protected ApplicationBase() {
            var assembly = Assembly.GetEntryAssembly();

            ApplicationPath = assembly.Location;
            ApplicationDirectory = System.IO.Path.GetDirectoryName(ApplicationPath) ?? ".";

            var version = assembly.GetName().Version;
            MajorVersion = version.Major;
            MinorVersion = version.Minor;
            VersionString = version.ToString();
        }

        /// <summary>
        /// プログラム本体のディレクトリ
        /// </summary>
        public string ApplicationPath { get; }

        /// <summary>
        /// プログラム本体のディレクトリ
        /// </summary>
        public string ApplicationDirectory { get; }

        /// <summary>
        /// バージョン情報を文字列として取得
        /// </summary>
        public string VersionString { get; }

        /// <summary>
        /// メジャーバージョン番号を取得
        /// </summary>
        public int MajorVersion { get; }

        /// <summary>
        /// マイナーバージョン番号を取得
        /// </summary>
        public int MinorVersion { get; }

        /// <summary>
        /// アプリケーション実行
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public int Run(string[] args) {
            if (System.Diagnostics.Debugger.IsAttached) {
                return DebugMain(args);
            } else {
                try {
                    return Main(args);
                } catch (Exception e) {
                    ReportException(e, args);
                    return -1;
                }
            }
        }

        /// <summary>
        /// 例外レポート
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="args"></param>
        public virtual void ReportException(Exception exception, string[] args) {
            Console.Error.WriteLine($@"
--------------------------------------------------------------------
アプリケーションの実行中に例外が発生したためプログラムを終了します。
お手数ですが以下の出力を添えて開発元までご連絡ください。
--------------------------------------------------------------------
■バージョン情報：
{VersionString}
--------------------------------------------------------------------
■起動時の引数：
{string.Join(" ", args)}
--------------------------------------------------------------------
■発生した例外：{exception.GetType().Name}
{exception.Message}
--------------------------------------------------------------------
■例外発生時のスタックトレース：
{exception.StackTrace}
--------------------------------------------------------------------
");
        }

        public virtual int Main(string[] args) { return 0; }
        public virtual int DebugMain(string[] args) { return Main(args); }
    }
}
