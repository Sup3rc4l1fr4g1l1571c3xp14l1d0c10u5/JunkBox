using System;
using System.Collections.Generic;
using AnsiCParser.DataType;
using AnsiCParser.SyntaxTree;

namespace AnsiCParser {
    /// <summary>
    /// リンケージ情報オブジェクト
    /// </summary>
    public class LinkageObject {

        // 6.2.2 識別子の結合
        // 異なる有効範囲で又は同じ有効範囲で 2 回以上宣言された識別子は，結合（linkage）と呼ぶ過程によって，同じオブジェクト又は関数を参照することができる。
        // 結合は，外部結合，内部結合及び無結合の 3 種類とする。
        // プログラム全体を構成する翻訳単位及びライブラリの集合の中で，外部結合（external linkage）をもつ一つの識別子の各々の宣言は，同じオブジェクト又は関数を表す。
        // 一つの翻訳単位の中で，内部結合（internal linkage）をもつ一つの識別子の各々の宣言は，同じオブジェクト又は関数を表す。
        // 無結合（no linkage）をもつ識別子の各々の宣言は，それぞれが別々の実体を表す。
        // オブジェクト又は関数に対するファイル有効範囲の識別子の宣言が記憶域クラス指定子 static を含む場合，その識別子は，内部結合をもつ。
        // 識別子が，その識別子の以前の宣言が可視である有効範囲において，記憶域クラス指定子 extern を伴って宣言される場合，次のとおりとする。
        // - 以前の宣言において内部結合又は外部結合が指定されているならば，新しい宣言における識別子は，以前の宣言と同じ結合をもつ。
        // - 可視である以前の宣言がない場合，又は以前の宣言が無結合である場合，この識別子は外部結合をもつ。
        // 関数の識別子の宣言が記憶域クラス指定子をもたない場合，その結合は，記憶域クラス指定子 externを伴って宣言された場合と同じ規則で決定する。
        // オブジェクトの識別子の宣言がファイル有効範囲をもち，かつ記憶域クラス指定子をもたない場合，その識別子の結合は，外部結合とする。
        // オブジェクト又は関数以外を宣言する識別子，関数仮引数を宣言する識別子，及び記憶域クラス指定子externを伴わないブロック有効範囲のオブジェクトを宣言する識別子は，無結合とする。
        // 翻訳単位の中で同じ識別子が内部結合と外部結合の両方で現れた場合，その動作は未定義とする。

        // 整理
        // - 異なる有効範囲で又は同じ有効範囲で 2 回以上宣言された識別子は，結合（linkage）と呼ぶ過程によって，同じオブジェクト又は関数を参照することができる。 
        //   => スコープに加えて linkage も解決しないと参照先が決まらない。
        //
        // - 結合は，外部結合，内部結合及び無結合の 3 種類
        //   - 外部結合（external linkage）
        //     - プログラム全体を構成する翻訳単位及びライブラリの集合の中で，外部結合（external linkage）をもつ一つの識別子の各々の宣言は，同じオブジェクト又は関数を表す。
        //       => 外部結合はスコープや翻訳単位の関係なく常に同じものを指す。
        //   - 内部結合（internal linkage）
        //     - 一つの翻訳単位の中で，内部結合（internal linkage）をもつ一つの識別子の各々の宣言は，同じオブジェクト又は関数を表す。 
        //       => 内部結合は翻訳単位内で常に同じものを指す。
        //     - オブジェクト又は関数に対するファイル有効範囲の識別子の宣言が記憶域クラス指定子 static を含む場合，その識別子は，内部結合をもつ。
        //   - 無結合（no linkage）
        //     - 無結合（no linkage）をもつ識別子の各々の宣言は，それぞれが別々の実体を表す。 => 指し示し先はすべて独立している
        //
        // - 識別子が，その識別子の以前の宣言が可視である有効範囲において，記憶域クラス指定子 extern を伴って宣言される場合，次のとおりとする。
        //   - 以前の宣言において内部結合又は外部結合が指定されているならば，新しい宣言における識別子は，以前の宣言と同じ結合をもつ。
        //   - 可視である以前の宣言がない場合，又は以前の宣言が無結合である場合，この識別子は外部結合をもつ。
        //   => externを伴う宣言が登場
        //      => 以前の宣言で結合の指定がある場合はその指定に従う。
        //      => 以前の宣言が無い場合や、結合の指定が無い場合は外部結合とする。
        //
        // - 関数の識別子の宣言が記憶域クラス指定子をもたない場合，その結合は，記憶域クラス指定子 externを伴って宣言された場合と同じ規則で決定する。
        //  => 関数宣言はデフォルトで extern 
        //
        // - オブジェクトの識別子の宣言がファイル有効範囲をもち，かつ記憶域クラス指定子をもたない場合，その識別子の結合は，外部結合とする。
        // 
        // - オブジェクト又は関数以外を宣言する識別子，関数仮引数を宣言する識別子，及び記憶域クラス指定子externを伴わないブロック有効範囲のオブジェクトを宣言する識別子は，無結合とする。
        //   => オブジェクト又は関数以外を宣言する識別子 = 型名宣言、タグ型宣言
        //   => 関数仮引数を宣言する識別子 = 関数宣言・関数定義・関数型宣言部
        //   => 記憶域クラス指定子externを伴わないブロック有効範囲のオブジェクトを宣言する識別子 = ローカルスコープ中での変数宣言
        // 
        // - 翻訳単位の中で同じ識別子が内部結合と外部結合の両方で現れた場合，その動作は未定義とする。

        private static int _idCnt;

        private LinkageObject(string ident, CType type, LinkageKind linkage, bool unique = false) {
            Id = _idCnt++;
            Ident = ident;
            Type = type;
            Linkage = linkage;
            LinkageId = (Linkage == LinkageKind.ExternalLinkage) ? $"_{ident}" : (Linkage == LinkageKind.NoLinkage && unique) ? $"{ident}.{_idCnt}" : $"{ident}";
        }

        public static LinkageObject Create(Declaration declaration, LinkageKind linkage) {
            return new LinkageObject(declaration.Ident, declaration.Type, linkage);
        }

        public static LinkageObject CreateUnique(Declaration definition, LinkageKind linkage) {
            return new LinkageObject(definition.Ident, definition.Type, linkage,true);
        }

        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// 名前
        /// </summary>
        public string Ident { get; }

        /// <summary>
        /// 型
        /// </summary>
        public CType Type { get; }

        /// <summary>
        /// リンケージ種別
        /// </summary>
        public LinkageKind Linkage { get; }

        /// <summary>
        /// 仮宣言（仮宣言は複数できる）
        /// </summary>
        public List<Declaration> TentativeDefinitions { get; } = new List<Declaration>();

        /// <summary>
        /// 本宣言（本宣言はひとつだけ）
        /// </summary>
        public Declaration Definition { get; set; }

        /// <summary>
        /// リンケージを考慮した名前
        /// </summary>
        public string LinkageId { get; }

    }
}
