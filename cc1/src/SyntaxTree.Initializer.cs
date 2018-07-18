using System.Collections.Generic;
using AnsiCParser.DataType;

namespace AnsiCParser.SyntaxTree {

    /// <summary>
    /// 6.7.8 初期化(初期化子)
    /// </summary>
    public abstract class Initializer : Ast {
        // 制約
        // 初期化する実体に含まれないオブジェクトに初期化子で値を格納してはならない。
        // 初期化する実体の型は，大きさの分からない配列型であるか，又は可変長配列型以外のオブジェクト型でなければならない。
        // 静的記憶域期間をもつオブジェクトの初期化子の中のすべての式は定数式又は文字列リテラルでなければならない。
        // 識別子の宣言がブロック有効範囲をもち，かつ識別子が外部結合又は内部結合をもつ場合，その宣言にその識別子に対する初期化子があってはならない。
        // 要素指示子が [ 定数式 ] という形式の場合，現オブジェクト（この箇条で定義する。）は配列型をもち，式は整数定数式でなければならない。(C99の要素指示子)
        // 配列の大きさが分からない場合，式の値は任意の非負数であってよい。(C99の要素指示子)
        // 要素指示子が .識別子 という形式の場合，現オブジェクト（この箇条で定義する。）は構造体型又は共用体型をもち，識別子はその型のメンバ名でなければならない。(C99の要素指示子)
        //
        // 意味規則
        // 初期化子は，オブジェクトに格納する初期値を指定する。
        // この規格で明示的に異なる規定を行わない限り，この箇条では構造体型又は共用体型のオブジェクトの名前のないメンバを初期化の対象とはしない。
        // 構造体オブジェクトの名前のないメンバは，初期化後であっても不定の値をもつ。
        // 自動記憶域期間をもつオブジェクトを明示的に初期化しない場合，その値は不定とする。
        // 静的記憶域期間をもつオブジェクトを明示的に初期化しない場合，次の規定に従う。
        // a) そのオブジェクトの型がポインタ型の場合，空ポインタに初期化する。
        // b) そのオブジェクトの型が算術型の場合，（正又は符号無しの）0 に初期化する。
        // c) そのオブジェクトが集成体の場合，各メンバに a）～d）の規定を（再帰的に）適用し初期化する。
        // d) そのオブジェクトが共用体の場合，最初の名前付きメンバに a）～d）の規定を（再帰的に）適用し初期化する。
        // スカラオブジェクトに対する初期化子は，単一の式でなければならない。それを波括弧で囲んでもよい。
        // そのオブジェクトの初期値は（型変換後の）その式の値とする。型の制限及び型変換は，単純代入と同じとする。
        // このとき，宣言した型の非修飾版を，スカラオブジェクトの型とみなす
        //
        // この箇条のこれ以降では，集成体型又は共用体型のオブジェクトの初期化子を扱う。
        //
        // 自動記憶域期間をもつ構造体オブジェクト又は共用体オブジェクトに対する初期化子は，この箇条で規定する初期化子並び，又は適合する構造体型若しくは共用体型の単一の式のいずれかでなければならない。
        // 後者の場合，名前のないメンバも含めて，その式の値を，そのオブジェクトの初期値とする。
        // 文字型の配列は，単純文字列リテラルで初期化してもよい。それを波括弧で囲んでもよい。
        // 単純文字列リテラルの文字（空きがある場合又は配列の大きさが分からない場合，終端ナル文字も含めて。）がその配列の要素を前から順に初期化する。
        // wchar_t 型と適合する要素型の配列は，ワイド文字列リテラルで初期化してもよい。それを波括弧で囲んでもよい。
        // ワイド文字列リテラルのワイド文字（空きがある場合又は配列の大きさが分からない場合，終端ナルワイド文字も含めて。）がその配列の要素を前から順に初期化する。
        // これら以外の場合，集成体型又は共用体型をもつオブジェクトに対する初期化子は，要素又は名前付きメンバに対する初期化子並びを波括弧で囲んだものでなければならない。
        // 波括弧で囲まれたそれぞれの初期化子並びに結び付くオブジェクトを，現オブジェクト（current object）という。
        // 指示がない場合，現オブジェクト中の部分オブジェクトを，現オブジェクトの型に従う順序で初期化する。
        // すなわち，配列要素は添字の昇順で初期化し，構造体メンバは宣言の順で初期化し，共用体では最初の名前付きメンバを初期化する。
        // 一方，指示が存在する場合，それに続く初期化子を使って要素指示子が示す部分オブジェクトを初期化する。
        // そして要素指示子で示される部分オブジェクトの次の部分オブジェクトから順に初期化を続ける。
        // 各要素指示子並びは，それを囲む最も近い波括弧の対に結び付けられた現オブジェクトに対する記述でメンバを指定する。
        // 要素指示子並びの各項目は（順に）現オブジェクトの特定のメンバを指定し，次の要素指示子があれば現オブジェクトをそのメンバに変更する。
        // 一つの要素指示子並びを処理した後の現オブジェクトは，続く初期化子で初期化される部分オブジェクトとする。
        // 初期化は，初期化子並びの順に行う。
        // 特定の部分オブジェクトに対する初期化子が，同じ部分オブジェクトに対する以前の初期化子を書き換えることもある。
        // 明示的に初期化されないすべての部分オブジェクトについては，静的記憶域期間をもつオブジェクトと同じ規則で暗黙に初期化する。
        // 集成体又は共用体が集成体又は共用体の要素又はメンバを含む場合，これらの規則をその部分集成体又は含まれる共用体に再帰的に適用する。
        // 部分集成体又は含まれる共用体の初期化子が左波括弧で始まる場合，その波括弧と対応する右波括弧で囲まれた初期化子は，その部分集成体又は含まれる共用体の要素又はメンバを初期化する。
        // そうでない場合，部分集成体の要素若しくはメンバ又は含まれる共用体の最初のメンバに見合うに十分なだけ並びから初期化子が取られる。
        // 残りの初期化子は，その部分集成体又は含まれる共用体の外側の集成体の次の要素又はメンバの初期化のために残す。
        // 集成体型の要素又はメンバの個数より波括弧で囲まれた並びにある初期化子が少ない場合，又は大きさが既知の配列の要素数よりその配列を初期化するための文字列リテラル中の文字数が少ない場合，
        // その集成体型の残りを，静的記憶域期間をもつオブジェクトと同じ規則で暗黙に初期化する。
        // 大きさの分からない配列を初期化する場合，明示的な初期化子をもつ要素の添字の最大値でその大きさを決定する。
        // 初期化子並びの終了時点で，その配列はもはや不完全型をもたない。
        // 初期化子並びの式中で副作用の発生する順序は，未規定とする

