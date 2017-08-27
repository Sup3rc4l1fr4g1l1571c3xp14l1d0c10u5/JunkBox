#include "kerneltimer.h"
#include "readyqueue.h"
#include "pausequeue.h"
#include "kerneldata.h"

/**
 * @addtogroup  システムタイマー
 *  - システムタイマーは 1ms 間隔でコンテキストスイッチを行う。
 *  - AVRのタイマー仕様
 *    - タイマー分周比: 1, 8, 64, 256, 1024 の5種類
 *    - 比較値レジスタ: タイマー0が8bit、タイマー1が16bit
 *  - タイマー割り込みが1ms間隔となる組み合わせは以下の通り
 *    - 20Mhzの場合
 *      - 分周比= 8、比較値=2500 (タイマー1を使用)
 *      - 20Mhz /  8 / 2500 = 2500Khz / 2500 = 1Khz
 *    - 16Mhzの場合
 *      - 分周比=64、比較値= 250 (タイマー0を使用)
 *      - 16Mhz / 64 /  250 =  250Khz /  250 = 1Khz
 *    - 12Mhzの場合
 *      - 分周比= 8、比較値=1500 (タイマー1を使用)
 *      - 12Mhz /  8 / 1500 = 1500Khz / 1500 = 1Khz
 *    -  8Mhzの場合
 *      - 分周比=64、比較値= 125 (タイマー0を使用)
 *      -  8Mhz / 64 /  125 =  125Khz /  125 = 1Khz
 * 
 */

/*@{*/

/**
 * @def     TIMER_DIVISION_RATIO
 * @brief   システムタイマーの分周比
 * 
 * タイマー分周比は1, 8, 64, 256, 1024 の5種類
 */

/**
 * @def     TIMER_COMPARATION_VALUE
 * @brief   システムタイマーの比較一致値
 * 
 * タイマーの間隔が T ミリ秒 で 比較一致値が C の場合、割り込みは T*(C+1) ミリ秒間隔で発生する。
 */

/**
 * @def     TCCRnA
 * @brief   タイマ/カウンタ制御レジスタA
 *
 * - 0bXXYY00ZZ
 *  - XX = COMnA1:COMnA0
 *  - YY = COMnB1:COMnB0
 *  - ZZ =  WGMn1: WGMn0
 */

/**
 * @def     TCCRnB
 * @brief   タイマ/カウンタ制御レジスタB
 *
 * - 0bXX00YZZZ
 *   - XX  = FOCnA:FOCnB
 *   - Y   = WGMn2
 *   - ZZZ = CSn2:CSn1:CSn0
 */

/**
 * @def     OCRnA
 * @brief   タイマ/カウンタ比較レジスタ
 * 
 * - タイマ/カウンタ0で利用するOCR0Aは8bitレジスタ
 * - タイマ/カウンタ1で利用するOCR1Aは16bitレジスタ
 */

/**
 * @def     TIMSKn
 * @brief   タイマカウンタ割り込みマスクレジスタ
 * 
 * - 0b00000XYZ
 *   - Z = TOIEn
 *   - Y = OCIEnA
 *   - X = OCIEnB
 */

/**
 * @def     TIMERn_COMPA_vect
 * @brief   タイマー割り込みハンドラ用ベクタ
 */

#if     defined(F_CPU)
#if     (F_CPU == 20000000UL)
#define     TIMER_DIVISION_RATIO    8
#define     TIMER_COMPARATION_VALUE     (2500)
#define     TCCRnA  TCCR1A
#define     TCCRnB  TCCR1B
#define     OCRnA   OCR1A
#define     TIMSKn  TIMSK1
#define     TIMERn_COMPA_vect   TIMER1_COMPA_vect
#elif   (F_CPU == 16000000UL)
#define     TIMER_DIVISION_RATIO    64
#define     TIMER_COMPARATION_VALUE     (250)
#define     TCCRnA  TCCR0A
#define     TCCRnB  TCCR0B
#define     OCRnA   OCR0A
#define     TIMSKn  TIMSK0
#define     TIMERn_COMPA_vect   TIMER0_COMPA_vect
#elif   (F_CPU == 12000000UL)
#define     TIMER_DIVISION_RATIO    8
#define     TIMER_COMPARATION_VALUE     (1500)
#define     TCCRnA  TCCR1A
#define     TCCRnB  TCCR1B
#define     OCRnA   OCR1A
#define     TIMSKn  TIMSK1
#define     TIMERn_COMPA_vect   TIMER1_COMPA_vect
#elif   (F_CPU ==  8000000UL)
#define     TIMER_DIVISION_RATIO    64
#define     TIMER_COMPARATION_VALUE     (125)
#define     TCCRnA  TCCR0A
#define     TCCRnB  TCCR0B
#define     OCRnA   OCR0A
#define     TIMSKn  TIMSK0
#define     TIMERn_COMPA_vect   TIMER0_COMPA_vect
#else
#error "Macro F_CPU's value is invalid. Please set one of 20000000UL, 16000000UL, 12000000UL, 8000000UL as the value of F_CPU."
#endif
#else
#error "Macro F_CPU is not defined."
#endif

