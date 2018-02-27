/*
 *   Redistribution and use in source and binary forms, with or without
 *   modification, are permitted provided that the following conditions are met:
 *
 *   * Redistributions of source code must retain the above copyright
 *     notice, this list of conditions and the following disclaimer.
 *
 *   * Redistributions in binary form must reproduce the above copyright
 *     notice, this list of conditions and the following disclaimer in
 *     the documentation and/or other materials provided with the
 *     distribution.
 *
 *   * Neither the name of the copyright holders nor the names of
 *     contributors may be used to endorse or promote products derived
 *     from this software without specific prior written permission.
 *
 *   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 *   AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 *   IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 *   ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 *   LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 *   CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 *   SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 *   INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 *   CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 *   ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 *   POSSIBILITY OF SUCH DAMAGE.
 */

/**
 * @file
 * @brief	Win32環境で割り込みや周辺機器を再現するための仮想ハードウェア
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */

#ifndef __virtualhw_h__
#define __virtualhw_h__

/**
 * @brief	カーネルが呼び出す割り込みルーチン
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */
typedef void (*interruptserviceroutine_t)(void);

/**
 * @brief	仮想デバイスのメイン処理
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */
typedef void (*virtualdevice_serviceroutine_t)(void);

/**
 * @typedef	struct virtualdevice_t virtualdevice_t;
 * @struct	virtualdevice_t
 * @brief	仮想デバイス登録用構造体
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */
typedef struct virtualdevice_t {
	char *name;								/**< デバイス名 */
	virtualdevice_serviceroutine_t vdsr;	/**< デバイスのメイン処理 */
} virtualdevice_t;

/**
 * @brief	仮想ハードウェアの初期化
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */
extern void initVirtualHardware(void);

/**
 * @brief			仮想デバイスのインストール
 * @param[in] vd	インストールする仮想デバイス
 * @author			Kazuya Fukuhara
 * @date			2010/09/01 21:36:49	作成
 */
extern void VHW_installVirtualDevice(virtualdevice_t* vd);

/**
 * @brief	仮想ハードウェアの動作を開始する
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 * 
 * インストールした仮想デバイスの動作を開始する
 */
extern void startVirtualHardware(void);

/**
 * @typedef		enum vhw_interruptid_t
 * @brief		仮想ハードウェアの割り込み番号
 * @author		Kazuya Fukuhara
 * @date		2010/09/01 21:36:49	作成
 */
typedef enum vhw_interruptid_t {
	INTID_TIMER,	/**< カーネルタイマー割り込み */
	INTID_BUTTON_DOWN,	/**< ボタン割り込み */
	INTID_BUTTON_UP,	/**< ボタン割り込み */
	INTID_BUTTON_DRAG,	/**< ボタン割り込み */
	INTID_MAX		/**< 割り込み番号の最大値 */
} vhw_interruptid_t;

/**
 * @brief			カーネル側の割り込みサービスルーチンを仮想ハードウェアの割り込みベクタに設定
 * @param[in] id	仮想ハードウェアの割り込み番号
 * @param[in] isr	仮想ハードウェアの割り込み番号に対応させるカーネル側の割り込みサービスルーチン
 * @author			Kazuya Fukuhara
 * @date			2010/09/01 21:36:49	作成
 */
extern void VHW_setInterruputServiceRoutine(vhw_interruptid_t id, interruptserviceroutine_t isr);

/**
 * @brief			仮想ハードウェアの割り込みベクタからカーネル側の割り込みサービスルーチンを取得
 * @param[in] id	割り込み番号
 * @return			割り込み番号に対応する割り込みルーチン NULLの場合は未設定
 * @author			Kazuya Fukuhara
 * @date			2010/09/01 21:36:49	作成
 */
extern interruptserviceroutine_t VHW_getInterruputServiceRoutine(vhw_interruptid_t id);

/**
 * @brief			仮想ハードウェアに割り込の発生を伝達する（前処理＋後処理）
 * @param[in] id	発生させる割り込み番号
 * @author			Kazuya Fukuhara
 * @date			2010/09/01 21:36:49	作成
 */
extern void VHW_onInterrupt(vhw_interruptid_t id);

/**
 * @brief	割り込みを禁止する
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */
extern void VHW_disableInterrupt(void);

/**
 * @brief	割り込み禁止解除
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */
extern void VHW_enableInterrupt(void);

#endif
