using System;
using System.Collections.Generic;
using System.Linq;
using AnsiCParser.DataType;

namespace AnsiCParser.SyntaxTree {

    /// <summary>
    /// 初期化式の検証付き解析
    /// </summary>
    public static class InitializerChecker {

        /// <summary>
        /// 初期化式の読み取りイテレータ
        /// </summary>
        private class InitializerIterator {
            private readonly Stack<Tuple<Initializer, int>> _inits = new Stack<Tuple<Initializer, int>>();
            private Initializer _initializer;
            private int _index;

            public Initializer Current {
                get; private set;
            }
            public InitializerIterator(Initializer it) {
                _initializer = null;
                _index = -1;
                Current = it;
            }

            public bool Next() {
                if (_initializer is Initializer.ComplexInitializer) {
                    var ini = (Initializer.ComplexInitializer)_initializer;
                    if (ini.Ret.Count == _index + 1) {
                        Current = null;
                        return false;
                    }

                    _index++;
                    Current = ini.Ret[_index];
                    return true;
                }

                if (_initializer is Initializer.SimpleInitializer) {
                    if (_index != -1) {
                        Current = null;
                        return false;
                    }

                    Current = _initializer;
                    _index = 0;
                    return true;
                }

                Current = null;
                return false;
            }
            public void Enter() {
                if (Current is Initializer.ComplexInitializer) {
                    _inits.Push(Tuple.Create(_initializer, _index));
                    _initializer = Current;
                    _index = -1;
                    Current = null;
                    return;
                }

                throw new Exception();
            }
            public void Leave() {
                Current = _initializer;
                var top = _inits.Pop();
                _initializer = top.Item1;
                _index = top.Item2;
            }

            public bool IsSimpleInitializer() {
                return Current is Initializer.SimpleInitializer;
            }

            public bool IsComplexInitializer() {
                return Current is Initializer.ComplexInitializer;
            }
            public bool IsInComplexInitializer() {
                return _initializer is Initializer.ComplexInitializer;
            }
            public Initializer.SimpleInitializer AsSimpleInitializer() {
                return Current as Initializer.SimpleInitializer;
            }

            public Initializer.ComplexInitializer AsComplexInitializer() {
                return Current as Initializer.ComplexInitializer;
            }
        }

        /// <summary>
        /// 初期化式の解析
        /// </summary>
        /// <param name="depth"></param>
        /// <param name="type"></param>
        /// <param name="it"></param>
        /// <param name="isLocalVariableInit"></param>
        /// <returns></returns>
        private static Initializer CheckInitializerBase(int depth, CType type, InitializerIterator it, bool isLocalVariableInit) {
            if (type.IsArrayType()) {
                return CheckInitializerArray(depth + 1, type.Unwrap() as ArrayType, it, isLocalVariableInit);
            }

            if (type.IsStructureType()) {
                return CheckInitializerStruct(depth, type.Unwrap() as TaggedType.StructUnionType, it, isLocalVariableInit);
            }

            if (type.IsUnionType()) {
                return CheckInitializerUnion(depth, type.Unwrap() as TaggedType.StructUnionType, it, isLocalVariableInit);
            }

            return CheckInitializer2Simple(depth, type, it, isLocalVariableInit);
        }

        /// <summary>
        /// 単純型（配列型、共用体型、構造体型以外）の初期化式の解析
        /// </summary>
        /// <param name="depth"></param>
        /// <param name="type"></param>
        /// <param name="it"></param>
        /// <param name="isLocalVariableInit"></param>
        /// <returns></returns>
        private static Initializer CheckInitializer2Simple(int depth, CType type, InitializerIterator it, bool isLocalVariableInit) {
            if (it.IsComplexInitializer()) {
                Logger.Warning(it.Current.LocationRange, "スカラー初期化子がブレースで囲まれています");
                it.Enter();
                it.Next();
                var ret = CheckInitializer2Simple(depth, type, it, isLocalVariableInit);
                if (it.Current != null) {
                    Logger.Warning(it.Current.LocationRange, "スカラー初期化子内の要素が多すぎます");
                }
                it.Leave();
                it.Next();
                return ret;
            } else {
                var expr = it.AsSimpleInitializer().AssignmentExpression;
                if (type.IsAggregateType() || type.IsUnionType()) {
                    if (isLocalVariableInit == false) {
                        throw new CompilerException.SpecificationErrorException(it.Current.LocationRange, "集成体型又は共用体型をもつオブジェクトに対する初期化子は，要素又は名前付きメンバに対する初期化子並びを波括弧で囲んだものでなければならない。");
                    }
                }
                // C89ではグローバル変数に対する初期化はコンパイル時定数式のみ許される。
                if (isLocalVariableInit == false && Parser._mode == Parser.LanguageMode.C89) {
                    ExpressionEvaluator.Eval(expr);
                }
                var assign = Expression.AssignmentExpression.SimpleAssignmentExpression.ApplyAssignmentRule(it.Current.LocationRange, type, expr);
                var ret = new Initializer.SimpleAssignInitializer(it.Current.LocationRange, type, assign);
                it.Next();
                return ret;

            }
        }

