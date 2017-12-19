/**@
 spec: union 中で typedef
 assertion: SyntaxErrorException
@**/

union Z {
    typedef int SINT ;  /* NG */
    SINT x;
};

