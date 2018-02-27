#pragma once

/** 
 * \mainpage uKernel-avr
 * \section intro_sec これはなに？
 *
 * - 教育用リアルタイムOS の uKernel をAVRマイコン(ATmega328P)に移植＋αしたものです。
 * - Atmel Studio内蔵のシミュレータデバッガや、ATmega328P評価ボードとして最も入手しやすい Arduino ボードで動作します。
 *
 * \section spec_sec 機能
 * - uKernel仕様書に記載されている機能のうち以下が実装済みです。
 *  - スケジューラ（固定優先度スケジューリング）
 *  - バイナリセマフォ
 *  - メッセージキュー
 *  - 割り込み管理機構
 * - 以下については移植していません。
 *  - スケジューラ: 優先度なしタイムシェアリングスケジューリング、優先度付きタイムシェアリングスケジューリング（ラウンドロビン）
 *
 * \section dev_sec 開発環境
 * - Windows 10 Pro (x64)
 * - Atmel Studio 7
 *
 */

#if defined(AVR)
#include <avr/interrupt.h>
#else
/* 以下はDoxygen実行時にAVR環境固有のシンボルや記法がエラーになることの対策 */
#define __attribute__(x)
#define cli()
#define _BV(x) (1 << (x))
#define ISC01 1
#define ISC00 1
#define volatile(x)
#ifdef _MSC_VER
#define asm(x)
#define asm
#else
#define asm __asm
#endif
#endif

#include <stdint.h>
#include <stdbool.h>
#include <stddef.h>

#define TASK_NUM            (6)
#define TASK_STACK_SIZE     (64)
#define KERNEL_STACK_SIZE   (64)
#define MESSAGE_NUM         (16)
#define SEMAPHORE_NUM       (16)
#define USE_SIMULATOR       (1)

#if !defined(F_CPU)
#define F_CPU 16000000UL
#endif

/**
 * @addtogroup  システムコール
 * @brief       システムコールに関する機能を集約
 * @{
 */

/**
 * @brief   システムコールの呼び出し結果
 */
typedef enum svcresultid_t {
    SUCCESS = 0,    /**< 正常終了 */
    ERR1 = 1,       /**< タスクＩＤが不正  */
    ERR2 = 2,       /**< DORMANT状態でないタスクを起動した */
    ERR3 = 3,       /**< ready 状態でないタスクを exit させようとした */
    ERR4 = 4,       /**< ready 状態でないタスクを pause させようとした */
    ERR5 = 5,       /**< wait 状態でないタスクを resume させようとした */
    ERR6 = 6,       /**< Pause指定時間が不正 */
    ERR7 = 7,       /**< [予約] */
    ERR8 = 8,       /**< [予約] */
    ERR9 = 9,       /**< [予約] */
    ERR10 = 10,     /**< セマフォＩＤが不正  */
    ERR11 = 11,     /**< 指定したセマフォが獲得できない */
    ERR12 = 12,     /**< 指定したセマフォを開放できない */
    ERR13 = 13,     /**< [予約] */
    ERR14 = 14,     /**< [予約] */
    ERR15 = 15,     /**< [予約] */
    ERR16 = 16,     /**< [予約] */
    ERR17 = 17,     /**< [予約] */
    ERR18 = 18,     /**< [予約] */
    ERR19 = 19,     /**< [予約] */
    ERR20 = 20,     /**< 自タスクを reset しようとした */
    ERR21 = 21,     /**< [予約] */
    ERR22 = 22,     /**< [予約] */
    ERR23 = 23,     /**< [予約] */
    ERR24 = 24,     /**< [予約] */
    ERR25 = 25,     /**< [予約] */
    ERR26 = 26,     /**< [予約] */
    ERR27 = 27,     /**< 不正な割込み番号  */
    ERR28 = 28,     /**< [予約] */
    ERR29 = 29,     /**< [予約] */
    ERR30 = 30,     /**< 不正なシステムコール呼び出し */
    ERR31 = 31,     /**< INIT/DIAGからは呼び出せないシステムコールを呼び出した */
    ERR40 = 40,     /**< dormant状態のタスクへのメッセージ送信 */
    ERR41 = 41,     /**< メッセージの空きスロットが無かった */
    ERR42 = 42,     /**< メッセージボックスは空 */
    ERR43 = 43,     /**< メッセージ取得失敗 */
} svcresultid_t;

