using System;

namespace AnsiCParser {
    public abstract class TestCase {
        public abstract void Run();
        public abstract class SuccessCase : TestCase {
            protected abstract string[] sources();
            public override void Run() {
                foreach (var source in sources()) {
                    new Parser(source).Parse();
                }
            }
        }
        public abstract class RaiseError<T> : TestCase where T : Exception {
            protected abstract string[] sources();
            public override void Run() {
                foreach (var source in sources()) {
                    try {
                        new Parser(source).Parse();
                    } catch (T ex) {
                        Console.Error.WriteLine($"(OK) {typeof(T).Name}: {ex.Message}");
                        continue;
                    }
                    throw new Exception($"例外{typeof(T).Name}が発生すべきだが発生しなかった");
                }
            }
        }

        public static void RunTest() {
            new FunctionReturnArrayCase().Run();
            new DefaultArgumentPromotionCase1().Run();
            new MixedFunctionCase().Run();
            new KandRStyleCase().Run();
            new HelloWorldCase().Run();
            new FunctionCallCase().Run();
            new QuickSortCase().Run();
            new LValueAndAddressOpCase().Run();
            new RedefineTypedefInSameScopeCase().Run();
            new RedefineTypedefInNestedScopeCase().Run();
            new TypedefInStructCase().Run();
            new EmptyStructCase().Run();
            new NoNameStructIsNotUsedCase().Run();
            new ConstantExprIsNullpointerCase().Run();
            new ConstantExprIsNotNullpointerCase().Run();
            new ValidAssignCase().Run();
            new InvalidAssignCase().Run();
            new IntegerPromotionTestCase().Run();
            new CastBetweenArrayAndStringCase().Run();
            new BadAssignmentCase().Run();
            new IncompatibleTypeAssignCase().Run();
            new RedefinitionOfParameterBadCase().Run();
            new RedefinitionOfParameterGoodCase().Run();
            new BadStorageClassOfParameterKAndRCase().Run();
            new BadControlCase().Run();
            new GotoLabelBadCase().Run();
            new Spec_6_9_1_Foornote_137_BadCase().Run();
            new Spec_6_9_1_Foornote_137_GoodCase().Run();
            new FunctionDeclGoodCase().Run();
            new FunctionDeclBadCase().Run();
            new VarDeclGoodCase().Run();
            new VarDeclBadCase().Run();


        }

        /// <summary>
        /// 関数型は戻り値型に構造体型を持てない
        /// </summary>
        public class FunctionReturnArrayCase : RaiseError<CompilerException.SpecificationErrorException> {
            protected override string[] sources() => new [] { @"
typedef char BUF[256];

BUF hoge(BUF buf) { /* エラー: hoge は配列を返す関数として宣言されています */
    return buf;
}
" };
        }

        /// <summary>
        /// 既定の実引数拡張のケース(1)
        /// </summary>
        public class DefaultArgumentPromotionCase1 : SuccessCase {
            protected override string[] sources() => new [] { @"
void f();

void foo(void) { 
  float x = 3.14f; 
  f(x);     // 既定の実引数拡張で float -> double になる
}

void f (double x) { 
  (int)x;
}
" };
        }

        /// <summary>
        /// ANSI形式の関数宣言とK&R形式の関数定義が併用されていて、既定の実引数拡張によって引数型の一致が怪しくなるケース
        /// </summary>
        /// <remarks>
        /// gcc    : -Wpedantic 時にのみ警告 promoted argument ‘x’ doesn’t match prototype が出力される。
        /// clang  :  warning: promoted type 'double' of K&R function parameter is not compatible with the parameter type 'float' declared in a previous prototype [-Wknr-promoted-parameter]
        /// splint : 宣言 float f(float); に対応する関数がないという警告。
        /// </remarks>
        public class MixedFunctionCase : RaiseError<CompilerException.TypeMissmatchError> {
            protected override string[] sources() => new [] { @"
float f(float);

int main(void)
{
float x;
f(x);
}

float f(x)
float x;
{ return x;}

" };
        }
        /// <summary>
        /// K&R形式の関数定義・宣言の例
        /// </summary>
        public class KandRStyleCase : SuccessCase {
            protected override string[] sources() => new [] { @"
int count();

int main(void) {
    int n = count(""hello"");
    return n;
}

int count(str)
char* str;
{
    char* p = str;
    while (*p != '\0') {
        p++;
    }
    return p - str;
}
" };
        }

