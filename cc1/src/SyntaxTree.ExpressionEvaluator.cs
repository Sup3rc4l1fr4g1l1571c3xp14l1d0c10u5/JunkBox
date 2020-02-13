using System;
using AnsiCParser.DataType;

namespace AnsiCParser.SyntaxTree {
    /// <summary>
    /// 評価器
    /// </summary>
    public static class ExpressionEvaluator {

        /// <summary>
        /// 式の評価結果をdouble型として得る。得られなかった場合は例外を投げる。
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        private static double DoubleValue(this Expression self) {
            if (self is Expression.PrimaryExpression.Constant.IntegerConstant) {
                return (double)(((Expression.PrimaryExpression.Constant.IntegerConstant)self).Value);
            }
            if (self is Expression.PrimaryExpression.Constant.CharacterConstant) {
                return (double)(((Expression.PrimaryExpression.Constant.CharacterConstant)self).Value);
            }
            if (self is Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant) {
                return (double)(((Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant)self).Info.Value);
            }
            if (self is Expression.PrimaryExpression.Constant.FloatingConstant) {
                return (double)(((Expression.PrimaryExpression.Constant.FloatingConstant)self).Value);
            }
            throw new NotSupportedException(self.GetType().Name);
        }

        /// <summary>
        /// 式の評価結果をlong型として得る。得られなかった場合は例外を投げる。
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        private static Tuple<Expression.PrimaryExpression.IdentifierExpression, long> LongValue(this Expression self) {
            var ret = AsLongValue(self);
            // 初期化子の要素が定数ではありません等
            if (ret!=null) {
                return ret;
            } else {
                throw new NotSupportedException(self.GetType().Name);
            }
        }

        /// <summary>
        /// 式の評価結果をlong型として得る
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        private static Tuple<Expression.PrimaryExpression.IdentifierExpression, long> AsLongValue(this Expression self) {
            if (self is Expression.PrimaryExpression.Constant.IntegerConstant) {
                return new Tuple<Expression.PrimaryExpression.IdentifierExpression, long>(null, ((Expression.PrimaryExpression.Constant.IntegerConstant)self).Value);
            }
            if (self is Expression.PrimaryExpression.Constant.CharacterConstant) {
                return new Tuple<Expression.PrimaryExpression.IdentifierExpression, long>(null, ((Expression.PrimaryExpression.Constant.CharacterConstant)self).Value);
            }
            if (self is Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant) {
                return new Tuple<Expression.PrimaryExpression.IdentifierExpression, long>(null, ((Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant)self).Info.Value);
            }
            if (self is Expression.PrimaryExpression.Constant.FloatingConstant) {
                return new Tuple<Expression.PrimaryExpression.IdentifierExpression, long>(null, (long)((Expression.PrimaryExpression.Constant.FloatingConstant)self).Value);
            }
            if (self is Expression.PrimaryExpression.StringExpression) {
                var se = ((Expression.PrimaryExpression.StringExpression)self);
                var id = new Expression.PrimaryExpression.IdentifierExpression.ObjectConstant(se.LocationRange, se.Type, se.Label, se);
                return new Tuple<Expression.PrimaryExpression.IdentifierExpression, long>(id, (long)0);
            }
            if (self is Expression.PrimaryExpression.AddressConstantExpression) {
                var ae = ((Expression.PrimaryExpression.AddressConstantExpression)self);
                return new Tuple<Expression.PrimaryExpression.IdentifierExpression, long>(ae.Identifier, ae.Offset.Value);
            }
            return null;
        }

        /// <summary>
        /// 定数式の評価を行うVisitor
        /// </summary>
        private class SyntaxTreeConstantEvaluatorVisitor : IVisitor<Expression, Expression> {
            // 6.6 定数式
            // 補足説明  
            // 定数式は，実行時ではなく翻訳時に評価することができる。したがって，定数を使用してよいところならばどこでも使用してよい。
            //
            // 制約
            // - 定数式は，代入，増分，減分，関数呼出し又はコンマ演算子を含んではならない。
            //   ただし，定数式が評価されない部分式(sizeof演算子のオペランド等)に含まれている場合を除く。
            // - 定数式を評価した結果は，その型で表現可能な値の範囲内にある定数でなければならない。
            // 

            private bool _failed = false;

            private Expression AcceptTo(Expression e, Expression value) {
                if (_failed) {
                    return null;
                } else {
                    return e.Accept(this, value);
                }
            }