        /// <summary>
        /// 6.7.8 初期化(代入式)
        /// </summary>
        public class SimpleInitializer : Initializer {
            public Expression AssignmentExpression {
                get;
            }

            public SimpleInitializer(LocationRange locationRange, Expression assignmentExpression) : base(locationRange) {
                AssignmentExpression = assignmentExpression;
            }
        }

        /// <summary>
        /// 6.7.8 初期化(初期化子並び)
        /// </summary>
        public class ComplexInitializer : Initializer {
            public List<Initializer> Ret {
                get;
            }

            public ComplexInitializer(LocationRange locationRange, List<Initializer> ret) : base(locationRange) {
                Ret = ret;
            }
        }

        // 以降は上の二つの初期化子並びをInitializerCheckerが解析した結果得られる具体的な初期化式

        /// <summary>
        /// 単純型に対する初期化式
        /// </summary>
        public class SimpleAssignInitializer : Initializer {
            public Expression Expr { get; }
            public CType Type { get; }

            public SimpleAssignInitializer(LocationRange locationRange, CType type, Expression expr) : base(locationRange) {
                Type = type;
                Expr = Specification.TypeConvert(type, expr);
            }
        }

        /// <summary>
        /// 配列型に対する初期化式
        /// </summary>
        public class ArrayAssignInitializer : Initializer {
            public List<Initializer> Inits { get; }
            public ArrayType Type { get; }

            public ArrayAssignInitializer(LocationRange locationRange, ArrayType type, List<Initializer> inits) : base(locationRange) {
                Type = type;
                Inits = inits;
            }
        }

        /// <summary>
        /// 構造体型/共用体型に対する初期化式
        /// </summary>
        public class StructUnionAssignInitializer : Initializer {
            public List<Initializer> Inits { get; }
            public TaggedType.StructUnionType Type { get; }

            public StructUnionAssignInitializer(LocationRange locationRange, TaggedType.StructUnionType type, List<Initializer> inits) : base(locationRange) {
                Type = type;
                Inits = inits;
            }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="locationRange"></param>
        protected Initializer(LocationRange locationRange) : base(locationRange) {
        }
    }
}