        /// <summary>
        /// お約束のhello, world(文字配列型から文字型へのポインタ型への暗黙の型変換の例)
        /// </summary>
        public class HelloWorldCase : SuccessCase {
            protected override string[] sources() => new [] { @"
extern int printf(const char *, ...);

int main(void) {
    printf(""hello, world"");
    return 0;
}

" };
        }

        /// <summary>
        /// 暗黙の型変換の例
        /// </summary>
        public class FunctionCallCase : SuccessCase {
            protected override string[] sources() => new [] { @"
static  unsigned short
__bswap_16 (unsigned short __x)
{
  return (__x >> 8) | (__x << 8);
}

static  unsigned int
__bswap_32 (unsigned int __x)
{
  return (__bswap_16 (__x & 0xffff) << 16) | (__bswap_16 (__x >> 16));
}

int main(void) {
    return __bswap_32(0x12345UL);
}
" };

        }

        /// <summary>
        /// Wikipediaのクイックソート
        /// </summary>
        public class QuickSortCase : SuccessCase {
            protected override string[] sources() => new [] { @"
typedef int value_type; /* ソートするキーの型 */

/* x, y, z の中間値を返す */
value_type med3(value_type x, value_type y, value_type z) {
    if (x < y) {
        if (y < z) return y; else if (z < x) return x; else return z;
    } else {
        if (z < y) return y; else if (x < z) return x; else return z;
    }
}

/* クイックソート
 * a     : ソートする配列
 * left  : ソートするデータの開始位置
 * right : ソートするデータの終了位置
 */
void quicksort(value_type a[], int left, int right) {
    if (left < right) {
        int i = left, j = right;
        value_type tmp, pivot = med3(a[i], a[i + (j - i) / 2], a[j]); /* (i+j)/2 ではオーバーフローしてしまう */
        while (1) { /* a[] を pivot 以上と以下の集まりに分割する */
            while (a[i] < pivot) i++; /* a[i] >= pivot となる位置を検索 */
            while (pivot < a[j]) j--; /* a[j] <= pivot となる位置を検索 */
            if (i >= j) break;
            tmp = a[i]; a[i] = a[j]; a[j] = tmp; /* a[i], a[j] を交換 */
            i++; j--;
        }
        quicksort(a, left, i - 1);  /* 分割した左を再帰的にソート */
        quicksort(a, j + 1, right); /* 分割した右を再帰的にソート */
    }
}
" };

        }

        /// <summary>
        /// 左辺値と単項&演算子の不正な組み合わせ例
        /// </summary>
        public class LValueAndAddressOpCase : RaiseError<CompilerException.SpecificationErrorException> {
            protected override string[] sources() => new [] { @"
int foo() {
    &""a"";
}
", @"
int foo() {
    &1;
}
", @"
int foo() {
    &a;
}
" };
        }
        /// <summary>
        /// typedef の再定義
        /// </summary>
        public class RedefineTypedefInSameScopeCase : RaiseError<CompilerException.SpecificationErrorException> {
            protected override string[] sources() => new [] { @"
typedef int SINT;
typedef int SINT;   // NG(redefine)

int main(void) {
SINT x = 1.0;
return (int)x;
}
" };
            }
        /// <summary>
        /// typedef の入れ子定義
        /// </summary>
        public class RedefineTypedefInNestedScopeCase : SuccessCase {
            protected override string[] sources() => new [] { @"
typedef int SINT;

int main(void) {
typedef double SINT;    // OK(override)
SINT x = 1.0;
return (int)x;
}

" };
        }

        /// <summary>
        /// struct中で typedef
        /// </summary>
        public class TypedefInStructCase : RaiseError<CompilerException.SyntaxErrorException> {
            protected override string[] sources() => new [] { @"
struct Z {
    typedef int SINT ;  // NG
    SINT x;
};


" };
        }