        /// <summary>
        /// 配列型に対する複合初期化式
        /// </summary>
        /// <param name="depth"></param>
        /// <param name="type"></param>
        /// <param name="it"></param>
        /// <param name="isLocalVariableInit"></param>
        /// <returns></returns>
        private static Initializer CheckInitializerArrayByComplexInitializer(int depth, ArrayType type, InitializerIterator it, bool isLocalVariableInit) {
            var inits = it.AsComplexInitializer().Ret;
            if (type.Length == -1) {
                if (depth != 1) {
                    throw new CompilerException.SpecificationErrorException(it.Current.LocationRange, "ネストした可変配列を初期化しています。");
                }
            }

#if false
            // 要素数分回す
            var assigns = new List<Initializer>();
            var loc = it.Current.LocationRange;
            it.Enter();
            it.Next();
            int i;
            for (i = 0; type.Length == -1 || i < type.Length; i++) {
                if (it.Current == null) {
                    break;
                }
                assigns.Add(CheckInitializerBase(depth, type.ElementType, it, isLocalVariableInit));
            }
            if (type.Length == -1) {
                // 型の長さを確定させる
                type.Length = i;
            }
            it.Leave();
            it.Next();
            return new Initializer.ArrayAssignInitializer(loc, type, assigns);
#else
            it.Enter();
            it.Next();
            var si = it.Current as Initializer.SimpleInitializer;
            if (si != null && si.AssignmentExpression is Expression.PrimaryExpression.StringExpression && type.ElementType.IsCharacterType()) {
                // char x[] = { "hello" }; のような雑なケース
                var sexpr = si.AssignmentExpression as Expression.PrimaryExpression.StringExpression;
                if (type.Length == -1) {
                    if (depth != 1) {
                        throw new CompilerException.SpecificationErrorException(it.Current.LocationRange, "ネストした可変配列を初期化しています。");
                    }
                    // 要素数分回す
                    List<Initializer> assigns = new List<Initializer>();
                    var loc = it.Current.LocationRange;
                    var len = sexpr.Value.Count;
                    foreach (var b in sexpr.Value) {
                        assigns.Add(new Initializer.SimpleAssignInitializer(loc, type.ElementType, new Expression.PrimaryExpression.Constant.IntegerConstant(loc, $"0x{b,0:X2}", b, BasicType.TypeKind.UnsignedChar)));
                    }
                    // 型の長さを確定させる
                    type.Length = len;
                    it.Leave();
                    it.Next();
                    return new Initializer.ArrayAssignInitializer(loc, type, assigns);
                } else {
                    var len = sexpr.Value.Count;
                    if (len > type.Length) {
                        Logger.Warning(sexpr.LocationRange, $"配列変数の要素数よりも長い文字列が初期化子として与えられているため、文字列末尾の {len - type.Length}バイトは無視され、終端文字なし文字列 (つまり、終端を示す 0 値のない文字列) が作成されます。");
                        len = type.Length;
                    }
                    // 要素数分回す
                    List<Initializer> assigns = new List<Initializer>();
                    var loc = it.Current.LocationRange;
                    foreach (var b in sexpr.Value.Take(len)) {
                        assigns.Add(new Initializer.SimpleAssignInitializer(loc, type.ElementType, new Expression.PrimaryExpression.Constant.IntegerConstant(loc, $"0x{b,0:X2}", b, BasicType.TypeKind.UnsignedChar)));
                    }
                    it.Leave();
                    it.Next();
                    return new Initializer.ArrayAssignInitializer(loc, type, assigns);
                }
            } else {
                // 要素数分回す
                var assigns = new List<Initializer>();
                var loc = it.Current.LocationRange;
                int i;
                for (i = 0; type.Length == -1 || i < type.Length; i++) {
                    if (it.Current == null) {
                        break;
                    }
                    assigns.Add(CheckInitializerBase(depth, type.ElementType, it, isLocalVariableInit));
                }
                if (type.Length == -1) {
                    // 型の長さを確定させる
                    type.Length = i;
                }
                it.Leave();
                it.Next();
                return new Initializer.ArrayAssignInitializer(loc, type, assigns);
            }
#endif
        }

