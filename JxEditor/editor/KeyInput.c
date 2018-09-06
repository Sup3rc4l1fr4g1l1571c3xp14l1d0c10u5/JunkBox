// キー入力、カーソル表示関連機能 by K.Tanaka
// PS/2キーボード入力システム、カラーテキスト出力システム利用

#include "TextVRAM.h"
#include "Keyboard.h"
#include "KeyInput.h"

#include "../arch/arch.h"

//static unsigned short lineinputbuf[256]; //lineinput関数用一時バッファ
static bool_t insertmode; //挿入モード：1、上書きモード：0

bool_t KeyInput_GetInsertMode(void)
{
	return insertmode;
}

void KeyInput_SetInsertMode(bool_t mode)
{
	insertmode = mode;
}

// キーボードから１キー入力読み取り、アスキーキーを得る
uint8_t KeyInput_getch(void) {
	while (1) {
		WaitMS(1);
		if (Keyboard_ReadKey() && Keyboard_GetCurrentVKeyCode() != 0) {
			return Keyboard_GetCurrentAsciiCode();
		}
	}
}

// カーソル表示しながらキーボードから通常文字キーの入力待ちし、入力された文字を表示（エコーあり）
// 戻り値 入力された文字のASCIIコード
uint8_t KeyInput_getchar(void) {
	unsigned char k;
	while (1) {
		WaitMS(1);
		if (Keyboard_ReadKey() && (k = Keyboard_GetCurrentAsciiCode()) != 0) {
			break;
		}
	}
	TextVRAM.putch(k);
	return k;
}



/**
 * @brief キー入力して文字列配列sに格納
 * @param str 初期文字列入りの入力結果取得配列(初期文字列を使用しない場合は*str=0)
 * @param sz バッファサイズ
 * @retval true: Enterで終了(OK)、false:ESCで終了(キャンセル)
 * カーソル位置はsetcursor関数で指定しておく
 */
bool_t KeyInput_gets(char16_t *str, size32_t sz) {
	if (sz == 0) {
		return false;
	}
	size32_t len, cur = 0;
	for (len=0; str[len] != '\0' && len < sz; len++) { /* loop */ }

	screen_pos_t baseCursorPos;
	TextVRAM.GetCursorPosition(&baseCursorPos);

	if (len > 0) {
		TextVRAM.puts(str); //初期文字列表示
	}
	for (;;) {
		uint8_t k1 = KeyInput_getch();				// アスキーコードを取得
		uint8_t k2 = Keyboard_GetCurrentVKeyCode(); // 仮想キーコード取得
		if (k1) {
			//通常文字の場合
			if (insertmode || str[cur + 1] == L'\0') {
				//挿入モードまたは最後尾の場合
				if (len+1 > sz) continue; //入力文字数最大値の場合は無視
				for (size32_t i = len; i > cur; i--) {
					str[i + 1] = str[i]; // カーソル位置から後ろを一つづつずらす
				}
				len++;
			}
			str[cur] = k1; //入力文字を追加
			TextVRAM.puts(&str[cur]); //入力文字以降を表示
			cur++;
			{
				screen_pos_t vcp = baseCursorPos;
				TextVRAM.CalcCursorPosition(str,str+cur,&vcp, VWIDTH_X);
				TextVRAM.SetCursorPosition(&vcp);
			}
		}
		else {
			switch (k2) {
				//制御文字の場合
			case VKEY_LEFT:
			case VKEY_NUMPAD4:
				//左矢印キー
				if (cur > 0) {
					cur--;
					{
						screen_pos_t vcp = baseCursorPos;
						TextVRAM.CalcCursorPosition(str, str + cur, &vcp, VWIDTH_X);
						TextVRAM.SetCursorPosition(&vcp);
					}
				}
				break;
			case VKEY_RIGHT:
			case VKEY_NUMPAD6:
				//右矢印キー
				if (cur != len) {
					cur++;
					{
						screen_pos_t vcp = baseCursorPos;
						TextVRAM.CalcCursorPosition(str, str + cur, &vcp, VWIDTH_X);
						TextVRAM.SetCursorPosition(&vcp);
					}
				}
				break;
			case VKEY_RETURN: //Enterキー
			case VKEY_SEPARATOR: //テンキーのEnter
				//終了
				//TextVRAM.putch('\n');
				return true;
			case VKEY_HOME:
			case VKEY_NUMPAD7:
				//Homeキー、文字列先頭にカーソル移動
				cur = 0;
				TextVRAM.SetCursorPosition(&baseCursorPos);
				break;
			case VKEY_END:
			case VKEY_NUMPAD1:
				//Endキー、文字列最後尾にカーソル移動
				cur = len;
				{
					screen_pos_t vcp = baseCursorPos;
					TextVRAM.CalcCursorPosition(str, str + cur, &vcp, VWIDTH_X);
					TextVRAM.SetCursorPosition(&vcp);
				}
				break;
			case VKEY_BACK:
				//Back Spaceキー、1文字左に移動しDelete処理
				if (cur == 0) break;//カーソルが先頭の場合、無視
				cur--;
				{
					screen_pos_t vcp = baseCursorPos;
					TextVRAM.CalcCursorPosition(str, str + cur, &vcp, VWIDTH_X);
					TextVRAM.SetCursorPosition(&vcp);
				}
				// fall thought
			case VKEY_DELETE:
			case VKEY_DECIMAL:
				//Deleteキー、カーソル位置の1文字削除
				if (cur == len) break;//カーソルが最後尾の場合、無視
				for (size32_t i = cur; i != len; i++) {
					str[i] = str[i + 1];
				}
				len--;
				TextVRAM.puts(str + cur);
				TextVRAM.putch(0);//NULL文字表示
				{
					screen_pos_t vcp = baseCursorPos;
					TextVRAM.CalcCursorPosition(str, str + cur, &vcp, VWIDTH_X);
					TextVRAM.SetCursorPosition(&vcp);
				}
				break;
			case VKEY_INSERT:
			case VKEY_NUMPAD0:
				//Insertキー、挿入モードトグル動作
				insertmode = !insertmode;
				break;
			case VKEY_ESCAPE:
				//ESCキー、-1で終了
				return false;
			}
		}
	}
}

const struct KeyInput KeyInput = {
	KeyInput_gets,
	KeyInput_getch,
	KeyInput_getchar,
	KeyInput_GetInsertMode,
	KeyInput_SetInsertMode,
};
