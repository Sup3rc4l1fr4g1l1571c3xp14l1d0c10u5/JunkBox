/**@
 spec: Block scope linkage C standard
 assertion: SpecificationErrorException
 note: https://stackoverflow.com/questions/7239911/block-scope-linkage-c-standard
@**/

int foo() {
    static int a; //no linkage
}

int foo() {
    static int a; //no linkage
    extern int a; //external linkage, and get an error because this code violates a constraint in 則6.7
}

