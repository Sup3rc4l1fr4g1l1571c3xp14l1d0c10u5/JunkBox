#pragma once
#include "kernel.h"

/**
 * @addtogroup  外部割り込み
 * @{
 */


/**
 * @brief   外部割り込み番号を示す列挙定数
 *
 * AVRのヘッダが提供する割り込み名の先頭に EXTINT_ を付与したもの
 */
enum extintid_t {
    EXTINT_RESET = 0,      /**< 電源ON, WDT, BOD等の各種リセット */
    EXTINT_INT0,           /**< 外部割り込み要求0 */
    EXTINT_INT1,           /**< 外部割り込み要求1 */
    EXTINT_PCINT0,         /**< ピン変化0群割り込み要求 */
    EXTINT_PCINT1,         /**< ピン変化1群割り込み要求 */
    EXTINT_PCINT2,         /**< ピン変化2群割り込み要求 */
    EXTINT_WDT,            /**< ウォッチドッグ計時完了 */
    EXTINT_TIMER2_COMPA,   /**< タイマ/カウンタ2比較A一致 */
    EXTINT_TIMER2_COMPB,   /**< タイマ/カウンタ2比較B一致 */
    EXTINT_TIMER2_OVF,     /**< タイマ/カウンタ2溢れ */
    EXTINT_TIMER1_CAPT,    /**< タイマ/カウンタ1捕獲発生 */
    EXTINT_TIMER1_COMPA,   /**< タイマ/カウンタ1比較A一致 */
    EXTINT_TIMER1_COMPB,   /**< タイマ/カウンタ1比較B一致 */
    EXTINT_TIMER1_OVF,     /**< タイマ/カウンタ1溢れ */
    EXTINT_TIMER0_COMPA,   /**< タイマ/カウンタ0比較A一致 */
    EXTINT_TIMER0_COMPB,   /**< タイマ/カウンタ0比較B一致 */
    EXTINT_TIMER0_OVF,     /**< タイマ/カウンタ0溢れ */
    EXTINT_SPI_STC,        /**< SPI転送完了 */
    EXTINT_USART_RX,       /**< USART 受信完了 */
    EXTINT_USART_UDRE,     /**< USART 送信緩衝部空き */
    EXTINT_USART_TX,       /**< USART 送信完了 */
    EXTINT_ADC,            /**< A/D変換完了 */
    EXTINT_EE_READY,       /**< EEPROM 操作可 */
    EXTINT_ANALOG_COMP,    /**< アナログ比較器出力遷移 */
    EXTINT_TWI,            /**< 2線直列インターフェース状態変化 */
    EXTINT_SPM_READY,      /**< SPM命令操作可 */
    EXTINT_MAX             /**< 割り込み番号個数 */
};

/**
 * @brief                   フック可能な外部割り込み番号か判定
 * @param   [in] extintid   外部割り込み番号
 * @retval  true            フック可能な外部割り込み番号
 * @retval  false           フック不可能な外部割り込み番号
 */
extern bool is_hookable_extint(extintid_t extintid);

/**
 * @brief                   外部割り込みフック表に情報を設定する
 * @param   [in] extintid   フックする外部割り込み番号。フック不能な外部割り込み番号が指定されている場合は何もしない。
 * @param   [in] taskid     外部割り込みで起動するタスクの番号。ユーザータスク以外が指定されている場合はタスクが起動しない。
 * @param   [in] argument   タスク起動時の引数
 */
extern void hook_extint(extintid_t extintid, taskid_t taskid, void *argument);

/*@}*/
