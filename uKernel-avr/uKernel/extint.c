#include "extint.h"
#include "readyqueue.h"
#include "task.h"
#include "kerneldata.h"

/**
 * @addtogroup  外部割り込み
 */

/*@{*/

/**
 * @brief   外部割り込みのフック表の要素を示す構造体
 */
typedef struct extinthook_t {
    taskid_t    taskid;     /**< 外部割り込みで起動するタスク番号（０は起動するタスク無し） */
    void*       argument;   /**< タスク起動時の引数 */
} extinthook_t;

/**
 * @brief   外部割り込みフック表
 */
static extinthook_t extinthooktable[INT_MAX];

/**
 * @brief                   フック可能な外部割り込み番号か判定
 * @param   [in] extintid   外部割り込み番号
 * @retval  true            フック可能な外部割り込み番号
 * @retval  false           フック不可能な外部割り込み番号
 */
bool is_hookable_extint(extintid_t extintid) {
    switch (extintid) {
        case INT_RESET:         /* リセット割り込みはフックできない */
        case INT_INT0:          /* INT0割り込みはシステムコール呼び出しで使うためフックできない */
#if     (F_CPU == 20000000UL) || (F_CPU == 16000000UL)
        case INT_TIMER1_CAPT:
        case INT_TIMER1_COMPA:  /* タイマー1はカーネルタイマーで使用中 */
        case INT_TIMER1_COMPB:  /* タイマー1はカーネルタイマーで使用中 */
        case INT_TIMER1_OVF:    /* タイマー1はカーネルタイマーで使用中 */
#elif   (F_CPU == 12000000UL) || (F_CPU ==  8000000UL)
        case INT_TIMER0_COMPA:  /* タイマー0はカーネルタイマーで使用中 */
        case INT_TIMER0_COMPB:  /* タイマー0はカーネルタイマーで使用中 */
        case INT_TIMER0_OVF:    /* タイマー0はカーネルタイマーで使用中 */
#endif
            return false;
        case INT_INT1:
        case INT_PCINT0:
        case INT_PCINT1:
        case INT_PCINT2:
        case INT_WDT:
        case INT_TIMER2_COMPA:
        case INT_TIMER2_COMPB:
        case INT_TIMER2_OVF:
#if     (F_CPU == 20000000UL) || (F_CPU == 16000000UL)
        case INT_TIMER0_COMPA:
        case INT_TIMER0_COMPB:
        case INT_TIMER0_OVF:
#elif   (F_CPU == 12000000UL) || (F_CPU ==  8000000UL)
        case INT_TIMER1_CAPT:
        case INT_TIMER1_COMPA:
        case INT_TIMER1_COMPB:
        case INT_TIMER1_OVF:
#endif
        case INT_SPI_STC:
        case INT_USART_RX:
        case INT_USART_UDRE:
        case INT_USART_TX:
        case INT_ADC:
        case INT_EE_READY:
        case INT_ANALOG_COMP:
        case INT_TWI:
        case INT_SPM_READY:
            return true;
        case INT_MAX:
        default:
            return false;
    }
}

/**
 * @brief                   外部割り込みフック表に情報を設定する
 * @param   [in] extintid   フックする外部割り込み番号。フック不能な外部割り込み番号が指定されている場合は何もしない。
 * @param   [in] taskid     外部割り込みで起動するタスクの番号。ユーザータスク以外が指定されている場合はタスクが起動しない。
 * @param   [in] argument   タスク起動時の引数
 */
void hook_extint(extintid_t extintid, taskid_t taskid, void *argument) {
    if (is_hookable_extint(extintid)) {
        extinthooktable[extintid].taskid   = taskid;
        extinthooktable[extintid].argument = argument;
    }
}

/**
 * @brief   外部割り込みに応じてタスクを起動
 */
void handle_extint(void) {
    /* 外部割り込み番号はグローバル変数 extint_id 経由で受け取る */
    extintid_t intid = extint_id;

    /* フック可能な外部割り込み番号のみ処理する */
    if (is_hookable_extint(intid)) {
        taskid_t tid = extinthooktable[intid].taskid;
        if (is_user_task(tid)) {    /* ユーザータスク以外が起動するように指定されている場合は何もしない（安全策） */
            tcb_t* tcb = &tcbs[tid];
            if (tcb->status == DORMANT) {   /* DORMANT状態以外のタスクは起動しない */

                /* 起動するタスクの設定 */
                tcb->status    = READY;
                tcb->next_task = NULL;
                tcb->argument  = extinthooktable[intid].argument;

                /* 引数を設定 */
                context_t* ctx = get_context_from_tcb(tcb);
                set_argument_to_context(ctx, tcb->argument);

                /* readyキューに挿入 */
                add_task_to_readyqueue(tcb);

                /* スケジューリング要求 */
                /* 自分よりも優先度の高いタスクが挿入された場合にのみスケジューリングを要求 */
                request_reschedule = (current_tcb > tcb) ? true : false;
            }
        }
    }
    /* スケジューリングを実行 */
    schedule();
}