/*@}*/

/**
 * @addtogroup  システムタイマー
 * @brief       システムタイマーに関する機能を集約
 * @{
 */

/**
 * @brief 待ち時間などで使う時間型(1ms単位)
 */
typedef uint16_t msec_t;

/*@}*/

/**
 * @addtogroup  外部割り込み
 * @brief       外部割り込みに関する機能を集約
 * @{
 */

/**
 * @brief 割り込みID型
 */
typedef enum extintid_t extintid_t;

/*@}*/

/**
 * @addtogroup  セマフォ
 * @brief       セマフォに関する機能を集約
 * @{
 */

/**
 * @brief セマフォID型
 */
typedef uint8_t semaphoreid_t;

/*@}*/

/**
 * @addtogroup  タスク
 * @brief       タスクに関する機能を集約
 * @{
 */

/**
 * @brief タスク関数型
 */
typedef void(*taskproc_t)(void* param);

/**
 * @brief タスクID型
 */
typedef uint8_t taskid_t;

/**
 * @brief アドレスを読み書きするための型
 *
 * 関数からの戻り先アドレスは上位バイトと下位バイトが入れ替わった状態でメモリに格納されるため
 * 読み書きを行う際には注意が必要
 */
typedef union address_t {
    taskproc_t  address;    /**< アドレス値へのワードアクセス用フィールド */
    uint8_t     byte[2];    /**< アドレス値へのバイトアクセス用フィールド */
} address_t;

/**
 * @brief コンテキスト型
 */
typedef struct context_t {
    uint8_t     R31, R30, R29, R28, R27, R26, R25, R24, R23, R22, R21, R20,
                R19, R18, R17, R16, R15, R14, R13, R12, R11, R10, R9, R8,
                R7, R6, R5, R4, R3, R2, R1; /**< 汎用レジスタ */
    uint8_t     RSTATUS;                    /**< ステータスレジスタ */
    uint8_t     R0;                         /**< 汎用レジスタR0 */
    address_t   address;                    /**< タスクの実行アドレス 。schedule()関数でタスクが再開される際のアドレスが格納される。*/
} context_t;

/**
 * @brief タスクエントリ型
 */
typedef struct taskentry_t {
    uint8_t*     stack_pointer;  /**< 初期スタックポインタ */
    address_t    start_proc;     /**< タスクの開始アドレス */
} taskentry_t;

/**
 * @brief タスク状態を示す定数型
 */
typedef enum state_t {
    NON_EXISTS,     /**< 未初期化状態  */
    DORMANT,        /**< 休止状態：未起動、実行可能 */
    READY,          /**< 実行状態：起動済み、実行可能 */
    PAUSE,          /**< 待ち状態：起動済み、実行不可能 */
    WAIT_MSG,       /**< メッセージ待ち状態：起動済み、実行不可能 */
    WAIT_SEMA,      /**< セマフォ待ち状態：起動済み、実行不可能 */
    WAIT_RESTART,   /**< リスタート待ち状態：起動済み、実行不可能 */
} state_t;

/**
 * @brief タスクコントロールブロック型
 */
typedef struct tcb_t {
    uint8_t*            stack_pointer;  /**< スタックポインタ */
    state_t             status;         /**< タスクの状態 */
    struct tcb_t*       next_task;      /**< 各行列に並べたときの次のタスク */
    msec_t              pause_msec;     /**< pause 時の待ち時間 */
    struct message_t*   message;        /**< 受信したメッセージのキュー */
    void*               argument;       /**< 起動時に与えられた引数 */
} tcb_t;

/*@}*/

/**
 * @addtogroup  メッセージ
 * @brief       メッセージに関する機能を集約
 * @{
 */

