/**@
 spec: clang/test/Parser/CompoundStmtScope.c
 assertion: SpecificationErrorException
@**/

void foo() {
  {
    typedef float X;
  }
  X Y;
}

