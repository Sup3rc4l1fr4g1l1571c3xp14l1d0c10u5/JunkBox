/**@
 spec: 規格書の 6.9.2 外部オブジェクト定義の違反例(2)
 assertion: SpecificationErrorException
@**/

int i1 = 1;  //定義，外部結合  
static int i2 = 2;  //定義，内部結合  
extern int i3 = 3;  //定義，外部結合  
int  i4;   //仮定義，外部結合  
static int i5;  //仮定義，内部結合  
int  i1;   //正しい仮定義，前の定義を参照する  
//int  i2;   //これ単体では外部結合だが、前に内部結合をもつ定義があるため，結合の不一致が生じ，6.2.2 によって動作は未定義となる  
int  i3;   //正しい仮定義，前の定義を参照する
int  i4;   //正しい仮定義，前の定義を参照する
int  i5;   //これ単体では外部結合だが、前に内部結合をもつ定義があるため，結合の不一致が生じ，6.2.2 によって動作は未定義となる  
extern int i1;  //外部結合をもつ前の定義を参照する  
extern int i2;  //内部結合をもつ前の定義を参照する
extern int i3; // 外部結合をもつ前の定義を参照する
extern int i4; // 外部結合をもつ前の定義を参照する
extern int i5; // 内部結合をもつ前の定義を参照する


extern double fahr(double); // 外部定義を生成する

//static double fahr(double t) { return (9.0 * t) / 5.0 + 32.0; }    // これ単体では内部結合だが、前に外部結合をもつ定義があるため，結合の不一致が生じ，6.2.2 によって動作は未定義となる  
double fahr(double t) { return (9.0 * t) / 5.0 + 32.0; }    // 記憶域クラス指定子が指定されていないのでexternが宣言されたと同様に処理する。その結果、外部結合となり、前の外部結合の定義と合致するため正しい
