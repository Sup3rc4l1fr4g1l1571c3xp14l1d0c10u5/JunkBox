using System;
using System.Linq;
using System.Collections.Generic;
using System.Dynamic;
using System.Security.Cryptography;
using AnsiCParser.DataType;
using System.CodeDom;

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

        /// <summary>
        /// 6.7.8 初期化子（指示初期化子）
        /// </summary>
        public class DesignatedInitializer : Initializer {

            /// <summary>
            /// 指示列
            /// </summary>
            public List<Designator> DesignatorParts { get; }

            /// <summary>
            /// 初期化式
            /// </summary>
            public Initializer InitializerExpression { get; }

            public DesignatedInitializer(LocationRange locationRange, List<Designator> designatorParts, Initializer initializerExpression) : base(locationRange) {
                DesignatorParts = designatorParts;
                InitializerExpression = initializerExpression;
            }
        }


        // 以降は上の三つの初期化子並びをInitializerCheckerが解析して生成する
        // 初期化式用の中間語で使うやつ
        // SimpleInitializerはそのまま使う。初期化子並びと指示初期化子はバラす。

        /// <summary>
        /// 具体的な初期子
        /// </summary>
        public class ConcreteInitializer : Initializer {
            /// <summary>
            /// 初期化式の型
            /// </summary>
            public CType Type { get; }

            /// <summary>
            /// この初期化子の生成本になった初期化子
            /// </summary>
            public Initializer OriginalInitializer { get; }

            /// <summary>
            /// 初期化子から生成された初期化コマンド列
            /// </summary>
            public InitializeCommand[] InitializeCommands { get; }

            public ConcreteInitializer(LocationRange locationRange, CType type, Initializer originalInitializer, InitializeCommand[] initializeCommands) : base(locationRange) {
                Type = type;
                OriginalInitializer = originalInitializer;
                InitializeCommands = initializeCommands;
            }
        }

        /// <summary>
        /// 指示
        /// </summary>
        public abstract class Designator {

            /// <summary>
            /// 添え字指示
            /// </summary>
            public class IndexDesignator : Designator {
                //public Expression IndexExpression { get; }
                public int Index { get; }

                public IndexDesignator(Expression indexExpression, int index) {
                    //IndexExpression = indexExpression;
                    Index = index;
                }
                public override string ToString() {
                    return $"[{Index}]";
                }
            }

            /// <summary>
            /// メンバー指示
            /// </summary>
            public class MemberDesignator : Designator {
                public string Member { get; }

                public MemberDesignator(string member) {
                    Member = member;
                }
                public override string ToString() {
                    return $".{Member}";
                }
            }
        }

        /// <summary>
        /// '{'に相当する要素
        /// </summary>
        public class EnterInitializer : Initializer {
            public EnterInitializer(LocationRange locationRange) : base(locationRange) { }
        }

        /// <summary>
        /// '}'に相当する要素
        /// </summary>
        public class LeaveInitializer : Initializer {

            public LeaveInitializer(LocationRange locationRange) : base(locationRange) { }
        }

        /// <summary>
        /// 指示に相当する要素
        /// </summary>
        public class DesignatorInitializer : Initializer {
            /// <summary>
            /// 指示子列
            /// </summary>
            public List<Designator> Path { get; }
            public DesignatorInitializer(LocationRange locationRange, List<Designator> path) : base(locationRange) {
                Path = path;
            }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="locationRange"></param>
        protected Initializer(LocationRange locationRange) : base(locationRange) {
        }
    }


    /// <summary>
    /// 初期化式に対する順方向イテレータ
    /// </summary>
    public class InitializerIterator {
        private List<Initializer> _expr { get; }
        private int _index { get; set; }

        /// <summary>
        /// 初期化式をフラットな式の形に変形する
        /// </summary>
        /// <param name="ret"></param>
        /// <param name="expr"></param>
        private void flatten(List<Initializer> ret, Initializer expr) {
            if (expr is Initializer.ComplexInitializer) {
                ret.Add(new Initializer.EnterInitializer(expr.LocationRange));
                foreach (var item in ((Initializer.ComplexInitializer)expr).Ret) {
                    this.flatten(ret, item);
                }
                ret.Add(new Initializer.LeaveInitializer(expr.LocationRange));
            } else if (expr is Initializer.DesignatedInitializer) {
                ret.Add(new Initializer.DesignatorInitializer(expr.LocationRange, ((Initializer.DesignatedInitializer)expr).DesignatorParts));
                this.flatten(ret, ((Initializer.DesignatedInitializer)expr).InitializerExpression);
            } else if (expr is Initializer.SimpleInitializer) {
                ret.Add(expr);
            } else {
                throw new CompilerException.InternalErrorException(expr.LocationRange, "初期化式の解析に不正な型が渡されました。おそらく処理系の誤りです。");
            }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="expr"></param>
        public InitializerIterator(Initializer expr) {
            this._expr = new List<Initializer>();
            this._index = 0;
            this.flatten(this._expr, expr);
        }

        /// <summary>
        /// イテレータが差し示す初期化式を得る
        /// </summary>
        public Initializer current {
            get {
                if (0 <= this._index && this._index < this._expr.Count) {
                    return this._expr[this._index];
                } else {
                    return null;
                }
            }
        }

        /// <summary>
        /// 次の初期化式に移動する
        /// </summary>
        /// <returns></returns>
        public bool next() {
            if (0 <= this._index && this._index < this._expr.Count) {
                this._index += 1;
                return true;
            } else {
                return false;
            }
        }

        /// <summary>
        /// 現在地を含めて現在地から最も近いInitializer.LeaveInitializerまで読み飛ばす。
        /// </summary>
        public void leave() {
            for (var p = this.current; (p = this.current) != null; this.next()) {
                if (p is Initializer.LeaveInitializer) {
                    this.next();
                    return;
                }
            }
        }

        /// <summary>
        /// 初期化子並びの最初の初期化子で初期化子並びを置き換える。（スカラ型に対して初期化子並びが使われている場合に使う）
        /// </summary>
        /// <returns>置き換えによって外された初期化子のリスト</returns>
        public List<Initializer> truncate_to_simple() {
            if (!(this.current is Initializer.EnterInitializer)) {
                throw new CompilerException.InternalErrorException(this.current.LocationRange, "初期化式の解析中に想定外の型が出現しました。おそらく処理系の誤りです。");
            }

            var initializers = new List<Initializer>();

            // 初期化子並びの入れ子を考慮しながら初期化子を列挙
            var nest = 0;
            for (var i = this._index;  i < this._expr.Count; i++) {
                var initializer = this._expr[i];
                if (initializer is Initializer.EnterInitializer) {
                    // 入れ子になっている初期化子並びが開始しているので中に入る
                    nest++;
                } else if (initializer is Initializer.LeaveInitializer) {
                    // 初期化子並びが終わっている
                    if (nest == 0) {
                        // 入れ子の対応が取れていないため、エラーを出力する
                        throw new CompilerException.SyntaxErrorException(this.current.LocationRange, "空の初期化式が存在します。");
                    } else {
                        nest--;
                        if (nest == 0) {
                            // 最初の初期化子並びの終端に出会ったので列挙終了
                            this._expr.RemoveRange(this._index, i - this._index + 1);
                            this._expr.Insert(this._index, initializers.First());
                            initializers.RemoveAt(0);
                            return initializers;
                        }
                    }
                } else {
                    // 初期化子が見つかった。
                    initializers.Add(initializer);
                }
            }
            throw new CompilerException.InternalErrorException(this.current.LocationRange, "初期化式の解析中でEnterとLeaveの数が一致しません。おそらく処理系の誤りです。");
        }
    }

    /// <summary>
    /// 型に対する順方向イテレータ
    /// </summary>
    public class TyNav {
        public class Context {
            public List<Tuple<CType, int, int>> _stack;
            public CType _current;
            public int _index;
            public int _fakepush;
        }


        public List<Tuple<CType, int, int>> _stack;
        public CType _current;
        public int _index;
        public int _fakepush;

        public TyNav(CType ty) {
            this._stack = new List<Tuple<CType, int, int>>();
            this._current = ty;
            this._index = -1;
            this._fakepush = 0;
        }

        /// <summary>
        /// メンバ式を構成する節
        /// </summary>
        public class PathPart {
            public CType ParentType { get; }
            public int Index { get; }


            public PathPart(CType parentType, int index) {
                ParentType = parentType;
                Index = index;
            }
        }

        public PathPart[] getPath() {
            return this._stack.Skip(1).Select((x) => new PathPart(x.Item1, x.Item2)).Concat(new[] { new PathPart(this._current, this._index) }).ToArray();
        }

        public Context getStack() {
            return new Context() {
                _stack = this._stack.ToList(),
                _current = this._current,
                _index = this._index,
                _fakepush = this._fakepush
            };
        }
        public void setStack(Context path) {
            this._stack = path._stack.ToList();
            this._current = path._current;
            this._index = path._index;
            this._fakepush = path._fakepush;
        }
        public CType current {
            get {
                if (this._index == -1) { return this._current; }
                TaggedType.StructUnionType suType;
                if (this._current.IsStructureType(out suType) || this._current.IsUnionType(out suType)) {
                    if (suType.Members.Count > this._index) {
                        return suType.Members[this._index].Type;
                    } else {
                        return null;
                    }
                }
                CType elementType;
                int len;
                if (this._current.IsArrayType(out elementType, out len)) {
                    if (len == -1 || len > this._index) {
                        return elementType;
                    } else {
                        return null;
                    }
                }
                return null;
            }
        }
        public CType parent {
            get {
                if (this._stack.Count == 0) {
                    return null;
                }
                var t1 = this._stack[this._stack.Count - 1];
                var p = t1.Item1;
                var i = t1.Item2;
                if (i == -1) {
                    return p;
                }

                TaggedType.StructUnionType suType;
                CType et;
                if (p.IsStructureType(out suType)) {
                    return suType.Members[i].Type;
                } else if (p.IsUnionType(out suType)) {
                    return suType.Members[i].Type;
                } else if (p.IsArrayType(out et)) {
                    return et;

                } else {
                    return null;
                }
            }
        }
        public bool isInMember() {
            var parent = this.parent;
            if (parent == null) {
                return false;
            }

            TaggedType.StructUnionType suType;
            CType et;
            int len;
            if (parent.IsStructureType(out suType)) {
                if (suType.Members.Count > this._index) {
                    return true;
                } else {
                    return false;
                }
            } else if (parent.IsUnionType()) {
                return this._index == 0;
            } else if (parent.IsArrayType(out et, out len)) {
                if (len == -1) {
                    return true;
                } else if (len > this._index) {
                    return true;
                } else {
                    return false;
                }
            } else {
                throw new CompilerException.InternalErrorException(LocationRange.Empty, "isInMember");
            }
        }
        public bool next() {
            var parent = this.parent;
            if (parent == null) {
                return false;
            }
            TaggedType.StructUnionType suType;
            CType et;
            int len;
            if (parent.IsStructureType(out suType)) {
                for (; ; ) {
                    if (suType.Members.Count <= this._index) {
                        return false;
                    }
                    if (this._index + 1 < suType.Members.Count && suType.Members[this._index+1].Ident == null) {
                        if (suType.Members[this._index + 1].Type.IsBitField()) {
                            this._index++;
                            continue;
                        } else {
                            Logger.Warning(Location.Empty, "匿名構造体/共用体を初期化しています。");
                        }
                    }
                    this._index++;
                    return true;

                }
            } else if (parent.IsUnionType(out suType)) {
                if (this._index == 0) {
                    this._index = suType.Members.Count;
                    return true;
                } else {
                    return false;
                }
            } else if (parent.IsArrayType(out et, out len)) {
                if (len == -1) {
                    this._index++;
                    return true;
                } else if (len > this._index + 1) {
                    this._index++;
                    return true;
                } else if (len == this._index + 1) {
                    this._index++;
                    return false;
                } else {
                    return false;
                }

            } else {
                throw new CompilerException.InternalErrorException(LocationRange.Empty, "next");
            }
        }

        public void enterFake() {
            this._fakepush += 1;
        }
        public bool isArrayType() {
            return this.current?.IsArrayType() == true;
        }
        public bool isInArrayType() {
            return this.parent?.IsArrayType() == true;
        }
        public void enterArray() {
            if (this.isArrayType() == false) { throw new CompilerException.InternalErrorException(LocationRange.Empty, "current is not array type"); }
            var cur = this.current.Unwrap() as ArrayType;
            this._stack.Add(Tuple.Create(this._current, this._index, this._fakepush));
            this._current = cur;
            this._index = 0;
            this._fakepush = 0;
        }

        /// <summary>
        /// カーソルを今指示している配列型の指定した添え字のメンバに移動する
        /// </summary>
        /// <param name="index"></param>
        public void selectIndex(int index) {
            CType et;
            int len;

            if (this.parent != null && this.parent.IsArrayType(out et, out len) == true) {
                if (len == -1) {
                    // 不完全配列型の場合
                    if (getPath().Length > 1) {
                        // 不完全配列型が許されるのは、もっとも外側の構造体のメンバのみ
                        throw new Exception("variable array legvel"); 
                    }
                } else {
                    if (len <= index) { throw new Exception("outof index"); }
                }
            } else {
                throw new CompilerException.InternalErrorException(LocationRange.Empty, "parent is not array type");
            }

            this._index = index;
        }

        /// <summary>
        /// 現在カーソルが指し示す要素は共用体か？
        /// </summary>
        /// <returns></returns>
        public bool isUnionType() {
            return this.current?.IsUnionType() == true;
        }

        /// <summary>
        /// 現在カーソルは共用体中にあるか？
        /// </summary>
        /// <returns></returns>
        public bool isInUnionType() {
            return this.parent?.IsUnionType() == true;
        }

        /// <summary>
        /// カーソルを今指示している共用体型の最初のメンバに移動する
        /// </summary>
        public void enterUnion() {
            if (this.isUnionType() == false) { throw new CompilerException.InternalErrorException(LocationRange.Empty, "current is not union type"); }
            var cur = this.current.Unwrap() as TaggedType.StructUnionType;
            this._stack.Add(Tuple.Create(this._current, this._index, this._fakepush));
            this._current = cur;
            this._index = 0;
            this._fakepush = 0;
        }

        /// <summary>
        /// 現在カーソルが指し示す要素は構造体か？
        /// </summary>
        /// <returns></returns>
        public bool isStructType() {
            return this.current?.IsStructureType() == true;
        }

        /// <summary>
        /// 現在カーソルは構造体中にあるか？
        /// </summary>
        /// <returns></returns>
        public bool isInStructType() {
            return this.parent?.IsStructureType() == true;
        }

        /// <summary>
        /// カーソルを今指示している構造体型の最初のメンバに移動する
        /// </summary>
        public void enterStruct() {
            if (this.isStructType() == false) { throw new CompilerException.InternalErrorException(LocationRange.Empty, "current is not struct type"); }
            var cur = this.current.Unwrap() as TaggedType.StructUnionType;
            this._stack.Add(Tuple.Create(this._current, this._index, this._fakepush));
            this._current = cur;
            this._index = 0;
            this._fakepush = 0;
        }

        /// <summary>
        /// カーソルを今いる構造体/共用体中の指定した名前と一致する兄弟メンバに移動する。
        /// </summary>
        /// <param name="member"></param>
        public void selectMember(string member) {
            TaggedType.StructUnionType suType;
            if (parent.IsStructureType(out suType) || parent.IsUnionType(out suType)) {
                this._index = suType.Members.FindIndex(x => x.Ident.Raw == member);
            } else {
                throw new CompilerException.InternalErrorException(LocationRange.Empty, "parent is not struct/union type");
            }
        }

        public void leave() {
            if (this._fakepush > 0) {
                this._fakepush -= 1;
                return;
            }
            var t = this._stack[this._stack.Count - 1];
            var p = t.Item1;
            var i = t.Item2;
            var f = t.Item3;
            this._stack.RemoveAt(this._stack.Count - 1);
            this._current = p;
            this._index = i;
            this._fakepush = f;
        }
    }


    public class InitializeCommand {
        public TyNav.PathPart[] path { get; set; }
        public Expression expr { get; set; }
        public override string ToString() {
            var lhs = Builder.path_to_string(path);
            var rhs = expr;
            return $"{lhs} = {rhs};";

        }

    }
    public static class Builder {
        public static Initializer.Designator path_to_designator(TyNav.PathPart path) {
            var p = path.ParentType;
            var i = path.Index;
            TaggedType.StructUnionType suType;
            if (p.IsStructureType(out suType) || p.IsUnionType(out suType)) {
                return new Initializer.Designator.MemberDesignator(suType.Members[i].Ident.Raw);
            } else if (p.IsArrayType()) {
                return new Initializer.Designator.IndexDesignator(
                    new Expression.PrimaryExpression.Constant.IntegerConstant(LocationRange.Empty, i.ToString(), i, BasicType.TypeKind.SignedInt),
                    i
                );
            } else {
                throw new CompilerException.InternalErrorException(LocationRange.Empty, "bad type");
            }

        }

        public static string path_to_string(IList<TyNav.PathPart> path) {
            return String.Concat(path.Select(path_to_designator).Select(x => x.ToString()));
        }

        public static InitializeCommand[] do_parse(TyNav ty, InitializerIterator expr, bool isLocalVariableInit) {
            List<InitializeCommand> ret = new List<InitializeCommand>();
            List<TyNav.Context> stack = new List<TyNav.Context>() { ty.getStack() };
            Stack<bool> exprEnter = new Stack<bool>();

            while (expr.current != null) {
                var currentExpr = expr.current;
                if (currentExpr == null) {
                    throw new Exception("current expr is null");
                }
                if (exprEnter.Any() && exprEnter.Peek() == false && ty.current == null) {
                    exprEnter.Pop();
                    ty.leave();
                    ty.next();
                    continue;
                }
                if (currentExpr is Initializer.DesignatorInitializer) {
                    var ce = currentExpr as Initializer.DesignatorInitializer;
                    ty.setStack(stack[stack.Count - 1]);
                    for (var i = 0; i < ce.Path.Count; i++) {
                        var p = ce.Path[i];
                        if (p is Initializer.DesignatorInitializer.Designator.IndexDesignator) {
                            var id = p as Initializer.DesignatorInitializer.Designator.IndexDesignator;
                            var cty = ty.parent;
                            if (cty == null) { throw new Exception("parent type is null"); }
                            CType elementType;
                            int len;
                            if (cty.IsArrayType(out elementType, out len) == false) {
                                throw new Exception("type mismatch");
                            }
                            if (len == -1) {
                                if (ty.getPath().Length > 1) { throw new Exception("variable array legvel"); }
                            } else {
                                if (len <= id.Index) { throw new Exception("outof index"); }
                            }
                            ty.selectIndex(id.Index);
                        } else if (p is Initializer.DesignatorInitializer.Designator.MemberDesignator) {
                            var md = p as Initializer.DesignatorInitializer.Designator.MemberDesignator;
                            var cty = ty.parent;
                            if (cty == null) { throw new Exception("parent type is null"); }
                            TaggedType.StructUnionType suType;
                            if (cty.IsStructureType(out suType) || cty.IsUnionType(out suType)) {
                                if (suType.Members.FindIndex(x => x.Ident.Raw == md.Member) == -1) {
                                    throw new Exception("no member");
                                }
                            } else {
                                throw new Exception("type mismatch");
                            }
                            ty.selectMember(md.Member);
                        } else {
                            throw new NotImplementedException();
                        }
                        if (i + 1 < ce.Path.Count) {
                            if (ty.isArrayType()) { ty.enterArray(); } else if (ty.isStructType()) { ty.enterStruct(); } else if (ty.isUnionType()) { ty.enterUnion(); } else {
                                throw new Exception("type mismatch");
                            }
                        }

                    }
                    expr.next();
                    continue;
                } else if (currentExpr is Initializer.LeaveInitializer) {
                    while (exprEnter.Any() && exprEnter.Peek() == false) {
                        ty.leave();
                        exprEnter.Pop();
                    }
                    if (ty.isInStructType()) {
                        ty.leave();
                        ty.next();
                    } else if (ty.isInUnionType()) {
                        ty.leave();
                        ty.next();
                    } else if (ty.isInArrayType()) {
                        ty.leave();
                        ty.next();
                    } else {
                        //console.log("mismatch2");
                        ty.leave();
                        ty.next();
                    }
                    expr.leave();
                    stack.RemoveAt(stack.Count - 1);
                    continue;
                } else if (ty.current == null && exprEnter.Peek() == false) {
                    while (ty.current == null) {
                        if (ty.parent != null && exprEnter.Peek() == false) {
                            ty.leave();
                            ty.next();
                            exprEnter.Pop();
                        } else {
                            break;
                        }
                    }
                    if (ty.parent == null) {
                        break;
                    }
                    continue;
                } else if (currentExpr is Initializer.EnterInitializer) {
                    if (ty.isStructType()) {
                        exprEnter.Push(true);
                        ty.enterStruct();
                        stack.Add(ty.getStack());
                        expr.next();
                    } else if (ty.isUnionType()) {
                        exprEnter.Push(true);
                        ty.enterUnion();
                        stack.Add(ty.getStack());
                        expr.next();
                    } else if (ty.isArrayType()) {
                        CType elementType;
                        int len;
                        System.Diagnostics.Debug.Assert(ty.current.IsArrayType(out elementType, out len));
                        if (len == -1 && ty.parent != null) { throw new Exception("variable array legvel"); }
                        exprEnter.Push(true);
                        ty.enterArray();
                        stack.Add(ty.getStack());
                        expr.next();
                    } else {
                        Logger.Warning(currentExpr.LocationRange, "too many braces around scalar initializer");
                        //ty.enterFake();
                        var skips = expr.truncate_to_simple();
                        foreach (var skip in skips) {
                            Logger.Warning(currentExpr.LocationRange, $"skip {skip.LocationRange}");
                        }
                    }
                    continue;
                } else {
                    if (ty.isStructType()) {
                        if (currentExpr is Initializer.SimpleInitializer && CType.IsEqual((currentExpr as Initializer.SimpleInitializer).AssignmentExpression.Type.Unwrap(), ty.current.Unwrap())) {
                            if (isLocalVariableInit == false && !((currentExpr as Initializer.SimpleInitializer).AssignmentExpression is Expression.PrimaryExpression.CompoundLiteralExpression)) {
                                throw new CompilerException.SpecificationErrorException(currentExpr.LocationRange, "集成体型又は共用体型をもつオブジェクトに対する初期化子は，要素又は名前付きメンバに対する初期化子並びを波括弧で囲んだものでなければならない。");
                            }
                            var r = new InitializeCommand() { path = ty.getPath(), expr = (currentExpr as Initializer.SimpleInitializer).AssignmentExpression };
                            ret.Add(r);
                            ty.next();
                            expr.next();
                        } else {
                            ty.enterStruct();
                            exprEnter.Push(false);
                        }
                        continue;
                    } else if (ty.isUnionType()) {
                        if (currentExpr is Initializer.SimpleInitializer && CType.IsEqual((currentExpr as Initializer.SimpleInitializer).AssignmentExpression.Type.Unwrap(), ty.current.Unwrap())) {
                            if (isLocalVariableInit == false) {
                                throw new CompilerException.SpecificationErrorException(currentExpr.LocationRange, "集成体型又は共用体型をもつオブジェクトに対する初期化子は，要素又は名前付きメンバに対する初期化子並びを波括弧で囲んだものでなければならない。");
                            }
                            ret.Add(new InitializeCommand() { path = ty.getPath(), expr = (currentExpr as Initializer.SimpleInitializer).AssignmentExpression });
                            ty.next();
                            expr.next();
                        } else {
                            ty.enterUnion();
                            exprEnter.Push(false);
                        }
                        continue;
                    } else if (ty.isArrayType()) {
                        var tyArray = ty.current.Unwrap() as ArrayType;
                        {
                            CType elementType;
                            int len;
                            System.Diagnostics.Debug.Assert(ty.current.IsArrayType(out elementType, out len));
                            if (len == -1 && ty.getPath().Length > 1) { throw new Exception("variable array legvel"); }
                        }
                        if (currentExpr is Initializer.SimpleInitializer) {
                            if ((currentExpr as Initializer.SimpleInitializer).AssignmentExpression is Expression.PrimaryExpression.StringExpression) {
                                var sexpr = (currentExpr as Initializer.SimpleInitializer).AssignmentExpression as Expression.PrimaryExpression.StringExpression;
                                if (sexpr.IsWide() == false) {
                                    if (tyArray.ElementType.IsCharacterType() == false) {
                                        throw new CompilerException.SpecificationErrorException(currentExpr.LocationRange, $"char型の文字列リテラルで{tyArray.ElementType.ToString()}型の配列は初期化できません。");
                                    }
                                    var len = 0;
                                    if (tyArray.Length == -1) {
                                        len = tyArray.Length = sexpr.Length;
                                    } else {
                                        len = Math.Min(tyArray.Length, sexpr.Length);
                                        if (tyArray.Length + 1 == sexpr.Length) {
                                            Logger.Warning(currentExpr.LocationRange, "末尾のヌル文字は切り捨てられます。これが意図した動作でない場合は修正を行ってください。");
                                        } else if (tyArray.Length < sexpr.Length) {
                                            Logger.Warning(currentExpr.LocationRange, "char配列の初期化文字列が長すぎます。");
                                        }
                                    }
                                    ty.enterArray();
                                    for (var i = 0; i < len; i++) {
                                        var ch = sexpr.GetValue(i);
                                        var chExpr = new Expression.PrimaryExpression.Constant.IntegerConstant(currentExpr.LocationRange, "", ch, BasicType.TypeKind.Char);
                                        var assign = Expression.AssignmentExpression.SimpleAssignmentExpression.ApplyAssignmentRule(currentExpr.LocationRange, tyArray.ElementType, chExpr);
                                        ret.Add(new InitializeCommand() { path = ty.getPath(), expr = chExpr });
                                        ty.next();
                                    }
                                    ty.leave();
                                    ty.next();
                                    expr.next();
                                } else {
                                    if (tyArray.ElementType.IsWideCharacterType() == false) {
                                        throw new CompilerException.SpecificationErrorException(currentExpr.LocationRange, $"wchar_t型の文字列リテラルで{tyArray.ElementType.ToString()}型の配列は初期化できません。");
                                    }
                                    var len = 0;
                                    if (tyArray.Length == -1) {
                                        len = tyArray.Length = sexpr.Length;
                                    } else {
                                        len = Math.Min(tyArray.Length, sexpr.Length);
                                        if (tyArray.Length + 1 == sexpr.Length) {
                                            Logger.Warning(currentExpr.LocationRange, "末尾のヌル文字は切り捨てられます。これが意図した動作でない場合は修正を行ってください。");
                                        } else if (tyArray.Length < sexpr.Length) {
                                            Logger.Warning(currentExpr.LocationRange, "wchar_t配列の初期化文字列が長すぎます。");
                                        }
                                    }
                                    ty.enterArray();
                                    for (var i = 0; i < len; i++) {
                                        var ch = sexpr.GetValue(i);
                                        var chExpr = new Expression.PrimaryExpression.Constant.IntegerConstant(currentExpr.LocationRange, "", ch, BasicType.TypeKind.SignedInt);
                                        var assign = Expression.AssignmentExpression.SimpleAssignmentExpression.ApplyAssignmentRule(currentExpr.LocationRange, tyArray.ElementType, chExpr);
                                        ret.Add(new InitializeCommand() { path = ty.getPath(), expr = chExpr });
                                        ty.next();
                                    }
                                    ty.leave();
                                    ty.next();
                                    expr.next();
                                }
                            } else {
                                ty.enterArray();
                                exprEnter.Push(false);
                                //throw new CompilerException.SpecificationErrorException(currentExpr.LocationRange, "配列初期化子は初期化子リストまたは文字列リテラルである必要があります");
                            }
                        } else {
                            ty.enterArray();
                            exprEnter.Push(false);
                        }
                        continue;
                    } else if (currentExpr is Initializer.SimpleInitializer) {
                        if (ty.current != null) {

                            var assign = Expression.AssignmentExpression.SimpleAssignmentExpression.ApplyAssignmentRule(currentExpr.LocationRange, ty.current, (currentExpr as Initializer.SimpleInitializer).AssignmentExpression);

                            ret.Add(new InitializeCommand() { path = ty.getPath(), expr = assign });
                            ty.next();
                        } else {
                            Logger.Warning(currentExpr.LocationRange, "初期化対象が存在しない初期化子です。無視されます。");
                        }
                        expr.next();
                        continue;
                    } else {
                        throw new Exception("来ないはず");
                    }

                }
            }
            return ret.ToArray();
        }

        private static InitializeCommand[] compaction(InitializeCommand[] inputs) {
            var outputs = new List<InitializeCommand>();

            foreach (var input in inputs) {
                for (var j = 0; j < outputs.Count; j++) {
                    var output = outputs[j];
                    var len = Math.Min(input.path.Length, output.path.Length);
                    for (var i = 0; i < len; i++) {
                        if (input.path[i].ParentType.IsArrayType() && CType.IsEqual(input.path[i].ParentType, output.path[i].ParentType) && input.path[i].Index == output.path[i].Index) {
                            if (i + 1 == len) {
                                goto skip;
                            } else {
                                continue;
                            }
                        }

                        if (!input.path[i].ParentType.IsUnionType() && !CType.IsEqual(input.path[i].ParentType, output.path[i].ParentType)) { break; }
                        if (!input.path[i].ParentType.IsUnionType() && CType.IsEqual(input.path[i].ParentType, output.path[i].ParentType) && input.path[i].Index != output.path[i].Index) { break; }
                        if (!input.path[i].ParentType.IsUnionType() && CType.IsEqual(input.path[i].ParentType, output.path[i].ParentType) && input.path[i].Index == output.path[i].Index) { continue; }
                        if (!input.path[i].ParentType.IsUnionType()) { throw new Exception("not reach"); }

                        if (input.path[i].ParentType.IsUnionType() && !CType.IsEqual(input.path[i].ParentType, output.path[i].ParentType)) { break; }
                        if (input.path[i].ParentType.IsUnionType() && CType.IsEqual(input.path[i].ParentType, output.path[i].ParentType) && input.path[i].Index != output.path[i].Index) { goto skip; }
                        if (input.path[i].ParentType.IsUnionType() && CType.IsEqual(input.path[i].ParentType, output.path[i].ParentType) && input.path[i].Index == output.path[i].Index) { continue; }
                        if (input.path[i].ParentType.IsUnionType()) { throw new Exception("not reach"); }
                    skip:
                        outputs.RemoveAt(j);
                        j--;
                        break;
                    }
                }
                outputs.Add(input);
            }
            return outputs.ToArray();
        }

        private static InitializeCommand[] sorting(InitializeCommand[] inputs) {
            var inp = inputs.ToList();
            inp.Sort((x, y) => {
                var last = (x.path.Length < y.path.Length) ? -1 : (x.path.Length > y.path.Length ? 1 : 0);
                var len = Math.Min(x.path.Length, y.path.Length);
                for (var i = 0; i < len; i++) {
                    if (x.path[i].Index < y.path[i].Index) { return -1; }
                    if (x.path[i].Index > y.path[i].Index) { return 1; }
                }
                return last;
            });
            return inp.ToArray();
        }

        public static InitializeCommand[] parsing(CType ty, Initializer expr, bool isLocalVariableInit) {
            if (expr != null) {
                var tyNav = new TyNav(ty);
                var exprIt = new InitializerIterator(expr);
                var ret = do_parse(tyNav, exprIt, isLocalVariableInit);
                if (exprIt.current != null) {
                    throw new Exception("excess elements in struct initializer");
                }
                ret = sorting(compaction(ret));
                if (ty.IsArrayType()) {
                    var at = ty.Unwrap() as ArrayType;
                    var last = ret.LastOrDefault()?.path.FirstOrDefault()?.Index;
                    if (at.Length == -1 && last != null) {
                        at.Length = last.Value+1;
                    } 
                }
                return ret;
            } else {
                return new InitializeCommand[0];
            }
        }
    }
}
