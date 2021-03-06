using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using AnsiCParser.DataType;

namespace AnsiCParser.SyntaxTree {
    /// <inheritdoc />
    /// <summary>
    /// 6.5 式 
    ///   - 式（expression）は，演算子及びオペランドの列とする。式は，値の計算を指定するか，オブジェクト若しくは関数を指し示すか，副作用を引き起こすか，又はそれらの組合せを行う。
    ///   - 直前の副作用完了点から次の副作用完了点までの間に，式の評価によって一つのオブジェクトに格納された値を変更する回数は，高々 1 回でなければならない。
    /// さらに，変更前の値の読取りは，格納される値を決定するためだけに行われなければならない。
    ///   - （関数呼出しの()，&&，||，?:及びコンマ演算子に対して）後で規定する場合を除いて，部分式の評価順序及び副作用が生じる順序は未規定とする。
    ///   - 幾つかの演算子［総称してビット単位の演算子（bitwise operator）と呼ぶ単項演算子~並びに 2 項演算子 &lt;&lt;，>>，&amp;，^及び|］は，整数型のオペランドを必要とする。
    /// これらの演算子は，整数の内部表現に依存した値を返すので，符号付き整数型に対して処理系定義又は未定義の側面をもつ。
    ///   - 式の評価中に例外条件（exceptional condition）が発生した場合（すなわち，結果が数学的に定義できないか，又は結果の型で表現可能な値の範囲にない場合），その動作は未定義とする。
    ///   - 格納された値にアクセスするときのオブジェクトの有効型（effective type）は，（もしあれば）そのオブジェクトの宣言された型とする。
    /// 宣言された型をもたないオブジェクトへ，文字型以外の型をもつ左辺値を通じて値を格納した場合，左辺値の型をそのアクセス及び格納された値を変更しないそれ以降のアクセスでのオブジェクトの有効型とする。
    /// 宣言された型をもたないオブジェクトに，memcpy 関数若しくは memmove関数を用いて値をコピーするか，又は文字型の配列として値をコピーした場合，
    /// そのアクセス及び値を変更しないそれ以降のアクセスでのオブジェクトの有効型は，値のコピー元となったオブジェクトの有効型があれば，その型とする。
    /// 宣言された型をもたないオブジェクトに対するその他のすべてのアクセスでは，そのアクセスでの左辺値の型を有効型とする。
    ///   - オブジェクトに格納された値に対するアクセスは，次のうちのいずれか一つの型をもつ左辺値によらなければならない
    /// - オブジェクトの有効型と適合する型
    /// - オブジェクトの有効型と適合する型の修飾版
    /// - オブジェクトの有効型に対応する符号付き型又は符号無し型
    /// - オブジェクトの有効型の修飾版に対応する符号付き型又は符号無し型
    /// - メンバの中に上に列挙した型の一つを含む集成体型又は共用体型（再帰的に包含されている部分集成体又は含まれる共用体のメンバを含む。）
    /// - 文字型
    ///   - 浮動小数点型の式は短縮（contract）してもよい。すなわち，ハードウェアによる不可分な操作として評価して，ソースコードの記述及び式の評価方法どおりなら生じるはずの丸め誤差を省いてもよい
    /// &lt;math.h%gt;の FP_CONTRACT プラグマは，式の短縮を禁止する方法を提供する。FP_CONTRACT プラグマがない場合，式が短縮されるかどうか，及びそれをどのように短縮するかは処理系定義とする。
    /// </summary>
    public abstract class Expression : Ast {

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="locationRange"></param>
        protected Expression(LocationRange locationRange) : base(locationRange) { }

        /// <summary>
        /// 式の結果の型
        /// </summary>
        public abstract CType Type {
            get;
        }

        /// <summary>
        /// 式の結果が左辺値と成りうるか
        /// </summary>
        public virtual bool IsLValue() {
            return false;
        }

        /// <summary>
        /// 式が register 記憶クラス指定子を含むか
        /// </summary>
        /// <returns></returns>
        public virtual bool HasStorageClassRegister() {
            return false;
        }

        /// <summary>
        /// 式が明示的な括弧でくくられているなら
        /// </summary>
        public bool EnclosedInParentheses { get; set; }

        /// <summary>
        /// 6.5.1 一次式
        /// </summary>
        public abstract class PrimaryExpression : Expression {
            protected PrimaryExpression(LocationRange locationRange) : base(locationRange) { }

            /// <summary>
            /// 識別子式
            /// </summary>
            /// <remarks>
            /// 識別子がオブジェクト（この場合，識別子は左辺値となる。），又は関数（この場合，関数指示子となる。）を指し示すと宣言されている場合，識別子は一次式とする。
            /// 宣言されていない識別子は構文規則違反である。（脚注：C89以降では宣言されていない識別子は構文規則違反であるとなっているが、K&amp;Rでは未定義識別子が許されちゃってるので文脈から変数/関数を判断する必要がある。）
            /// </remarks>
            public abstract class IdentifierExpression : PrimaryExpression {

                public string Ident {
                    get;
                }

                protected IdentifierExpression(LocationRange locationRange, string ident) : base(locationRange) {
                    Ident = ident;
                }

                /// <summary>
                /// 未定義識別子式
                /// </summary>
                public class UndefinedIdentifierExpression : IdentifierExpression {

                    public override CType Type {
                        get {
                            throw new InvalidOperationException();
                        }
                    }

                    public UndefinedIdentifierExpression(LocationRange locationRange, string ident) : base(locationRange, ident) {
                    }

                    public override bool IsLValue() {
                        throw new CompilerException.SpecificationErrorException(LocationRange.Start, LocationRange.End, "左辺値が必要な場所に未定義の識別子が登場しています。");
                    }
                }

                /// <summary>
                /// 変数識別子式
                /// </summary>
                public class VariableExpression : IdentifierExpression {
                    public Declaration.VariableDeclaration Decl {
                        get;
                    }
                    public override CType Type {
                        get {
                            return Decl.Type;
                        }
                    }
                    public override bool IsLValue() {
                        // 6.5.1 一次式
                        // 識別子がオブジェクト（この場合，識別子は左辺値となる。）
                        return true; // !Type.GetTypeQualifier().HasFlag(TypeQualifier.Const);
                    }

                    public override bool HasStorageClassRegister() {
                        return Decl.StorageClass == StorageClassSpecifier.Register;
                    }

                    public VariableExpression(LocationRange locationRange, string ident, Declaration.VariableDeclaration decl) : base(locationRange, ident) {
                        Decl = decl;
                    }
                }

                /// <summary>
                /// 引数識別子式
                /// </summary>
                public class ArgumentExpression : IdentifierExpression {
                    public Declaration.ArgumentDeclaration Decl {
                        get;
                    }
                    public override CType Type {
                        get {
                            return Decl.Type;
                        }
                    }
                    public override bool IsLValue() {
                        // 6.5.1 一次式
                        // 識別子がオブジェクト（この場合，識別子は左辺値となる。）
                        return !Type.GetTypeQualifier().HasFlag(TypeQualifier.Const);
                    }
                    public override bool HasStorageClassRegister() {
                        return Decl.StorageClass == StorageClassSpecifier.Register;
                    }

                    public ArgumentExpression(LocationRange locationRange, string ident, Declaration.ArgumentDeclaration decl) : base(locationRange, ident) {
                        Decl = decl;
                    }
                }

                /// <summary>
                /// 関数識別子式
                /// </summary>
                public class FunctionExpression : IdentifierExpression {
                    public Declaration.FunctionDeclaration Decl {
                        get;
                    }
                    public override CType Type {
                        get {
                            return Decl.Type;
                        }
                    }

                    public FunctionExpression(LocationRange locationRange, string ident, Declaration.FunctionDeclaration decl) : base(locationRange, ident) {
                        Decl = decl;
                    }
                }

                /// <summary>
                /// 列挙定数式
                /// </summary>
                public class EnumerationConstant : IdentifierExpression {
                    public TaggedType.EnumType.MemberInfo Info {
                        get;
                    }
                    public override CType Type {
                        get {
                            return CType.CreateSignedInt();
                        }
                    }

                    public EnumerationConstant(LocationRange locationRange, TaggedType.EnumType.MemberInfo info) : base(locationRange, info.Ident.Raw) {
                        Info = info;
                    }
                }

                /// <summary>
                /// @@@オブジェクト定数（C文法上には出現しない。コンパイラ作成の都合で導入）
                /// </summary>
                public class ObjectConstant : IdentifierExpression
                {
                    public override CType Type { get; }
                    public Expression Obj { get; }
                     
                    public ObjectConstant(LocationRange locationRange, CType type, string label, Expression obj) : base(locationRange, label) {
                        Type = type;
                        Obj = obj;
                    }
                }
            }

            /// <summary>
            /// 定数
            /// </summary>
            /// <remarks>
            /// 定数は，一次式とする。その型は，その形式と値によって決まる（6.4.4 で規定する。）
            /// </remarks>
            public abstract class Constant : Expression {
                /// <summary>
                /// 整数定数式
                /// </summary>
                public class IntegerConstant : Constant {

                    public string Str {
                        get;
                    }
                    public long Value {
                        get;
                    }

                    private CType ConstantType {
                        get;
                    }
                    public override CType Type {
                        get {
                            return ConstantType;
                        }
                    }

                    public IntegerConstant(LocationRange locationRange, string str, long value, BasicType.TypeKind kind) : base(locationRange) {
                        var ctype = BasicType.Create(kind);

                        int lowerBits = 8 * ctype.SizeOf();
                        Debug.Assert(lowerBits > 0);

                        int upperBits = (8 * sizeof(long)) - lowerBits;

                        if (upperBits > 0) {

                            // 符号拡張を実行
                            if (ctype.IsSignedIntegerType()) {
                                value = (value << upperBits) >> upperBits;
                            } else {
                                value = unchecked((long)((ulong)(value << upperBits) >> upperBits));
                            }
                        }

                        Str = str;
                        Value = value;
                        ConstantType = ctype;


                    }

                }

                /// <summary>
                /// 文字定数式
                /// </summary>
                public class CharacterConstant : Constant {
                    public string Str {
                        get;
                    }
                    public int Value {
                        get;
                    }

                    private CType ConstantType {
                        get;
                    }

                    public override CType Type {
                        get {
                            return ConstantType;
                        }
                    }

                    public CharacterConstant(LocationRange locationRange, string str, bool isWide) : base(locationRange) {
                        Str = str;
                        // 6.4.4.4 文字定数
                        // 意味規則  単純文字定数は，型 int をもつ。
                        ConstantType = CType.CreateSignedInt();

                        int[] i = { 1 };
                        int value = 0;
                        if (isWide) {
                            Lexer.CharIterator(locationRange.Start, () => str[i[0]], () => i[0]++, b => value = b[0], ch => Encoding.UTF32.GetBytes(new[] { (char)ch }));
                        } else {
                            Lexer.CharIterator(locationRange.Start, () => str[i[0]], () => i[0]++, b => value = b[0], ch => Encoding.GetEncoding(932).GetBytes(new[] { (char)ch }));
                        }
                        Value = (sbyte)value;
                    }

                }

                /// <summary>
                /// 浮動小数点定数式
                /// </summary>
                public class FloatingConstant : Constant {

                    public string Str {
                        get;
                    }

                    public double Value {
                        get;
                    }

                    private CType ConstantType {
                        get;
                    }

                    public override CType Type {
                        get {
                            return ConstantType;
                        }
                    }

                    public FloatingConstant(LocationRange locationRange, string str, double value, BasicType.TypeKind kind) : base(locationRange) {
                        ConstantType = BasicType.Create(kind);
                        Str = str;
                        Value = kind == BasicType.TypeKind.Float ? (float)value : value;
                    }
                }

                protected Constant(LocationRange locationRange) : base(locationRange) {
                }
            }

            /// <summary>
            /// 文字列リテラル式
            /// </summary>
            /// <remarks>
            /// 文字列リテラルは，一次式とする。それは，6.4.5 の規則で決まる型をもつ左辺値とする。
            /// </remarks>
            public class StringExpression : PrimaryExpression {



                public List<string> Strings {
                    get;
                }

                private List<byte> Value { get; }


                public bool IsWide() {
                    return ((ArrayType)ConstantType).ElementType.IsWideCharacterType();
                }
                public int Length { get { return ((ArrayType)ConstantType).Length; } }
                public int GetValue(int i) { 
                    if (IsWide()) {
                        return BitConverter.ToInt32(new[] { Value[i * 4 + 0], Value[i * 4 + 1] , Value[i * 4 + 2] , Value[i * 4 + 3] }, 0);
                    } else {
                        return Value[i];
                    }
                }

                public string Label { get; }

                private CType ConstantType {
                    get;
                }

                public override CType Type {
                    get {
                        return ConstantType;
                    }
                }

                public override bool IsLValue() {
                    // 文字列リテラルは，一次式とする。それは，6.4.5 の規則で決まる型をもつ左辺値とする。
                    return true;
                }

