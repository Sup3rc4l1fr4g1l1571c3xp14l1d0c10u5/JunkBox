/**@
spec: 代入時に暗黙の型変換ができない不正なケース(2)
 assertion: SpecificationErrorException
@**/

void foo(void) { 
    double         d   = 0;
    unsigned int   *p  = 0;

    // ポインターは、浮動小数点型に変換できない
    d  = p; // gcc =>  error: incompatible types when assigning to type ‘double’ from type ‘float *’

}

