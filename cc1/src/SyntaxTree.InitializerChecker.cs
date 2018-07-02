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
            public bool Enter() {
                if (Current is Initializer.ComplexInitializer) {
                    _inits.Push(Tuple.Create(_initializer, _index));
                    _initializer = Current;
                    _index = -1;
                    Current = null;
                    return true;
                }

                throw new Exception();
            }
            public bool Leave() {
                Current = _initializer;
                var top = _inits.Pop();
                _initializer = top.Item1;
                _index = top.Item2;
                return true;
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
                return ret;
            } else {
                var expr = it.AsSimpleInitializer().AssignmentExpression;
                if (type.IsAggregateType() || type.IsUnionType()) {
                    if (isLocalVariableInit == false) {
                        throw new CompilerException.SpecificationErrorException(it.Current.LocationRange, "集成体型又は共用体型をもつオブジェクトに対する初期化子は，要素又は名前付きメンバに対する初期化子並びを波括弧で囲んだものでなければならない。");
                    }
                }
                // 単純代入の規則を適用して検証

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
                assigns.Add(CheckInitializerBase(depth, type.BaseType, it, isLocalVariableInit));
            }
            if (type.Length == -1) {
                // 型の長さを確定させる
                type.Length = i;
            }
            it.Leave();
            it.Next();
            return new Initializer.ArrayAssignInitializer(loc, type, assigns);
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
            var sexpr = it.AsSimpleInitializer().AssignmentExpression as Expression.PrimaryExpression.StringExpression;
            if (type.Length == -1) {
                if (depth != 1) {
                    throw new CompilerException.SpecificationErrorException(it.Current.LocationRange, "ネストした可変配列を初期化しています。");
                }
                // 要素数分回す
                List<Initializer> assigns = new List<Initializer>();
                var loc = it.Current.LocationRange;
                var len = sexpr.Value.Count;
                foreach (var b in sexpr.Value) {
                    assigns.Add(new Initializer.SimpleAssignInitializer(loc, type.BaseType, new Expression.PrimaryExpression.Constant.IntegerConstant(loc, $"0x{b,0:X2}", b, BasicType.TypeKind.UnsignedChar)));
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
                    assigns.Add(new Initializer.SimpleAssignInitializer(loc, type.BaseType, new Expression.PrimaryExpression.Constant.IntegerConstant(loc, $"0x{b,0:X2}", b, BasicType.TypeKind.UnsignedChar)));
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
                    assigns.Add(CheckInitializerBase(depth, type.BaseType, it, isLocalVariableInit));
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
                    assigns.Add(CheckInitializerBase(depth, type.BaseType, it, isLocalVariableInit));
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
            if (type.BaseType.IsBasicType(BasicType.TypeKind.Char, BasicType.TypeKind.SignedChar, BasicType.TypeKind.UnsignedChar)) {
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
                    assigns.Add(CheckInitializerBase(depth, type.BaseType, it, isLocalVariableInit));
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
                    assigns.Add(CheckInitializerBase(depth, type.BaseType, it, isLocalVariableInit));
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
            if (it.AsSimpleInitializer().AssignmentExpression is Expression.PrimaryExpression.StringExpression) {
                return CheckInitializerArrayByStringExpressionInitializer(depth, type, it, isLocalVariableInit);
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
                        break;
                    }
                    if (member == flexibleArrayMember) {
                        throw new CompilerException.SpecificationErrorException(it.Current.LocationRange, "フレキシブルメンバ要素を初期化することはできません。");
                    }
                    if (member.Ident == null) {
                        // padding
                        if (member.Type.IsBitField()) {
                            assigns.Add(new Initializer.SimpleAssignInitializer(it.Current.LocationRange, member.Type, new Expression.PrimaryExpression.Constant.IntegerConstant(it.Current.LocationRange, "0", 0, ((member.Type as BitFieldType).Type as BasicType).Kind)));
                        } else {
                            assigns.Add(new Initializer.SimpleAssignInitializer(it.Current.LocationRange, member.Type, new Expression.PrimaryExpression.Constant.IntegerConstant(it.Current.LocationRange, "0", 0, (member.Type as BasicType).Kind)));
                        }
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
                // 初期化リスト内を舐めてる場合
                // 要素数分回す
                List<Initializer> assigns = new List<Initializer>();
                if (it.Current != null) {
                    var loc = it.Current.LocationRange;
                    var flexibleArrayMember = type.HasFlexibleArrayMember ? type.Members.Last() : null;
                    foreach (var member in type.Members) {
                        if (it.Current == null) {
                            break;
                        }
                        if (member == flexibleArrayMember) {
                            throw new CompilerException.SpecificationErrorException(it.Current.LocationRange, "フレキシブルメンバ要素を初期化することはできません。");
                        }
                        if (member.Ident == null) {
                            // padding
                            if (member.Type.IsBitField()) {
                                assigns.Add(new Initializer.SimpleAssignInitializer(it.Current.LocationRange, member.Type, new Expression.PrimaryExpression.Constant.IntegerConstant(it.Current.LocationRange, "0", 0, ((member.Type as BitFieldType).Type as BasicType).Kind)));
                            } else {
                                assigns.Add(new Initializer.SimpleAssignInitializer(it.Current.LocationRange, member.Type, new Expression.PrimaryExpression.Constant.IntegerConstant(it.Current.LocationRange, "0", 0, (member.Type as BasicType).Kind)));
                            }
                            continue;
                        }
                        assigns.Add(CheckInitializerBase(depth, member.Type, it, isLocalVariableInit));
                    }
                    return new Initializer.StructUnionAssignInitializer(loc, type, assigns);
                }

                return new Initializer.StructUnionAssignInitializer(LocationRange.Empty, type, assigns);
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
                assigns.Add(CheckInitializerBase(depth, type.Members[0].Type, it, isLocalVariableInit));
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
                assigns.Add(CheckInitializerBase(depth, type.Members[0].Type, it, isLocalVariableInit));
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
