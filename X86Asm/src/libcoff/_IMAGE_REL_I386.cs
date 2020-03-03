using System;

namespace X86Asm.libcoff {

    /// <summary>
    /// i386の再配置種別
    /// </summary>
    public enum _IMAGE_REL_I386 : UInt16 {
        /// <summary>
        /// この再配置は無視されます。
        /// </summary>
        IMAGE_REL_I386_ABSOLUTE = 0,
        /// <summary>
        /// ターゲットの16ビット仮想アドレス。（サポートされていません。）
        /// </summary>
        IMAGE_REL_I386_DIR16 = 1,
        /// <summary>
        /// ターゲットの16ビット相対仮想アドレス。（サポートされていません。）
        /// </summary>
        IMAGE_REL_I386_REL16 = 2,
        /// <summary>
        /// ターゲットの32ビット仮想アドレス。
        /// </summary>
        IMAGE_REL_I386_DIR32 = 6,
        /// <summary>
        /// ターゲットの32ビット相対仮想アドレス。
        /// </summary>
        IMAGE_REL_I386_DIR32NB = 7,
        /// <summary>
        /// サポートされていません。
        /// </summary>
        IMAGE_REL_I386_SEG12 = 9,
        /// <summary>
        /// ターゲットを含んでいるセクションの16ビット セクション インデックス。これはデバッグ情報をサポートするために使われます。
        /// </summary>
        IMAGE_REL_I386_SECTION = 10,
        /// <summary>
        /// ターゲットのセクションの先頭からの32ビット オフセット。これはデバッグ情報と静的スレッド ローカル ストレージをサポートするために使用されます。
        /// </summary>
        IMAGE_REL_I386_SECREL = 11,
        /// <summary>
        /// CLRのトークン
        /// </summary>
        IMAGE_REL_I386_TOKEN = 12,
        /// <summary>
        /// ターゲットを含んでいるセクションの7ビット セクション インデックス
        /// </summary>
        IMAGE_REL_I386_SECREL7 = 13,
        /// <summary>
        /// ターゲットに対する32ビット相対ディスプレースメント。これはx86相対分岐および呼び出し命令をサポートします。
        /// </summary>
        IMAGE_REL_I386_REL32 = 20,
    }
}
