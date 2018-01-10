using System;
using System.Reflection;

namespace AnsiCParser {
    /// <summary>
    /// 評価器
    /// </summary>
    public static class Evaluator {

        private static double DoubleValue(this SyntaxTree.Expression self) {
            if (self is SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant) {
                return (double)(((SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant)self).Value);
            }
            if (self is SyntaxTree.Expression.PrimaryExpression.Constant.CharacterConstant) {
                return (double)(((SyntaxTree.Expression.PrimaryExpression.Constant.CharacterConstant)self).Value);
            }
            if (self is SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant) {
                return (double)(((SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant)self).Info.Value);
            }
            if (self is SyntaxTree.Expression.PrimaryExpression.Constant.FloatingConstant) {
                return (double)(((SyntaxTree.Expression.PrimaryExpression.Constant.FloatingConstant)self).Value);
            }
            throw new NotSupportedException(self.GetType().Name);
        }
        private static long LongValue(this SyntaxTree.Expression self) {
            if (self is SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant) {
                return (long)(((SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant)self).Value);
            }
            if (self is SyntaxTree.Expression.PrimaryExpression.Constant.CharacterConstant) {
                return (long)(((SyntaxTree.Expression.PrimaryExpression.Constant.CharacterConstant)self).Value);
            }
            if (self is SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant) {
                return (long)(((SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant)self).Info.Value);
            }
            if (self is SyntaxTree.Expression.PrimaryExpression.Constant.FloatingConstant) {
                return (long)(((SyntaxTree.Expression.PrimaryExpression.Constant.FloatingConstant)self).Value);
            }
            throw new NotSupportedException(self.GetType().Name);
        }
        /// <summary>
        /// 定数式の評価
        /// </summary>
        /// <param name="expr"></param>
        /// <returns></returns>
        public static SyntaxTree.Expression ConstantEval(SyntaxTree.Expression expr) {
            // 6.6 定数式
            // 補足説明  
            // 定数式は，実行時ではなく翻訳時に評価することができる。したがって，定数を使用してよいところならばどこでも使用してよい。
            //
            // 制約
            // - 定数式は，代入，増分，減分，関数呼出し又はコンマ演算子を含んではならない。
            //   ただし，定数式が評価されない部分式(sizeof演算子のオペランド等)に含まれている場合を除く。
            // - 定数式を評価した結果は，その型で表現可能な値の範囲内にある定数でなければならない。
            // 

            // ToDo: 初期化子中の定数式の扱いを実装

            if (expr is SyntaxTree.Expression.PostfixExpression.AdditiveExpression) {
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

                var e = expr as SyntaxTree.Expression.PostfixExpression.AdditiveExpression;

                var lhs = ConstantEval(e.Lhs);
                var rhs = ConstantEval(e.Rhs);

                if (expr.Type.IsRealFloatingType()) {
                    switch (e.Op) {
                        case SyntaxTree.Expression.AdditiveExpression.OperatorKind.Add:
                            return new SyntaxTree.Expression.PrimaryExpression.Constant.FloatingConstant($"", lhs.DoubleValue() + rhs.DoubleValue(), (e.Type.Unwrap() as CType.BasicType).Kind);
                        case SyntaxTree.Expression.AdditiveExpression.OperatorKind.Sub:
                            return new SyntaxTree.Expression.PrimaryExpression.Constant.FloatingConstant($"", lhs.DoubleValue() - rhs.DoubleValue(), (e.Type.Unwrap() as CType.BasicType).Kind);
                        default:
                            throw new CompilerException.InternalErrorException(Location.Empty, Location.Empty, "定数式中の加算式部分で加算でも減算でもない演算子が登場しています。（本処理系の誤りが原因です。）");
                    }
                } else if (expr.Type.IsIntegerType()) {
                    switch (e.Op) {
                        case SyntaxTree.Expression.AdditiveExpression.OperatorKind.Add:
                            return new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant($"", lhs.LongValue() + rhs.LongValue(), (e.Type.Unwrap() as CType.BasicType).Kind);
                        case SyntaxTree.Expression.AdditiveExpression.OperatorKind.Sub:
                            return new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant($"", lhs.LongValue() - rhs.LongValue(), (e.Type.Unwrap() as CType.BasicType).Kind);
                        default:
                            throw new CompilerException.InternalErrorException(Location.Empty, Location.Empty, "定数式中の加算式部分で加算でも減算でもない演算子が登場しています。（本処理系の誤りが原因です。）");
                    }
                } else {
                    return new SyntaxTree.Expression.AdditiveExpression(SyntaxTree.Expression.AdditiveExpression.OperatorKind.Add, lhs, rhs);
                }
            }
            if (expr is SyntaxTree.Expression.PostfixExpression.AndExpression) {
                var e = expr as SyntaxTree.Expression.PostfixExpression.AndExpression;
                var lhs = ConstantEval(e.Lhs).LongValue();
                long ret = 0;
                if (lhs != 0) {
                    ret = ConstantEval(e.Rhs).LongValue() == 0 ? 0 : 1;
                }
                return new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant($"", ret, (e.Type.Unwrap() as CType.BasicType).Kind);
            }
            if (expr is SyntaxTree.Expression.PostfixExpression.ArraySubscriptingExpression) {
                var e = expr as SyntaxTree.Expression.PostfixExpression.ArraySubscriptingExpression;
                throw new Exception("");
            }
            if (expr is SyntaxTree.Expression.PostfixExpression.AssignmentExpression) {
                var e = expr as SyntaxTree.Expression.PostfixExpression.AssignmentExpression;
                throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "定数式は，代入，増分，減分，関数呼出し又はコンマ演算子を含んではならない。");
            }
            if (expr is SyntaxTree.Expression.PostfixExpression.TypeConversionExpression) {
                var e = expr as SyntaxTree.Expression.PostfixExpression.TypeConversionExpression;
                // 6.3.1.2 論理型  
                // 任意のスカラ値を_Bool 型に変換する場合，その値が 0 に等しい場合は結果は 0 とし，それ以外の場合は 1 とする。
                if (e.Type.IsBoolType()) {
                    if (e.Expr.Type.IsScalarType()) {
                        var ret = ConstantEval(e.Expr).LongValue() == 0 ? 0 : 1;
                        return new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant($"", ret, CType.BasicType.TypeKind._Bool);
                    } else {
                        throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "スカラ値以外を_Bool 型に変換しようとした。");
                    }
                }