            public Expression OnAdditiveExpression(Expression.AdditiveExpression self, Expression value) {
                // 意味規則
                // 両オペランドが算術型をもつ場合，通常の算術型変換をそれらに適用する(実装注釈: AdditiveExpressionクラスのコンストラクタ内で適用済み)
                // 2項+演算子の結果は，両オペランドの和とする。
                // 2項-演算子の結果は，第 1 オペランドから第 2 オペランドを引いた結果の差とする。
                // これらの演算子に関しては，配列の要素でないオブジェクトへのポインタは，要素型としてそのオブジェクトの型をもつ長さ 1 の配列の最初の要素へのポインタと同じ動作をする。
                // 整数型をもつ式をポインタに加算又はポインタから減算する場合，結果は，ポインタオペランドの型をもつ。
                // ポインタオペランドが配列オブジェクトの要素を指し，配列が十分に大きい場合，その結果は，その配列の要素を指し，演算結果の要素と元の配列要素の添字の差は，整数式の値に等しい。
                // すなわち，式 P が配列オブジェクトの i 番目の要素を指している場合，式(P)+N（N+(P)と等しい）及び(P)-N（N は値nをもつと仮定する。）は，それらが存在するのであれば，それぞれ配列オブジェクトのi+n番目及びi-n番目の要素を指す。
                // さらに，式 P が配列オブジェクトの最後の要素を指す場合，式(P)+1 はその配列オブジェクトの最後の要素を一つ越えたところを指し，式 Q が配列オブジェクトの最後の要素を一つ越えたところを指す場合，式(Q)-1 はその配列オブジェクトの最後の要素を指す。
                // ポインタオペランド及びその結果の両方が同じ配列オブジェクトの要素，又は配列オブジェクトの最後の要素を一つ越えたところを指している場合，演算によって，オーバフローを生じてはならない。
                // それ以外の場合，動作は未定義とする。
                // 結果が配列オブジェクトの最後の要素を一つ越えたところを指す場合，評価される単項*演算子のオペランドとしてはならない。

                var lhs = self.Lhs.Accept(this, value);
                var rhs = self.Rhs.Accept(this, value);

                if (self.Type.IsRealFloatingType()) {
                    switch (self.Op) {
                        case Expression.AdditiveExpression.OperatorKind.Add:
                            return new Expression.PrimaryExpression.Constant.FloatingConstant(self.LocationRange, "", lhs.DoubleValue() + rhs.DoubleValue(), ((BasicType)self.Type.Unwrap()).Kind);
                        case Expression.AdditiveExpression.OperatorKind.Sub:
                            return new Expression.PrimaryExpression.Constant.FloatingConstant(self.LocationRange, "", lhs.DoubleValue() - rhs.DoubleValue(), ((BasicType)self.Type.Unwrap()).Kind);
                        default:
                            throw new CompilerException.InternalErrorException(self.LocationRange, "定数式中の加算式部分で加算でも減算でもない演算子が登場しています。（本処理系の誤りが原因です。）");
                    }
                } else if (self.Type.IsIntegerType()) {
                    switch (self.Op) {
                        case Expression.AdditiveExpression.OperatorKind.Add: {
                                var lhv = lhs.LongValue();
                                var rhv = rhs.LongValue();
                                if (lhv.Item1 == null && rhv.Item1 == null) {
                                    // どちらも定数
                                    return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", lhv.Item2 + rhv.Item2, ((BasicType)self.Type.Unwrap()).Kind);
                                } else if (lhv.Item1 != null && rhv.Item1 == null) {
                                    // 左辺がラベル定数で右辺が定数
                                    var off = lhv.Item2 + rhv.Item2;
                                    return new Expression.PrimaryExpression.AddressConstantExpression(self.LocationRange, lhv.Item1, self.Type, new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, off.ToString(), off, BasicType.TypeKind.SignedInt));
                                } else if (lhv.Item1 == null && rhv.Item1 != null) {
                                    // 左辺が定数で右辺がラベル定数
                                    var off = lhv.Item2 + rhv.Item2;
                                    return new Expression.PrimaryExpression.AddressConstantExpression(self.LocationRange, rhv.Item1, self.Type, new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, off.ToString(), off, BasicType.TypeKind.SignedInt));
                                } else {
                                    // 両方がラベル定数
                                    throw new NotImplementedException();
                                }
                            }
                        case Expression.AdditiveExpression.OperatorKind.Sub: {
                                var lhv = lhs.LongValue();
                                var rhv = rhs.LongValue();
                                if (lhv.Item1 == null && rhv.Item1 == null) {
                                    // どちらも定数
                                    return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", lhv.Item2 - rhv.Item2, ((BasicType)self.Type.Unwrap()).Kind);
                                } else if (lhv.Item1 != null && rhv.Item1 == null) {
                                    // 左辺がラベル定数で右辺が定数
                                    var off = lhv.Item2 - rhv.Item2;
                                    return new Expression.PrimaryExpression.AddressConstantExpression(self.LocationRange, lhv.Item1, self.Type, new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, off.ToString(), off, BasicType.TypeKind.SignedInt));
                                } else if (lhv.Item1 == null && rhv.Item1 != null) {
                                    // 左辺が定数で右辺がラベル定数
                                    var off = lhv.Item2 - rhv.Item2;
                                    return new Expression.PrimaryExpression.AddressConstantExpression(self.LocationRange, rhv.Item1, self.Type, new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, off.ToString(), off, BasicType.TypeKind.SignedInt));
                                } else if (lhv.Item1 == rhv.Item1) {
                                    // 両方が同一のラベル定数
                                    return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", lhv.Item2 - rhv.Item2, ((BasicType)self.Type.Unwrap()).Kind);
                                } else {
                                    // 両方がラベル定数
                                    throw new NotImplementedException();
                                }
                            }

