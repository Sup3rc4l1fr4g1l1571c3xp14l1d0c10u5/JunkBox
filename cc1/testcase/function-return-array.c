/**@
 spec: 配列を返す関数
 assertion: SpecificationErrorException
@**/
typedef char BUF[256];

BUF hoge(BUF buf) { /* エラー: hoge は配列を返す関数として宣言されています */
    return buf;
}
