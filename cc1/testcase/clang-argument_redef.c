/**@
 spec: clang/test/Parser/argument_redef.c
 assertion: SpecificationErrorException
@**/

void foo(int A) { /* expected-note {{previous definition is here}} */
  int A; /* expected-error {{redefinition of 'A'}} */
}
