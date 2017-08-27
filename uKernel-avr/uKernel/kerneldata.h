#pragma once
#include "kernel.h"

#if !defined(AVR)
extern volatile uint8_t TCCR0A;
extern volatile uint8_t TCCR0B;
extern volatile uint8_t OCR0A;
extern volatile uint8_t TIMSK0;
#define PINB5 5
extern volatile uint8_t DDRB;
extern volatile uint8_t PORTB;
#define PORTD2 2
extern volatile uint8_t DDRD;
extern volatile uint8_t PORTD;
extern volatile uint8_t EICRA;
extern volatile uint8_t EIMSK;
extern volatile uint8_t INT0;
#endif

/**
 * @addtogroup  タスク
 */

/*@{*/

/**
 * @brief   タスクエントリ配列
 */
extern const taskentry_t task_entries[TASK_NUM];

/**
 * @brief   タスクスタック配列
 */
extern uint8_t  task_stack[TASK_NUM][TASK_STACK_SIZE];

/**
 * @brief   タスクコントロールブロック配列
 */
extern tcb_t    tcbs[TASK_NUM];

/**
 * @brief   カーネルスタック
 */
extern uint8_t  kernel_stack[KERNEL_STACK_SIZE];

/**
 * @brief   現在のタスクを示すタスクコントロールブロック
 */
extern tcb_t*   current_tcb;

/**
 * @brief   INITTASKを示すタスクコントロールブロック
 */
extern tcb_t* const INITTASK;

/**
 * @brief   DIAGTASKを示すタスクコントロールブロック
 */
extern tcb_t* const DIAGTASK;

/*@}*/

/**
 * @addtogroup  スケジューリング
 */

/*@{*/

/**
 * @brief   スケジューリング要求フラグ
 */
extern bool request_reschedule;

/*@}*/

/**
 * @addtogroup メッセージ
 */

/*@{*/

/**
 * @brief   メッセージ領域
 */
extern message_t    messages[MESSAGE_NUM];

/**
 * @brief   空きメッセージ領域を管理するリンクリスト
 */
extern message_t*   free_message_list;

/*@}*/

/**
 * @addtogroup  外部割り込み
 */

/*@{*/

/**
 * @brief   発生した外部割り込みの番号
 */
extern volatile extintid_t extint_id;

/*@}*/

/**
 * @addtogroup  Readyタスクキュー
 */

/*@{*/

/**
 * @brief   ready状態のタスクを並べるキュー
 */
extern readyqueue_t readyqueue;

/*@}*/

/**
 * @addtogroup  セマフォ
 */

/*@{*/

/**
 * @brief   セマフォキュー(各セマフォ毎の待ち行列、優先度付き)
 */
extern semaphoqueue_t semaphorequeue[SEMAPHORE_NUM];

/*@}*/

/**
 * @addtogroup  一時停止タスクキュー
 */

/*@{*/

/**
 * @brief   Pause状態のタスクを並べるキュー
 */
extern pausequeue_t pausequeue;

/*@}*/