/**
 * @brief       タスク間通信で用いるメッセージ構造体
 */
typedef struct message_t {
    struct message_t *  next_message;   /**< 次のメッセージへのリンク */
    taskid_t            from_taskid;    /**< メッセージ送信元のタスクID */
    void*               data;           /**< メッセージ本文 */
} message_t;

/*@}*/

/**
 * @addtogroup  Readyタスクキュー
 * @brief       Readyタスクキューに関する機能を集約
 * @{
 */

/**
 * @brief   ready状態のタスクを並べるキュー型
 */
typedef struct readyqueue_t {
    tcb_t*  next_task;      /**< リンクリスト用 */
} readyqueue_t;

/*@}*/

/**
 * @addtogroup  セマフォ
 * @brief       セマフォに関する機能を集約
 * @{
 */

/**
 * @brief       カーネルが管理するセマフォキュー構造体
 * @note        INITタスクとDIAGタスクではセマフォ使用を禁止する
 */
typedef struct semaphoqueue_t {
    /**
     * @brief   セマフォ待ちタスクのリンクリスト
     * @note    このリストにはタスクが優先度順で並んでいる
     *          NULLの場合は待ちタスクなし
     */
    tcb_t*  next_task;

    /**
     * @brief   今セマフォを獲得しているタスクのポインタ
     * @note    NULLの場合は獲得しているタスクなし
     */
    tcb_t*  take_task;
} semaphoqueue_t;

/*@}*/

/**
 * @addtogroup  一時停止タスクキュー
 * @brief       一時停止タスクキューに関する機能を集約
 * @{
 */

/**
 * @brief   待ち時間が有限のPause状態のタスクを並べるキュー
 */
typedef struct pausequeue_t {
    tcb_t*  next_task;  /**< リンクリスト用 */
} pausequeue_t;

/*@}*/

/**
 * @addtogroup  タスク
 * @{
 */

/**
 * @brief コンテキストを現在のスタックに待避
 * 
 * 以下の内容をすべてスタックに保存する。
 * - 全ての汎用レジスタ(R0からR31)
 * - ステータスレジスタ(__SREG__)
 * - スタックポインタ(__SP_H__、__SP_L__)
 *
 * 注意事項
 * - ステータスレジスタを保存してから割り込み禁止(cli)を行わないとの全体割り込み許可フラグ(I)を正常に復帰できない。
 * - ステータスレジスタやスタックポインタは直接スタックに保存できないため、汎用レジスタ(R0)経由で保存する。
 * - 復帰時にはスタックポインタを一番最初に復帰させる必要があるので、スタックポインタが一番最後に保存されるように保存順序に注意が必要。
 */
#define SAVE_CONTEXT()                      \
asm volatile (                              \
    ";                              \n\t"   \
    "push    r0                     \n\t"   \
    "in      r0   ,__SREG__         \n\t"   \
    "cli                            \n\t"   \
    "push    r0                     \n\t"   \
    "push    r1                     \n\t"   \
    "clr     r1                     \n\t"   \
    "push    r2                     \n\t"   \
    "push    r3                     \n\t"   \
    "push    r4                     \n\t"   \
    "push    r5                     \n\t"   \
    "push    r6                     \n\t"   \
    "push    r7                     \n\t"   \
    "push    r8                     \n\t"   \
    "push    r9                     \n\t"   \
    "push    r10                    \n\t"   \
    "push    r11                    \n\t"   \
    "push    r12                    \n\t"   \
    "push    r13                    \n\t"   \
    "push    r14                    \n\t"   \
    "push    r15                    \n\t"   \
    "push    r16                    \n\t"   \
    "push    r17                    \n\t"   \
    "push    r18                    \n\t"   \
    "push    r19                    \n\t"   \
    "push    r20                    \n\t"   \
    "push    r21                    \n\t"   \
    "push    r22                    \n\t"   \
    "push    r23                    \n\t"   \
    "push    r24                    \n\t"   \
    "push    r25                    \n\t"   \
    "push    r26                    \n\t"   \
    "push    r27                    \n\t"   \
    "push    r28                    \n\t"   \
    "push    r29                    \n\t"   \
    "push    r30                    \n\t"   \
    "push    r31                    \n\t"   \
    "lds     r26  , current_tcb     \n\t"   \
    "lds     r27  , current_tcb + 1 \n\t"   \
    "in      r0   , __SP_L__        \n\t"   \
    "st      x+   , r0              \n\t"   \
    "in      r0   , __SP_H__        \n\t"   \
    "st      x+   , r0              \n\t"   \
);