/**
 * @brief    コンテキストをカーネル側に切り替えた後に、外部割込みをカーネルに通達する
 */
__attribute__((naked, noreturn)) void notice_extint(void) {
    /* 現在のタスクのコンテキストを保存 */
    SAVE_CONTEXT();

    /* カーネルスタックに切り替え */
    SET_KERNEL_STACKPOINTER();

    /* 以降はカーネルスタックの利用が可能になったので、関数呼び出しなどが可能となる */

    /* カーネルの外部割込み処理ハンドラを呼び出す（戻ってこない） */
    handle_extint();
}

/**
 * @def             ENABLE_EXTINT_CAPTURE
 * @brief           外部割り込みを捕捉するコードを生成するマクロ
 * @param   name    AVRの割り込み名
 * 
 * 発生した割り込みに対応する割り込み番号をグローバル変数 extint_id に設定してから、割り込み処理ハンドラ notice_extint を呼び出している。
 */
#define ENABLE_EXTINT_CAPTURE(name)                                 \
__attribute__((naked,noreturn))                                     \
void name##_vect(void) {                                            \
    asm volatile("PUSH  R24             ");                         \
    asm volatile("LDI   R24, %0         " : : "M" (INT_##name) );   \
    asm volatile("STS   extint_id, R24  ");                         \
    asm volatile("POP   R24             ");                         \
    asm volatile("JMP   notice_extint   ");                         \
}

/**
 * @def             DISABLE_EXTINT_CAPTURE
 * @brief           外部割り込みを捕捉しないことを示すマクロ
 * @param   name    AVRの割り込み名
 */
#define DISABLE_EXTINT_CAPTURE(name) 

/* 各外部割り込みを捕捉するためのコードを生成 */

/* リセット割り込みはフック禁止 */
DISABLE_EXTINT_CAPTURE(RESET)
/* INT0割り込みはソフトウェア割り込みで使用するためフック禁止 */
DISABLE_EXTINT_CAPTURE(INT0)
ENABLE_EXTINT_CAPTURE(INT1)
ENABLE_EXTINT_CAPTURE(PCINT0)
ENABLE_EXTINT_CAPTURE(PCINT1)
ENABLE_EXTINT_CAPTURE(PCINT2)
ENABLE_EXTINT_CAPTURE(WDT)
ENABLE_EXTINT_CAPTURE(TIMER2_COMPA)
ENABLE_EXTINT_CAPTURE(TIMER2_COMPB)
ENABLE_EXTINT_CAPTURE(TIMER2_OVF)
#if     (F_CPU == 20000000UL) || (F_CPU == 16000000UL)
/* TIMER0はカーネルタイマ割り込みで使用するためフック禁止 */
ENABLE_EXTINT_CAPTURE(TIMER1_CAPT)
ENABLE_EXTINT_CAPTURE(TIMER1_COMPA)
ENABLE_EXTINT_CAPTURE(TIMER1_COMPB)
ENABLE_EXTINT_CAPTURE(TIMER1_OVF)
DISABLE_EXTINT_CAPTURE(TIMER0_COMPA)
DISABLE_EXTINT_CAPTURE(TIMER0_COMPB)
DISABLE_EXTINT_CAPTURE(TIMER0_OVF)
#elif   (F_CPU == 12000000UL) || (F_CPU ==  8000000UL)
/* TIMER1はカーネルタイマ割り込みで使用するためフック禁止 */
DISABLE_EXTINT_CAPTURE(TIMER1_CAPT)
DISABLE_EXTINT_CAPTURE(TIMER1_COMPA)
DISABLE_EXTINT_CAPTURE(TIMER1_COMPB)
DISABLE_EXTINT_CAPTURE(TIMER1_OVF)
ENABLE_EXTINT_CAPTURE(TIMER0_COMPA)
ENABLE_EXTINT_CAPTURE(TIMER0_COMPB)
ENABLE_EXTINT_CAPTURE(TIMER0_OVF)
#endif
ENABLE_EXTINT_CAPTURE(SPI_STC)
ENABLE_EXTINT_CAPTURE(USART_RX)
ENABLE_EXTINT_CAPTURE(USART_UDRE)
ENABLE_EXTINT_CAPTURE(USART_TX)
ENABLE_EXTINT_CAPTURE(ADC)
ENABLE_EXTINT_CAPTURE(EE_READY)
ENABLE_EXTINT_CAPTURE(ANALOG_COMP)
ENABLE_EXTINT_CAPTURE(TWI)
ENABLE_EXTINT_CAPTURE(SPM_READY)

/*@}*/