        /// <summary>
        /// メンバが空の構造体
        /// </summary>
        public class EmptyStructCase : RaiseError<CompilerException.SpecificationErrorException> {
            protected override string[] sources() => new [] { @"
struct foo {};
" };
        }

        /// <summary>
        /// タグ型の宣言、変数宣言のどちらももならない（意味を持たない）構造体の宣言。
        /// </summary>
        public class NoNameStructIsNotUsedCase : RaiseError<CompilerException.SpecificationErrorException> {
            protected override string[] sources() => new [] { @"
struct { int x; };
" };
        }


        /// <summary>
        /// 定数式のヌルポインタ扱い
        /// </summary>
        public class ConstantExprIsNullpointerCase : SuccessCase {
            protected override string[] sources() => new [] { @"
const char *str = (2*4/8-1);    // clang: warning: expression which evaluates to zero treated as a null pointer constant of type 'const char *' [-Wnon-literal-null-conversion]

int main(void) {
	if (str == 0) {
		return 1;
	}
	return 0;
}
" };
        }

        /// <summary>
        /// 定数式のポインタ扱い
        /// </summary>
        public class ConstantExprIsNotNullpointerCase : SuccessCase {
            protected override string[] sources() => new [] { @"
const char *str = (2*4/8);  // warning: incompatible integer to pointer conversion initializing 'const char *' with an expression of type 'int' [-Wint-conversion]

int main(void) {
	if (str == 0) {
		return 1;
	}
	return 0;
}
" };
        }