                public StringExpression(LocationRange locationRange, string label, List<string> strings, bool isWide) : base(locationRange) {
                    if (isWide == false) {

                        // ascii 
                        Label = label;
                        Value = new List<byte>();
                        var strParts = new List<string>();
                        var enc = Encoding.GetEncoding(932);
                        foreach (var str in strings) {
                            int[] i = { 1 };
                            while (str[i[0]] != '"') {
                                Lexer.CharIterator(locationRange.Start, () => str[i[0]], () => i[0]++, b => Value.AddRange(b), ch => { return enc.GetBytes(new[] { (char)ch });  });
                            }
                            strParts.Add(str.Substring(1, i[0] - 1));
                        }
                        Value.Add(0x00);

                        Strings = strParts;
                        ConstantType = CType.CreateArray(Value.Count, CType.CreateMultiByteChar());
                    } else {
                        // wide 
                        Label = label;
                        Value = new List<byte>();
                        var strParts = new List<string>();
                        var enc = Encoding.UTF32;
                        foreach (var str in strings) {
                            int[] i = { 2 };
                            while (str[i[0]] != '"') {
                                Lexer.CharIterator(locationRange.Start, () => str[i[0]], () => i[0]++, b => Value.AddRange(b), ch => { return enc.GetBytes(new[] { (char)ch }); });
                            }
                            strParts.Add(str.Substring(2, i[0] - 1));
                        }
                        Value.Add(0x00);
                        Value.Add(0x00);
                        Value.Add(0x00);
                        Value.Add(0x00);

                        Strings = strParts;
                        ConstantType = CType.CreateArray(Value.Count/4, CType.CreateWideChar());

                    }
                }

                public byte[] GetBytes() {
                    return Value.ToArray();
                }
            }

            ///// <summary>
            ///// 括弧で囲まれた式
            ///// </summary>
            ///// <remarks>
            ///// 括弧で囲まれた式は，一次式とする。その型及び値は，括弧の中の式のそれらと同じとする。
            ///// 括弧の中の式が左辺値，関数指示子又はボイド式である場合，それは，それぞれ左辺値，関数指示子又はボイド式とする。
            ///// </remarks>
            //public class EnclosedInParenthesesExpression : PrimaryExpression {
            //    public Expression ParenthesesExpression {
            //        get;
            //    }
            //    public override CType Type {
            //        get {
            //            return ParenthesesExpression.Type;
            //        }
            //    }
            //    public override bool IsLValue() {
            //        // 6.5.1 一次式
            //        // 括弧の中の式が左辺値である場合，それは，左辺値とする
            //        return ParenthesesExpression.IsLValue();
            //    }
            //    public override bool HasStorageClassRegister() {
            //        return ParenthesesExpression.HasStorageClassRegister();
            //    }

            //    public EnclosedInParenthesesExpression(LocationRange locationRange, Expression parenthesesExpression) : base(locationRange) {
            //        ParenthesesExpression = parenthesesExpression;
            //    }
            //}


            /// <summary>
            /// アドレス定数(定数式の解釈結果として得られる構文ノード)
            /// </summary>
            public class AddressConstantExpression : PrimaryExpression {

                public override CType Type { get; }
                public IdentifierExpression Identifier { get; }
                public Constant.IntegerConstant Offset { get; }

                public AddressConstantExpression(LocationRange locationRange, IdentifierExpression identifier, CType type, Constant.IntegerConstant offset) : base(locationRange) {
                    Identifier = identifier;
                    Type = type;
                    Offset = offset;
                }

            }


            /// <summary>
            /// (C99) 複合リテラル
            /// </summary>
            public class CompoundLiteralExpression : Expression {
                public override CType Type { get; }
                public Initializer OriginalInitializer { get; }
                public InitializeCommand[] InitializeCommands { get; }

                public CompoundLiteralExpression(LocationRange locationRange, CType type, Initializer originalInitializer, InitializeCommand[] initializeCommands) : base(locationRange) {
                    this.Type = type;
                    this.OriginalInitializer = originalInitializer;
                    this.InitializeCommands = initializeCommands;
                }
            }
        }

        /// <summary>
        /// 6.5.2 後置演算子式
        /// </summary>
        public abstract class PostfixExpression : Expression {
            /// <summary>
            /// 6.5.2.1 配列の添字付け
            /// </summary>
            public class ArraySubscriptingExpression : PostfixExpression {
                /// <summary>
                /// 型“オブジェクト型T型へのポインタ”（もしくは配列）の式
                /// </summary>
                public Expression Target {
                    get;
                }
                /// <summary>
                /// 添え字式（整数側）の式
                /// </summary>
                public Expression Index {
                    get;
                }
                /// <summary>
                /// 構文上での左辺側
                /// </summary>
                public Expression Lhs {
                    get;
                }
                /// <summary>
                /// 構文上での右辺側
                /// </summary>
                public Expression Rhs {
                    get;
                }

                private CType ReferencedType {
                    get;
                }

                public override CType Type {
                    get {
                        return ReferencedType;
                    }
                }
                public override bool IsLValue() {
                    return true; //!Type.GetTypeQualifier().HasFlag(TypeQualifier.Const);
                }
                public override bool HasStorageClassRegister() {
                    return Target.HasStorageClassRegister();
                }

                public ArraySubscriptingExpression(LocationRange locationRange, Expression lhs, Expression rhs) : base(locationRange) {
                    // 6.3 型変換

                    // 制約
                    //   式の一方は，型“オブジェクト型T型へのポインタ”をもたなければならない。
                    //   もう一方の式は，整数型をもたなければならない。
                    //   結果は，型“T型”をもつ。
                    // 
                    // 脚注 
                    //   C言語の特徴として有名な話だが「式の一方」とあるように、他の言語と違って配列式の要素を入れ替えても意味は変わらない。すなわち、x[1] と 1[x]は同じ意味。

                    CType referencedType;
                    if (((lhs.Type.IsPointerType(out referencedType) || lhs.Type.IsArrayType(out referencedType)) && referencedType.IsObjectType())
                        && (rhs.Type.IsIntegerType())) {
                        ReferencedType = referencedType;
                        lhs = Specification.ImplicitConversion(CType.CreatePointer(ReferencedType), lhs);
                        rhs = Specification.ImplicitConversion(CType.CreateSignedInt(), rhs);
                        Target = lhs;
                        Index = rhs;
                    } else if (((rhs.Type.IsPointerType(out referencedType) || rhs.Type.IsArrayType(out referencedType)) && referencedType.IsObjectType())
                               && (lhs.Type.IsIntegerType())) {
                        ReferencedType = referencedType;
                        lhs = Specification.ImplicitConversion(CType.CreateSignedInt(), lhs);
                        rhs = Specification.ImplicitConversion(CType.CreatePointer(ReferencedType), rhs);
                        Target = rhs;
                        Index = lhs;
                    } else {

                        throw new CompilerException.SpecificationErrorException(LocationRange.Start, LocationRange.End, "式の一方は，型“オブジェクト型へのポインタ”をもたなければならず、もう一方の式は，整数型をもたなければならない。");
                    }
                    Lhs = lhs;
                    Rhs = rhs;
                }
            }

            /// <summary>
            /// 6.5.2.2  関数呼出し
            /// </summary>
            public class FunctionCallExpression : PostfixExpression {
                public Expression Expr {
                    get;
                }
                public List<Expression> Args {
                    get;
                }
                private CType ResultType {
                    get;
                }

                public override CType Type {
                    get {
                        return ResultType;
                    }
                }

                public FunctionCallExpression(LocationRange locationRange, Expression expr, List<Expression> args) : base(locationRange) {
                    // 6.3 型変換 
                    // 関数呼出しの準備の段階で，実引数を評価し，各実引数の値を対応する仮引数に代入する
                    // 関数は，その仮引数の値を変更してもよいが，これらの変更が実引数の値に影響を与えることはできない。
                    // 一方，オブジェクトへのポインタを渡すことは可能であり，関数はそれによって指されるオブジェクトの値を変更してもよい。配列型又は関数型をもつと宣言された仮引数は，ポインタ型をもつように型調整される

                    expr = Specification.ImplicitConversion(null, expr);

                    // 制約
                    // 呼び出される関数を表す式は，void を返す関数へのポインタ型，又は配列型以外のオブジェクト型を返す関数へのポインタ型をもたなければならない。
                    CType referencedType = null;
                    FunctionType functionType = null;
                    if (expr.Type.IsPointerType(out referencedType) && referencedType.IsFunctionType(out functionType)) {
                        if (functionType.ResultType.IsVoidType() || (functionType.ResultType.IsObjectType() && !functionType.ResultType.IsArrayType())) {
                            goto Valid;
                        }
                    }
                    throw new CompilerException.TypeMissmatchError(LocationRange.Start, LocationRange.End, "呼び出される関数を表す式は，void を返す関数へのポインタ型，又は配列型以外のオブジェクト型を返す関数へのポインタ型をもたなければならない");
                    Valid:
                    if (functionType.Arguments != null) {
                        if (functionType.Arguments.Length == 1 && functionType.Arguments[0].Type.IsVoidType()) {
                            if (0 != args.Count) {
                                throw new CompilerException.SpecificationErrorException(LocationRange.Start, LocationRange.End, $"実引数の個数({args.Count}個)が，仮引数の個数(0個)と一致しない。");
                            }
                        } else {
                            // 呼び出される関数を表す式が関数原型を含む型をもつ場合，実引数の個数は，仮引数の個数と一致しなければならない。
                            if (functionType.HasVariadic) {
                                // 可変長引数を持つ
                                if (functionType.Arguments.Length > args.Count) {
                                    throw new CompilerException.SpecificationErrorException(LocationRange.Start, LocationRange.End, $"実引数の個数({args.Count}個)が，仮引数の個数({functionType.Arguments.Length}個)よりも少ない。");
                                }
                            } else {
                                if (functionType.Arguments.Length != args.Count) {
                                    throw new CompilerException.SpecificationErrorException(LocationRange.Start, LocationRange.End, $"実引数の個数({args.Count}個)が，仮引数の個数({functionType.Arguments.Length}個)と一致しない。");
                                }
                            }

                            // 関数が呼び出される又は定義されるときには完全型になっていなければならない。
                            // 各実引数は，対応する仮引数の型の非修飾版をもつオブジェクトにその値を代入することのできる型をもたなければならない。
                            for (var i = 0; i < functionType.Arguments.Length; i++) {
                                var targ = functionType.Arguments[i];
                                if (targ.Type.IsIncompleteType()) {
                                    throw new CompilerException.SpecificationErrorException(targ.Range, $"呼び出し先の関数の引数が不完全型をもっています。");
                                }

                                var lhs = targ.Type.UnwrapTypeQualifier();
                                var rhs = args[i];
                                args[i] = AssignmentExpression.SimpleAssignmentExpression.ApplyAssignmentRule(rhs.LocationRange, lhs, rhs);
                            }

                            if (functionType.HasVariadic) {
                                for (var i = functionType.Arguments.Length; i < args.Count; i++) {
                                    args[i] = TypeConversionExpression.Apply(args[i].LocationRange, args[i].Type.DefaultArgumentPromotion(), args[i]);
                                }
                            }
                        }
                    } else {
                        // 呼び出される関数を表す式が，関数原型を含まない型をもつ場合，各実引数に対して既定の実引数拡張を行う。
                        args = args.Select(x => (Expression)TypeConversionExpression.Apply(x.LocationRange, Specification.DefaultArgumentPromotion(x.Type), x)).ToList();
                    }
                    // 各実引数は，対応する仮引数の型の非修飾版をもつオブジェクトにその値を代入することのできる型をもたなければならない
                    ResultType = functionType.ResultType;
                    Expr = expr;
                    Args = args;
                }
            }

            /// <summary>
            /// 6.5.2.3 構造体及び共用体のメンバ(.演算子)
            /// </summary>
            public class MemberDirectAccess : PostfixExpression {
                public Expression Expr {
                    get;
                }
                public Token Ident {
                    get;
                }
                private CType MemberType {
                    get;
                }
                public TaggedType.StructUnionType.MemberInfo MemberInfo {
                    get;
                }

                public override bool IsLValue() {
                    return true;// /* !Expr.Type.GetTypeQualifier().HasFlag(TypeQualifier.Const) &&*/
            Expr.IsLValue();
                }
                public override bool HasStorageClassRegister() {
                    // 集成体又は共用体のオブジェクトが typedef 以外の記憶域クラス指定子を用いて宣言されたとき，結合に関するものを除いて，記憶域クラス指定子による性質をオブジェクトのメンバにも適用する。
                    // また再帰的に集成体又は共用体であるすべてのメンバオブジェクトに適用する。
                    return Expr.HasStorageClassRegister();
                }

                public override CType Type {
                    get {
                        return MemberType;
                    }
                }

                public MemberDirectAccess(LocationRange locationRange, Expression expr, Token ident) : base(locationRange) {
                    // 制約  
                    // .演算子の最初のオペランドは，構造体型又は共用体型の修飾版又は非修飾版をもたなければならず，2 番目のオペランドは，その型のメンバの名前でなければならない
                    TaggedType.StructUnionType sType;
                    if (!expr.Type.IsStructureType(out sType) && !expr.Type.IsUnionType(out sType)) {
                        throw new CompilerException.SpecificationErrorException(expr.LocationRange.Start, expr.LocationRange.End, ".演算子の最初のオペランドは，構造体型又は共用体型の修飾版又は非修飾版をもたなければならない。");
                    }
                    if (sType.Members == null) {
                        throw new CompilerException.SpecificationErrorException(expr.LocationRange.Start, expr.LocationRange.End, ".演算子の最初のオペランドの構造体/共用体が不完全型です。");
                    }
                    var memberInfo = sType.Members.FirstOrDefault(x => x.Ident != null && x.Ident.Raw == ident.Raw);
                    if (memberInfo == null) {
                        throw new CompilerException.SpecificationErrorException(ident.Start, ident.End, $".演算子の2番目のオペランドは，その型のメンバの名前でなければならない。(メンバ名{ident.Raw}が見つかりません)");
                    }

                    // 意味規則
                    // 演算子及び識別子を後ろに伴う後置式は，構造体又は共用体オブジェクトの一つのメンバを指し示す。
                    // その値は，指定されたメンバの値とする。
                    // 最初の式が左辺値の場合，その式は，左辺値とする。
                    // 最初の式が修飾型をもつ場合，結果の型は，指定されたメンバの型に同じ修飾を加えた型とする。
                    Expr = expr;
                    Ident = ident;
                    MemberInfo = memberInfo;

                    var qual = expr.Type.GetTypeQualifier();
                    if (qual != TypeQualifier.None) {
                        MemberType = memberInfo.Type.WrapTypeQualifier(qual);
                    } else {
                        MemberType = memberInfo.Type.UnwrapTypeQualifier();
                    }
                }
            }

