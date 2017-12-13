/**@
 spec: clang/test/Parser/argument_scope.c
 assertion: SpecificationErrorException
@**/

typedef struct foo foo;

void blah(int foo) {
  foo = 1;
}