        /// <summary>
        /// 配列型に対する単純初期化式（文字列が初期化子として与えられている場合で、初期化対象が文字配列型の場合）
        /// </summary>
        /// <param name="depth"></param>
        /// <param name="type"></param>
        /// <param name="it"></param>
        /// <param name="isLocalVariableInit"></param>
        /// <returns></returns>
        private static Initializer CheckInitializerArrayByStringExpressionInitializerToCharArray(int depth, ArrayType type, InitializerIterator it, bool isLocalVariableInit) {
            // 文字配列が対象の場合
            var expr = it.AsSimpleInitializer().AssignmentExpression;
            while (expr is Expression.PrimaryExpression.EnclosedInParenthesesExpression) {
                expr = ((Expression.PrimaryExpression.EnclosedInParenthesesExpression)expr).ParenthesesExpression;
            }

            if (!(expr is Expression.PrimaryExpression.StringExpression)) {
                throw new CompilerException.SpecificationErrorException(it.Current.LocationRange, "文字列以外が文字配列型に単純初期化子として与えられている。");
            }

            var sexpr = expr as Expression.PrimaryExpression.StringExpression;
            if (type.Length == -1) {
                if (depth != 1) {
                    throw new CompilerException.SpecificationErrorException(it.Current.LocationRange, "ネストした可変配列を初期化しています。");
                }
                // 要素数分回す
                List<Initializer> assigns = new List<Initializer>();
                var loc = it.Current.LocationRange;
                var len = sexpr.Value.Count;
                foreach (var b in sexpr.Value) {
                    assigns.Add(new Initializer.SimpleAssignInitializer(loc, type.ElementType, new Expression.PrimaryExpression.Constant.IntegerConstant(loc, $"0x{b,0:X2}", b, BasicType.TypeKind.UnsignedChar)));
                }
                // 型の長さを確定させる
                type.Length = len;
                it.Next();
                return new Initializer.ArrayAssignInitializer(loc, type, assigns);
            } else {
                var len = sexpr.Value.Count;
                if (len > type.Length) {
                    Logger.Warning(sexpr.LocationRange, $"配列変数の要素数よりも長い文字列が初期化子として与えられているため、文字列末尾の {len - type.Length}バイトは無視され、終端文字なし文字列 (つまり、終端を示す 0 値のない文字列) が作成されます。");
                    len = type.Length;
                }
                // 要素数分回す
                List<Initializer> assigns = new List<Initializer>();
                var loc = it.Current.LocationRange;
                foreach (var b in sexpr.Value.Take(len)) {
                    assigns.Add(new Initializer.SimpleAssignInitializer(loc, type.ElementType, new Expression.PrimaryExpression.Constant.IntegerConstant(loc, $"0x{b,0:X2}", b, BasicType.TypeKind.UnsignedChar)));
                }
                it.Next();
                return new Initializer.ArrayAssignInitializer(loc, type, assigns);
            }
        }

