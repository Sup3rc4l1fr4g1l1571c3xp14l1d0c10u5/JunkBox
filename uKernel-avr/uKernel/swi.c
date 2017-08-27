#include "swi.h"
#include "systemcall.h"
#include "kerneldata.h"

/**
 * @addtogroup  ソフトウェア割り込み
 */

/*@{*/

/**
 * @brief   ソフトウェア割り込み初期化
 */
void init_swi(void) {
    /* 外部割り込み INT0(PD2) は出力モード */
    DDRD |= _BV(PORTD2);
    PORTD &= ~_BV(PORTD2);

    /* 外部割り込み条件: INT0(PD2) の立ち上がりで発生 */
    EICRA |= (_BV(ISC01) | _BV(ISC00));

    /* 外部割り込みマスクレジスタ: INT0(PD2) の割り込みを許可 */
    EIMSK |= _BV(INT0);
}

/**
* @brief    システムコール呼び出し（カーネル側）
* @return   ステータスコード
* @note     AVRマイコンにはソフトウェア割り込みが無いため外部割り込み0で代用する
*/
__attribute__((signal, naked)) void INT0_vect(void);
void INT0_vect(void) {

    /* 割り込みの直前まで実行されていたコンテキストを保存 */
    SAVE_CONTEXT();

    /* カーネルスタックに切り替え */
    SET_KERNEL_STACKPOINTER();

    /* EXT0割り込みをリセット */
    PORTD &= ~_BV(PORTD2);

    /* システムコール呼び出し処理に移動 */
    syscall_entry();
}

/*@}*/