            /// <summary>
            /// 6.5.2.3 構造体及び共用体のメンバ(->演算子)
            /// </summary>
            public class MemberIndirectAccess : PostfixExpression {
                public Expression Expr {
                    get;
                }
                public Token Ident {
                    get;
                }
                private CType MemberType {
                    get;
                }
                public TaggedType.StructUnionType.MemberInfo MemberInfo {
                    get;
                }

                public override bool IsLValue() {
                    // 6.5.2.3
                    // ->演算子及び識別子を後ろに伴う後置式は，構造体又は共用体オブジェクトの一つのメンバを指し示す。
                    // その値は，最初の式が指すオブジェクトの指定されたメンバの値とする。
                    // その式は左辺値とする。 <---- とあるので、その式は左辺値
                    return true;// !Expr.Type.GetTypeQualifier().HasFlag(TypeQualifier.Const);// && Expr.IsLValue();
                }

                public override CType Type {
                    get {
                        return MemberType;
                    }
                }

                public MemberIndirectAccess(LocationRange locationRange, Expression expr, Token ident) : base(locationRange) {
                    // 制約  
                    // ->演算子の最初のオペランドは，型“構造体の修飾版若しくは非修飾版へのポインタ”，又は型“共用体の修飾版若しくは非修飾版へのポインタ”をもたなければならず，2 番目のオペランドは，指される型のメンバの名前でなければならない
                    TaggedType.StructUnionType sType = null;
                    if (!(expr.Type.IsPointerType() && (expr.Type.GetBasePointerType().IsStructureType(out sType) || expr.Type.GetBasePointerType().IsUnionType(out sType)))) {
                        throw new CompilerException.SpecificationErrorException(expr.LocationRange.Start, expr.LocationRange.End, "->演算子の最初のオペランドは，型“構造体の修飾版若しくは非修飾版へのポインタ”，又は型“共用体の修飾版若しくは非修飾版へのポインタ”をもたなければならない。");
                    }
                    if (sType.Members == null) {
                        throw new CompilerException.SpecificationErrorException(expr.LocationRange.Start, expr.LocationRange.End, "->演算子の最初のオペランドの構造体/共用体が不完全型です。");
                    }
                    var memberInfo = sType.Members.FirstOrDefault(x => x.Ident != null && x.Ident.Raw == ident.Raw);
                    if (memberInfo == null) {
                        throw new CompilerException.SpecificationErrorException(ident.Start, ident.End, $"->演算子の2番目のオペランドは，その型のメンバの名前でなければならない。(メンバ名{ident.Raw}が見つかりません)");
                    }

                    // 意味規則
                    // ->演算子及び識別子を後ろに伴う後置式は，構造体又は共用体オブジェクトの一つのメンバを指し示す。
                    // 最初の式が指すオブジェクトの指定されたメンバの値とする。
                    // その式は左辺値とする。
                    // 最初の式の型が修飾型へのポインタである場合，結果の型は，指定されたメンバの型に同じ修飾を加えた型とする。
                    Expr = expr;
                    Ident = ident;
                    MemberInfo = memberInfo;

                    var qual = expr.Type.GetBasePointerType().GetTypeQualifier();
                    MemberType = memberInfo.Type.UnwrapTypeQualifier().WrapTypeQualifier(qual);
                }
            }

            /// <summary>
            /// 6.5.2.4 後置増分及び後置減分演算子
            /// </summary>
            public class UnaryPostfixExpression : PostfixExpression {
                public enum OperatorKind {
                    None, Inc, Dec
                }
                public OperatorKind Op {
                    get;
                }

                public Expression Expr {
                    get;
                }

                public override CType Type {
                    get {
                        return Expr.Type;
                    }
                }

                public override bool IsLValue() {
                    return Expr.IsLValue();
                }

                public override bool HasStorageClassRegister() {
                    return Expr.HasStorageClassRegister();
                }

                public UnaryPostfixExpression(LocationRange locationRange, OperatorKind op, Expression expr) : base(locationRange) {
                    // 制約  
                    // 後置増分演算子又は後置減分演算子のオペランドは，実数型又はポインタ型の修飾版又は非修飾版 をもたなければならず，
                    // 変更可能な左辺値でなければならない。
                    if (!expr.IsLValue()) {
                        throw new CompilerException.SpecificationErrorException(expr.LocationRange.Start, expr.LocationRange.End, "変更可能な左辺値でなければならない。");
                    }

                    if (!(expr.Type.IsRealType() || expr.Type.IsPointerType())) {
                        throw new CompilerException.SpecificationErrorException(expr.LocationRange.Start, expr.LocationRange.End, "後置増分演算子又は後置減分演算子のオペランドは，実数型又はポインタ型の修飾版又は非修飾版 をもたなければならない。");
                    }

                    // 意味規則  
                    // 後置++演算子の結果は，そのオペランドの値とする。
                    // 結果を取り出した後，オペランドの値を増分する（すなわち，適切な型の値 1 をそれに加える。 ） 。
                    // 制約，型，並びにポインタに対する型変換及び 演算の効果については，加減演算子及び複合代入の規定のとおりとする。
                    // ToDo: とあるので、加減演算子及び複合代入の規定をコピーしてくること
                    Op = op;
                    Expr = expr;
                    // TypeConversionExpression.Apply(expr.Type, Specification.TypeConvert(expr.Type, expr));

                }

            }

            // Todo: C99の複合リテラル式はここに入る
            protected PostfixExpression(LocationRange locationRange) : base(locationRange) {
            }
        }

        /// <summary>
        /// 6.5.3.1 前置増分及び前置減分演算子
        /// </summary>
        public class UnaryPrefixExpression : Expression {
            public enum OperatorKind {
                None, Inc, Dec
            }
            public OperatorKind Op {
                get;
            }
            public Expression Expr {
                get;
            }
            public override CType Type {
                get {
                    return Expr.Type;
                }
            }

            public override bool IsLValue() {
                return Expr.IsLValue();
            }
            public override bool HasStorageClassRegister() {
                return Expr.HasStorageClassRegister();
            }

            public UnaryPrefixExpression(LocationRange locationRange, OperatorKind op, Expression expr) : base(locationRange) {
                // 制約 
                // 前置増分演算子又は前置減分演算子のオペランドは，実数型又はポインタ型の修飾版又は非修飾版をもたなければならず，
                // 変更可能な左辺値でなければならない。    
                if (!(expr.Type.IsRealType() || expr.Type.IsPointerType())) {
                    throw new CompilerException.SpecificationErrorException(expr.LocationRange.Start, expr.LocationRange.End, "前置増分演算子又は前置減分演算子のオペランドは，実数型又はポインタ型の修飾版又は非修飾版をもたなければならない。");
                }
                // ToDo: 変更可能な左辺値でなければならない。    

                // 意味規則
                // 制約，型，副作用，並びにポインタに対する型変換及び演算の効果については，加減演算子及び複合代入の規定のとおりとする。
                // ToDo: とあるので、加減演算子及び複合代入の規定をコピーしてくること
                Op = op;
                Expr = expr;    // TypeConversionExpression.Apply(expr.Type, Specification.ImplicitConversion(expr.Type, expr));
            }
        }

        /// <summary>
        /// 6.5.3.2 アドレス及び間接演算子(アドレス演算子)
        /// </summary>
        public class UnaryAddressExpression : Expression {
            public Expression Expr {
                get;
            }
            private CType ResultType {
                get;
            }
            public override CType Type {
                get {
                    return ResultType;
                }
            }

            public override bool IsLValue() {
                return Expr.IsLValue();
            }


            public UnaryAddressExpression(LocationRange locationRange, Expression expr) : base(locationRange) {
                // 制約  
                // 単項&演算子のオペランドは，関数指示子，[]演算子若しくは単項*演算子の結果，又は左辺値でなければならない。
                // 左辺値の場合，ビットフィールドでもなく，register 記憶域クラス指定子付きで宣言されてもいないオブジェクトを指し示さなければならない。
                if (
                    (expr is PrimaryExpression.IdentifierExpression.FunctionExpression) // オペランドは，関数指示子
                    || (expr is PostfixExpression.ArraySubscriptingExpression) // オペランドは，[]演算子(ToDo:の結果にすること)
                    || (expr is UnaryReferenceExpression) // オペランドは，単項*演算子(ToDo:の結果にすること)
                ) {
                    // ok
                } else if (
                    expr.IsLValue() &&  // オペランドは，左辺値
                    (!(  // ビットフィールドでない
                            (expr is PostfixExpression.MemberDirectAccess && ((PostfixExpression.MemberDirectAccess)expr).Type.IsBitField()) ||
                            (expr is PostfixExpression.MemberIndirectAccess && ((PostfixExpression.MemberIndirectAccess)expr).Type.IsBitField())
                        )) &&
                    !expr.HasStorageClassRegister()// register 記憶域クラス指定子付きで宣言されてもいないオブジェクト
                ) {

                } else {
                    throw new CompilerException.SpecificationErrorException(expr.LocationRange.Start, expr.LocationRange.End, "単項&演算子のオペランドは，関数指示子，[]演算子若しくは単項*演算子の結果，又は左辺値でなければならない。左辺値の場合，ビットフィールドでもなく，register 記憶域クラス指定子付きで宣言されてもいないオブジェクトを指し示さなければならない。");
                }


                // 意味規則  
                // 単項 &演算子は，そのオペランドのアドレスを返す。
                // オペランドが型“～型”をもっている場合，結果は，型“～型へのポインタ”をもつ。
                // オペランドが，単項*演算子の結果の場合，*演算子も&演算子も評価せず，両演算子とも取り除いた場合と同じ結果となる。
                // ただし，その場合でも演算子に対する制約を適用し，結果は左辺値とならない。
                // 同様に，オペランドが[]演算子の結果の場合，単項&演算子と，[]演算子が暗黙に意味する単項*演算子は評価されず，&演算子を削除し[]演算子を+演算子に変更した場合と同じ結果となる。
                // これら以外の場合，結果はそのオペランドが指し示すオブジェクト又は関数へのポインタとなる。

                if (expr is UnaryReferenceExpression) {
                    // オペランドが，単項*演算子の結果の場合，*演算子も&演算子も評価せず，両演算子とも取り除いた場合と同じ結果となる。
                    // ToDo: ただし，その場合でも演算子に対する制約を適用し，結果は左辺値とならない。
                    expr = ((UnaryReferenceExpression)expr).Expr;
                    Expr = expr;
                    ResultType = expr.Type;
                } else if (expr is PostfixExpression.ArraySubscriptingExpression) {
                    // 同様に，オペランドが[]演算子の結果の場合，単項&演算子と，[]演算子が暗黙に意味する単項*演算子は評価されず，
                    // &演算子を削除し[]演算子を+演算子に変更した場合と同じ結果となる。
                    var aexpr = (PostfixExpression.ArraySubscriptingExpression)expr;
                    expr =
                        new AdditiveExpression(
                            locationRange,
                            AdditiveExpression.OperatorKind.Add,
                            TypeConversionExpression.Apply(aexpr.Target.LocationRange, aexpr.Target.Type, aexpr.Target),
                            Specification.ImplicitConversion(CType.CreateSignedInt(), aexpr.Index)
                        );
                    Expr = expr;
                    ResultType = expr.Type;
                } else {
                    // これら以外の場合，結果はそのオペランドが指し示すオブジェクト又は関数へのポインタとなる
                    Expr = expr;
                    ResultType = CType.CreatePointer(expr.Type);
                    //Console.Error.WriteLine($"&:{locationRange.ToString()}: ExprType={expr.Type.ToString()}, ResultType={ResultType.ToString()}");
                }
            }
        }

        /// <summary>
        /// 6.5.3.2 アドレス及び間接演算子(間接演算子)
        /// </summary>
        public class UnaryReferenceExpression : Expression {
            public Expression Expr {
                get;
            }
            private CType ResultType {
                get;
            }
            public override CType Type {
                get {
                    return ResultType;
                }
            }

            public override bool IsLValue() {
                return true;
                //Expr.IsLValue();
            }