        /// <summary>
        /// 配列型に対する単純初期化式（文字列が初期化子として与えられている場合で、初期化対象が文字配列型以外の場合）
        /// </summary>
        /// <param name="depth"></param>
        /// <param name="type"></param>
        /// <param name="it"></param>
        /// <param name="isLocalVariableInit"></param>
        /// <returns></returns>
        private static Initializer CheckInitializerArrayByStringExpressionInitializerToNotCharArray(int depth, ArrayType type, InitializerIterator it, bool isLocalVariableInit) {
            // 文字型以外が初期化対象の場合
            if (type.Length == -1) {
                if (depth != 1) {
                    throw new CompilerException.SpecificationErrorException(it.Current.LocationRange, "ネストした可変配列を初期化しています。");
                }
                // 要素数分回す
                List<Initializer> assigns = new List<Initializer>();
                var loc = it.Current.LocationRange;
                var len = 0;
                while (it.Current != null) {
                    assigns.Add(CheckInitializerBase(depth, type.ElementType, it, isLocalVariableInit));
                    len++;
                }
                // 型の長さを確定させる
                type.Length = len;
                return new Initializer.ArrayAssignInitializer(loc, type, assigns);
            } else {
                var len = type.Length;
                // 要素数分回す
                List<Initializer> assigns = new List<Initializer>();
                var loc = it.Current.LocationRange;
                while (it.Current != null && len > 0) {
                    assigns.Add(CheckInitializerBase(depth, type.ElementType, it, isLocalVariableInit));
                    len--;
                }
                return new Initializer.ArrayAssignInitializer(loc, type, assigns);
            }
        }

        /// <summary>
        /// 配列型に対する単純初期化式（単純初期化式が文字列の場合）の解析
        /// </summary>
        /// <param name="depth"></param>
        /// <param name="type"></param>
        /// <param name="it"></param>
        /// <param name="isLocalVariableInit"></param>
        /// <returns></returns>
        private static Initializer CheckInitializerArrayByStringExpressionInitializer(int depth, ArrayType type, InitializerIterator it, bool isLocalVariableInit) {
            if (type.ElementType.IsBasicType(BasicType.TypeKind.Char, BasicType.TypeKind.SignedChar, BasicType.TypeKind.UnsignedChar)) {
                return CheckInitializerArrayByStringExpressionInitializerToCharArray(depth, type, it, isLocalVariableInit);
            }

            return CheckInitializerArrayByStringExpressionInitializerToNotCharArray(depth, type, it, isLocalVariableInit);
        }

        /// <summary>
        /// 配列型に対する単純初期化式（ただし、単純初期化式は複合初期化式中に登場する）の解析
        /// （struct {int len; char data[16] } = { 10, 1, 2, 3} のdataに対する初期化式は複合初期化式の一部分に対応している。このような複合初期化式の「一部分」の解析をする場合に対応）
        /// </summary>
        /// <param name="depth"></param>
        /// <param name="type"></param>
        /// <param name="it"></param>
        /// <param name="isLocalVariableInit"></param>
        /// <returns></returns>
        private static Initializer CheckInitializerArrayBySimpleInitializerInComplexInitializer(int depth, ArrayType type, InitializerIterator it, bool isLocalVariableInit) {
            if (type.Length == -1) {
                if (depth != 1) {
                    throw new CompilerException.SpecificationErrorException(it.Current.LocationRange, "ネストした可変配列を初期化しています。");
                }
                // 要素数分回す
                List<Initializer> assigns = new List<Initializer>();
                var loc = it.Current.LocationRange;
                var len = 0;
                while (it.Current != null) {
                    assigns.Add(CheckInitializerBase(depth, type.ElementType, it, isLocalVariableInit));
                    len++;
                }
                // 型の長さを確定させる
                type.Length = len;
                return new Initializer.ArrayAssignInitializer(loc, type, assigns);
            } else {
                var len = type.Length;
                // 要素数分回す
                List<Initializer> assigns = new List<Initializer>();
                var loc = it.Current.LocationRange;
                while (it.Current != null && len > 0) {
                    assigns.Add(CheckInitializerBase(depth, type.ElementType, it, isLocalVariableInit));
                    len--;
                }
                return new Initializer.ArrayAssignInitializer(loc, type, assigns);
            }
        }

        /// <summary>
        /// 配列型に対する単純初期化式の解析
        /// </summary>
        /// <param name="depth"></param>
        /// <param name="type"></param>
        /// <param name="it"></param>
        /// <param name="isLocalVariableInit"></param>
        /// <returns></returns>
        private static Initializer CheckInitializerArrayBySimpleInitializer(int depth, ArrayType type, InitializerIterator it, bool isLocalVariableInit) {
            if (it.IsSimpleInitializer()) {
                Expression ae = it.AsSimpleInitializer().AssignmentExpression;
                while (ae is Expression.PrimaryExpression.EnclosedInParenthesesExpression) {
                    ae = ((Expression.PrimaryExpression.EnclosedInParenthesesExpression)ae).ParenthesesExpression;
                }
                if (ae is Expression.PrimaryExpression.StringExpression) {
                    return CheckInitializerArrayByStringExpressionInitializer(depth, type, it, isLocalVariableInit);
                }
                //throw new CompilerException.SpecificationErrorException(it.Current.LocationRange, "配列型に対する単純初期化式の要素が文字列ではありません。");
            }

            if (it.IsInComplexInitializer()) {
                return CheckInitializerArrayBySimpleInitializerInComplexInitializer(depth, type, it, isLocalVariableInit);
            }
            throw new Exception();
        }