/**
 * @brief コンテキストの復帰
 * 以下の内容をすべてスタックから復帰する。
 * - 全ての汎用レジスタ(R0からR31)
 * - ステータスレジスタ(__SREG__)
 * - スタックポインタ(__SP_H__、__SP_L__)
 *
 * 注意事項
 * - ステータスレジスタ(__SREG__)の復帰で全体割り込み許可フラグ(I)も復帰されるため、SEI命令(割り込み禁止解除)は不要。
 * - スタックポインタのみは汎用レジスタ経由で一番最初に復帰させ、残りのレジスタはpop命令で復帰させる。
 */
#define RESTORE_CONTEXT()                           \
asm volatile (                                      \
    "lds     r26        , current_tcb       \n\t"   \
    "lds     r27        , current_tcb + 1   \n\t"   \
    "ld      r28        , x+                \n\t"   \
    "out     __SP_L__   , r28               \n\t"   \
    "ld      r29        , x+                \n\t"   \
    "out     __SP_H__   , r29               \n\t"   \
    "pop     r31                            \n\t"   \
    "pop     r30                            \n\t"   \
    "pop     r29                            \n\t"   \
    "pop     r28                            \n\t"   \
    "pop     r27                            \n\t"   \
    "pop     r26                            \n\t"   \
    "pop     r25                            \n\t"   \
    "pop     r24                            \n\t"   \
    "pop     r23                            \n\t"   \
    "pop     r22                            \n\t"   \
    "pop     r21                            \n\t"   \
    "pop     r20                            \n\t"   \
    "pop     r19                            \n\t"   \
    "pop     r18                            \n\t"   \
    "pop     r17                            \n\t"   \
    "pop     r16                            \n\t"   \
    "pop     r15                            \n\t"   \
    "pop     r14                            \n\t"   \
    "pop     r13                            \n\t"   \
    "pop     r12                            \n\t"   \
    "pop     r11                            \n\t"   \
    "pop     r10                            \n\t"   \
    "pop     r9                             \n\t"   \
    "pop     r8                             \n\t"   \
    "pop     r7                             \n\t"   \
    "pop     r6                             \n\t"   \
    "pop     r5                             \n\t"   \
    "pop     r4                             \n\t"   \
    "pop     r3                             \n\t"   \
    "pop     r2                             \n\t"   \
    "pop     r1                             \n\t"   \
    "pop     r0                             \n\t"   \
    "out     __SREG__,    r0                \n\t"   \
    "pop     r0                             \n\t"   \
);

/**
 * @brief カーネル用スタックへの切り替え
 *
 * 注意事項
 * - 切り替え後のカーネルスタックは常に空の状態。
 * - 切り替え時に汎用レジスタR28,R29は破壊される。
 * - 切り替え前のスタックポインタは保存されない。
 */
#define SET_KERNEL_STACKPOINTER()                       \
asm volatile (                                          \
    "ldi    r28     , lo8(kernel_stack+%0)      \n\t"   \
    "ldi    r29     , hi8(kernel_stack+%0)      \n\t"   \
    "out    __SP_H__, r29                       \n\t"   \
    "out    __SP_L__, r28                       \n\t"   \
    : : "M"((uint8_t)(KERNEL_STACK_SIZE-1))             \
);

/*@}*/

/**
 * @brief   強制停止
 */
__attribute__((noreturn)) extern void halt(void);

/**
 * @brief    カーネル起動
 */
__attribute__((naked, noreturn)) extern void start_kernel(void);
