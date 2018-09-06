#pragma once

#include "../pico/pico.types.h"

extern const struct KeyInput {
	/**
	* @brief キー入力を文字列配列sに格納
	* @param str 初期文字列入りの入力結果取得配列(初期文字列を使用しない場合は*str=0)
	* @param sz バッファサイズ
	* @retval true: Enterで終了(OK)、false:ESCで終了(キャンセル)
	* カーソル位置はsetcursor関数で指定しておく
	*/
	bool_t (*gets)(char16_t *s, size32_t sz);

	// キーボードから1キー入力待ち(ASCIIコードが得られるキーに限る)
	// 戻り値 通常文字の場合ASCIIコード
	uint8_t(*getch)(void);

	// キーボードから通常文字キー入力待ちし、入力された文字を表示
	// 戻り値 入力された文字のASCIIコード、グローバル変数vkeyに最後に押されたキーの仮想キーコード
	uint8_t (*getchar)(void);

	// 挿入モード：true、上書きモード：false
	bool_t (*GetInsertMode)(void);
	void (*SetInsertMode)(bool_t mode);

} KeyInput;
