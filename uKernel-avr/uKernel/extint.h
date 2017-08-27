#pragma once
#include "kernel.h"

/**
 * @addtogroup  外部割り込み
 */

/*@{*/

/**
 * @brief   割り込み番号
 *
 * AVRのヘッダが提供する割り込み名の先頭に INT_ を付与したもの
 */
enum extintid_t {
    INT_RESET = 0,      /**< 電源ON, WDT, BOD等の各種リセット */
    INT_INT0,           /**< 外部割り込み要求0 */
    INT_INT1,           /**< 外部割り込み要求1 */
    INT_PCINT0,         /**< ピン変化0群割り込み要求 */
    INT_PCINT1,         /**< ピン変化1群割り込み要求 */
    INT_PCINT2,         /**< ピン変化2群割り込み要求 */
    INT_WDT,            /**< ウォッチドッグ計時完了 */
    INT_TIMER2_COMPA,   /**< タイマ/カウンタ2比較A一致 */
    INT_TIMER2_COMPB,   /**< タイマ/カウンタ2比較B一致 */
    INT_TIMER2_OVF,     /**< タイマ/カウンタ2溢れ */
    INT_TIMER1_CAPT,    /**< タイマ/カウンタ1捕獲発生 */
    INT_TIMER1_COMPA,   /**< タイマ/カウンタ1比較A一致 */
    INT_TIMER1_COMPB,   /**< タイマ/カウンタ1比較B一致 */
    INT_TIMER1_OVF,     /**< タイマ/カウンタ1溢れ */
    INT_TIMER0_COMPA,   /**< タイマ/カウンタ0比較A一致 */
    INT_TIMER0_COMPB,   /**< タイマ/カウンタ0比較B一致 */
    INT_TIMER0_OVF,     /**< タイマ/カウンタ0溢れ */
    INT_SPI_STC,        /**< SPI転送完了 */
    INT_USART_RX,       /**< USART 受信完了 */
    INT_USART_UDRE,     /**< USART 送信緩衝部空き */
    INT_USART_TX,       /**< USART 送信完了 */
    INT_ADC,            /**< A/D変換完了 */
    INT_EE_READY,       /**< EEPROM 操作可 */
    INT_ANALOG_COMP,    /**< アナログ比較器出力遷移 */
    INT_TWI,            /**< 2線直列インターフェース状態変化 */
    INT_SPM_READY,      /**< SPM命令操作可 */
    INT_MAX             /**< 割り込み番号個数 */
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
 * @param   [in] extintid   フックする外部割り込み番号
 * @param   [in] taskid     外部割り込みで起動するタスクの番号
 * @param   [in] argument   タスク起動時の引数
 */
extern void hook_extint(extintid_t extintid, taskid_t taskid, void *argument);

/*@}*/