            public UnaryReferenceExpression(LocationRange locationRange, Expression expr) : base(locationRange) {
                // 暗黙の型変換
                expr = Specification.ImplicitConversion(null, expr);

                // 制約
                // 単項*演算子のオペランドは，ポインタ型をもたなければならない。
                if (!expr.Type.IsPointerType()) {
                    throw new CompilerException.SpecificationErrorException(expr.LocationRange.Start, expr.LocationRange.End, "ポインタ型の式以外に単項参照演算子を適用しようとした。（左辺値型とか配列型とか色々見なければいけない部分は未実装。）");
                }

                // 意味規則
                // 単項*演算子は，間接参照を表す。
                // オペランドが関数を指している場合，その結果は関数指示子とする。
                // オペランドがオブジェクトを指している場合，その結果はそのオブジェクトを指し示す左辺値とする。
                // オペランドが型“～型へのポインタ”をもつ場合，その結果は型“～型”をもつ。
                // 正しくない値がポインタに代入されている場合，単項*演算子の動作は，未定義とする
                Expr = expr;
                CType bt;
                if (expr.Type.IsPointerType(out bt) && bt.IsFunctionType()) {
                    //Console.Error.WriteLine($"expr is pointer of function");
                    ResultType = expr.Type;
                } else {
                    ResultType = expr.Type.GetBasePointerType();
                }
                //Console.Error.WriteLine($"*:{locationRange.ToString()}: ExprType={expr.Type.ToString()}, ResultType={ResultType.ToString()}");
            }
        }

        /// <summary>
        /// 6.5.3.3 単項算術演算子(単項+演算子)
        /// </summary>
        public class UnaryPlusExpression : Expression {
            public Expression Expr {
                get;
            }
            public override CType Type {
                get {
                    return Expr.Type;
                }
            }

            public UnaryPlusExpression(LocationRange locationRange, Expression expr) : base(locationRange) {
                // 制約 
                // 単項+演算子のオペランドは，算術型をもたなければならない。
                if (!expr.Type.IsArithmeticType()) {
                    throw new CompilerException.SpecificationErrorException(expr.LocationRange.Start, expr.LocationRange.End, "単項+演算子のオペランドは，算術型をもたなければならない。");
                }

                // 意味規則 
                // 単項 +演算子の結果は，その（拡張された）オペランドの値とする。
                // オペランドに対して整数拡張を行い，その結果は，拡張された型をもつ。
                Expr = Specification.IntegerPromotion(expr);
            }
        }

        /// <summary>
        /// 6.5.3.3 単項算術演算子(単項-演算子)
        /// </summary>
        public class UnaryMinusExpression : Expression {
            public Expression Expr {
                get;
            }
            public override CType Type {
                get {
                    return Expr.Type;
                }
            }

            public UnaryMinusExpression(LocationRange locationRange, Expression expr) : base(locationRange) {
                // 制約 
                // 単項-演算子のオペランドは，算術型をもたなければならない。
                if (!expr.Type.IsArithmeticType()) {
                    throw new CompilerException.SpecificationErrorException(expr.LocationRange.Start, expr.LocationRange.End, "単項-演算子のオペランドは，算術型をもたなければならない。");
                }

                // 意味規則 
                // 単項-演算子の結果は，その（拡張された）オペランドの符号を反転した値とする。
                // オペランドに対して整数拡張を行い，その結果は，拡張された型をもつ。
                Expr = Specification.IntegerPromotion(expr);
            }
        }

        /// <summary>
        /// 6.5.3.3 単項算術演算子(~演算子)
        /// </summary>
        public class UnaryNegateExpression : Expression {
            public Expression Expr {
                get;
            }
            public override CType Type {
                get {
                    return Expr.Type;
                }
            }

            public UnaryNegateExpression(LocationRange locationRange, Expression expr) : base(locationRange) {
                // 制約 
                // ~演算子のオペランドは，整数型をもたなければならない。
                if (!expr.Type.IsIntegerType()) {
                    throw new CompilerException.SpecificationErrorException(expr.LocationRange.Start, expr.LocationRange.End, "~演算子のオペランドは，整数型をもたなければならない。");
                }
                if (!expr.Type.IsUnsignedIntegerType()) {
                    Logger.Warning(expr.LocationRange, "単項演算子~のオペランドに符号付き整数型が使われていますが、この演算子は，整数の内部表現に依存した値を返すので，符号付き整数型に対して処理系定義又は未定義の側面をもつことになります。");
                }
                // 意味規則 
                // ~演算子の結果は，その（拡張された）オペランドのビット単位の補数とする（すなわち，結果の各ビットは，拡張されたオペランドの対応するビットがセットされていない場合，そしてその場合に限り，セットされる。）。
                // オペランドに対して整数拡張を行い，その結果は，拡張された型をもつ。
                // 拡張された型が符号無し整数型である場合，~E はその型で表現可能な最大値から E を減算した値と等価とする。
                Expr = Specification.IntegerPromotion(expr);
            }
        }

        /// <summary>
        /// 6.5.3.3 単項算術演算子(論理否定演算子!)
        /// </summary>
        public class UnaryNotExpression : Expression {
            public Expression Expr {
                get;
            }
            public override CType Type {
                get {
                    return CType.CreateSignedInt();
                }
            }

            public UnaryNotExpression(LocationRange locationRange, Expression expr) : base(locationRange) {
                // 制約
                // !演算子のオペランドは，スカラ型をもたなければならない。
                if (!expr.Type.IsScalarType()) {
                    throw new CompilerException.SpecificationErrorException(expr.LocationRange.Start, expr.LocationRange.End, "!演算子のオペランドは，スカラ型をもたなければならない。");
                }

                // 意味規則 
                // 論理否定演算子!の結果は，そのオペランドの値が 0 と比較して等しくない場合 0 とし，等しい場合 1 とする。
                // 結果の型は，int とする。式!E は，(0 == E)と等価とする。
                Expr = Specification.TypeConvert(CType.CreateBool(), expr);
            }
        }

        /// <summary>
        /// 6.5.3.4 sizeof演算子(型を対象)
        /// </summary>
        public class SizeofTypeExpression : Expression {
            public CType TypeOperand {
                get;
            }
            public override CType Type {
                get {
                    return CType.CreateSizeT();
                }
            }

            public SizeofTypeExpression(LocationRange locationRange, CType operand) : base(locationRange) {
                // 制約
                // sizeof 演算子は，関数型若しくは不完全型をもつ式，それらの型の名前を括弧で囲んだもの，
                // 又はビットフィールドメンバを指し示す式に対して適用してはならない。
                if (operand.IsIncompleteType() || operand.IsFunctionType() || operand.IsBitField()) {
                    throw new CompilerException.SpecificationErrorException(locationRange.Start, locationRange.End, "sizeof 演算子は，関数型若しくは不完全型をもつ式，それらの型の名前を括弧で囲んだもの，又はビットフィールドメンバを指し示す式に対して適用してはならない。");
                }
                TypeOperand = operand;
            }
        }

        /// <summary>
        /// 6.5.3.4 sizeof演算子(式を対象)
        /// </summary>
        public class SizeofExpression : Expression {
            public Expression ExprOperand {
                get;
            }
            public override CType Type {
                get {
                    return CType.CreateSizeT();
                }
            }

            public SizeofExpression(LocationRange locationRange, Expression expr) : base(locationRange) {
                if (expr.Type.IsIncompleteType() || expr.Type.IsFunctionType()) {
                    throw new CompilerException.SpecificationErrorException(expr.LocationRange.Start, expr.LocationRange.End, "sizeof 演算子は，関数型若しくは不完全型をもつ式，それらの型の名前を括弧で囲んだもの，又はビットフィールドメンバを指し示す式に対して適用してはならない。");
                }
                // ToDo: ビットフィールドメンバを示す式のチェック
                ExprOperand = expr;
            }
        }


        /// <summary>
        /// (c11) alignof演算子
        /// </summary>
        public class AlignofExpression : Expression {
            public CType TypeOperand {
                get;
            }
            public override CType Type {
                get {
                    return CType.CreateSizeT();
                }
            }

            public AlignofExpression(LocationRange locationRange, CType operand) : base(locationRange) {
                // 制約
                // alignof 演算子は，関数型若しくは不完全型に対して適用してはならない。
                // 配列型に対して使った場合は配列要素の型に対して使ったものとして考える。
                if (operand.IsIncompleteType() || operand.IsFunctionType()) {
                    throw new CompilerException.SpecificationErrorException(locationRange.Start, locationRange.End, "alignof 演算子は，関数型若しくは不完全型に対して適用してはならない。");
                }
                TypeOperand = operand;
            }
        }

        /// <summary>
        /// 6.5.4 キャスト演算子(キャスト式)
        /// </summary>
        public class CastExpression : TypeConversionExpression {
            public CastExpression(LocationRange locationRange, CType type, Expression expr) : base(locationRange, type, expr) {
                // 制約 
                // 型名が void 型を指定する場合を除いて，型名はスカラ型の修飾版又は非修飾版を指定しなければならず，オペランドは，スカラ型をもたなければならない。

                if (type.IsVoidType()) {
                    // 型名が void 型を指定する場合なのでOK
                    return;
                }

                if (!type.IsScalarType()) {
                    //if (type.IsBoolType()) {
                    //    // _Bool型にはキャスト可能である。
                    //} else {
                    // 型名がスカラ型の修飾版又は非修飾版を指定していない。
                    throw new CompilerException.SpecificationErrorException(locationRange.Start, locationRange.End, "型名が void 型を指定する場合を除いて，型名はスカラ型の修飾版又は非修飾版を指定しなければならない。");
                    // GCC拡張なら可能
                    // 
                    //}
                }

                if (!expr.Type.IsScalarType()) {
                    // オペランドが，スカラ型をもっていない。
                    throw new CompilerException.SpecificationErrorException(expr.LocationRange.Start, expr.LocationRange.End, "型名が void 型を指定する場合を除いて，オペランドは，スカラ型をもたなければならない。");
                }

            }
            public override bool HasStorageClassRegister() {
                return Expr.HasStorageClassRegister();
            }
        }


        /// <summary>
        /// 6.5.5 乗除演算子(乗除式)
        /// </summary>
        public class MultiplicativeExpression : Expression {
            public enum OperatorKind {
                Mul, Div, Mod
            }
            public OperatorKind Op {
                get;
            }
            public Expression Lhs {
                get;
            }
            public Expression Rhs {
                get;
            }
            private CType ResultType {
                get;
            }
            public override CType Type {
                get {
                    return ResultType;
                }
            }

            public MultiplicativeExpression(OperatorKind op, Expression lhs, Expression rhs) : base(new LocationRange(lhs.LocationRange.Start, rhs.LocationRange.End)) {
                // 制約 
                // 各オペランドは，算術型をもたなければならない。
                // %演算子のオペランドは，整数型をもたなければならない
                if (op == OperatorKind.Mod) {
                    if (!lhs.Type.IsIntegerType()) {
                        throw new CompilerException.SpecificationErrorException(lhs.LocationRange.Start, lhs.LocationRange.End, "%演算子のオペランドは，整数型をもたなければならない。");
                    }
                    if (!rhs.Type.IsIntegerType()) {
                        throw new CompilerException.SpecificationErrorException(rhs.LocationRange.Start, rhs.LocationRange.End, "%演算子のオペランドは，整数型をもたなければならない。");
                    }
                } else {
                    if (!lhs.Type.IsArithmeticType()) {
                        throw new CompilerException.SpecificationErrorException(lhs.LocationRange.Start, lhs.LocationRange.End, "各オペランドは，算術型をもたなければならない。");
                    }
                    if (!rhs.Type.IsArithmeticType()) {
                        throw new CompilerException.SpecificationErrorException(rhs.LocationRange.Start, rhs.LocationRange.End, "各オペランドは，算術型をもたなければならない。");
                    }
                }
                // 意味規則  
                // 通常の算術型変換をオペランドに適用する。
                ResultType = Specification.UsualArithmeticConversion(ref lhs, ref rhs, Specification.UsualArithmeticConversionOperator.MulDiv);

                Op = op;
                Lhs = lhs;
                Rhs = rhs;
            }
        }

        /// <summary>
        /// 6.5.6 加減演算子(加減式)
        /// </summary>
        public class AdditiveExpression : Expression {
            public enum OperatorKind {
                None, Add, Sub
            }
            public OperatorKind Op {
                get;
            }
            public Expression Lhs {
                get;
            }
            public Expression Rhs {
                get;
            }
            private CType ResultType {
                get;
            }
            public override CType Type {
                get {
                    return ResultType;
                }
            }