                        default:
                            throw new CompilerException.InternalErrorException(self.LocationRange, "定数式中の加算式部分で加算でも減算でもない演算子が登場しています。（本処理系の誤りが原因です。）");
                    }
                } else {
                    if (lhs is Expression.PrimaryExpression.AddressConstantExpression && rhs is Expression.PrimaryExpression.Constant && rhs.Type.IsIntegerType()) {
                        var adc = lhs as Expression.PrimaryExpression.AddressConstantExpression;
                        Expression.PrimaryExpression.Constant.IntegerConstant off;
                        CType elementType;
                        if (adc.Type.IsArrayType(out elementType) == false) {
                            elementType = adc.Type.GetBasePointerType();
                        }
                        //var stride = adc.Type.GetBasePointerType().SizeOf();
                        var stride = elementType.SizeOf();
                        switch (self.Op) {
                            case Expression.AdditiveExpression.OperatorKind.Add: {
                                var adv = adc.Offset.LongValue();
                                var rhv = rhs.LongValue();
                                    if (adv.Item1 == null && rhv.Item1 == null) {
                                        // どちらも定数
                                        off = new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", adv.Item2 + rhv.Item2 * stride, ((BasicType)adc.Offset.Type.Unwrap()).Kind);
                                    } else {
                                        // どちらか一方、もしくは両方がラベル定数
                                        throw new NotImplementedException();
                                    }
                                    break;
                                }
                            case Expression.AdditiveExpression.OperatorKind.Sub: {
                                    var adv = adc.Offset.LongValue();
                                    var rhv = rhs.LongValue();
                                    if (adv.Item1 == null && rhv.Item1 == null) {
                                        // どちらも定数
                                        off = new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", adv.Item2 - rhv.Item2 * stride, ((BasicType)adc.Offset.Type.Unwrap()).Kind);
                                    } else {
                                        // どちらか一方、もしくは両方がラベル定数
                                        throw new NotImplementedException();
                                    }
                                break;
                                }
                            default:
                                throw new CompilerException.InternalErrorException(self.LocationRange, "定数式中の加算式部分で加算でも減算でもない演算子が登場しています。（本処理系の誤りが原因です。）");
                        }

                        return new Expression.PrimaryExpression.AddressConstantExpression(adc.LocationRange, adc.Identifier, /* adc.Type */ CType.CreatePointer(elementType), off);
                    } else if (rhs is Expression.PrimaryExpression.AddressConstantExpression && lhs is Expression.PrimaryExpression.Constant && lhs.Type.IsIntegerType()) {
                        var adc = rhs as Expression.PrimaryExpression.AddressConstantExpression;
                        Expression.PrimaryExpression.Constant.IntegerConstant off;
                        CType elementType;
                        if (adc.Type.IsArrayType(out elementType) == false) {
                            elementType = adc.Type.GetBasePointerType();
                        }
                        //var stride = adc.Type.GetBasePointerType().SizeOf();
                        var stride = elementType.SizeOf();
                        switch (self.Op) {
                            case Expression.AdditiveExpression.OperatorKind.Add: {
                                    var adv = adc.Offset.LongValue();
                                    var lhv = lhs.LongValue();
                                    if (adv.Item1 == null && lhv.Item1 == null) {
                                        // どちらも定数
                                        off = new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", adv.Item2 + lhv.Item2 * stride, ((BasicType)adc.Offset.Type.Unwrap()).Kind);
                                    } else {
                                        // どちらか一方、もしくは両方がラベル定数
                                        throw new NotImplementedException();
                                    }
                                    break;
                                }
                            case Expression.AdditiveExpression.OperatorKind.Sub: {
                                    var adv = adc.Offset.LongValue();
                                    var lhv = lhs.LongValue();
                                    if (adv.Item1 == null && lhv.Item1 == null) {
                                        // どちらも定数
                                        off = new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", adv.Item2 - lhv.Item2 * stride, ((BasicType)adc.Offset.Type.Unwrap()).Kind);
                                    } else {
                                        // どちらか一方、もしくは両方がラベル定数
                                        throw new NotImplementedException();
                                    }
                                    break;
                                }
                            //case Expression.AdditiveExpression.OperatorKind.Add:
                            //    off = new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", adc.Offset.LongValue() + lhs.LongValue() * stride, ((BasicType)adc.Offset.Type.Unwrap()).Kind);
                            //    break;
                            //case Expression.AdditiveExpression.OperatorKind.Sub:
                            //    off = new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", adc.Offset.LongValue() - lhs.LongValue() * stride, ((BasicType)adc.Offset.Type.Unwrap()).Kind);
                            //    break;
                            default:
                                throw new CompilerException.InternalErrorException(self.LocationRange, "定数式中の加算式部分で加算でも減算でもない演算子が登場しています。（本処理系の誤りが原因です。）");
                        }

                        return new Expression.PrimaryExpression.AddressConstantExpression(adc.LocationRange, adc.Identifier, /* adc.Type */ CType.CreatePointer(elementType), off);
                    }
                    switch (self.Op) {

                        case Expression.AdditiveExpression.OperatorKind.Add:
                            return new Expression.AdditiveExpression(self.LocationRange, Expression.AdditiveExpression.OperatorKind.Add, lhs, rhs);
                        case Expression.AdditiveExpression.OperatorKind.Sub:
                            return new Expression.AdditiveExpression(self.LocationRange, Expression.AdditiveExpression.OperatorKind.Sub, lhs, rhs);
                        default:
                            throw new CompilerException.InternalErrorException(self.LocationRange, "定数式中の加算式部分で加算でも減算でもない演算子が登場しています。（本処理系の誤りが原因です。）");
                    }
                }
            }

            public Expression OnAddressConstantExpression(Expression.PrimaryExpression.AddressConstantExpression self, Expression value) {
                throw new Exception();
            }

            public Expression OnAndExpression(Expression.BitExpression.AndExpression self, Expression value) {
                var lhs = self.Lhs.Accept(this, value);
                var rhs = self.Rhs.Accept(this, value);
                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    var lhv = lhs.LongValue();
                    var rhv = rhs.LongValue();
                    if (lhv.Item1 == null && rhv.Item1 == null) {
                        var ret = lhv.Item2 & rhv.Item2;
                        return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", ret, ((BasicType)self.Type.Unwrap()).Kind);
                    }
                }
                throw new Exception();
            }

            public Expression OnArgumentDeclaration(Declaration.ArgumentDeclaration self, Expression value) {
                throw new NotImplementedException();
            }

            public Expression OnArgumentExpression(Expression.PrimaryExpression.IdentifierExpression.ArgumentExpression self, Expression value) {
                throw new CompilerException.InternalErrorException(self.LocationRange, "定数式中で引数変数は利用できません。");
            }

            public Expression OnArrayAssignInitializer(Initializer.ArrayAssignInitializer self, Expression value) {
                throw new NotImplementedException();
            }

            public Expression OnArraySubscriptingExpression(Expression.PostfixExpression.ArraySubscriptingExpression self, Expression value) {
                throw new Exception("");
            }

            public Expression OnBreakStatement(Statement.BreakStatement self, Expression value) {
                throw new NotImplementedException();
            }

            public Expression OnCaseStatement(Statement.CaseStatement self, Expression value) {
                throw new NotImplementedException();
            }

            public Expression OnCastExpression(Expression.CastExpression self, Expression value) {
                var ret = self.Expr.Accept(this, value);

                if (ret is Expression.PrimaryExpression.Constant.IntegerConstant) {
                    long reti = (ret as Expression.PrimaryExpression.Constant.IntegerConstant).Value;

                    if (self.Type.Unwrap() is BasicType) {
                        var bt = ((BasicType)self.Type.Unwrap()).Kind;
                        switch (bt) {
                            case BasicType.TypeKind.Char:
                            case BasicType.TypeKind.SignedChar:
                                return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", (long)((sbyte)reti), bt);
                            case BasicType.TypeKind.SignedShortInt:
                                return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", (long)((short)reti), bt);
                            case BasicType.TypeKind.SignedInt:
                            case BasicType.TypeKind.SignedLongInt:
                            case BasicType.TypeKind._Bool:
                                return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", (long)((int)reti), bt);
                            case BasicType.TypeKind.SignedLongLongInt:
                                return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", (long)((long)reti), bt);
                            case BasicType.TypeKind.UnsignedChar:
                                return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", (long)((byte)reti), bt);
                            case BasicType.TypeKind.UnsignedShortInt:
                                return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", (long)((ushort)reti), bt);
                            case BasicType.TypeKind.UnsignedInt:
                            case BasicType.TypeKind.UnsignedLongInt:
                                return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", (long)((uint)reti), bt);
                            case BasicType.TypeKind.UnsignedLongLongInt:
                                return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", (long)((ulong)reti), bt);
                            case BasicType.TypeKind.Float:
                                return new Expression.PrimaryExpression.Constant.FloatingConstant(self.LocationRange, "", (float)reti, bt);
                            case BasicType.TypeKind.Double:
                                return new Expression.PrimaryExpression.Constant.FloatingConstant(self.LocationRange, "", (double)reti, bt);
                            case BasicType.TypeKind.LongDouble:
                                return new Expression.PrimaryExpression.Constant.FloatingConstant(self.LocationRange, "", (double)reti, bt);
                            default:
                                throw new NotSupportedException();
                        }
                    } else if (self.Type.Unwrap().IsPointerType()) {
                        var rev = ret.LongValue();
                        return new Expression.PrimaryExpression.AddressConstantExpression(ret.LocationRange, rev.Item1, self.Type, new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "0", rev.Item2, BasicType.TypeKind.SignedInt));
                    } else {
                        throw new NotSupportedException();
                    }
                } else if (ret is Expression.PrimaryExpression.Constant.FloatingConstant) {
                    double reti = (ret as Expression.PrimaryExpression.Constant.FloatingConstant).Value;

                    if (self.Type.Unwrap() is BasicType) {
                        var bt = ((BasicType)self.Type.Unwrap()).Kind;
                        switch (bt) {
                            case BasicType.TypeKind.Char:
                            case BasicType.TypeKind.SignedChar:
                                return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", (long)((sbyte)reti), bt);
                            case BasicType.TypeKind.SignedShortInt:
                                return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", (long)((short)reti), bt);
                            case BasicType.TypeKind.SignedInt:
                            case BasicType.TypeKind.SignedLongInt:
                            case BasicType.TypeKind._Bool:
                                return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", (long)((int)reti), bt);
                            case BasicType.TypeKind.SignedLongLongInt:
                                return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", (long)((long)reti), bt);
                            case BasicType.TypeKind.UnsignedChar:
                                return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", (long)((byte)reti), bt);
                            case BasicType.TypeKind.UnsignedShortInt:
                                return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", (long)((ushort)reti), bt);
                            case BasicType.TypeKind.UnsignedInt:
                            case BasicType.TypeKind.UnsignedLongInt:
                                return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", (long)((uint)reti), bt);
                            case BasicType.TypeKind.UnsignedLongLongInt:
                                return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", (long)((ulong)reti), bt);
                            case BasicType.TypeKind.Float:
                                return new Expression.PrimaryExpression.Constant.FloatingConstant(self.LocationRange, "", (float)reti, bt);
                            case BasicType.TypeKind.Double:
                                return new Expression.PrimaryExpression.Constant.FloatingConstant(self.LocationRange, "", (double)reti, bt);
                            case BasicType.TypeKind.LongDouble:
                                return new Expression.PrimaryExpression.Constant.FloatingConstant(self.LocationRange, "", (double)reti, bt);
                            default:
                                throw new NotSupportedException();
                        }
                    } else {
                        throw new NotSupportedException();
                    }
                } else if (ret is Expression.PrimaryExpression.AddressConstantExpression) {
                    var ace = ret as Expression.PrimaryExpression.AddressConstantExpression;
                    return new Expression.PrimaryExpression.AddressConstantExpression(ace.LocationRange, ace.Identifier, self.Type, ace.Offset);
                } else if (ret is Expression.PrimaryExpression.StringExpression) {
                    // @@@
                    var ace = ret as Expression.PrimaryExpression.StringExpression;
                    var id = new Expression.PrimaryExpression.IdentifierExpression.ObjectConstant(ace.LocationRange, ace.Type, ace.Label, ace);
                    return new Expression.PrimaryExpression.AddressConstantExpression(ace.LocationRange, id, self.Type, new Expression.PrimaryExpression.Constant.IntegerConstant(ace.LocationRange,"0",0,BasicType.TypeKind.SignedInt));
                } else {
                    throw new NotSupportedException();
                }

                throw new NotImplementedException();
            }

            public Expression OnCharacterConstant(Expression.PrimaryExpression.Constant.CharacterConstant self, Expression value) {
                return self;
            }

            public Expression OnCommaExpression(Expression.CommaExpression self, Expression value) {
                throw new CompilerException.SpecificationErrorException(self.LocationRange, "定数式は，代入，増分，減分，関数呼出し又はコンマ演算子を含んではならない。");
            }

            public Expression OnComplexInitializer(Initializer.ComplexInitializer self, Expression value) {
                throw new NotImplementedException();
            }

            public Expression OnCompoundAssignmentExpression(Expression.AssignmentExpression.CompoundAssignmentExpression self, Expression value) {
                throw new NotImplementedException();
            }

            public Expression OnCompoundStatementC89(Statement.CompoundStatementC89 self, Expression value) {
                throw new NotImplementedException();
            }
            public Expression OnCompoundStatementC99(Statement.CompoundStatementC99 self, Expression value) {
                throw new NotImplementedException();
            }

            public Expression OnConditionalExpression(Expression.ConditionalExpression self, Expression value) {
                var cond = self.CondExpr.Accept(this, value);
                var condv = cond.LongValue();
                if (condv.Item1 == null && condv.Item2 == 0) {
                    return self.ElseExpr.Accept(this, value);
                } else {
                    return self.ThenExpr.Accept(this, value);
                }
            }

            public Expression OnContinueStatement(Statement.ContinueStatement self, Expression value) {
                throw new NotImplementedException();
            }

            public Expression OnDefaultStatement(Statement.DefaultStatement self, Expression value) {
                throw new NotImplementedException();
            }

            public Expression OnDoWhileStatement(Statement.DoWhileStatement self, Expression value) {
                throw new NotImplementedException();
            }

            public Expression OnEmptyStatement(Statement.EmptyStatement self, Expression value) {
                throw new NotImplementedException();
            }

            public Expression OnEnclosedInParenthesesExpression(Expression.PrimaryExpression.EnclosedInParenthesesExpression self, Expression value) {
                return self.ParenthesesExpression.Accept(this, value);
            }

            public Expression OnEnumerationConstant(Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant self, Expression value) {
                return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", self.Info.Value, ((BasicType)self.Type.Unwrap()).Kind);
            }

            public Expression OnEqualityExpression(Expression.EqualityExpression self, Expression value) {
                var lhs = self.Lhs.Accept(this, value);
                var rhs = self.Rhs.Accept(this, value);
                bool ret;
                if (lhs.Type.IsRealFloatingType() || rhs.Type.IsRealFloatingType()) {
                    // Todo: 浮動小数点数同士の比較を警告
                    ret = lhs.DoubleValue() == rhs.DoubleValue();
                } else {
                    ret = lhs.LongValue() == rhs.LongValue();
                }
                switch (self.Op) {
                    case Expression.EqualityExpression.OperatorKind.Equal:
                        return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", ret ? 1 : 0, ((BasicType)self.Type.Unwrap()).Kind);
                    case Expression.EqualityExpression.OperatorKind.NotEqual:
                        return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", ret ? 1 : 0, ((BasicType)self.Type.Unwrap()).Kind);
                    default:
                        throw new Exception();
                }
            }

            public Expression OnExclusiveOrExpression(Expression.BitExpression.ExclusiveOrExpression self, Expression value) {
                var lhs = self.Lhs.Accept(this, value);
                var rhs = self.Rhs.Accept(this, value);
                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    var lhv = lhs.LongValue();
                    var rhv = rhs.LongValue();
                    if (lhv.Item1 == null && rhv.Item1 == null) {
                        var ret = lhv.Item2 ^ rhv.Item2;
                        return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", ret, ((BasicType)self.Type.Unwrap()).Kind);
                    }
                }
                throw new Exception();
            }

            public Expression OnExpressionStatement(Statement.ExpressionStatement self, Expression value) {
                throw new NotImplementedException();
            }

            public Expression OnFloatingConstant(Expression.PrimaryExpression.Constant.FloatingConstant self, Expression value) {
                return self;
            }

            public Expression OnForStatement(Statement.ForStatement self, Expression value) {
                throw new NotImplementedException();
            }

            public Expression OnFunctionCallExpression(Expression.PostfixExpression.FunctionCallExpression self, Expression value) {
                throw new CompilerException.SpecificationErrorException(self.LocationRange, "定数式は，代入，増分，減分，関数呼出し又はコンマ演算子を含んではならない。");
            }

            public Expression OnFunctionDeclaration(Declaration.FunctionDeclaration self, Expression value) {
                throw new NotImplementedException();
            }

            public Expression OnFunctionExpression(Expression.PrimaryExpression.IdentifierExpression.FunctionExpression self, Expression value) {
                return new Expression.PrimaryExpression.AddressConstantExpression(self.LocationRange, self, CType.CreatePointer(self.Type), new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "0", 0, BasicType.TypeKind.SignedInt));
            }

            public Expression OnGccStatementExpression(Expression.GccStatementExpression self, Expression value) {
                throw new NotImplementedException();
            }

            public Expression OnGenericLabeledStatement(Statement.GenericLabeledStatement self, Expression value) {
                throw new NotImplementedException();
            }

            public Expression OnGotoStatement(Statement.GotoStatement self, Expression value) {
                throw new NotImplementedException();
            }

            public Expression OnIfStatement(Statement.IfStatement self, Expression value) {
                throw new NotImplementedException();
            }

            public Expression OnInclusiveOrExpression(Expression.BitExpression.InclusiveOrExpression self, Expression value) {
                var lhs = self.Lhs.Accept(this, value);
                var rhs = self.Rhs.Accept(this, value);
                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    var lhv = lhs.LongValue();
                    var rhv = rhs.LongValue();
                    if (lhv.Item1 == null && rhv.Item1 == null) {
                        var ret = lhv.Item2 | rhv.Item2;
                        return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", ret, ((BasicType)self.Type.Unwrap()).Kind);
                    }
                }
                throw new Exception();
            }

            public Expression OnIntegerConstant(Expression.PrimaryExpression.Constant.IntegerConstant self, Expression value) {
                return self;
            }

            public Expression OnIntegerPromotionExpression(Expression.IntegerPromotionExpression self, Expression value) {
                var ret = self.Expr.Accept(this, value);
                var rev = ret.LongValue();
                if (rev.Item1 == null) {
                    return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", rev.Item2, ((BasicType)self.Type.Unwrap()).Kind);
                } else {
                    return new Expression.PrimaryExpression.AddressConstantExpression(self.LocationRange, rev.Item1, self.Type, new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", rev.Item2, ((BasicType)self.Type.Unwrap()).Kind));
                }
            }

            public Expression OnLogicalAndExpression(Expression.LogicalAndExpression self, Expression value) {
                var lhs = self.Lhs.Accept(this, value);
                var lhv = lhs.LongValue();
                var ret = false;
                if (lhv.Item1 == null && lhv.Item2 == 0) {
                    ret = false;
                } else {
                    var rhs = self.Lhs.Accept(this, value);
                    var rhv = rhs.LongValue();
                    if (rhv.Item1 == null && rhv.Item2 == 0) {
                        ret = false;
                    } else {
                        ret = true;
                    }
                }
                return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", ret ? 1 : 0, ((BasicType)self.Type.Unwrap()).Kind);
            }

            public Expression OnLogicalOrExpression(Expression.LogicalOrExpression self, Expression value) {
                var lhs = self.Lhs.Accept(this, value);
                var lhv = lhs.LongValue();
                var ret = true;
                if (lhv.Item1 == null && lhv.Item2 == 0) {
                    var rhs = self.Lhs.Accept(this, value);
                    var rhv = rhs.LongValue();
                    if (rhv.Item1 == null && rhv.Item2 == 0) {
                        ret = false;
                    } else {
                        ret = true;
                    }
                } else {
                    ret = true;
                }
                return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", ret ? 1 : 0, ((BasicType)self.Type.Unwrap()).Kind);
            }

            public Expression OnMemberDirectAccess(Expression.PostfixExpression.MemberDirectAccess self, Expression value) {
                throw new CompilerException.SpecificationErrorException(self.LocationRange, "定数式中に直接メンバアクセス演算子が含まれています。");
            }

            public Expression OnMemberIndirectAccess(Expression.PostfixExpression.MemberIndirectAccess self, Expression value) {
                var expr = this.AcceptTo(self.Expr, value);
                if (!(expr is Expression.PrimaryExpression.AddressConstantExpression)) {
                    throw new CompilerException.SpecificationErrorException(self.LocationRange, "定数式中ではアドレス定数以外に間接メンバアクセス演算子を適用できません。");
                }

                var addrConstExpr = expr as Expression.PrimaryExpression.AddressConstantExpression;
                return new Expression.PrimaryExpression.AddressConstantExpression(
                    self.LocationRange,
                    addrConstExpr.Identifier,
                    self.MemberInfo.Type,
                    new Expression.PrimaryExpression.Constant.IntegerConstant(
                        self.LocationRange,
                        "",
                        self.MemberInfo.Offset + addrConstExpr.Offset.Value,
                        BasicType.TypeKind.SignedInt
                    )
                );
            }

            public Expression OnMultiplicativeExpression(Expression.MultiplicativeExpression self, Expression value) {
                var lhs = self.Lhs.Accept(this, value);
                var rhs = self.Rhs.Accept(this, value);

                if (self.Type.IsRealFloatingType()) {
                    switch (self.Op) {
                        case Expression.MultiplicativeExpression.OperatorKind.Mul:
                            return new Expression.PrimaryExpression.Constant.FloatingConstant(self.LocationRange, "", lhs.DoubleValue() * rhs.DoubleValue(), ((BasicType)self.Type.Unwrap()).Kind);
                        case Expression.MultiplicativeExpression.OperatorKind.Div:
                            return new Expression.PrimaryExpression.Constant.FloatingConstant(self.LocationRange, "", lhs.DoubleValue() / rhs.DoubleValue(), ((BasicType)self.Type.Unwrap()).Kind);
                        case Expression.MultiplicativeExpression.OperatorKind.Mod:
                            throw new CompilerException.SpecificationErrorException(self.LocationRange, "定数式中で浮動小数点数の剰余算が行われています。");
                        default:
                            throw new CompilerException.InternalErrorException(self.LocationRange, "定数式中の乗除算式部分で乗算でも除算でも剰余算でもない演算子が登場しています。（本処理系の誤りが原因です。）");
                    }
                } else if (self.Type.IsIntegerType()) {
                    var lhv = lhs.LongValue();
                    var rhv = rhs.LongValue();
                    if (lhv.Item1 != null || rhv.Item1 != null) {
                        throw new CompilerException.InternalErrorException(self.LocationRange, "コンパイル時定数ではない式への乗算・除算・剰余算が行われています。");
                    }
                    switch (self.Op) {
                        case Expression.MultiplicativeExpression.OperatorKind.Mul:
                            return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", lhv.Item2 * rhv.Item2, ((BasicType)self.Type.Unwrap()).Kind);
                        case Expression.MultiplicativeExpression.OperatorKind.Div:
                            return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", lhv.Item2 / rhv.Item2, ((BasicType)self.Type.Unwrap()).Kind);
                        case Expression.MultiplicativeExpression.OperatorKind.Mod:
                            return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", lhv.Item2 % rhv.Item2, ((BasicType)self.Type.Unwrap()).Kind);
                        default:
                            throw new CompilerException.InternalErrorException(self.LocationRange, "定数式中の乗除算式部分で乗算でも除算でも剰余算でもない演算子が登場しています。（本処理系の誤りが原因です。）");
                    }
                } else {
                    throw new NotImplementedException();
                }
            }

            public Expression OnRelationalExpression(Expression.RelationalExpression self, Expression value) {
                var lhs = self.Lhs.Accept(this, value);
                var rhs = self.Rhs.Accept(this, value);
                bool le;
                bool ge;
                if (lhs.Type.IsRealFloatingType() || rhs.Type.IsRealFloatingType()) {
                    var vl = lhs.DoubleValue();
                    var vr = rhs.DoubleValue();
                    le = vl <= vr;
                    ge = vl >= vr;
                } else {
                    var vl = lhs.LongValue();
                    var vr = rhs.LongValue();
                    if (vl.Item1 != vr.Item1) {
                        throw new CompilerException.InternalErrorException(self.LocationRange, "コンパイル時定数ではない式への乗算・除算・剰余算が行われています。");
                    }
                    le = vl.Item2 <= vr.Item2;
                    ge = vl.Item2 >= vr.Item2;
                }
                switch (self.Op) {
                    case Expression.RelationalExpression.OperatorKind.LessThan:
                        return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", (le && !ge) ? 1 : 0, ((BasicType)self.Type.Unwrap()).Kind);
                    case Expression.RelationalExpression.OperatorKind.GreaterThan:
                        return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", (!le && ge) ? 1 : 0, ((BasicType)self.Type.Unwrap()).Kind);
                    case Expression.RelationalExpression.OperatorKind.LessOrEqual:
                        return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", (le) ? 1 : 0, ((BasicType)self.Type.Unwrap()).Kind);
                    case Expression.RelationalExpression.OperatorKind.GreaterOrEqual:
                        return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", (!le) ? 1 : 0, ((BasicType)self.Type.Unwrap()).Kind);
                    default:
                        throw new Exception();
                }
            }

            public Expression OnReturnStatement(Statement.ReturnStatement self, Expression value) {
                throw new NotImplementedException();
            }

            public Expression OnShiftExpression(Expression.ShiftExpression self, Expression value) {
                var lhs = self.Lhs.Accept(this, value);
                var rhs = self.Rhs.Accept(this, value);
                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    long v;
                    var lhv = lhs.LongValue();
                    var rhv = rhs.LongValue();
                    if (lhv.Item1 != null || rhv.Item1 != null) {
                        throw new CompilerException.InternalErrorException(self.LocationRange, "コンパイル時定数ではない式へのシフト演算が行われています。");
                    }

                    switch (self.Op) {
                        case Expression.ShiftExpression.OperatorKind.Left:
                            v = lhv.Item2 << (int)rhv.Item2;
                            break;
                        case Expression.ShiftExpression.OperatorKind.Right:
                            v = lhv.Item2 >> (int)rhv.Item2;
                            break;
                        default:
                            throw new Exception();
                    }
                    return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", v, ((BasicType)self.Type.Unwrap()).Kind);
                }
                throw new Exception();
            }

            public Expression OnSimpleAssignInitializer(Initializer.SimpleAssignInitializer self, Expression value) {
                throw new NotImplementedException();
            }

            public Expression OnSimpleAssignmentExpression(Expression.AssignmentExpression.SimpleAssignmentExpression self, Expression value) {
                throw new CompilerException.SpecificationErrorException(self.LocationRange, "定数式は，代入，増分，減分，関数呼出し又はコンマ演算子を含んではならない。");
            }

            public Expression OnSimpleInitializer(Initializer.SimpleInitializer self, Expression value) {
                throw new NotImplementedException();
            }

            public Expression OnSizeofExpression(Expression.SizeofExpression self, Expression value) {
                return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", self.Type.SizeOf(), ((BasicType)self.Type.Unwrap()).Kind);
            }

            public Expression OnSizeofTypeExpression(Expression.SizeofTypeExpression self, Expression value) {
                return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", self.TypeOperand.SizeOf(), ((BasicType)self.Type.Unwrap()).Kind);
            }

            public Expression OnStringExpression(Expression.PrimaryExpression.StringExpression self, Expression value) {
                return self;
            }

            public Expression OnStructUnionAssignInitializer(Initializer.StructUnionAssignInitializer self, Expression value) {
                throw new NotImplementedException();
            }

            public Expression OnSwitchStatement(Statement.SwitchStatement self, Expression value) {
                throw new NotImplementedException();
            }

            public Expression OnTranslationUnit(TranslationUnit self, Expression value) {
                throw new NotImplementedException();
            }

            public Expression OnTypeConversionExpression(Expression.TypeConversionExpression self, Expression value) {
                // 6.3.1.2 論理型  
                // 任意のスカラ値を_Bool 型に変換する場合，その値が 0 に等しい場合は結果は 0 とし，それ以外の場合は 1 とする。
                if (self.Type.IsBoolType()) {
                    if (self.Expr.Type.IsScalarType()) {
                        var rv = self.Expr.Accept(this, value).LongValue();
                        var ret = (rv.Item1 != null && rv.Item2 == 0) ? 0 : 1;
                        return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", ret, BasicType.TypeKind._Bool);
                    } else {
                        throw new CompilerException.SpecificationErrorException(self.LocationRange, "スカラ値以外を _Bool 型に変換しようとした。");
                    }
                }

                // 6.3.1.3 符号付き整数型及び符号無し整数型  
                // 整数型の値を_Bool 型以外の他の整数型に変換する場合，その値が新しい型で表現可能なとき，値は変化しない。
                // 新しい型で表現できない場合，新しい型が符号無し整数型であれば，新しい型で表現しうる最大の数に1加えた数を加えること又は減じることを，新しい型の範囲に入るまで繰り返すことによって得られる値に変換する。
                // そうでない場合，すなわち，新しい型が符号付き整数型であって，値がその型で表現できない場合は，結果が処理系定義の値となるか，又は処理系定義のシグナルを生成するかのいずれかとする。
                if (self.Type.IsIntegerType() && self.Expr.Type.IsIntegerType()) {
                    var e = self.Expr.Accept(this, value);
                    var v = e.AsLongValue();
                    if (v != null) {
                        if (v.Item1 != null) {
                            return new Expression.PrimaryExpression.AddressConstantExpression(self.LocationRange, v.Item1, self.Type, new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", v.Item2, ((BasicType)self.Type.Unwrap()).Kind));
                        } else {
                            return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", v.Item2, self.Type.IsEnumeratedType() ? BasicType.TypeKind.SignedInt : ((BasicType)self.Type.Unwrap()).Kind);
                        }
                    } else {
                        return e;
                    }
                }
                if (self.Type.IsIntegerType() && self.Expr.Type.IsFloatingType()) {
                    var e = self.Expr.Accept(this, value);
                    var v = e.AsLongValue();
                    if (v != null) {
                        if (v.Item1 != null) {
                            throw new CompilerException.InternalErrorException(self.LocationRange, "コンパイル時定数ではない浮動小数点式から整数型への変換が行われています。");
                        } else {
                            return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", v.Item2, self.Type.IsEnumeratedType() ? BasicType.TypeKind.SignedInt : ((BasicType)self.Type.Unwrap()).Kind);
                        }
                    } else {
                        return e;
                    }
                }


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
                if (self.Type.IsFloatingType() && self.Expr.Type.IsIntegerType()) {
                    var v = self.Expr.Accept(this, value).DoubleValue();
                    return new Expression.PrimaryExpression.Constant.FloatingConstant(self.LocationRange, "", v, ((BasicType)self.Type.Unwrap()).Kind);
                }
                if (self.Type.IsFloatingType() && self.Expr.Type.IsFloatingType()) {
                    var v = self.Expr.Accept(this, value).DoubleValue();
                    return new Expression.PrimaryExpression.Constant.FloatingConstant(self.LocationRange, "", v, ((BasicType)self.Type.Unwrap()).Kind);
                }

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
                return self.Expr.Accept(this, value);
            }

            public Expression OnTypeDeclaration(Declaration.TypeDeclaration self, Expression value) {
                throw new NotImplementedException();
            }

            public Expression OnUnaryAddressExpression(Expression.UnaryAddressExpression self, Expression value) {
                return self.Expr.Accept(this, value);
            }

            public Expression OnUnaryMinusExpression(Expression.UnaryMinusExpression self, Expression value) {
                var ret = self.Expr.Accept(this, value);
                if (ret.Type.IsRealFloatingType()) {
                    return new Expression.PrimaryExpression.Constant.FloatingConstant(self.LocationRange, "", -ret.DoubleValue(), ((BasicType)self.Type.Unwrap()).Kind);
                } else if (ret.Type.IsIntegerType()) {
                    var rv = ret.LongValue();
                    if (rv.Item1 != null) {
                        throw new CompilerException.InternalErrorException(self.LocationRange, "コンパイル時定数ではない式に対する単項マイナス演算が行われています。");
                    } else {
                        return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", -rv.Item2, ((BasicType)self.Type.Unwrap()).Kind);
                    }
                } else {
                    throw new Exception();
                }
            }

            public Expression OnUnaryNegateExpression(Expression.UnaryNegateExpression self, Expression value) {
                var ret = self.Expr.Accept(this, value);
                if (ret.Type.IsIntegerType()) {
                    var rv = ret.LongValue();
                    if (rv.Item1 != null) {
                        throw new CompilerException.InternalErrorException(self.LocationRange, "コンパイル時定数ではない式に対するビット反転演算が行われています。");
                    } else {
                        return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", ~rv.Item2, ((BasicType)self.Type.Unwrap()).Kind);
                    }
                } else {
                    throw new Exception();
                }

            }

            public Expression OnUnaryNotExpression(Expression.UnaryNotExpression self, Expression value) {
                var ret = self.Expr.Accept(this, value);
                var rv = ret.LongValue();
                return new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "", (rv.Item1 == null || rv.Item2 == 0) ? 1 : 0, BasicType.TypeKind.SignedInt);
            }

            public Expression OnUnaryPlusExpression(Expression.UnaryPlusExpression self, Expression value) {
                return self.Expr.Accept(this, value);
            }

            public Expression OnUnaryPostfixExpression(Expression.PostfixExpression.UnaryPostfixExpression self, Expression value) {
                throw new CompilerException.SpecificationErrorException(self.LocationRange, "定数式は，代入，増分，減分，関数呼出し又はコンマ演算子を含んではならない。");
            }

            public Expression OnUnaryPrefixExpression(Expression.UnaryPrefixExpression self, Expression value) {
                throw new CompilerException.SpecificationErrorException(self.LocationRange, "定数式は，代入，増分，減分，関数呼出し又はコンマ演算子を含んではならない。");
            }

            public Expression OnUnaryReferenceExpression(Expression.UnaryReferenceExpression self, Expression value) {
                if (!(self.Expr is Expression.UnaryAddressExpression)) {
                    throw new CompilerException.SpecificationErrorException(self.LocationRange, "定数式中でアドレス演算子と対にならない間接演算子が使用されています。");
                } else {
                    return this.AcceptTo(self.Expr, value);
                }
            }

            public Expression OnUndefinedIdentifierExpression(Expression.PrimaryExpression.IdentifierExpression.UndefinedIdentifierExpression self, Expression value) {
                throw new CompilerException.SpecificationErrorException(self.LocationRange, "定数式中で未定義の識別子が使われています。");
            }

            public Expression OnVariableDeclaration(Declaration.VariableDeclaration self, Expression value) {
                throw new NotImplementedException();
            }

            public Expression OnVariableExpression(Expression.PrimaryExpression.IdentifierExpression.VariableExpression self, Expression value) {
                //return self;
                CType bType;
                if (self.Type.IsArrayType(out bType)) {
                    return new Expression.PrimaryExpression.AddressConstantExpression(self.LocationRange, self, CType.CreatePointer(bType), new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "0", 0, BasicType.TypeKind.SignedInt));
                } else {
                    return new Expression.PrimaryExpression.AddressConstantExpression(self.LocationRange, self, CType.CreatePointer(self.Type), new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "0", 0, BasicType.TypeKind.SignedInt));
                }
            }

            public Expression OnWhileStatement(Statement.WhileStatement self, Expression value) {
                throw new NotImplementedException();
            }

        }

        /// <summary>
        /// 式を定数式として評価する
        /// </summary>
        /// <param name="expr"></param>
        /// <returns></returns>
        public static Expression Eval(Expression expr) {
            var evaluator = new SyntaxTreeConstantEvaluatorVisitor();
            return expr.Accept(evaluator, expr);
        }

        /// <summary>
        /// 定数式からlong値を得る
        /// </summary>
        /// <param name="ret"></param>
        /// <returns></returns>
        public static long? ToLong(Expression ret) {
            if (ret is Expression.PrimaryExpression.Constant.IntegerConstant) {
                return (ret as Expression.PrimaryExpression.Constant.IntegerConstant).Value;
            } else if (ret is Expression.PrimaryExpression.Constant.CharacterConstant) {
                return (ret as Expression.PrimaryExpression.Constant.CharacterConstant).Value;
            } else if (ret is Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant) {
                return (ret as Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant).Info.Value;
            }
            else {
                return null;
            }
        }
    }
}