        /// <summary>
        /// 暗黙の型変換を伴う妥当な代入式
        /// </summary>
        public class ValidAssignCase : SuccessCase {
            protected override string[] sources() => new [] { @"
void foo(void) { 
    unsigned char  u8  = 0;
    signed   char  s8  = 0;
    unsigned short u16 = 0;
    signed   short s16 = 0;
    unsigned long  u32 = 0;
    signed   long  s32 = 0;
    float          f   = 0;
    double         d   = 0;
    long double    ld  = 0;

    unsigned int   *p  = 0;
    double         *q  = 0;
    void           *v  = 0;

    s8  = s8;   // signed char -> signed char    : ビットパターンを維持
    s16 = s8;   // signed char -> signed short   : 符号拡張
    s32 = s8;   // signed char -> signed long    : 符号拡張
    u8  = s8;   // signed char -> unsigned char  : ビットパターンを維持、上位ビットは符号ビットとしての機能を失う。
    u16 = s8;   // signed char -> unsigned short : short への符号拡張、short から unsigned short への変換。
    u32 = s8;   // signed char -> unsigned long  : long への符号拡張、long から unsigned long への変換。
    f   = s8;   // signed char -> float          : long への符号拡張、long から float への変換。
    d   = s8;   // signed char -> double         : long への符号拡張、long から double への変換。
    ld  = s8;   // signed char -> long double    : long への符号拡張、long から long double への変換。

    s8  = s16;   // signed short -> signed char    : 下位バイトを維持。上位バイトは消失
    s16 = s16;   // signed short -> signed short   : ビットパターンを維持
    s32 = s16;   // signed short -> signed long    : 符号拡張
    u8  = s16;   // signed short -> unsigned char  : 下位バイトを維持。上位バイトは消失
    u16 = s16;   // signed short -> unsigned short : ビットパターンを維持、上位ビットは符号ビットとしての機能を失う。
    u32 = s16;   // signed short -> unsigned long  : long への符号拡張、long から unsigned long への変換。
    f   = s16;   // signed short -> float          : long への符号拡張、long から float への変換。
    d   = s16;   // signed short -> double         : long への符号拡張、long から double への変換。
    ld  = s16;   // signed short -> long double    : long への符号拡張、long から long double への変換。

    s8  = s32;   // signed long -> signed char    : 下位バイトを維持。上位バイトは消失
    s16 = s32;   // signed long -> signed short   : 下位ワードを維持。上位ワードは消失
    s32 = s32;   // signed long -> signed long    : ビットパターンを維持
    u8  = s32;   // signed long -> unsigned char  : 下位バイトを維持。上位バイトは消失
    u16 = s32;   // signed long -> unsigned short : 下位ワードを維持。上位ワードは消失
    u32 = s32;   // signed long -> unsigned long  : ビット パターンを維持、上位ビットは符号ビットとしての機能を失う。
    f   = s32;   // signed long -> float          : float として表される。 long を正確に表すことができない場合、精度が低下する場合がある。
    d   = s32;   // signed long -> double         : double として表される。 long を double として正確に表すことができない場合、精度が低下する場合がある。
    ld  = s32;   // signed long -> long double    : long double として表される。 long を long double として正確に表すことができない場合、精度が低下する場合がある。

    s8  = u8;   // unsigned char -> signed char    : ビットパターンを維持、上位ビットが符号ビットになる。
    s16 = u8;   // unsigned char -> signed short   : ゼロ拡張。
    s32 = u8;   // unsigned char -> signed long    : ゼロ拡張。
    u8  = u8;   // unsigned char -> unsigned char  : ビットパターンを維持。
    u16 = u8;   // unsigned char -> unsigned short : ゼロ拡張。
    u32 = u8;   // unsigned char -> unsigned long  : ゼロ拡張。
    f   = u8;   // unsigned char -> float          : long への符号拡張、long から float への変換。
    d   = u8;   // unsigned char -> double         : long への符号拡張、long から double への変換。
    ld  = u8;   // unsigned char -> long double    : long への符号拡張、long から long double への変換。

    s8  = u16;   // unsigned short -> signed char    : 下位バイトを維持。上位バイトは消失
    s16 = u16;   // unsigned short -> signed short   : ビット パターンを維持、上位ビットが符号ビットになる。
    s32 = u16;   // unsigned short -> signed long    : ゼロ拡張。
    u8  = u16;   // unsigned short -> unsigned char  : 下位バイトを維持。上位バイトは消失
    u16 = u16;   // unsigned short -> unsigned short : ビットパターンを維持。
    u32 = u16;   // unsigned short -> unsigned long  : ゼロ拡張。
    f   = u16;   // unsigned short -> float          : long への変換、long から float への変換。
    d   = u16;   // unsigned short -> double         : long への変換、long から double への変換。
    ld  = u16;   // unsigned short -> long double    : long への変換、long から long double への変換。

    s8  = u32;   // unsigned long -> signed char    : 下位バイトを維持。上位バイトは消失
    s16 = u32;   // unsigned long -> signed short   : 下位ワードを維持。上位ワードは消失
    s32 = u32;   // unsigned long -> signed long    : ビット パターンを維持、上位ビットが符号ビットになる。
    u8  = u32;   // unsigned long -> unsigned char  : 下位バイトを維持。上位バイトは消失
    u16 = u32;   // unsigned long -> unsigned short : 下位ワードを維持。上位ワードは消失
    u32 = u32;   // unsigned long -> unsigned long  : ビットパターンを維持
    f   = u32;   // unsigned long -> float          : long への変換、long から float への変換。
    d   = u32;   // unsigned long -> double         : long への変換、long から double への変換。
    ld  = u32;   // unsigned long -> long double    : long への変換、long から long double への変換。

    s8  = f;   // float -> signed char    : long への変換、long から char への変換
    s16 = f;   // float -> signed short   : long への変換、long から short への変換
    s32 = f;   // float -> signed long    : 小数点で切り捨てます。 結果が long で表すには大きすぎる場合、結果は未定義になります。
    u8  = f;   // float -> unsigned char  : long への変換、long から unsigned char への変換
    u16 = f;   // float -> unsigned short : long への変換、long から unsigned short への変換
    u32 = f;   // float -> unsigned long  : long への変換、long から unsigned long への変換
    f   = f;   // float -> float          : ビットパターンを維持
    d   = f;   // float -> double         : 内部表現を変更します
    ld  = f;   // float -> long double    : 内部表現を変更します

    s8  = d;   // double -> signed char    : long への変換、long から char への変換
    s16 = d;   // double -> signed short   : long への変換、long から short への変換
    s32 = d;   // double -> signed long    : 小数点で切り捨てます。 結果が long で表すには大きすぎる場合、結果は未定義になります。
    u8  = d;   // double -> unsigned char  : long への変換、long から unsigned char への変換
    u16 = d;   // double -> unsigned short : long への変換、long から unsigned short への変換
    u32 = d;   // double -> unsigned long  : long への変換、long から unsigned long への変換
    f   = d;   // double -> float          : 内部表現を変更します
    d   = d;   // double -> double         : ビットパターンを維持
    ld  = d;   // double -> long double    : 内部表現を変更します

    s8  = ld;   // long double -> signed char    : long への変換、long から char への変換
    s16 = ld;   // long double -> signed short   : long への変換、long から short への変換
    s32 = ld;   // long double -> signed long    : 小数点で切り捨てます。 結果が long で表すには大きすぎる場合、結果は未定義になります。
    u8  = ld;   // long double -> unsigned char  : long への変換、long から unsigned char への変換
    u16 = ld;   // long double -> unsigned short : long への変換、long から unsigned short への変換
    u32 = ld;   // long double -> unsigned long  : long への変換、long から unsigned long への変換
    f   = ld;   // long double -> float          : 内部表現を変更します
    d   = ld;   // long double -> double         : 内部表現を変更します
    ld  = ld;   // long double -> long double    : ビットパターンを維持

    // ポインター型との間の変換
    // ある型の値へのポインターは、別の型へのポインターに変換できます。 ただし、結果は、各型のストレージのアラインメント要件とサイズの違いにより、未定義になることがあります。
    p = q;  // gcc -std=c89 => 警告: assignment from incompatible pointer type

    // void へのポインターは、情報の制限や損失なしに任意の型へのポインターとの間で変換できます。 
    p = v;  // gcc -Wall -Wextra -std=c89 -pedantic test.c => 警告なし

    // ポインター値は、整数値に変換することもできます。 変換パスは、次の規則に従い、ポインターのサイズと整数型のサイズによって決まります。
    // sizeof(void *) == sizeof(uint) の場合

    // ポインターのサイズが整数型のサイズ以上である場合、変換で符号なしの値と同様の動作をします。
    s8  = p; // この場合は unsigned long -> signed char  と同様の動作なので 下位バイトを維持。上位バイトは消失 ）
    s16 = p; // この場合は unsigned long -> signed short と同様の動作なので 下位ワードを維持。上位ワードは消失 ）
    s32 = p; // この場合は unsigned long -> signed long  と同様の動作なので ビット パターンを維持、上位ビットが符号ビットになる。 ）
    u8  = p; // この場合は unsigned long -> unsigned char  と同様の動作なので 下位バイトを維持。上位バイトは消失 ）
    u16 = p; // この場合は unsigned long -> unsigned short と同様の動作なので 下位ワードを維持。上位ワードは消失 ）
    u32 = p; // この場合は unsigned long -> unsigned long  と同様の動作なので ビットパターンを維持 ）
}
" };
        }