        /// <summary>
        /// 配列型に対する初期化式の解析
        /// </summary>
        /// <param name="depth"></param>
        /// <param name="type"></param>
        /// <param name="it"></param>
        /// <param name="isLocalVariableInit"></param>
        /// <returns></returns>
        private static Initializer CheckInitializerArray(int depth, ArrayType type, InitializerIterator it, bool isLocalVariableInit) {
            if (it.IsComplexInitializer()) {
                return CheckInitializerArrayByComplexInitializer(depth, type, it, isLocalVariableInit);
            }

            if (it.IsSimpleInitializer()) {
                return CheckInitializerArrayBySimpleInitializer(depth, type, it, isLocalVariableInit);
            }

            throw new Exception();
        }

        /// <summary>
        /// 構造体型に対する初期化式の解析
        /// </summary>
        /// <param name="depth"></param>
        /// <param name="type"></param>
        /// <param name="it"></param>
        /// <param name="isLocalVariableInit"></param>
        /// <returns></returns>
        private static Initializer CheckInitializerStruct(int depth, TaggedType.StructUnionType type, InitializerIterator it, bool isLocalVariableInit) {
            if (it.IsComplexInitializer()) {
                // 要素数分回す
                List<Initializer> assigns = new List<Initializer>();
                var loc = it.Current.LocationRange;
                it.Enter();
                it.Next();
                var flexibleArrayMember = type.HasFlexibleArrayMember ? type.Members.Last() : null;
                foreach (var member in type.Members) {
                    if (it.Current == null) {
                        // 初期化要素の無いパディングも一応ゼロクリア
                        BitFieldType bft;
                        if (member.Type.IsBitField(out bft)) {
                            var kind = (bft.Type as BasicType).Kind;
                            assigns.Add(new Initializer.SimpleAssignInitializer(loc, member.Type, new Expression.PrimaryExpression.Constant.IntegerConstant(loc, "0", 0, kind)));
                        } else if (member.Type.IsBasicType()) {
                            var kind = (member.Type as BasicType).Kind;
                            switch (kind) {
                                case BasicType.TypeKind.Void:
                                    throw new CompilerException.InternalErrorException(it.Current.LocationRange, "void型フィールドを初期化しようとしています。（本処理系の不具合です。）");
                                case BasicType.TypeKind.KAndRImplicitInt:
                                case BasicType.TypeKind.Char:
                                case BasicType.TypeKind.SignedChar:
                                case BasicType.TypeKind.UnsignedChar:
                                case BasicType.TypeKind.SignedShortInt:
                                case BasicType.TypeKind.UnsignedShortInt:
                                case BasicType.TypeKind.SignedInt:
                                case BasicType.TypeKind.UnsignedInt:
                                case BasicType.TypeKind.SignedLongInt:
                                case BasicType.TypeKind.UnsignedLongInt:
                                case BasicType.TypeKind.SignedLongLongInt:
                                case BasicType.TypeKind.UnsignedLongLongInt:
                                    assigns.Add(new Initializer.SimpleAssignInitializer(loc, member.Type, new Expression.PrimaryExpression.Constant.IntegerConstant(loc, "0", 0, kind)));
                                    break;
                                case BasicType.TypeKind.Float:
                                case BasicType.TypeKind.Double:
                                case BasicType.TypeKind.LongDouble:
                                    assigns.Add(new Initializer.SimpleAssignInitializer(loc, member.Type, new Expression.PrimaryExpression.Constant.FloatingConstant(loc, "0", 0, kind)));
                                    break;
                                case BasicType.TypeKind._Bool:
                                    assigns.Add(new Initializer.SimpleAssignInitializer(loc, member.Type, new Expression.PrimaryExpression.Constant.IntegerConstant(loc, "0", 0, kind)));
                                    break;
                                case BasicType.TypeKind.Float_Complex:
                                case BasicType.TypeKind.Double_Complex:
                                case BasicType.TypeKind.LongDouble_Complex:
                                case BasicType.TypeKind.Float_Imaginary:
                                case BasicType.TypeKind.Double_Imaginary:
                                case BasicType.TypeKind.LongDouble_Imaginary:
                                default:
                                    throw new CompilerException.SpecificationErrorException(it.Current.LocationRange, "初期化ができない型です。");
                            }
                        } else if (member.Type.IsStructureType() || member.Type.IsUnionType()) {
                            assigns.Add(CheckInitializerStruct(depth + 1, member.Type as TaggedType.StructUnionType, new InitializerIterator(new Initializer.ComplexInitializer(loc, new List<Initializer>()) ), isLocalVariableInit));
                        } else if (member.Type.IsPointerType() || member.Type.IsEnumeratedType()) {
                            var kind = BasicType.TypeKind.UnsignedLongInt; // fake pointer type
                            assigns.Add(new Initializer.SimpleAssignInitializer(loc, member.Type, new Expression.PrimaryExpression.Constant.IntegerConstant(loc, "0", 0, kind)));
                        } else {
                            throw new CompilerException.SpecificationErrorException(it.Current.LocationRange, "初期化ができない型です。");
                        }
                        continue;
                    }
                    if (member == flexibleArrayMember) {
                        throw new CompilerException.SpecificationErrorException(it.Current.LocationRange, "フレキシブルメンバ要素を初期化することはできません。");
                    }
                    if (member.Ident == null) {
                        // padding
                        var kind = (member.Type.IsBitField()) ? ((member.Type as BitFieldType).Type as BasicType).Kind : (member.Type as BasicType).Kind;

                        assigns.Add(new Initializer.SimpleAssignInitializer(it.Current.LocationRange, member.Type, new Expression.PrimaryExpression.Constant.IntegerConstant(it.Current.LocationRange, "0", 0, kind)));
                        continue;
                    }
                    assigns.Add(CheckInitializerBase(depth, member.Type, it, isLocalVariableInit));
                }
                if (it.Current != null) {
                    Logger.Warning(it.Current.LocationRange, "初期化子の要素数が型で指定された領域サイズを超えているため、切り捨てられました。");
                }
                it.Leave();
                it.Next();
                return new Initializer.StructUnionAssignInitializer(loc, type, assigns);
            }

            if (it.IsInComplexInitializer()) {

#if true
                if (it.IsSimpleInitializer() && CType.IsEqual(it.AsSimpleInitializer().AssignmentExpression.Type,type)) {
                    if (isLocalVariableInit) {
                        return CheckInitializer2Simple(depth, type, it, true);   // 単純初期化に丸投げ
                    }

                    throw new CompilerException.SpecificationErrorException(it.Current.LocationRange, " 初期化子の要素が定数ではありません。");
                } else { 
#endif

                    // 初期化リスト内を舐めてる場合
                    // 要素数分回す
                    List<Initializer> assigns = new List<Initializer>();
                    if (it.Current != null) {
                        var loc = it.Current.LocationRange;
                        var flexibleArrayMember = type.HasFlexibleArrayMember ? type.Members.Last() : null;
                        foreach (var member in type.Members) {
                            if (it.Current == null) {
                                // 初期化要素の無いパディングも一応ゼロクリア
                                BitFieldType bft;
                                if (member.Type.IsBitField(out bft)) {
                                    var kind = (bft.Type as BasicType).Kind;
                                    assigns.Add(new Initializer.SimpleAssignInitializer(loc, member.Type, new Expression.PrimaryExpression.Constant.IntegerConstant(loc, "0", 0, kind)));
                                } else if (member.Type.IsBasicType()) {
                                    var kind = (member.Type as BasicType).Kind;
                                    assigns.Add(new Initializer.SimpleAssignInitializer(loc, member.Type, new Expression.PrimaryExpression.Constant.IntegerConstant(loc, "0", 0, kind)));
                                } else if (member.Type.IsStructureType() || member.Type.IsUnionType()) {
                                    assigns.Add(CheckInitializerStruct(depth + 1, member.Type as TaggedType.StructUnionType, new InitializerIterator(new Initializer.ComplexInitializer(loc, new List<Initializer>())), isLocalVariableInit));
                                } else if (member.Type.IsPointerType() || member.Type.IsEnumeratedType()) {
                                    var kind = BasicType.TypeKind.UnsignedLongInt; // fake pointer type
                                    assigns.Add(new Initializer.SimpleAssignInitializer(loc, member.Type, new Expression.PrimaryExpression.Constant.IntegerConstant(loc, "0", 0, kind)));
                                } else {
                                    throw new CompilerException.SpecificationErrorException(it.Current.LocationRange, "初期化ができない型です。");
                                }
                                continue;
                            }
                            if (member == flexibleArrayMember) {
                                throw new CompilerException.SpecificationErrorException(it.Current.LocationRange, "フレキシブルメンバ要素を初期化することはできません。");
                            }
                            if (member.Ident == null) {
                                // padding
                                var kind = (member.Type.IsBitField()) ? ((member.Type as BitFieldType).Type as BasicType).Kind : (member.Type as BasicType).Kind;
                                assigns.Add(new Initializer.SimpleAssignInitializer(it.Current.LocationRange, member.Type, new Expression.PrimaryExpression.Constant.IntegerConstant(it.Current.LocationRange, "0", 0, kind)));
                                continue;
                            }
                            assigns.Add(CheckInitializerBase(depth, member.Type, it, isLocalVariableInit));
                        }
                        return new Initializer.StructUnionAssignInitializer(loc, type, assigns);
                    }

                    return new Initializer.StructUnionAssignInitializer(LocationRange.Empty, type, assigns);
#if true
                }
#endif
            }

            if (it.IsSimpleInitializer()) {
                if (isLocalVariableInit) {
                    return CheckInitializer2Simple(depth, type, it, true);   // 単純初期化に丸投げ
                }

                throw new CompilerException.SpecificationErrorException(it.Current.LocationRange, " 初期化子の要素が定数ではありません。");
            }

            throw new Exception();
        }