            public AdditiveExpression(LocationRange locationRange, OperatorKind op, Expression lhs, Expression rhs) : base(locationRange) {
                // 制約  
                // 加算の場合，両オペランドが算術型をもつか，又は一方のオペランドがオブジェクト型へのポインタで，もう一方のオペランドの型が整数型でなければならない。
                // 減算の場合，次のいずれかの条件を満たさなければならない
                // - 両オペランドが算術型をもつ。 
                // - 両オペランドが適合するオブジェクト型の修飾版又は非修飾版へのポインタである。
                // - 左オペランドがオブジェクト型へのポインタで，右オペランドの型が整数型である。（減分は 1 の減算に等しい。）
                //
                // 意味規則  
                // 両オペランドが算術型をもつ場合，通常の算術型変換をそれらに適用する。
                // 2項 + 演算子の結果は，両オペランドの和とする。
                // 2項 - 演算子の結果は，第 1 オペランドから第 2 オペランドを引いた結果の差とする。
                // これらの演算子に関しては，配列の要素でないオブジェクトへのポインタは，要素型としてそのオブジェクトの型をもつ長さ 1 の配列の最初の要素へのポインタと同じ動作をする。
                // 整数型をもつ式をポインタに加算又はポインタから減算する場合，結果は，ポインタオペランドの型をもつ。
                // 二つのポインタを減算する場合，その両方のポインタは同じ配列オブジェクトの要素か，その配列オブジェクトの最後の要素を一つ越えたところを指していなければならない。
                // その結果は，二つの配列要素の添字の差とする。
                // 結果の大きさは処理系定義とし，その型（符号付き整数型）は，ヘッダ<stddef.h>で定義される ptrdiff_t とする。
                if (op == OperatorKind.Add) {
                    if (lhs.Type.IsArithmeticType() && rhs.Type.IsArithmeticType()) {
                        // 両オペランドが算術型をもつ
                        // 意味規則 両オペランドが算術型をもつ場合，通常の算術型変換をそれらに適用する。
                        ResultType = Specification.UsualArithmeticConversion(ref lhs, ref rhs, Specification.UsualArithmeticConversionOperator.AddSub);
                    } else {
                        // ポインタ型への暗黙的型変換を試みる
                        var lhsPtr = Specification.ToPointerTypeExpr(lhs);
                        var rhsPtr = Specification.ToPointerTypeExpr(rhs);

                        // 一方のオペランドがオブジェクト型へのポインタで，もう一方のオペランドの型が整数型。
                        if (lhsPtr != null && lhsPtr.Type.IsPointerType() && lhsPtr.Type.GetBasePointerType().IsObjectType() && rhs.Type.IsIntegerType()) {
                            lhs = lhsPtr;
                            // 意味規則 整数型をもつ式をポインタに加算又はポインタから減算する場合，結果は，ポインタオペランドの型をもつ。
                            ResultType = lhs.Type;
                            rhs = TypeConversionExpression.Apply(locationRange, CType.CreatePtrDiffT(), rhs);//*
                        } else if (rhsPtr != null && lhs.Type.IsIntegerType() && rhsPtr.Type.IsPointerType() && rhsPtr.Type.GetBasePointerType().IsObjectType()) {
                            rhs = rhsPtr;
                            // 意味規則 整数型をもつ式をポインタに加算又はポインタから減算する場合，結果は，ポインタオペランドの型をもつ。
                            ResultType = rhs.Type;
                            lhs = TypeConversionExpression.Apply(locationRange, CType.CreatePtrDiffT(), lhs);//*
                        } else {
                            throw new CompilerException.SpecificationErrorException(locationRange.Start, locationRange.End, "加算の場合，両オペランドが算術型をもつか，又は一方のオペランドがオブジェクト型へのポインタで，もう一方のオペランドの型が整数型でなければならない。");
                        }
                    }

                } else {
                    if (lhs.Type.IsArithmeticType() && rhs.Type.IsArithmeticType()) {
                        // 両オペランドが算術型をもつ
                        // 意味規則 両オペランドが算術型をもつ場合，通常の算術型変換をそれらに適用する。
                        ResultType = Specification.UsualArithmeticConversion(ref lhs, ref rhs, Specification.UsualArithmeticConversionOperator.AddSub);
                    } else {
                        // ポインタ型への暗黙的型変換を試みる
                        var lhsPtr = Specification.ToPointerTypeExpr(lhs);
                        var rhsPtr = Specification.ToPointerTypeExpr(rhs);

                        if (
                            lhsPtr != null && lhsPtr.Type.IsPointerType()
                                           && rhsPtr != null && rhsPtr.Type.IsPointerType()
                                           && CType.IsEqual(lhsPtr.Type.GetBasePointerType().Unwrap(), rhsPtr.Type.GetBasePointerType().Unwrap())
                        ) {
                            // 両オペランドが適合するオブジェクト型の修飾版又は非修飾版へのポインタ。
                            // 意味規則 二つのポインタを減算する場合(中略)，その型（符号付き整数型）は，ヘッダ<stddef.h>で定義される ptrdiff_t とする。
                            lhs = lhsPtr;
                            rhs = rhsPtr;

                            ResultType = CType.CreatePtrDiffT();
                        } else if (
                            lhsPtr != null && lhsPtr.Type.IsPointerType() && lhsPtr.Type.GetBasePointerType().IsObjectType() && rhs.Type.IsIntegerType()
                        ) {
                            // 左オペランドがオブジェクト型へのポインタで，右オペランドの型が整数型である。（減分は 1 の減算に等しい。）
                            // 意味規則 整数型をもつ式をポインタに加算又はポインタから減算する場合，結果は，ポインタオペランドの型をもつ。
                            lhs = lhsPtr;
                            ResultType = lhs.Type;
                            rhs = TypeConversionExpression.Apply(locationRange, CType.CreatePtrDiffT(), rhs);   //*
                        } else {
                            throw new CompilerException.SpecificationErrorException(locationRange.Start, locationRange.End, "両オペランドがどちらも算術型もしくは適合するオブジェクト型の修飾版又は非修飾版へのポインタ、または、左オペランドがオブジェクト型へのポインタで，右オペランドの型が整数型、でなければならない。");
                        }
                    }
                }
                Op = op;
                Lhs = lhs;
                Rhs = rhs;
            }
        }

        /// <summary>
        /// 6.5.7 ビット単位のシフト演算子(シフト式)
        /// </summary>
        public class ShiftExpression : Expression {
            public enum OperatorKind {
                None, Left, Right
            }
            public OperatorKind Op {
                get;
            }

            public Expression Lhs {
                get;
            }
            public Expression Rhs {
                get;
            }
            public override CType Type {
                get {
                    return Lhs.Type;
                }
            }

            public ShiftExpression(LocationRange locationRange, OperatorKind op, Expression lhs, Expression rhs) : base(locationRange) {
                // 制約  
                // 各オペランドは，整数型をもたなければならない。
                if (!lhs.Type.IsIntegerType()) {
                    throw new CompilerException.SpecificationErrorException(lhs.LocationRange.Start, lhs.LocationRange.End, "各オペランドは，整数型をもたなければならない。");
                }
                if (!rhs.Type.IsIntegerType()) {
                    throw new CompilerException.SpecificationErrorException(rhs.LocationRange.Start, rhs.LocationRange.End, "各オペランドは，整数型をもたなければならない。");
                }
                // 意味規則 
                // 整数拡張を各オペランドに適用する。
                // 結果の型は，左オペランドを拡張した後の型とする。
                // 右オペランドの値が負であるか，又は拡張した左オペランドの幅以上の場合，その動作は，未定義とする。

                if (!lhs.Type.IsUnsignedIntegerType()) {
                    Logger.Warning(lhs.LocationRange, "ビットシフト演算子の左オペランドに符号付き整数型が使われていますが、この演算子は，整数の内部表現に依存した値を返すので，符号付き整数型に対して処理系定義又は未定義の側面をもつことになります。");
                }

                lhs = Specification.IntegerPromotion(lhs);
                rhs = Specification.IntegerPromotion(rhs);
                Op = op;
                Lhs = lhs;
                Rhs = rhs;
            }
        }

        /// <summary>
        /// 6.5.8 関係演算子(関係式)
        /// </summary>
        public class RelationalExpression : Expression {
            public enum OperatorKind {
                None, GreaterThan, LessThan, GreaterOrEqual, LessOrEqual
            }
            public OperatorKind Op {
                get;
            }
            public Expression Lhs {
                get;
            }
            public Expression Rhs {
                get;
            }
            public override CType Type {
                get {
                    return CType.CreateSignedInt();
                }
            }

            public RelationalExpression(LocationRange locationRange, OperatorKind op, Expression lhs, Expression rhs) : base(locationRange) {
                // 制約  
                // 次のいずれかの条件を満たさなければならない。 
                // - 両オペランドが実数型をもつ。 
                // - 両オペランドが適合するオブジェクト型の修飾版又は非修飾版へのポインタである。
                // - 両オペランドが適合する不完全型の修飾版又は非修飾版へのポインタである。

                if (lhs.Type.IsRealType() && rhs.Type.IsRealType()) {
                    // 両オペランドが実数型をもつ。 
                } else if (lhs.Type.IsPointerType() && rhs.Type.IsPointerType() && lhs.Type.GetBasePointerType().IsObjectType() && rhs.Type.GetBasePointerType().IsObjectType() && CType.IsEqual(lhs.Type.GetBasePointerType(), rhs.Type.GetBasePointerType())) {
                    // - 両オペランドが適合するオブジェクト型の修飾版又は非修飾版へのポインタである。
                } else if (lhs.Type.IsPointerType() && rhs.Type.IsPointerType() && lhs.Type.GetBasePointerType().IsIncompleteType() && rhs.Type.GetBasePointerType().IsIncompleteType() && CType.IsEqual(lhs.Type.GetBasePointerType(), rhs.Type.GetBasePointerType())) {
                    // - 両オペランドが適合する不完全型の修飾版又は非修飾版へのポインタである。
                } else {
                    throw new CompilerException.SpecificationErrorException(locationRange.Start, locationRange.End, "関係演算子は両オペランドが実数型をもつ、もしくは、両オペランドが適合するオブジェクト型の修飾版又は非修飾版へのポインタでなければならない。");
                }
                // 意味規則  
                // 両オペランドが算術型をもつ場合，通常の算術型変換を適用する。
                // 関係演算子に関しては，配列の要素でないオブジェクトへのポインタは，要素型としてそのオブジェクトの型をもつ長さ 1 の配列の最初の要素へのポインタと同じ動作をする。
                // <（小さい），>（大きい），<=（以下）及び>=（以上）の各演算子は，指定された関係が真の場合は 1を，偽の場合は 0 を返す。その結果は，型 int をもつ。

                if (lhs.Type.IsArithmeticType() && rhs.Type.IsArithmeticType()) {
                    // 両オペランドが算術型をもつ場合，通常の算術型変換を適用する。
                    Specification.UsualArithmeticConversion(ref lhs, ref rhs);
                }
                Op = op;
                Lhs = lhs;
                Rhs = rhs;
            }
        }

        /// <summary>
        /// 6.5.9 等価演算子(等価式)
        /// </summary>
        public class EqualityExpression : Expression {
            public enum OperatorKind {
                None, Equal, NotEqual
            }
            public OperatorKind Op {
                get;
            }
            public Expression Lhs {
                get;
            }
            public Expression Rhs {
                get;
            }
            public override CType Type {
                get {
                    return CType.CreateBool();
                }
            }

            public EqualityExpression(LocationRange locationRange, OperatorKind op, Expression lhs, Expression rhs) : base(locationRange) {
                // 制約
                // 次のいずれかの条件を満たさなければならない。
                // - 両オペランドは算術型をもつ。
                // - 両オペランドとも適合する型の修飾版又は非修飾版へのポインタである。
                // - 一方のオペランドがオブジェクト型又は不完全型へのポインタで他方が void の修飾版又は非修飾版へのポインタである。
                // - 一方のオペランドがポインタで他方が空ポインタ定数である。

                if (lhs.Type.IsArithmeticType() && rhs.Type.IsArithmeticType()) {
                    // 両オペランドが算術型をもつ場合，通常の算術型変換を適用する。
                    Specification.UsualArithmeticConversion(ref lhs, ref rhs);
                } else {
                    // ポインタ型への暗黙的型変換を試みる
                    var lhsPtr = Specification.ToPointerTypeExpr(lhs);
                    var rhsPtr = Specification.ToPointerTypeExpr(rhs);
                    if (
                        lhsPtr != null && lhsPtr.Type.IsPointerType()
                                       && rhsPtr != null && rhsPtr.Type.IsPointerType()
                                       && CType.IsEqual(lhsPtr.Type.GetBasePointerType().Unwrap(), rhsPtr.Type.GetBasePointerType().Unwrap())) {
                        // 両オペランドとも適合する型の修飾版又は非修飾版へのポインタである。
                        lhs = lhsPtr;
                        rhs = rhsPtr;
                    } else if (
                        (lhsPtr != null && rhsPtr != null)
                        && (
                            (lhsPtr.Type.IsPointerType() && (lhsPtr.Type.GetBasePointerType().IsObjectType() || lhsPtr.Type.GetBasePointerType().IsIncompleteType()) && (rhsPtr.Type.IsPointerType() && rhsPtr.Type.GetBasePointerType().IsVoidType())) ||
                            (rhsPtr.Type.IsPointerType() && (rhsPtr.Type.GetBasePointerType().IsObjectType() || rhsPtr.Type.GetBasePointerType().IsIncompleteType()) && (lhsPtr.Type.IsPointerType() && lhsPtr.Type.GetBasePointerType().IsVoidType()))
                        )
                    ) {
                        // 一方のオペランドがオブジェクト型又は不完全型へのポインタで他方が void の修飾版又は非修飾版へのポインタである。
                        lhs = lhsPtr;
                        rhs = rhsPtr;
                    } else if (lhsPtr != null && lhsPtr.Type.IsPointerType() && rhs.IsNullPointerConstant()) {
                        // 左辺のオペランドがポインタで右辺が空ポインタ定数である。
                        lhs = lhsPtr;
                        rhs = TypeConversionExpression.Apply(rhs.LocationRange, CType.CreatePointer(CType.CreateVoid()), rhs);
                    } else if (rhsPtr != null && rhsPtr.Type.IsPointerType() && lhs.IsNullPointerConstant()) {
                        // 右辺のオペランドがポインタで左辺が空ポインタ定数である。
                        lhs = TypeConversionExpression.Apply(lhs.LocationRange, CType.CreatePointer(CType.CreateVoid()), lhs);
                        rhs = rhsPtr;
                    } else {
                        throw new CompilerException.SpecificationErrorException(locationRange.Start, locationRange.End, "等価演算子は両オペランドは算術型をもつ、両オペランドとも適合する型の修飾版又は非修飾版へのポインタである、一方のオペランドがオブジェクト型又は不完全型へのポインタで他方が void の修飾版又は非修飾版へのポインタである、一方のオペランドがポインタで他方が空ポインタ定数であるの何れかを満たさなければならない。");
                    }
                }

                // 意味規則
                // 両オペランドが算術型をもつ場合，通常の算術型変換を適用する。
                // 関係演算子に関しては，配列の要素でないオブジェクトへのポインタは，要素型としてそのオブジェクトの型をもつ長さ 1 の配列の最初の要素へのポインタと同じ動作をする。
                // 二つのポインタを比較する場合，その結果は指されているオブジェクトのアドレス空間内の相対位置に依存する。
                // オブジェクト型又は不完全型への二つのポインタがいずれも同じオブジェクトを指しているか，いずれも同じ配列オブジェクトの最後の要素を一つ越えたところを指している場合，それらは比較して等しいとする。指されている両オブジェクトが同一の集成体オブジェクトのメンバの場合，後方で宣言された構造体のメンバへのポインタは，その構造体中で前方に宣言されたメンバへのポインタと比較すると大きく，大きな添字の値をもつ配列の要素へのポインタは，より小さな添字の値をもつ同じ配列の要素へのポインタと比較すると大きいとする。
                // 同じ共用体オブジェクトのメンバへのポインタは，すべて等しいとする。
                // 式 P が配列オブジェクトの要素を指しており，式 Q が同じ配列オブジェクトの最後の要素を指している場合，ポインタ式 Q+1 は，P と比較してより大きいとする。
                // その他のすべての場合，動作は未定義とする。
                // <（小さい），>（大きい），<=（以下）及び>=（以上）の各演算子は，指定された関係が真の場合は 1を，偽の場合は 0 を返す。その結果は，型 int をもつ。

                Op = op;
                Lhs = lhs;
                Rhs = rhs;
            }
        }

