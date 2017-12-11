/**@
 spec: 代入時に暗黙の型変換が起きるケース
 assertion: 
@**/

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

