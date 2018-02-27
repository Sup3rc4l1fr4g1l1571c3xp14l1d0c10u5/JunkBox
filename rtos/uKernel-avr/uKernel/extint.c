#include "extint.h"
#include "readyqueue.h"
#include "task.h"
#include "kerneldata.h"

/**
 * @addtogroup  外部割り込み
 * @brief       外部割り込みに関する機能を集約
 * @{
 */

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
static extinthook_t extinthooktable[EXTINT_MAX];

/**
 * @def             EXTINT_IS_HOOKABLE
 * @brief           ENABLE_EXTINT_CAPTURE(name) が設定されている際に is_hookable_extint(name) が true(フック可能) を返すことを宣言
 * @param   name    AVRの割り込み名
 */
#define EXTINT_IS_HOOKABLE(name)  case EXTINT_##name: return true;

/**
 * @def             EXTINT_ISNOT_HOOKABLE
 * @brief           DISABLE_EXTINT_CAPTURE(name) が設定されている際に is_hookable_extint(name) が false(フック不可能) を返すことを宣言
 * @param   name    AVRの割り込み名
 */
#define EXTINT_ISNOT_HOOKABLE(name)  case EXTINT_##name: return true;

/**
 * @brief                   フック可能な外部割り込み番号か判定
 * @param   [in] extintid   外部割り込み番号
 * @retval  true            フック可能な外部割り込み番号
 * @retval  false           フック不可能な外部割り込み番号
 */
bool is_hookable_extint(extintid_t extintid) {

    switch (extintid) {
#define ENABLE_EXTINT_CAPTURE(name)  EXTINT_IS_HOOKABLE(name)
#define DISABLE_EXTINT_CAPTURE(name) EXTINT_ISNOT_HOOKABLE(name)
#include "extintconf.h"
#undef ENABLE_EXTINT_CAPTURE
#undef DISABLE_EXTINT_CAPTURE
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
 * @def             GEN_EXTINT_CAPTURE_HANDLER
 * @brief           外部割り込みをフックする処理を生成する
 * @param   name    AVRの割り込み名
 * 
 * - 発生した割り込みに対応する割り込みフックハンドラを生成する。
 * - フックハンドラは割り込み番号をグローバル変数 extint_id に設定してから、割り込み処理ハンドラ notice_extint を呼び出す。
 */
#define GEN_EXTINT_CAPTURE_HANDLER(name)                                     \
__attribute__((naked,noreturn))                                         \
void name##_vect(void) {                                                \
    asm volatile("PUSH  R24             ");                             \
    asm volatile("LDI   R24, %0         " : : "M" (EXTINT_##name) );    \
    asm volatile("STS   extint_id, R24  ");                             \
    asm volatile("POP   R24             ");                             \
    asm volatile("JMP   notice_extint   ");                             \
}

#define ENABLE_EXTINT_CAPTURE(name) GEN_EXTINT_CAPTURE_HANDLER(name)
#define DISABLE_EXTINT_CAPTURE(name)
#include "extintconf.h"
#undef ENABLE_EXTINT_CAPTURE
#undef DISABLE_EXTINT_CAPTURE

/*@}*/