        /// <summary>
        ///  6.5.10-12 ビット単位演算子基底クラス
        /// </summary>
        public abstract class BitExpression : Expression {
            public Expression Lhs {
                get;
            }
            public Expression Rhs {
                get;
            }
            private CType ResultType {
                get;
            }
            public override CType Type {
                get {
                    return ResultType;
                }
            }

            protected BitExpression(string name, LocationRange locationRange, Expression lhs, Expression rhs) : base(locationRange) {
                // 制約
                // 各オペランドの型は，整数型でなければならない。
                if (!lhs.Type.IsIntegerType() || !rhs.Type.IsIntegerType()) {
                    throw new CompilerException.SpecificationErrorException(lhs.LocationRange.Start, lhs.LocationRange.End, $"{ name }の各オペランドは，整数型をもたなければならない。");
                }
                if (!lhs.Type.IsUnsignedIntegerType()) {
                    Logger.Warning(lhs.LocationRange, $"{name}の左オペランドに符号付き整数型が使われていますが、この演算子は，整数の内部表現に依存した値を返すので，符号付き整数型に対して処理系定義又は未定義の側面をもつことになります。");
                }
                if (!rhs.Type.IsUnsignedIntegerType()) {
                    Logger.Warning(rhs.LocationRange, $"{name}の右オペランドに符号付き整数型が使われていますが、この演算子は，整数の内部表現に依存した値を返すので，符号付き整数型に対して処理系定義又は未定義の側面をもつことになります。");
                }

                // 意味規則  
                // オペランドに対して通常の算術型変換を適用する。
                //  - 2項&演算子の結果は，オペランドのビット単位の論理積とする（すなわち，型変換されたオペランドの対応するビットが両者ともセットされている場合，そしてその場合に限り，結果のそのビットをセットする。）。
                //  - ^演算子の結果は，オペランドのビット単位の排他的論理和とする（すなわち，型変換されたオペランドの対応するビットのいずれか一方だけがセットされている場合，そしてその場合に限り，結果のそのビットをセットする。） 。
                //  - |演算子の結果は，オペランドのビット単位の論理和とする（すなわち，型変換されたオペランドの対応するビットの少なくとも一方がセットされている場合，そしてその場合に限り，結果のそのビットをセットする。）。
                ResultType = Specification.UsualArithmeticConversion(ref lhs, ref rhs);

                Lhs = lhs;
                Rhs = rhs;
            }

            /// <summary>
            ///  6.5.10 ビット単位の AND 演算子(AND式)
            /// </summary>
            public class AndExpression : BitExpression {

                public AndExpression(LocationRange locationRange, Expression lhs, Expression rhs) : base("ビット単位の AND 演算子", locationRange, lhs, rhs) { }
            }

            /// <summary>
            /// 6.5.11 ビット単位の排他 OR 演算子(排他OR式)
            /// </summary>
            public class ExclusiveOrExpression : BitExpression {
                public ExclusiveOrExpression(LocationRange locationRange, Expression lhs, Expression rhs) : base("ビット単位の 排他 OR 演算子", locationRange, lhs, rhs) { }

            }

            /// <summary>
            /// 6.5.12 ビット単位の OR 演算子(OR式)
            /// </summary>
            public class InclusiveOrExpression : BitExpression {
                public InclusiveOrExpression(LocationRange locationRange, Expression lhs, Expression rhs) : base("ビット単位の OR 演算子", locationRange, lhs, rhs) { }
            }
        }


        /// <summary>
        /// 6.5.13 論理 AND 演算子(論理AND式)
        /// </summary>
        public class LogicalAndExpression : Expression {
            public Expression Lhs {
                get;
            }
            public Expression Rhs {
                get;
            }
            public override CType Type {
                get {
                    return CType.CreateBool();
                }
            }

            public LogicalAndExpression(LocationRange locationRange, Expression lhs, Expression rhs) : base(locationRange) {
                // 通常の単項変換
                lhs = Specification.TypeConvert(null, lhs);
                rhs = Specification.TypeConvert(null, rhs);

                // 制約
                // 各オペランドの型は，スカラ型でなければならない。
                if (!lhs.Type.IsScalarType()) {
                    throw new CompilerException.SpecificationErrorException(lhs.LocationRange.Start, lhs.LocationRange.End, "各オペランドは，スカラ型でなければならない。");
                }
                if (!rhs.Type.IsScalarType()) {
                    throw new CompilerException.SpecificationErrorException(rhs.LocationRange.Start, rhs.LocationRange.End, "各オペランドは，スカラ型でなければならない。");
                }
                // 意味規則
                // &&演算子の結果の値は，両オペランドの値が 0 と比較してともに等しくない場合は 1，それ以外の場合は 0 とする。
                // 結果の型は，int とする。
                // ビット単位の 2 項&演算子と異なり，&&演算子は左から右への評価を保証する。
                // 第 1 オペランドの評価の直後を副作用完了点とする。
                // 第 1 オペランドの値が 0 と比較して等しい場合，第 2 オペランドは評価しない。

                Lhs = Specification.TypeConvert(CType.CreateBool(), lhs);
                Rhs = Specification.TypeConvert(CType.CreateBool(), rhs);
            }
        }

        /// <summary>
        /// 6.5.14 論理 OR 演算子(論理OR式)
        /// </summary>
        public class LogicalOrExpression : Expression {
            public Expression Lhs {
                get;
            }
            public Expression Rhs {
                get;
            }
            public override CType Type {
                get {
                    return CType.CreateBool();
                }
            }

            public LogicalOrExpression(LocationRange locationRange, Expression lhs, Expression rhs) : base(locationRange) {
                // 通常の単項変換
                lhs = Specification.TypeConvert(null, lhs);
                rhs = Specification.TypeConvert(null, rhs);

                // 制約
                // 各オペランドの型は，スカラ型でなければならない。
                if (!lhs.Type.IsScalarType()) {
                    throw new CompilerException.SpecificationErrorException(lhs.LocationRange.Start, lhs.LocationRange.End, "各オペランドは，スカラ型でなければならない。");
                }
                if (!rhs.Type.IsScalarType()) {
                    throw new CompilerException.SpecificationErrorException(rhs.LocationRange.Start, rhs.LocationRange.End, "各オペランドは，スカラ型でなければならない。");
                }

                // 意味規則
                // ||演算子の結果の値は，両オペランドを 0 と比較していずれか一方でも等しくない場合は 1，それ以外の場合は 0 とする。
                // 結果の型は int とする。
                // ビット単位の|演算子と異なり，||演算子は左から右への評価を保証する。
                // 第 1 オペランドの評価の直後を副作用完了点とする。
                // 第 1 オペランドの値が 0 と比較して等しくない場合，第 2 オペランドは評価しない
                Lhs = Specification.TypeConvert(CType.CreateBool(), lhs);
                Rhs = Specification.TypeConvert(CType.CreateBool(), rhs);

            }

        }

        /// <summary>
        /// 6.5.15 条件演算子
        /// </summary>
        public class ConditionalExpression : Expression {
            public Expression CondExpr {
                get;
            }
            public Expression ThenExpr {
                get;
            }
            public Expression ElseExpr {
                get;
            }
            private CType ResultType {
                get;
            }
            public override CType Type {
                get {
                    return ResultType;
                }
            }

            public ConditionalExpression(LocationRange locationRange, Expression cond, Expression thenExpr, Expression elseExpr) : base(locationRange) {

                //// 暗黙の型変換を適用
                //thenExpr = Specification.TypeConvert(null, thenExpr);
                //elseExpr = Specification.TypeConvert(null, elseExpr);

                // 制約
                // 第 1 オペランドの型は，スカラ型でなければならない。
                // 第 2 及び第 3 オペランドの型は，次のいずれかの条件を満たさなければならない。
                // - 両オペランドの型が算術型である。
                // - 両オペランドの型が同じ構造体型又は共用体型である。
                // - 両オペランドの型が void 型である。
                // - 両オペランドが適合する型の修飾版又は非修飾版へのポインタである。
                // - 一方のオペランドがポインタであり，かつ他方が空ポインタ定数である。
                // - 一方のオペランドがオブジェクト型又は不完全型へのポインタであり，かつ他方が void の修飾版又は非修飾版へのポインタである。

                // 第 1 オペランドの型は，スカラ型でなければならない。
                if (!cond.Type.IsScalarType()) {
                    throw new CompilerException.SpecificationErrorException(cond.LocationRange.Start, cond.LocationRange.End, "条件演算子の第 1 オペランドの型は，スカラ型でなければならない。");
                }
                CType rt;
                // 意味規則
                // 第 1 オペランドを評価し，その評価の直後を副作用完了点とする。
                // 第 1 オペランドが 0 と比較して等しくない場合だけ，第 2 オペランドを評価する。
                // 第 1 オペランドが 0 と比較して等しい場合だけ，第 3 オペランドを評価する。
                // 第 2 又は第 3 オペランド（いずれか評価したほう）の値を結果とする。
                // 結果の型は 6.5.15 の規定に従って型変換する。
                // 条件演算子の結果を変更するか，又は次の副作用完了点の後，それにアクセスしようとした場合，その動作は，未定義とする。
                // 第 2 及び第 3 オペランドの型がともに算術型ならば，通常の算術型変換をこれら二つのオペランドに適用することによって決まる型を結果の型とする。
                // 両オペランドの型がともに構造体型又は共用体型ならば，結果の型はその型とする。
                // 両オペランドの型がともに void  型ならば，結果の型は void 型とする。
                // 第 2 及び第 3 オペランドがともにポインタである場合，又は，一方が空ポインタ定数かつ他方がポインタである場合，結果の型は両オペランドが指す型のすべての型修飾子で修飾された型へのポインタとする。
                // さらに，両オペランドが適合する型へのポインタ又は適合する型の異なる修飾版へのポインタである場合，結果の型は適切に修飾された合成型へのポインタとする。
                // 一方のオペランドが空ポインタ定数である場合，結果の型は他方のオペランドの型とする。
                // これら以外の場合（一方のオペランドが void 又は void の修飾版へのポインタである場合），結果の型は，適切に修飾された void 型へのポインタとする。

                // 第 2 及び第 3 オペランドの型は，次のいずれかの条件を満たさなければならない。
                if (thenExpr.Type.IsArithmeticType() && elseExpr.Type.IsArithmeticType()) {
                    // 制約 両オペランドの型が算術型である。
                    // 意味規則 第 2 及び第 3 オペランドの型がともに算術型ならば，通常の算術型変換をこれら二つのオペランドに適用することによって決まる型を結果の型とする。
                    rt = Specification.UsualArithmeticConversion(ref thenExpr, ref elseExpr);
                } else if (thenExpr.Type.IsStructureType() && elseExpr.Type.IsStructureType() && CType.IsEqual(thenExpr.Type, elseExpr.Type)) {
                    // - 両オペランドの型が同じ構造体型又は共用体型である。
                    rt = thenExpr.Type;
                } else if (thenExpr.Type.IsVoidType() && elseExpr.Type.IsVoidType()) {
                    // 制約 両オペランドの型が void 型である。
                    // 意味規則 両オペランドの型がともに void  型ならば，結果の型は void 型とする。
                    rt = CType.CreateVoid();
                } else {
                    // ポインタ型への暗黙的型変換を試みる
                    var thenExprPtr = Specification.ToPointerTypeExpr(thenExpr);
                    var elseExprPtr = Specification.ToPointerTypeExpr(elseExpr);

                    if (thenExprPtr != null && thenExprPtr.Type.IsPointerType()
                     && elseExprPtr != null && elseExprPtr.Type.IsPointerType()
                     && Specification.IsCompatible(thenExprPtr.Type.GetBasePointerType().Unwrap(), elseExprPtr.Type.GetBasePointerType().Unwrap())
                    ) {
                        // 制約 両オペランドが適合する型の修飾版又は非修飾版へのポインタである。
                        // 意味規則 第 2 及び第 3 オペランドがともにポインタである場合，結果の型は両オペランドが指す型のすべての型修飾子で修飾された型へのポインタとする。
                        // さらに，両オペランドが適合する型へのポインタ又は適合する型の異なる修飾版へのポインタである場合，結果の型は適切に修飾された合成型へのポインタとする。

                        thenExpr = thenExprPtr;
                        elseExpr = elseExprPtr;

                        if (Specification.IsCompatible(thenExpr.Type.GetBasePointerType().Unwrap(), elseExpr.Type.GetBasePointerType().Unwrap()) == false) {
                            throw new CompilerException.SpecificationErrorException(thenExpr.LocationRange.Start, elseExpr.LocationRange.End, "条件演算子の第 2, 第 3 オペランドが適合する型ではない。");
                        }
                        var baseType = CType.CompositeType(thenExpr.Type.GetBasePointerType().Unwrap(), elseExpr.Type.GetBasePointerType().Unwrap());
                        Debug.Assert(baseType != null);
                        TypeQualifier tq = thenExpr.Type.GetBasePointerType().GetTypeQualifier() | elseExpr.Type.GetBasePointerType().GetTypeQualifier();
                        baseType = baseType.WrapTypeQualifier(tq);
                        rt = CType.CreatePointer(baseType);
                    } else if (
                        (thenExprPtr != null && thenExprPtr.Type.IsPointerType() && elseExpr.IsNullPointerConstant()) ||
                        (elseExprPtr != null && elseExprPtr.Type.IsPointerType() && thenExpr.IsNullPointerConstant())
                    ) {
                        // 制約 一方のオペランドがポインタであり，かつ他方が空ポインタ定数である。
                        // 意味規則 第 2 及び第 3 オペランドが，一方が空ポインタ定数かつ他方がポインタである場合，結果の型は両オペランドが指す型のすべての型修飾子で修飾された型へのポインタとする。
                        if (thenExprPtr != null) {
                            thenExpr = thenExprPtr;
                        } else {
                            elseExpr = elseExprPtr;
                        }

                        var baseType = thenExpr.IsNullPointerConstant() ? elseExpr.Type.GetBasePointerType().Unwrap() : thenExpr.Type.GetBasePointerType().Unwrap();
                        TypeQualifier tq = (thenExpr.Type.IsPointerType() ? thenExpr.Type.GetBasePointerType().GetTypeQualifier() : TypeQualifier.None) | (elseExpr.Type.IsPointerType() ? elseExpr.Type.GetBasePointerType().GetTypeQualifier() : TypeQualifier.None);
                        baseType = baseType.WrapTypeQualifier(tq);
                        rt = CType.CreatePointer(baseType);
                    } else if (
                        (thenExprPtr != null && elseExprPtr != null) &&
                        (
                            (thenExprPtr.Type.IsPointerType() && (thenExprPtr.Type.GetBasePointerType().IsObjectType() || thenExprPtr.Type.GetBasePointerType().IsIncompleteType()) && (elseExprPtr.Type.IsPointerType() && elseExprPtr.Type.GetBasePointerType().IsVoidType()))
                            || (elseExprPtr.Type.IsPointerType() && (elseExprPtr.Type.GetBasePointerType().IsObjectType() || elseExprPtr.Type.GetBasePointerType().IsIncompleteType()) && (thenExprPtr.Type.IsPointerType() && thenExprPtr.Type.GetBasePointerType().IsVoidType()))
                        )
                    ) {
                        // 制約 一方のオペランドがオブジェクト型又は不完全型へのポインタで他方が void の修飾版又は非修飾版へのポインタである。
                        // 意味規則 これら以外の場合（一方のオペランドが void 又は void の修飾版へのポインタである場合），結果の型は，適切に修飾された void 型へのポインタとする。
                        thenExpr = thenExprPtr;
                        elseExpr = elseExprPtr;

                        CType baseType = CType.CreatePointer(CType.CreateVoid());
                        TypeQualifier tq = thenExpr.Type.GetBasePointerType().GetTypeQualifier() | elseExpr.Type.GetBasePointerType().GetTypeQualifier();
                        baseType = baseType.WrapTypeQualifier(tq);
                        rt = CType.CreatePointer(baseType);
                    } else {
                        throw new CompilerException.SpecificationErrorException(thenExpr.LocationRange.Start, elseExpr.LocationRange.End, "条件演算子の第 2 及び第 3 オペランドの型がクソ長い条件を満たしていない。");
                    }
                }


                CondExpr = Specification.TypeConvert(CType.CreateBool(), cond);
                ThenExpr = thenExpr;
                ElseExpr = elseExpr;
                ResultType = rt;
            }
        }

