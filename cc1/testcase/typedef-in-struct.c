/**@
 spec: struct 中で typedef
 assertion: SyntaxErrorException
@**/

struct Z {
    typedef int SINT ;  /* NG */
    SINT x;
};