/* 以下はシミュレータデバッガで実行する際に、タイマ割り込みを最速で発生させる仕掛け */
#if defined(USE_SIMULATOR)
#undef TIMER_DIVISION_RATIO
#define TIMER_DIVISION_RATIO        1
#undef TIMER_COMPARATION_VALUE
#define TIMER_COMPARATION_VALUE     1
#endif

/**
 * @brief   分周比に対応したCS02:CS01:CS00に設定するビットパターン
 */
#define CS_BIT \
    (TIMER_DIVISION_RATIO ==    1) ? 0b001 :\
    (TIMER_DIVISION_RATIO ==    8) ? 0b010 :\
    (TIMER_DIVISION_RATIO ==   64) ? 0b011 :\
    (TIMER_DIVISION_RATIO ==  256) ? 0b100 :\
    (TIMER_DIVISION_RATIO == 1024) ? 0b101 :\
                                     0b000 ;

/**
 * @brief   システムタイマー初期化
 */
void init_kerneltimer(void) {
    /**
     * |タイマー設定              |設定内容       |設定ビット         |設定値 |
     * |--------------------------|---------------|-------------------|-------|
     * |タイマー/カウンター動作   |CTC動作        |WGMn2:WGMn1:WGMn0  | 0b010 |
     * |非PWM動作比較A出力選択    |標準ポート動作 |COMnA1:COMnA0      | 0b00  |
     * |非PWM動作比較B出力選択    |標準ポート動作 |COMnB1:COMnB0      | 0b00  |
     * |OCnA強制変更              |無効           |FOCnA              | 0b0   |
     * |OCnB強制変更              |無効           |FOCnB              | 0b0   |
     *
     * |タイマー割り込み間隔(16Mhzの場合)   |設定内容   |設定ビット     |設定値 |
     * |------------------------------------|-----------|---------------|-------|
     * |分周比                              |64分周     |CSn2:CSn1:CSn0 | 0b011 |
     * |タイマカウンタ比較値A               |250        |OCRnA          | 250   |
     *
     * |タイマー割り込みタイミング  |設定内容   |設定ビット |設定値 |
     * |----------------------------|-----------|-----------|-------|
     * |オーバーフロー割り込み      |禁止       |TOIEn  = 0 |0b0    |
     * |比較A割り込み               |許可       |OCIEnA = 1 |0b1    |
     * |比較B割り込み               |禁止       |OCIEnB = 0 |0b0    |
     */

    TCCRnA  = 0b00000010;
    TCCRnB  = 0b00000000 | CS_BIT;
    OCRnA   = TIMER_COMPARATION_VALUE - 1;
    TIMSKn  = 0b00000010;
}

/**
 * @brief   タイマー割り込みハンドラ
 * 
 * AVRで一般的なISR(...)を用いて作成する割り込みハンドラでは、
 * レジスタをスタック上に待避・復帰するコードが自動生成される。
 * ```
 * ISR(TIMER0_COMPA_vect) { 
 *   (レジスタの待避コードが挿入される)
 *   本体
 *   (レジスタの復帰コードが挿入される)
 *  }
 * ```
 * 一般的なアプリでは問題ない挙動だが、OSのコンテキスト切り替えではこのレジスタ待避・復帰が邪魔となる。
 * そのため、ISR(...)を使わずnaked属性を付与してコードが生成されないようにしている。
 */
__attribute__((signal, naked))  void TIMERn_COMPA_vect(void);
void TIMERn_COMPA_vect(void) {
    /* この時点ではどのコンテキストを使っているか不明なのでスタックを使えない */

    /* 割り込みの直前まで実行されていたコンテキストを保存 */
    SAVE_CONTEXT();

    /* カーネルスタックに切り替え */
    SET_KERNEL_STACKPOINTER();

    /* 以降はカーネルスタックの利用が可能になったので、関数呼び出しなどが可能となる */

    /* スリープ状態タスクの待ち時間を更新する */
    update_pausequeue();

    /* スケジューリングを行い、次に実行するタスクを選択する */
    schedule();
}

/*@}*/
