#include "kerneldata.h"
#include "extint.h"

#if !defined(AVR)
volatile uint8_t TCCR0A;
volatile uint8_t TCCR0B;
volatile uint8_t OCR0A;
volatile uint8_t TIMSK0;
volatile uint8_t DDRB;
volatile uint8_t PORTB;
volatile uint8_t DDRD;
volatile uint8_t PORTD;
volatile uint8_t EICRA;
volatile uint8_t EIMSK;
volatile uint8_t INT0;
#endif

/**
 * @addtogroup  タスク
 */

/*@{*/

/**
 * @brief   タスクスタックの配列
 * 
 * - 1タスクあたり TASK_STACK_SIZE バイトのスタックを割り当て
 * - システム全体で 最大 TASK_NUM 個のタスクを生成可能(ただし、INITとDIAGの必須タスクが二つあるため、ユーザーが自由に使えるのはTASK_NUM-2個)

 */
uint8_t task_stack[TASK_NUM][TASK_STACK_SIZE];

/**
 * @brief   タスクコントロールブロック配列
 */
tcb_t   tcbs[TASK_NUM];

/**
 * @brief   カーネルスタック
 */
uint8_t kernel_stack[KERNEL_STACK_SIZE];

/**
 * @brief 現在のタスクを示すタスクコントロールブロック
 */
tcb_t  *current_tcb;

/**
 * @brief   INITTASKを示すタスクコントロールブロック
 */
tcb_t* const INITTASK = &tcbs[0];

/**
 * @brief   DIAGTASKを示すタスクコントロールブロック
 */
tcb_t* const DIAGTASK = &tcbs[TASK_NUM - 1];

/*@}*/

/**
 * @addtogroup  スケジューリング
 */

/*@{*/

/**
 * @brief   スケジューリング要求フラグ
 */
bool    request_reschedule;

/*@}*/

/**
 * @addtogroup メッセージ
 */

/*@{*/

/**
 * @brief   メッセージ領域
 */
message_t   messages[MESSAGE_NUM];

/**
 * @brief   空きメッセージ領域を管理するリンクリスト
 */
message_t*  free_message_list;

/*@}*/

/**
 * @addtogroup  外部割り込み
 */

/*@{*/

/**
 * @brief   発生した外部割り込みの番号が保持されるグローバル変数
 *
 * 外部割り込みが発生すると、発生した外部割り込みに対応するextintid_t型の値がこの変数に設定される
 */
volatile extintid_t extint_id;

/*@}*/

/**
 * @addtogroup  Readyタスクキュー
 */

/*@{*/

/**
 * @brief   ready状態のタスクを並べるキュー
 */
readyqueue_t readyqueue;

/*@}*/

/**
 * @addtogroup  セマフォ
 */

/*@{*/

/**
 * @brief   セマフォキュー(各セマフォ毎の待ち行列、優先度付き)
 */
semaphoqueue_t semaphorequeue[SEMAPHORE_NUM];

/*@}*/

/**
 * @addtogroup  一時停止タスクキュー
 */

/*@{*/

/**
 * @brief   Pause状態のタスクを並べるキュー
 */
pausequeue_t pausequeue;

/*@}*/
