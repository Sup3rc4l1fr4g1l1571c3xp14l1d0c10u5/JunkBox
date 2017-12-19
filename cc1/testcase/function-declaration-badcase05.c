/**@
 spec: 不正なな関数宣言のケース(5)
 assertion: SpecificationErrorException
@**/

typedef int Foo(int, int);

Foo {	/* 関数定義で宣言する識別子（その関数の名前）の型が関数型であることは，その関数定義の宣言子の部分で指定しなければならない。 */
}