                // 6.3.1.3 符号付き整数型及び符号無し整数型  
                // 整数型の値を_Bool 型以外の他の整数型に変換する場合，その値が新しい型で表現可能なとき，値は変化しない。
                // 新しい型で表現できない場合，新しい型が符号無し整数型であれば，新しい型で表現しうる最大の数に1加えた数を加えること又は減じることを，新しい型の範囲に入るまで繰り返すことによって得られる値に変換する。
                // そうでない場合，すなわち，新しい型が符号付き整数型であって，値がその型で表現できない場合は，結果が処理系定義の値となるか，又は処理系定義のシグナルを生成するかのいずれかとする。
                if (e.Type.IsIntegerType() && e.Expr.Type.IsIntegerType()) {
                    var value = ConstantEval(e.Expr).LongValue();
                    return new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant($"", value, (e.Type.Unwrap() as CType.BasicType).Kind);
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
                return ConstantEval(e.Expr);
            }
            if (expr is SyntaxTree.Expression.PrimaryExpression.Constant.CharacterConstant) {
                return expr as SyntaxTree.Expression.PrimaryExpression.Constant.CharacterConstant;
            }
            if (expr is SyntaxTree.Expression.PostfixExpression.CommaExpression) {
                var e = expr as SyntaxTree.Expression.PostfixExpression.CommaExpression;
                throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "定数式は，代入，増分，減分，関数呼出し又はコンマ演算子を含んではならない。");
            }
            if (expr is SyntaxTree.Expression.PostfixExpression.ConditionalExpression) {
                var e = expr as SyntaxTree.Expression.PostfixExpression.ConditionalExpression;
                var cond = ConstantEval(e.CondExpr);
                if (cond.LongValue() != 0) {
                    return ConstantEval(e.ThenExpr);
                } else {
                    return ConstantEval(e.ElseExpr);
                }
            }
            if (expr is SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant) {
                var e = expr as SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant;
                return new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant($"", e.Info.Value, (e.Type.Unwrap() as CType.BasicType).Kind);
            }
            if (expr is SyntaxTree.Expression.PostfixExpression.EqualityExpression) {
                var e = expr as SyntaxTree.Expression.PostfixExpression.EqualityExpression;
                var lhs = ConstantEval(e.Lhs);
                var rhs = ConstantEval(e.Rhs);
                var ret = false;
                if (lhs.Type.IsRealFloatingType() || rhs.Type.IsRealFloatingType()) {
                    ret = lhs.DoubleValue() == rhs.DoubleValue();
                } else {
                    ret = lhs.LongValue() == rhs.LongValue();
                }
                switch (e.Op) {
                    case SyntaxTree.Expression.EqualityExpression.OperatorKind.Equal:
                        return new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant($"", ret ? 1 : 0, (e.Type.Unwrap() as CType.BasicType).Kind);
                    case SyntaxTree.Expression.EqualityExpression.OperatorKind.NotEqual:
                        return new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant($"", ret ? 1 : 0, (e.Type.Unwrap() as CType.BasicType).Kind);
                    default:
                        throw new Exception();
                }
            }
            if (expr is SyntaxTree.Expression.ExclusiveOrExpression) {
                var e = expr as SyntaxTree.Expression.ExclusiveOrExpression;
                var lhs = ConstantEval(e.Lhs);
                var rhs = ConstantEval(e.Rhs);
                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    var ret = lhs.LongValue() ^ rhs.LongValue();
                    return new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant($"", ret, (e.Type.Unwrap() as CType.BasicType).Kind);
                }
                throw new Exception();
            }
            if (expr is SyntaxTree.Expression.PrimaryExpression.Constant.FloatingConstant) {
                return expr as SyntaxTree.Expression.PrimaryExpression.Constant.FloatingConstant;
            }
            if (expr is SyntaxTree.Expression.PostfixExpression.FunctionCallExpression) {
                var e = expr as SyntaxTree.Expression.PostfixExpression.FunctionCallExpression;
                throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "定数式は，代入，増分，減分，関数呼出し又はコンマ演算子を含んではならない。");
            }
            if (expr is SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.FunctionExpression) {
                var e = expr as SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.FunctionExpression;
                return new SyntaxTree.Expression.PrimaryExpression.AddressConstantExpression( e, new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant("0", 0, CType.BasicType.TypeKind.SignedInt) );
            }
            if (expr is SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.UndefinedIdentifierExpression) {
                var e = expr as SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.UndefinedIdentifierExpression;
                throw new Exception();
            }
            if (expr is SyntaxTree.Expression.InclusiveOrExpression) {
                var e = expr as SyntaxTree.Expression.ExclusiveOrExpression;
                var lhs = ConstantEval(e.Lhs);
                var rhs = ConstantEval(e.Rhs);
                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    var ret = lhs.LongValue() | rhs.LongValue();
                    return new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant($"", ret, (e.Type.Unwrap() as CType.BasicType).Kind);
                }
                throw new Exception();
            }
            if (expr is SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant) {
                return expr as SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant;
            }
            if (expr is SyntaxTree.Expression.LogicalAndExpression) {
                var e = expr as SyntaxTree.Expression.LogicalAndExpression;
                var lhs = ConstantEval(e.Lhs);
                var ret = false;
                if (lhs.LongValue() != 0) {
                    var rhs = ConstantEval(e.Rhs);
                    ret = rhs.LongValue() != 0;
                }
                return new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant($"",  ret ? 1 : 0, (e.Type.Unwrap() as CType.BasicType).Kind);
            }
            if (expr is SyntaxTree.Expression.LogicalOrExpression) {
                var e = expr as SyntaxTree.Expression.LogicalAndExpression;
                var lhs = ConstantEval(e.Lhs);
                var ret = true;
                if (lhs.LongValue() == 0) {
                    var rhs = ConstantEval(e.Rhs);
                    ret = rhs.LongValue() != 0;
                }
                return new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant($"", ret ? 1 : 0, (e.Type.Unwrap() as CType.BasicType).Kind);
            }
            if (expr is SyntaxTree.Expression.PostfixExpression.MemberDirectAccess) {
                var e = expr as SyntaxTree.Expression.PostfixExpression.MemberDirectAccess;
                throw new Exception();
            }
            if (expr is SyntaxTree.Expression.PostfixExpression.MemberIndirectAccess) {
                var e = expr as SyntaxTree.Expression.PostfixExpression.MemberIndirectAccess;
                throw new Exception();
            }
            if (expr is SyntaxTree.Expression.MultiplicitiveExpression) {
                var e = expr as SyntaxTree.Expression.MultiplicitiveExpression;
                var lhs = ConstantEval(e.Lhs);
                var rhs = ConstantEval(e.Rhs);

                if (expr.Type.IsRealFloatingType()) {
                    switch (e.Op) {
                        case SyntaxTree.Expression.MultiplicitiveExpression.OperatorKind.Mul:
                            return new SyntaxTree.Expression.PrimaryExpression.Constant.FloatingConstant($"", lhs.DoubleValue() * rhs.DoubleValue(), (e.Type.Unwrap() as CType.BasicType).Kind);
                        case SyntaxTree.Expression.MultiplicitiveExpression.OperatorKind.Div:
                            return new SyntaxTree.Expression.PrimaryExpression.Constant.FloatingConstant($"", lhs.DoubleValue() / rhs.DoubleValue(), (e.Type.Unwrap() as CType.BasicType).Kind);
                        //case SyntaxTree.Expression.MultiplicitiveExpression.OperatorKind.Mod:
                        //    return new SyntaxTree.Expression.PrimaryExpression.Constant.FloatingConstant($"", lhs.DoubleValue() % rhs.DoubleValue(), (e.Type.Unwrap() as CType.BasicType).Kind);
                        default:
                            throw new CompilerException.InternalErrorException(Location.Empty, Location.Empty, "定数式中の乗除算式部分で乗算でも除算でも剰余算でもない演算子が登場しています。（本処理系の誤りが原因です。）");
                    }
                } else if (expr.Type.IsIntegerType()) {
                    switch (e.Op) {
                        case SyntaxTree.Expression.MultiplicitiveExpression.OperatorKind.Mul:
                            return new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant($"", lhs.LongValue() * rhs.LongValue(), (e.Type.Unwrap() as CType.BasicType).Kind);
                        case SyntaxTree.Expression.MultiplicitiveExpression.OperatorKind.Div:
                            return new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant($"", lhs.LongValue() / rhs.LongValue(), (e.Type.Unwrap() as CType.BasicType).Kind);
                        case SyntaxTree.Expression.MultiplicitiveExpression.OperatorKind.Mod:
                            return new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant($"", lhs.LongValue() % rhs.LongValue(), (e.Type.Unwrap() as CType.BasicType).Kind);
                        default:
                            throw new CompilerException.InternalErrorException(Location.Empty, Location.Empty, "定数式中の乗除算式部分で乗算でも除算でも剰余算でもない演算子が登場しています。（本処理系の誤りが原因です。）");
                    }
                } else {
                    throw new NotImplementedException();
                }
            }
            if (expr is SyntaxTree.Expression.RelationalExpression) {
                var e = expr as SyntaxTree.Expression.RelationalExpression;
                var lhs = ConstantEval(e.Lhs);
                var rhs = ConstantEval(e.Rhs);
                var le = false;
                var ge = false;
                if (lhs.Type.IsRealFloatingType() || rhs.Type.IsRealFloatingType()) {
                    var vl = lhs.DoubleValue();
                    var vr = rhs.DoubleValue();
                    le = vl <= vr;
                    ge = vl >= vr;
                } else {
                    var vl = lhs.LongValue();
                    var vr = rhs.LongValue();
                    le = vl <= vr;
                    ge = vl >= vr;
                }
                switch (e.Op) {
                    case SyntaxTree.Expression.RelationalExpression.OperatorKind.LessThan:
                        return new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant($"", (le && !ge) ? 1 : 0, (e.Type.Unwrap() as CType.BasicType).Kind);
                    case SyntaxTree.Expression.RelationalExpression.OperatorKind.GreaterThan:
                        return new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant($"", (!le && ge) ? 1 : 0, (e.Type.Unwrap() as CType.BasicType).Kind);
                    case SyntaxTree.Expression.RelationalExpression.OperatorKind.LessOrEqual:
                        return new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant($"", (le ) ? 1 : 0, (e.Type.Unwrap() as CType.BasicType).Kind);
                    case SyntaxTree.Expression.RelationalExpression.OperatorKind.GreaterOrEqual:
                        return new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant($"", (!le) ? 1 : 0, (e.Type.Unwrap() as CType.BasicType).Kind);
                    default:
                        throw new Exception();
                }
            }
            if (expr is SyntaxTree.Expression.ShiftExpression) {
                var e = expr as SyntaxTree.Expression.ShiftExpression;
                var lhs = ConstantEval(e.Lhs);
                var rhs = ConstantEval(e.Rhs);
                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    long v;
                    switch (e.Op) {
                        case SyntaxTree.Expression.ShiftExpression.OperatorKind.Left:
                            v= lhs.LongValue() << (int)rhs.LongValue();
                            break;
                        case SyntaxTree.Expression.ShiftExpression.OperatorKind.Right:
                            v = lhs.LongValue() >> (int)rhs.LongValue();
                            break;
                        default:
                            throw new Exception();
                    }
                    return new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant($"", v, (e.Type.Unwrap() as CType.BasicType).Kind);
                }
                throw new Exception();
            }
            if (expr is SyntaxTree.Expression.SizeofExpression) {
                var e = expr as SyntaxTree.Expression.SizeofExpression;
                return new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant($"", e.Type.Sizeof(), (e.Type.Unwrap() as CType.BasicType).Kind);
            }
            if (expr is SyntaxTree.Expression.SizeofTypeExpression) {
                var e = expr as SyntaxTree.Expression.SizeofTypeExpression;
                return new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant($"", e.TypeOperand.Sizeof(), (e.Type.Unwrap() as CType.BasicType).Kind);
            }
            if (expr is SyntaxTree.Expression.PrimaryExpression.StringExpression) {
                var e = expr as SyntaxTree.Expression.PrimaryExpression.StringExpression;
                return e;
            }
            if (expr is SyntaxTree.Expression.UnaryAddressExpression) {
                var e = expr as SyntaxTree.Expression.UnaryAddressExpression;
                var ret = ConstantEval(e.Expr);
                return ret;
            }
            if (expr is SyntaxTree.Expression.UnaryMinusExpression) {
                var e = expr as SyntaxTree.Expression.UnaryMinusExpression;
                var ret = ConstantEval(e.Expr);
                if (ret.Type.IsRealFloatingType()) {
                    return new SyntaxTree.Expression.PrimaryExpression.Constant.FloatingConstant($"", -ret.DoubleValue(), (e.Type.Unwrap() as CType.BasicType).Kind);
                } else if (ret.Type.IsIntegerType()) {
                    return new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant($"", -ret.LongValue(), (e.Type.Unwrap() as CType.BasicType).Kind);
                }
                else {
                    throw new Exception();
                }
            }
            if (expr is SyntaxTree.Expression.UnaryNegateExpression) {
                var e = expr as SyntaxTree.Expression.UnaryNegateExpression;
                var ret = ConstantEval(e.Expr);
                if (ret.Type.IsIntegerType()) {
                    return new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant($"", ~ret.LongValue(), (e.Type.Unwrap() as CType.BasicType).Kind);
                } else {
                    throw new Exception();
                }
            }
            if (expr is SyntaxTree.Expression.UnaryNotExpression) {
                var e = expr as SyntaxTree.Expression.UnaryNotExpression;
                var ret = ConstantEval(e.Expr);
                return new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant($"", ret.LongValue() == 0 ? 1 : 0, (e.Type.Unwrap() as CType.BasicType).Kind);
            }
            if (expr is SyntaxTree.Expression.UnaryPlusExpression) {
                var e = expr as SyntaxTree.Expression.UnaryPlusExpression;
                return ConstantEval(e.Expr);
            }
            if (expr is SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression) {
                var e = expr as SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression;
                throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "定数式は，代入，増分，減分，関数呼出し又はコンマ演算子を含んではならない。");
            }
            if (expr is SyntaxTree.Expression.UnaryPrefixExpression) {
                var e = expr as SyntaxTree.Expression.UnaryPrefixExpression;
                throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "定数式は，代入，増分，減分，関数呼出し又はコンマ演算子を含んではならない。");
            }
            if (expr is SyntaxTree.Expression.UnaryReferenceExpression) {
                var e = expr as SyntaxTree.Expression.UnaryReferenceExpression;
                throw new Exception();
            }
            if (expr is SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.ArgumentExpression) {
                var e = expr as SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.ArgumentExpression;
                throw new Exception();
            }
            if (expr is SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.VariableExpression) {
                var e = expr as SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.VariableExpression;
                return e;
                //throw new Exception();
            }
            if (expr is SyntaxTree.Expression.PrimaryExpression.EnclosedInParenthesesExpression) {
                var e = expr as SyntaxTree.Expression.PrimaryExpression.EnclosedInParenthesesExpression;
                return ConstantEval(e.ParenthesesExpression);
            }
            if (expr is SyntaxTree.Expression.PrimaryExpression.AddressConstantExpression) {
                var e = expr as SyntaxTree.Expression.PrimaryExpression.AddressConstantExpression;
                throw new Exception();
            }
            
            throw new Exception();
        }
    }
}