        /// <summary>
        /// 6.5.16 代入演算子(代入式)
        /// </summary>
        public abstract class AssignmentExpression : Expression {
            public Expression Lhs {
                get; protected set;
            }
            public Expression Rhs {
                get; protected set;
            }
            protected CType ResultType {
                get; set;
            }
            public override CType Type {
                get {
                    return ResultType;
                }
            }

            /// <summary>
            /// 6.5.16.1 単純代入
            /// </summary>
            public class SimpleAssignmentExpression : AssignmentExpression {
                /// <summary>
                /// 単純代入の制約規則（いろいろな部分で使うため規則として独立させている）
                /// </summary>
                /// <param name="locationRange"></param>
                /// <param name="lType"></param>
                /// <param name="rhs"></param>
                public static Expression ApplyAssignmentRule(LocationRange locationRange, CType lType, Expression rhs) {
                    //if (lType.IsStructureType() && CType.IsEqual(lType.Unwrap(), rhs.Type.Unwrap())) {
                    //    // 構造体・共用体については暗黙的型変換を用いない
                    //} else {
                    // 代入元式に対して代入先型への(暗黙的)型変換を適用
                    rhs = Specification.ImplicitConversion(lType, rhs);
                    //}

                    // 制約 (単純代入)
                    // 次のいずれかの条件が成立しなければならない。
                    // - 左オペランドの型が算術型の修飾版又は非修飾版であり，かつ右オペランドの型が算術型である。
                    // - 左オペランドの型が右オペランドの型に適合する構造体型又は共用体型の修飾版又は非修飾版である。
                    // - 両オペランドが適合する型の修飾版又は非修飾版へのポインタであり，かつ左オペランドで指される型が右オペランドで指される型の型修飾子をすべてもつ。
                    // - 一方のオペランドがオブジェクト型又は不完全型へのポインタであり，かつ他方が void の修飾版又は非修飾版へのポインタである。
                    //   さらに，左オペランドで指される型が，右オペランドで指される型の型修飾子をすべてもつ。
                    // - 左オペランドがポインタであり，かつ右オペランドが空ポインタ定数である。
                    // - 左オペランドの型が_Bool 型であり，かつ右オペランドがポインタである。

                    if (lType.IsArithmeticType() && rhs.Type.IsArithmeticType()) {
                        // 左オペランドの型が算術型の修飾版又は非修飾版であり，かつ右オペランドの型が算術型である。
                    } else if (lType.IsStructureType() && CType.IsEqual(lType.Unwrap(), rhs.Type.Unwrap())) {
                        // 左オペランドの型が右オペランドの型に適合する構造体型又は共用体型の修飾版又は非修飾版である。
                    } else {
                        // 左辺型への暗黙的型変換を試みる
                        if (rhs != null && CType.IsEqual(lType, rhs.Type) && lType.GetTypeQualifier().HasFlag(rhs.Type.GetTypeQualifier())) {
                            // 両オペランドが適合する型の修飾版又は非修飾版へのポインタであり，かつ左オペランドで指される型が右オペランドで指される型の型修飾子をすべてもつ。
                        } else if (
                            (rhs != null)
                            && (
                                (lType.IsPointerType() && (lType.GetBasePointerType().IsObjectType() || lType.GetBasePointerType().IsIncompleteType()) && (rhs.Type.IsPointerType() && rhs.Type.GetBasePointerType().IsVoidType())) ||
                                (rhs.Type.IsPointerType() && (rhs.Type.GetBasePointerType().IsObjectType() || rhs.Type.GetBasePointerType().IsIncompleteType()) && (lType.IsPointerType() && lType.GetBasePointerType().IsVoidType()))
                            )
                            && lType.GetTypeQualifier().HasFlag(rhs.Type.GetTypeQualifier())) {
                            // 一方のオペランドがオブジェクト型又は不完全型へのポインタであり，かつ他方が void の修飾版又は非修飾版へのポインタである。
                            // さらに，左オペランドで指される型が，右オペランドで指される型の型修飾子をすべてもつ。
                        } else if (lType.IsPointerType() && rhs.IsNullPointerConstant()) {
                            // - 左オペランドがポインタであり，かつ右オペランドが空ポインタ定数である。
                        } else if (lType.IsBoolType() && rhs != null && rhs.Type.IsPointerType()) {
                            // - 左オペランドの型が_Bool 型であり，かつ右オペランドがポインタである。
                        } else {
                            throw new CompilerException.SpecificationErrorException(locationRange, "代入元と代入先の間で単純代入の条件を満たしていない。");
                        }
                    }


                    // 意味規則(代入演算子(代入式))
                    // 代入演算子は，左オペランドで指し示されるオブジェクトに値を格納する。
                    // 代入式は，代入後の左オペランドの値をもつが，左辺値ではない。
                    // 代入式の型は，左オペランドの型とする。
                    // ただし，左オペランドの型が修飾型である場合は，左オペランドの型の非修飾版とする。
                    // 左オペランドに格納されている値を更新する副作用は，直前の副作用完了点から次の副作用完了点までの間に起こらなければならない。
                    // オペランドの評価順序は，未規定とする。
                    // 代入演算子の結果を変更するか，又は次の副作用完了点の後，それにアクセスしようとした場合，その動作は未定義とする。

                    // 意味規則(単純代入)
                    //（=）は，右オペランドの値を代入式の型に型変換し，左オペランドで指し示されるオブジェクトに格納されている値をこの値で置き換える。
                    // オブジェクトに格納されている値を，何らかの形でそのオブジェクトの記憶域に重なる他のオブジェクトを通してアクセスする場合，重なりは完全に一致していなければならない。
                    // さらに，二つのオブジェクトの型は，適合する型の修飾版又は非修飾版でなければならない。
                    // そうでない場合，動作は未定義とする。

                    if (!CType.IsEqual(lType, rhs.Type)) {
                        //（=）は，右オペランドの値を代入式の型に型変換し，左オペランドで指し示されるオブジェクトに格納されている値をこの値で置き換える。
                        rhs = TypeConversionExpression.Apply(rhs.LocationRange, lType, rhs);
                    }

                    return rhs;

                }

                public SimpleAssignmentExpression(Expression lhs, Expression rhs) : base(new LocationRange(lhs.LocationRange.Start, rhs.LocationRange.End)) {

                    // 制約(代入演算子(代入式))
                    // 代入演算子の左オペランドは，変更可能な左辺値でなければならない。
                    if (Specification.IsModifiableLvalue(lhs) == false) {
                        throw new CompilerException.SpecificationErrorException(lhs.LocationRange, "代入演算子の左オペランドは，変更可能な左辺値でなければならない。");
                    }
                    // 代入の制約条件と意味規則を適用する
                    rhs = ApplyAssignmentRule(LocationRange, lhs.Type, rhs);

                    Lhs = lhs;
                    Rhs = Specification.TypeConvert(lhs.Type, rhs);
                    // 代入式の型は，左オペランドの型とする。
                    // ただし，左オペランドの型が修飾型である場合は，左オペランドの型の非修飾版とする。
                    ResultType = lhs.Type.UnwrapTypeQualifier();
                }
            }