        /// <summary>
        /// 妥当ではない代入式(1)
        /// </summary>
        public class InvalidAssignCase : RaiseError<CompilerException.SpecificationErrorException> {
            protected override string[] sources() => new [] { @"
void foo(void) { 
    float          f   = 0;
    unsigned int   *p  = 0;

    // ポインターは、浮動小数点型に変換できない
    f  = p; // gcc =>  error: incompatible types when assigning to type ‘float’ from type ‘float *’
}
" , @"
void foo(void) { 
    double         d   = 0;
    unsigned int   *p  = 0;

    // ポインターは、浮動小数点型に変換できない
    d  = p; // gcc =>  error: incompatible types when assigning to type ‘double’ from type ‘float *’

}
" , @"
void foo(void) { 
    long double    ld  = 0;
    unsigned int   *p  = 0;

    // ポインターは、浮動小数点型に変換できない
    ld = p; // gcc =>  error: incompatible types when assigning to type ‘long double’ from type ‘float *’

}
" };
        }

        /// <summary>
        /// 整数拡張後の型
        /// </summary>
        public class IntegerPromotionTestCase : SuccessCase {
            protected override string[] sources() => new [] { @"
int main(void) {
    signed char c = 0;
    signed char shift = 0;
    if (sizeof( c)  != sizeof(char)) {}
    if (sizeof(+c)  != sizeof(int)) {}
    if (sizeof(-c)  != sizeof(int)) {}
    if (sizeof(~c)  != sizeof(int)) {}
    if (sizeof(c,c) != sizeof(char)) {}
    if (sizeof(c<<shift) != sizeof(int)) {}
    if (sizeof(c>>shift) != sizeof(int)) {}
    return 0;
}

" };
        }
        
