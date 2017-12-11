/**@
spec: 代入時に暗黙の型変換ができない不正なケース(1)
 assertion: SpecificationErrorException
@**/

void foo(void) { 
    float          f   = 0;
    unsigned int   *p  = 0;

    // ポインターは、浮動小数点型に変換できない
    f  = p; // gcc =>  error: incompatible types when assigning to type ‘float’ from type ‘float *’
}