            /// <summary>
            /// 6.5.16.2 複合代入
            /// </summary>
            public class CompoundAssignmentExpression : AssignmentExpression {
                public enum OperatorKind {
                    None,
                    MUL_ASSIGN, DIV_ASSIGN, MOD_ASSIGN, ADD_ASSIGN, SUB_ASSIGN, LEFT_ASSIGN, RIGHT_ASSIGN, AND_ASSIGN, XOR_ASSIGN, OR_ASSIGN
                }
                public OperatorKind Op {
                    get;
                }
                public CompoundAssignmentExpression(OperatorKind op, Expression lhs, Expression rhs) : base(new LocationRange(lhs.LocationRange.Start, rhs.LocationRange.End)) {
                    // 制約(代入演算子(代入式))
                    // 代入演算子の左オペランドは，変更可能な左辺値でなければならない。
                    if (!lhs.IsLValue()) {
                        // ToDo: 変更可能であることをチェック
                        throw new CompilerException.SpecificationErrorException(lhs.LocationRange, "代入演算子の左オペランドは，変更可能な左辺値でなければならない。");
                    }

                    // 制約(複合代入)
                    // 演算子 +=及び-=の場合は，次のいずれかの条件を満たさなければならない。
                    // - 左オペランドがオブジェクト型へのポインタであり，かつ右オペランドの型が整数型である。
                    // - 左オペランドの型が算術型の修飾版又は非修飾版であり，かつ右オペランドの型が算術型である。
                    // その他の演算子の場合，各オペランドの型は，対応する 2 項演算子に対して許される算術型でなければならない。
                    switch (op) {
                        case OperatorKind.ADD_ASSIGN:
                        case OperatorKind.SUB_ASSIGN: {
                                if (lhs.Type.IsPointerType() && lhs.Type.GetBasePointerType().IsObjectType() && rhs.Type.IsIntegerType()) {
                                    // 左オペランドがオブジェクト型へのポインタであり，かつ右オペランドの型が整数型である。
                                } else if (lhs.Type.IsArithmeticType() && rhs.Type.IsArithmeticType()) {
                                    // 左オペランドの型が算術型の修飾版又は非修飾版であり，かつ右オペランドの型が算術型である。
                                } else {
                                    throw new CompilerException.SpecificationErrorException(LocationRange, "複合代入演算子+=及び-=の場合に満たさなければならない制約を満たしていない。");
                                }
                                break;
                            }
                        case OperatorKind.MUL_ASSIGN:
                        case OperatorKind.DIV_ASSIGN:
                        case OperatorKind.MOD_ASSIGN: {
                                // 制約(複合代入)
                                // その他の演算子の場合，各オペランドの型は，対応する 2 項演算子に対して許される算術型でなければならない。

                                // 制約(6.5.5 乗除演算子)
                                // 各オペランドは，算術型をもたなければならない。
                                // %演算子のオペランドは，整数型をもたなければならない
                                if (op == OperatorKind.MOD_ASSIGN) {
                                    if (!lhs.Type.IsIntegerType()) {
                                        throw new CompilerException.SpecificationErrorException(lhs.LocationRange, "%=演算子のオペランドは，整数型をもたなければならない。");
                                    }
                                    if (!rhs.Type.IsIntegerType()) {
                                        throw new CompilerException.SpecificationErrorException(rhs.LocationRange, "%=演算子のオペランドは，整数型をもたなければならない。");
                                    }
                                } else {
                                    if (!lhs.Type.IsArithmeticType()) {
                                        throw new CompilerException.SpecificationErrorException(lhs.LocationRange, "各オペランドは，算術型をもたなければならない。");
                                    }
                                    if (!rhs.Type.IsArithmeticType()) {
                                        throw new CompilerException.SpecificationErrorException(rhs.LocationRange, "各オペランドは，算術型をもたなければならない。");
                                    }
                                }
                                break;
                            }
                        case OperatorKind.LEFT_ASSIGN:
                        case OperatorKind.RIGHT_ASSIGN:
                        case OperatorKind.AND_ASSIGN:
                        case OperatorKind.XOR_ASSIGN:
                        case OperatorKind.OR_ASSIGN: {
                                // 制約(複合代入)
                                // その他の演算子の場合，各オペランドの型は，対応する 2 項演算子に対して許される算術型でなければならない。

                                // 制約(6.5.7 ビット単位のシフト演算子)  
                                // 制約(6.5.10 ビット単位の AND 演算子)
                                // 制約(6.5.11 ビット単位の排他 OR 演算子)
                                // 制約(6.5.12 ビット単位の OR 演算子)
                                // 各オペランドの型は，整数型でなければならない。
                                if (!lhs.Type.IsIntegerType()) {
                                    throw new CompilerException.SpecificationErrorException(lhs.LocationRange, "各オペランドは，整数型をもたなければならない。");
                                }
                                if (!rhs.Type.IsIntegerType()) {
                                    throw new CompilerException.SpecificationErrorException(rhs.LocationRange, "各オペランドは，整数型をもたなければならない。");
                                }
                                break;
                            }
                        default: {
                                throw new Exception();
                            }
                    }

                    // 意味規則(代入演算子(代入式))
                    // 代入演算子は，左オペランドで指し示されるオブジェクトに値を格納する。
                    // 代入式は，代入後の左オペランドの値をもつが，左辺値ではない。
                    // 代入式の型は，左オペランドの型とする。
                    // ただし，左オペランドの型が修飾型である場合は，左オペランドの型の非修飾版とする。
                    // 左オペランドに格納されている値を更新する副作用は，直前の副作用完了点から次の副作用完了点までの間に起こらなければならない。
                    // オペランドの評価順序は，未規定とする。
                    // 代入演算子の結果を変更するか，又は次の副作用完了点の後，それにアクセスしようとした場合，その動作は未定義とする。


                    if (lhs.Type.IsPointerType() && lhs.Type.GetBasePointerType().IsObjectType() && rhs.Type.IsIntegerType()) {
                        // 左オペランドがオブジェクト型へのポインタであり，かつ右オペランドの型が整数型である。
                        rhs = TypeConversionExpression.Apply(rhs.LocationRange, CType.CreatePtrDiffT(), rhs);//*
                    } else if (lhs.Type.IsArithmeticType() && rhs.Type.IsArithmeticType()) {
                        // 左オペランドの型が算術型の修飾版又は非修飾版であり，かつ右オペランドの型が算術型である。
                        rhs = TypeConversionExpression.Apply(rhs.LocationRange, lhs.Type, rhs);//*これでいいのだろうか？
                    } else {
                        throw new CompilerException.SpecificationErrorException(LocationRange, "右辺値を左辺値型にキャストできない。");
                    }

                    Op = op;
                    Lhs = lhs;
                    Rhs = rhs;
                    // 代入式の型は，左オペランドの型とする。
                    // ただし，左オペランドの型が修飾型である場合は，左オペランドの型の非修飾版とする。
                    ResultType = lhs.Type.UnwrapTypeQualifier();
                }

            }

            protected AssignmentExpression(LocationRange locationRange) : base(locationRange) {
            }
        }

        /// <summary>
        /// 6.5.17 コンマ演算子
        /// </summary>
        public class CommaExpression : Expression {
            public List<Expression> Expressions { get; } = new List<Expression>();
            public override CType Type {
                get {
                    return Expressions.Last().Type;
                }
            }
            public override bool HasStorageClassRegister() {
                return Expressions.Last().HasStorageClassRegister();
            }

            public CommaExpression(LocationRange locationRange) : base(locationRange) {
                // 意味規則 
                // コンマ演算子は，左オペランドをボイド式として評価する。
                // その評価の直後を副作用完了点とする。
                // 次に右オペランドを評価する。
                // コンマ演算子の結果は，右オペランドの型及び値をもつ
            }
        }


        /// <summary>
        /// X.X.X GCC拡張：式中に文
        /// </summary>
        public class GccStatementExpression : Expression {
            public Statement Statements {
                get;
            }
            private CType ResultType {
                get;
            }
            public override CType Type {
                get {
                    return ResultType;
                }
            }

            public GccStatementExpression(LocationRange locationRange, Statement statements, CType resultType) : base(locationRange) {
                Statements = statements;
                ResultType = resultType;
            }
        }

        /// <summary>
        /// 6.3 型変換（キャスト式ではなく、強制的に型を変更する）
        /// </summary>
        public class TypeConversionExpression : Expression {
            // 6.3.1.2 論理型  
            // 任意のスカラ値を_Bool 型に変換する場合，その値が 0 に等しい場合は結果は 0 とし，それ以外の場合は 1 とする。
            //
            // 6.3.1.3 符号付き整数型及び符号無し整数型  
            // 整数型の値を_Bool 型以外の他の整数型に変換する場合，その値が新しい型で表現可能なとき，値は変化しない。
            // 新しい型で表現できない場合，新しい型が符号無し整数型であれば，新しい型で表現しうる最大の数に1加えた数を加えること又は減じることを，新しい型の範囲に入るまで繰り返すことによって得られる値に変換する。
            // そうでない場合，すなわち，新しい型が符号付き整数型であって，値がその型で表現できない場合は，結果が処理系定義の値となるか，又は処理系定義のシグナルを生成するかのいずれかとする。
            //
            // 6.3.1.4実浮動小数点型及び整数型  
            // 実浮動小数点型の有限の値を_Bool 型以外の整数型に型変換する場合，小数部を捨てる（すなわち，値を 0 方向に切り捨てる。）。
            // 整数部の値が整数型で表現できない場合，その動作は未定義とする。
            // 整数型の値を実浮動小数点型に型変換する場合，変換する値が新しい型で正確に表現できるとき，その値は変わらない。
            // 変換する値が表現しうる値の範囲内にあるが正確に表現できないならば，その値より大きく最も近い表現可能な値，又はその値より小さく最も近い表現可能な値のいずれかを処理系定義の方法で選ぶ。
            // 変換する値が表現しうる値の範囲外にある場合，その動作は未定義とする。
            //
            // 6.3.1.5 実浮動小数点型  
            // float を double 若しくは long double に拡張する場合，又は double を long double に拡張する場合，その値は変化しない。 
            // double を float に変換する場合，long double を double 若しくは float に変換する場合，又は，意味上の型（6.3.1.8 参照）が要求するより高い精度及び広い範囲で表現された値をその意味上の型に明示的に変換する場合，変換する値がその新しい型で正確に表現できるならば，その値は変わらない。
            // 変換する値が，表現しうる値の範囲内にあるが正確に表現できない場合，その結果は，その値より大きく最も近い表現可能な値，又はその値より小さく最も近い表現可能な値のいずれかを処理系定義の方法で選ぶ。
            // 変換する値が表現しうる値の範囲外にある場合，その動作は未定義とする。
            // 
            // 6.3.1.6 複素数型  
            // 複素数型の値を他の複素数型に変換する場合，実部と虚部の両方に，対応する実数型の変換規則を適用する。
            // 
            // 6.3.1.7 実数型及び複素数型
            // 実数型の値を複素数型に変換する場合，複素数型の結果の実部は対応する実数型への変換規則により決定し，複素数型の結果の虚部は正の 0 又は符号無しの 0 とする。
            // 複素数型の値を実数型に変換する場合，複素数型の値の虚部を捨て，実部の値を，対応する実数型の変換規則に基づいて変換する。
            //
            // 6.3.2.2 void ボイド式（void expression）
            // （型 void をもつ式）の（存在しない）値は，いかなる方法で も使ってはならない。
            // ボイド式には，暗黙の型変換も明示的な型変換（void への型変換を除く。 ）も適用してはならない。
            // 他の型の式をボイド式として評価する場合，その値又は指示子は捨てる。（ボイド式は， 副作用のために評価する。 ）
            // 
            // 6.3.2.3 ポインタ
            // void へのポインタは，任意の不完全型若しくはオブジェクト型へのポインタに，又はポインタから，型変換してもよい。
            // 任意の不完全型又はオブジェクト型へのポインタを，void へのポインタに型変換して再び戻した場合，結果は元のポインタと比較して等しくなければならない。
            // 任意の型修飾子qに対して非q修飾型へのポインタは，その型のq修飾版へのポインタに型変換してもよい。
            // 元のポインタと変換されたポインタに格納された値は，比較して等しくなければならない。
            // 値0をもつ整数定数式又はその定数式を型void *にキャストした式を，空ポインタ定数（null pointerconstant）と呼ぶ。
            // 空ポインタ定数をポインタ型に型変換した場合，その結果のポインタを空ポインタ（null pointer）と呼び，いかなるオブジェクト又は関数へのポインタと比較しても等しくないことを保証する。
            // 空ポインタを他のポインタ型に型変換すると，その型の空ポインタを生成する。
            // 二つの空ポインタは比較して等しくなければならない。
            // 整数は任意のポインタ型に型変換できる。
            // これまでに規定されている場合を除き，結果は処理系定義とし，正しく境界調整されていないかもしれず，被参照型の実体を指していないかもしれず，トラップ表現であるかもしれない(56)。
            // 任意のポインタ型は整数型に型変換できる。
            // これまでに規定されている場合を除き，結果は処理系定義とする。
            // 結果が整数型で表現できなければ，その動作は未定義とする。
            // 結果は何らかの整数型の値の範囲に含まれているとは限らない。
            // オブジェクト型又は不完全型へのポインタは，他のオブジェクト型又は不完全型へのポインタに型変換できる。
            // その結果のポインタが，被参照型に関して正しく境界調整されていなければ，その動作は未定義とする。
            // そうでない場合，再び型変換で元の型に戻すならば，その結果は元のポインタと比較して等しくなければならない。
            // オブジェクトへのポインタを文字型へのポインタに型変換する場合，その結果はオブジェクトの最も低位のアドレスを指す。
            // その結果をオブジェクトの大きさまで連続して増分すると，そのオブジェクトの残りのバイトへのポインタを順次生成できる。
            // ある型の関数へのポインタを，別の型の関数へのポインタに型変換することができる。
            // さらに再び型変換で元の型に戻すことができるが，その結果は元のポインタと比較して等しくなければならない。
            // 型変換されたポインタを関数呼出しに用い，関数の型がポインタが指すものの型と適合しない場合，その動作は未定義とする。
            // 
            private CType Ty {
                get;
            }
            public Expression Expr {
                get;
            }
            public override CType Type {
                get {
                    return Ty;
                }
            }

            public override bool IsLValue() {
                return !Type.GetTypeQualifier().HasFlag(TypeQualifier.Const) && Expr.IsLValue();
            }

            protected TypeConversionExpression(LocationRange locationRange, CType type, Expression expr) : base(locationRange) {
                //System.Diagnostics.Debug.Assert(!CType.IsEqual(type, expr.Type));
                Ty = type;
                Expr = expr;
            }
            public static Expression Apply(LocationRange locationRange, CType type, Expression expr) {
                if (CType.IsEqual(type, expr.Type)) {
                    return expr;
                } else {
                    return new TypeConversionExpression(locationRange, type, expr);
                }
            }

        }

        /// <summary>
        /// 6.3.1.1 整数拡張（AST生成時に挿入）
        /// </summary>
        public class IntegerPromotionExpression : Expression {
            private BasicType Ty {
                get;
            }
            public Expression Expr {
                get;
            }
            public override CType Type {
                get {
                    return Ty;
                }
            }

            public IntegerPromotionExpression(LocationRange locationRange, BasicType type, Expression expr) : base(locationRange) {
                Ty = type;
                Expr = expr;
            }
        }

    }
}