        /// <summary>
        /// 配列とポインタ間での代入式と比較式
        /// </summary>
        public class CastBetweenArrayAndStringCase : SuccessCase {
            protected override string[] sources() => new [] { @"
int foo() {
    char *p = ""hello2"";
    char q[] = { ""hello2"" };
    p == ""hello, world"";
    q == ""hello, world"";
    p == q;
    ""hello2"" == ""hello, world"";
    
    return 0;
}
" };
        }

        /// <summary>
        /// 無効な初期化の例
        /// </summary>
        public class BadAssignmentCase : RaiseError<CompilerException.SpecificationErrorException> {
            protected override string[] sources() => new [] { @"
typedef char buf[4];
buf dummy = {'f','r','e','e'};
buf x = dummy; // 無効な初期化

int main() {
    return 0;
}

" };
        }

        /// <summary>
        /// 無効な代入
        /// </summary>
        public class IncompatibleTypeAssignCase : RaiseError<CompilerException.SpecificationErrorException> {
            protected override string[] sources() => new [] { @"
typedef char buf[4];
buf dummy = {'f','r','e','e'};
buf x;

int main() {
    x = dummy;
    return 0;
}

" };
        }

        /// <summary>
        /// 引数の定義のだめなケース
        /// </summary>
        public class RedefinitionOfParameterBadCase : RaiseError<CompilerException.SpecificationErrorException> {
            protected override string[] sources() => new[] { @"
int f(x) 
int x;
int x;  // 同名の宣言並びが存在
{
    return x;
}

", @"
// 6.9.1 関数定義
// 各仮引数は，自動記憶域期間をもつ。その識別子は左辺値とし，関数本体を構成する複合文の先頭で宣言されたとみなす（このため，関数本体では更に内側のブロックの中以外では再宣言できない。）。
int f(int x) {
    int x; // 関数本体ブロック中での再定義なのでNG
    return x;
}

", @"
// 6.9.1 関数定義
// 型定義名として宣言された識別子を仮引数として再宣言してはならない。
// K&Rでは関数宣言中、typedef 名を仮パラメータ名として使用できる。つまり、typedef 宣言を隠す。 	
// ANSI以降ではtypedef 名として宣言された識別子を仮パラメータとして使用できない。
// gccでは警告すら出ないためか、あまり知られていない。

typedef struct foo foo;

void blah(int foo) {
  foo = 1;
}
" };
        }

        /// <summary>
        /// 引数の再定義とはならないケース
        /// </summary>
        public class RedefinitionOfParameterGoodCase : SuccessCase {
            protected override string[] sources() => new[] { @"
// 6.9.1 関数定義
// 各仮引数は，自動記憶域期間をもつ。その識別子は左辺値とし，関数本体を構成する複合文の先頭で宣言されたとみなす（このため，関数本体では更に内側のブロックの中以外では再宣言できない。）。
int f(int x) {
    {
        int x; // 関数本体の更に内側のブロックの中で再定義なのでOK
        return x;
    }
}

", @"
// 6.9.1 関数定義
// 型定義名として宣言された識別子を仮引数として再宣言してはならない。

typedef struct foo foo;

void blah(int foo); // 
" };
        }

        /// <summary>
        /// K&R識別子並び中ではregister以外の記憶クラス指定子を使えない
        /// </summary>
        public class BadStorageClassOfParameterKAndRCase : RaiseError<CompilerException.SpecificationErrorException> {
            protected override string[] sources() => new [] { @"
int f(x) 
typedef int SINT;
SINT x;
{
return x;
}
",@"
int f(x) 
extern int x;
{
return x;
}
", @"
int f(x) 
static int  x;
{
return x;
}
", @"
int f(x) 
auto int  x;
{
return x;
}
" };
        }


