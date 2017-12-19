/**@
 spec: 既定の実引数拡張
 assertion: 
@**/
void f();

void foo(void) { 
  float x = 3.14f; 
  f(x);     /* 既定の実引数拡張で float -> double になる */
}

void f (double x) { 
  (int)x;
}
