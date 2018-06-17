using System.Reflection;

namespace CSCPP {

	/// <summary>
    /// バージョン情報
	/// </summary>
	public static class Version {
        /// <summary>
        /// コンストラクタ
        /// </summary>
		static Version() {
            var asm = Assembly.GetExecutingAssembly();
            var vers = asm.GetName().Version;
            Major = vers.Major;
            Minor = vers.Minor;
            VersionString = vers.ToString();
		}

        /// <summary>
        /// バージョン情報を文字列として取得
        /// </summary>
        public static string VersionString { get; }

        /// <summary>
        /// メジャーバージョン番号を取得
        /// </summary>
        public static int Major { get; }

        /// <summary>
        /// マイナーバージョン番号を取得
        /// </summary>
        public static int Minor { get; }

	}

}