        /// <summary>
        /// K&R識別子並び中ではregister以外の記憶クラス指定子を使えない
        /// </summary>
        public class BadControlCase : RaiseError<CompilerException.SyntaxErrorException> {
            protected override string[] sources() => new[] { @"
void foo() { 
  break; /* expected-error {{'break' statement not in loop or switch statement}} */
}
", @"
void foo2() { 
  continue; /* expected-error {{'continue' statement not in loop statement}} */
}
", @"
int pr8880_9 (int first) {
  switch(({ if (first) { first = 0; break; } 1; })) { // expected-error {{'break' statement not in loop or switch statement}}
  case 2: return 2;
  default: return 0;
  }
}
", @"
void pr8880_24() {
  for (({break;});;); // expected-error {{'break' statement not in loop or switch statement}}
}
", @"
void pr8880_25() {
  for (({continue;});;); // expected-error {{'continue' statement not in loop statement}}
}
" };
        }



        /// <summary>
        /// ラベル付き文とgoto文のケース
        /// </summary>
        public class GotoLabelBadCase : RaiseError<CompilerException.SpecificationErrorException> {
            protected override string[] sources() => new[] { @"
void foo() { 
  goto L1; // 参照先がない
}
", @"
void foo2() { 
 goto L1;
L1: ; 
L1: ; // 再定義
}
", @"
void foo3() { 
L1: ; 
}
void foo4() { 
 goto L1; // スコープが違うから参照できない
}
" };
        }


        /// <summary>
        /// 規格書の 6.9.1 脚注(137)の不正ケース
        /// </summary>
        public class Spec_6_9_1_Foornote_137_BadCase : RaiseError<CompilerException.SpecificationErrorException> {
            protected override string[] sources() => new[] { @"
typedef int F(void);          // 型 F は“仮引数をもたず，int を返す関数”
F   f { /* ... */ }           // 誤：6.9.1 関数定義の制約違反。
", @"
typedef int F(void);          // 型 F は“仮引数をもたず，int を返す関数”
F   g() { /* ... */ }         // 誤：g が関数を返すことになる
" };
        }


        /// <summary>
        /// 規格書の 6.9.1 脚注(137)の妥当ケース
        /// </summary>
        public class Spec_6_9_1_Foornote_137_GoodCase : SuccessCase {
            protected override string[] sources() => new[] { @"
typedef int F(void);          // 型 F は“仮引数をもたず，int を返す関数”
F   f,  g;                    // 正：f と g はいずれも F と適合する型をもつ
", @"
typedef int F(void);          // 型 F は“仮引数をもたず，int を返す関数”
int f(void)  {  /* ... */ }   // 正：f は F と適合する型をもつ
int g() { /* ... */ }         // 正：g は F と適合する型をもつ
", @"
typedef int F(void);          // 型 F は“仮引数をもたず，int を返す関数”
F  *e(void) { /* ... */ }     // 正：e は関数へのポインタを返す
", @"
typedef int F(void);          // 型 F は“仮引数をもたず，int を返す関数”
F  *((e))(void) { /*... */ }  // 正：同上，括弧は無関係
", @"
typedef int F(void);          // 型 F は“仮引数をもたず，int を返す関数”
int (*fp)(void);              // 正：fpは型 F の関数を指す
", @"
typedef int F(void);          // 型 F は“仮引数をもたず，int を返す関数”
F  *Fp;                       // 正：Fpは型 F の関数を指す
" };
        }

        /// <summary>
        /// 関数宣言の妥当ケース
        /// </summary>
        public class FunctionDeclGoodCase : SuccessCase {
            protected override string[] sources() => new[] { @"
", @"
int g(void) { return 0; }
int (*f)(void) = g; // 関数ポインタ変数の初期値設定なので妥当
",@"
int foo(x,y)    // yの型は int になる（警告対象）
int x;
{
return x;
}
"
            };
        }