        /// <summary>
        /// 共用体型に対する初期化式の解析
        /// </summary>
        /// <param name="depth"></param>
        /// <param name="type"></param>
        /// <param name="it"></param>
        /// <param name="isLocalVariableInit"></param>
        /// <returns></returns>
        private static Initializer CheckInitializerUnion(int depth, TaggedType.StructUnionType type, InitializerIterator it, bool isLocalVariableInit) {
            if (it.IsComplexInitializer()) {
                var inits = it.AsComplexInitializer().Ret;
                // 最初の要素とのみチェック
                List<Initializer> assigns = new List<Initializer>();
                var loc = it.Current.LocationRange;
                it.Enter();
                it.Next();
                var mem = type.Members.FirstOrDefault(x => !((x.Type is BitFieldType) && (x.Ident == null || (x.Type as BitFieldType).BitWidth == 0)));
                if (mem == null) {
                    throw new CompilerException.SpecificationErrorException(it.Current.LocationRange, "無名もしくはビット幅0のビットフィールドしかない共用体を初期化しようとしました。");
                }
                assigns.Add(CheckInitializerBase(depth, mem.Type, it, isLocalVariableInit));
                if (it.Current != null) {
                    Logger.Warning(it.Current.LocationRange, "初期化子の要素数が型で指定された領域サイズを超えているため、切り捨てられました。");
                }
                it.Leave();
                it.Next();
                return new Initializer.StructUnionAssignInitializer(loc, type, assigns);
            }

            if (it.IsInComplexInitializer()) {
                // 初期化リスト内を舐めてる場合
                // 最初の要素とのみチェック
                List<Initializer> assigns = new List<Initializer>();
                var loc = it.Current.LocationRange;
                var mem = type.Members.FirstOrDefault(x => !((x.Type is BitFieldType) && (x.Ident == null || (x.Type as BitFieldType).BitWidth == 0)));
                if (mem == null) {
                    throw new CompilerException.SpecificationErrorException(it.Current.LocationRange, "無名もしくはビット幅0のビットフィールドしかない共用体を初期化しようとしました。");
                }
                assigns.Add(CheckInitializerBase(depth, mem.Type, it, isLocalVariableInit));
                return new Initializer.StructUnionAssignInitializer(loc, type, assigns);
            }

            throw new Exception();
        }

        public static Initializer CheckInitializer(CType type, Initializer init, bool isLocalVariableInit) {
            var it = new InitializerIterator(init);
            var ret = CheckInitializerBase(0, type, it, isLocalVariableInit);
            return ret;
        }
    }
}