        /// <summary>
        /// 関数宣言の間違いケース
        /// </summary>
        public class FunctionDeclBadCase : RaiseError<CompilerException.SpecificationErrorException> {
            protected override string[] sources() => new[] { @"
int g(void) { return 0; }
int f(void) = g; // 関数定義に初期値設定はダメ
",@"
int foo() 
int x; // gcc: 仮引数 ‘x’ 用の宣言がありますが、そのような仮引数はありません
{
return x;
}
",@"
int foo(x) 
int x;
int x;  // gcc 仮引数 ‘x’ が再宣言されました
{
return x;
}
",@"
int foo(const x,  const y)  // ANSI形式の仮引数並びになるので
int x, y;   // これは不正
{
return x;
}

"
            };
        }

        /// <summary>
        /// 変数宣言の妥当なケース
        /// </summary>
        public class VarDeclGoodCase : SuccessCase {
            protected override string[] sources() => new[] { @"
int main(void) {
	extern int x;   // 外部定義なので問題なし
	extern int x;
	return 0;
}
", @"
int x;   // 不完全な定義同士の多重宣言なので問題なし
int x;
int main(void) {
	return 0;
}
", @"
extern int x;
int x;
int main(void) {
	return 0;
}
int x = 0;
", @"
int x;  // 不完全な定義
extern int x;  // 不完全な定義
int main(void) {
	return 0;
}
extern int x = 0;   // 完全な定義（ただし警告付き）
", @"
int main(void) {
    extern int hoge(void);
    int hoge(void);
	return 4;
}
int hoge(void) {
}
", @"
int hoge(void) {
}
int main(void) {
    extern int hoge(void);
    int hoge(void);
	return 4;
}
", @"
int main(void) {
    extern int hoge(void);
    extern int hoge(void);
	return 4;
}

extern int hoge(void);

int boo(void) {
	return hoge(); 
}
",@"
typedef int I32;
I32 main(void) {
	typedef short int I32;  // 宣言されるスコープが違うのでＯＫ
	I32 x = 4;
	return x;
}
",@"
typedef int (Func)();

int main(void) {
	extern Func f;  // 変数宣言ではなく、関数宣言
    extern int f(); // なのでこれと等価
	return f();
}

int f(void) {
return 0;
}
"
            };
        }

        /// <summary>
        /// 変数宣言の間違いケース
        /// </summary>
        public class VarDeclBadCase : RaiseError<CompilerException> {
            protected override string[] sources() => new[] { @"
int main(void) {
	int x;
	int x;  // 再定義エラー
	return 0;
}
", @"
int main(void) {
	static int x;
	int x;  // 再定義エラー
	return 1;
}
", @"
int main(void) {
	static int x;
	static int x;  // 再定義エラー
	return 2;
}
", @"
int x = 0;
int x = 1;  // 再定義エラー
int main(void) {
	return 3;
}
", @"
int x;
double x;  // 再定義エラー
int main(void) {
	return 4;
}
", @"
int x;
int main(void) {
    extern double x;  // 型の競合エラー
	return 4;
}
", @"
static int x;
int main(void) {
    extern double x;  // 型の競合エラー
	return 4;
}
", @"
int main(void) {
    staic int hoge(void);
	return 4;
}
static int hoge(void) {
}
", @"
int main(void) {
    int hoge(void);
	return 4;
}
double hoge(void) {
}
", @"
int main(void) {
    int hoge(double);
	return 4;
}
int hoge(void) {
}
", @"
int main(void) {
    int hoge(void);
	return 4;
}
static int hoge(void) {
}
", @"
int hoge(double y) {
}
int main(void) {
    int hoge(void);
	return 4;
}
", @"
static int hoge(void) {
}
int main(void) {
    int hoge(void);
	return 4;
}
", @"
int main(void) {
    staic int hoge(void);
    extern int hoge(void);
	return 4;
}
", @"
typedef int (Func)();

int main(void) {
	Func f = 0;  // 変数宣言ではなく、関数宣言なので初期化式は使えない
    // int f(); // 上のコードはこれと等価
	return f();
}

int f(void) {
return 0;
}
"
            };
        }

    }
}