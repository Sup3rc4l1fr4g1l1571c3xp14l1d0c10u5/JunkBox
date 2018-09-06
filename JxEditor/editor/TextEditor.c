
#include "../pico/pico.types.h"
#include "../pico/pico.assert.h"
#include "../pico/pico.memory.h"
#include "../pico/pico.file.h"
#include "../pico/pico.dictionary.h"

#include "TextVRAM.h"
#include "Keyboard.h"
#include "keyinput.h"
#include "TextBuffer.h"
#include "Font.h"

#define EXPERIMENTAL

#ifdef EXPERIMENTAL
#include "../ime/roma2kana.h"	// experimental
#include "../ime/ime.h"	// experimental
#include "../ime/TinyPobox.h"	// experimental
#endif

#include "Config.h"
#include "../arch/arch.h"

#define COLOR_NORMALTEXT 7 //通常テキスト色
#define COLOR_NORMALTEXT_BG 0	 //通常テキスト背景
#define COLOR_ERRORTEXT 4 //エラーメッセージテキスト色
#define COLOR_AREASELECTTEXT 4 //範囲選択テキスト色
#define COLOR_BOTTOMLINE 5 //画面最下行のテキスト色
#define COLOR_CURSOR 6 //カーソル色
#define COLOR_BOTTOMLINE_BG 8 //画面最下行背景
#define FILEBUFSIZE 256 //ファイルアクセス用バッファサイズ
#define FILE_NAME_BUF_LEN (8 + 1 + 3 + 1) // ファイル名バッファサイズ
#define MAXFILENUM 50 //利用可能ファイル最大数

#define ERR_FILETOOBIG -1
#define ERR_CANTFILEOPEN -2
#define ERR_CANTWRITEFILE -3

/**
* 現在のカーソル位置に対応するテキスト位置
* [0] は現在位置を示す値
* [1] はsave_cursorで退避された値
*/
static text_buffer_cursor_t Cursor[2] = { INVALID_TEXTBUFFER_CURSOR , INVALID_TEXTBUFFER_CURSOR };

/**
* 現在表示中の画面の左上に対応するテキスト位置
* [0] は現在位置を示す値
* [1] はsave_cursorで退避された値
*/
static text_buffer_cursor_t DisplayLeftTop[2] = { INVALID_TEXTBUFFER_CURSOR , INVALID_TEXTBUFFER_CURSOR };

/**
* 画面上でのカーソルの位置
* [0] は現在位置を示す値
* [1] はsave_cursorで退避された値
*/
static screen_pos_t CursorScreenPos[2];

static int cx2; //上下移動時の仮カーソルX座標

// 範囲選択時のカーソルのスタート位置
static text_buffer_cursor_t SelectStart = INVALID_TEXTBUFFER_CURSOR;
// 範囲選択時のカーソルのスタート座標（画面上）
static screen_pos_t SelectStartCursorScreenPos;

static bool_t edited; //保存後に変更されたかを表すフラグ

static char16_t filebuf[FILEBUFSIZE]; //ファイルアクセス用バッファ
static char16_t CurrentFileName[FILE_NAME_BUF_LEN]; //編集中のファイル名
static char16_t filenames[MAXFILENUM][FILE_NAME_BUF_LEN];

static screen_pos_t calc_screen_pos_from_line_head(text_buffer_cursor_t head, text_buffer_cursor_t cur);

/**
 * 画面上のカーソル位置と再算出した位置情報が一致しているか検査（デバッグ用）
 */
void ValidateDisplayCursorPos(void) {
	screen_pos_t p = calc_screen_pos_from_line_head(DisplayLeftTop[0], Cursor[0]);
	Diagnotics.Assert((p.X == CursorScreenPos[0].X) && (p.Y == CursorScreenPos[0].Y));
}

// 画面上の位置と折り返しを考慮に入れながらカーソルと画面上の位置を連動させてひとつ進める
static bool_t iterate_cursor_with_screen_pos(screen_pos_t *p, text_buffer_cursor_t cur)
{
	// １文字先（移動先）を読む
	char16_t c1 = TextBuffer_TakeCharacatorOnCursor(cur);
	if (c1 == 0x0000) {
		// 1文字先が文末なので打ち切り
		return false;
	}
	int w1 = UniWidth1(c1);

	// ２文字先を読む（後述の折り返し判定のため）
	TextBuffer_CursorForward(cur);
	char16_t c2 = TextBuffer_TakeCharacatorOnCursor(cur);
	int w2 = UniWidth1(c2);

	// タブ・折り返しを考慮して文字の位置を決定

	if (c1 == L'\n') {
		// １文字目が改行文字の場合は次行に移動
		p->X = 0;
		p->Y++;
	} else if (c1 == L'\t') {
		// タブ文字の場合
		int tabSize = 4 - (p->X % 4);
		// タブの位置に応じて処理変更
		if (p->X + tabSize >= EDITOR_SCREEN_WIDTH) {
			// タブで行末まで埋まる
			p->X = 0;
			p->Y++;
		}
		else {
			// 行末までは埋まらないのでタブを入れる
			p->X += tabSize;
			// 次の文字を改めてチェックして折り返すかどうか考える
			if (p->X + w2 > EDITOR_SCREEN_WIDTH) {
				p->X = 0;
				p->Y++;
			}
		}
	} else if (p->X + w1 + w2 > EDITOR_SCREEN_WIDTH) {
		// 一文字目、二文字目両方を受け入れると行末を超える場合
		p->X = 0;
		p->Y++;
	} else {
		p->X += w1;
	}
	return true;
}

// headのスクリーン座標を(0,0)とした場合のcurのスクリーン座標位置を算出
static screen_pos_t calc_screen_pos_from_line_head(text_buffer_cursor_t head, text_buffer_cursor_t cur) {
	TextBufferCursor.CheckValid(head);
	TextBufferCursor.CheckValid(cur);

	screen_pos_t p = { 0,0 };
	text_buffer_cursor_t h = TextBufferCursor.Duplicate(head);
	while  (TextBuffer_EqualCursor(h, cur) == false) {
		if (iterate_cursor_with_screen_pos(&p, h) == false) {
			break;
		}
	}
	TextBufferCursor.Dispose(h);
	return p;
}

// 画面原点を基準に現在の行の画面上での最左位置に対応する位置にカーソルを移動
static bool_t move_cursor_to_display_line_head(screen_pos_t *pos, text_buffer_cursor_t cur) {
	TextBufferCursor.CheckValid(cur);
	screen_pos_t p = { 0,0 };
	text_buffer_cursor_t c = TextBufferCursor.Duplicate(DisplayLeftTop[0]);
	text_buffer_cursor_t cprev = TextBufferCursor.Allocate();
	// タブや折り返しなどがあるため、単純に逆順に辿ることができない場合もあって場合分けすると面倒くさい
	//画面原点に対応するバッファカーソルは常に計算済みなのでそこからの距離を求める
	bool_t result;
	for (;;) {
		screen_pos_t pprev = p;
		TextBufferCursor.Copy(cprev, c);
		if (p.Y == pos->Y && p.X == 0)
		{
			*pos = p;
			TextBufferCursor.Copy(cur, c);
			result = true;
			break;
		}
		else if (iterate_cursor_with_screen_pos(&p, c) == false)
		{
			*pos = pprev;
			TextBufferCursor.Copy(cur, cprev);
			result = false;
			break;
		}
		else if (p.Y == pos->Y)
		{
			Diagnotics.Assert(p.X == 0);
			*pos = p;
			TextBufferCursor.Copy(cur, c);
			result = true;
			break;
		}
	}
	TextBufferCursor.Dispose(c);
	TextBufferCursor.Dispose(cprev);
	return result;
}

// 画面上で現在行の末尾まで移動
static bool_t move_cursor_to_display_line_tail(screen_pos_t *pos, text_buffer_cursor_t cur) {
	TextBufferCursor.CheckValid(cur);

	screen_pos_t p = *pos;
	text_buffer_cursor_t c = TextBufferCursor.Duplicate(cur);
	text_buffer_cursor_t cprev = TextBufferCursor.Allocate();
	bool_t result;
	for (;;) {
		screen_pos_t pprev = p;
		TextBufferCursor.Copy(cprev, c);
		if (iterate_cursor_with_screen_pos(&p, c) == false)
		{
			*pos = p;
			TextBufferCursor.Copy(cur, c);
			result = false;
			break;
		}
		else if (p.Y == pos->Y + 1)
		{
			*pos = pprev;
			TextBufferCursor.Copy(cur, cprev);
			result = true;
			break;
		}
	}
	TextBufferCursor.Dispose(c);
	TextBufferCursor.Dispose(cprev);
	return result;
}

// 画面上で現在の文字の手前まで移動
static bool_t move_cursor_to_prev(screen_pos_t *pos, text_buffer_cursor_t cur) {
	TextBufferCursor.CheckValid(cur);
	screen_pos_t p = { 0,0 };
	screen_pos_t pprev = p;
	text_buffer_cursor_t c = TextBufferCursor.Duplicate(DisplayLeftTop[0]);
	text_buffer_cursor_t cprev = TextBufferCursor.Duplicate(DisplayLeftTop[0]);
	while (!(p.Y == pos->Y && p.X== pos->X)) {
		pprev = p;
		TextBufferCursor.Copy(cprev, c);
		Diagnotics.Assert(iterate_cursor_with_screen_pos(&p, c));
	}
	*pos = pprev;
	TextBufferCursor.Copy(cur, cprev);
	TextBufferCursor.Dispose(c);
	TextBufferCursor.Dispose(cprev);
	return true;
}

// 画面上のtarget.y の行でtarget.xを超えない位置(rp)のカーソルcをDisplayLeftTopを原点として求める
// 行末で停止した場合は false 
static bool_t move_cursor_to_display_pos(screen_pos_t target, screen_pos_t *rp, text_buffer_cursor_t c)
{
	screen_pos_t p = { 0,0 };
	screen_pos_t pp = p;
	TextBufferCursor.Copy(c, DisplayLeftTop[0]);
	text_buffer_cursor_t pc = TextBufferCursor.Duplicate(DisplayLeftTop[0]);
	bool_t ret;
	for (;;)
	{
		if (p.Y == target.Y && p.X >= target.X)
		{
			*rp = p;
			ret = true;
			break;
		}
		if (p.Y > target.Y)
		{
			*rp = pp;
			TextBufferCursor.Copy(c, pc);
			ret = true;
			break;
		}
		pp = p;
		TextBufferCursor.Copy(pc, c);
		if (iterate_cursor_with_screen_pos(&p, c) == false)
		{
			// 文書末に到達
			*rp = pp;
			TextBufferCursor.Copy(c, pc);
			ret = false;
			break;
		}
	}
	TextBufferCursor.Dispose(pc);
	return ret;
}


static bool_t OverwriteCharactor(char16_t ch) {
	int i = TextBuffer_OverwriteCharacatorOnCursor(Cursor[0], ch);//テキストバッファに１文字上書き
	if (i != 0) {
		return false;
	}
	CursorScreenPos[0] = calc_screen_pos_from_line_head(DisplayLeftTop[0], Cursor[0]);
	ValidateDisplayCursorPos();
	return true;
}

static bool_t InsertCharactor(char16_t ch) {
	int i = TextBuffer_InsertCharacatorOnCursor(Cursor[0], ch);//テキストバッファに１文字挿入
	if (i != 0) {
		return false;
	}
	CursorScreenPos[0] = calc_screen_pos_from_line_head(DisplayLeftTop[0], Cursor[0]);
	ValidateDisplayCursorPos();
	return true;
}

// 画面を１行下にスクロールする
static bool_t screen_scrolldown() {
	text_buffer_cursor_t c = TextBufferCursor.Duplicate(DisplayLeftTop[0]);
	screen_pos_t p = { 0,0 };
	for (;;)
	{
		if (p.Y == 1)
		{
			TextBufferCursor.Copy(DisplayLeftTop[0], c);
			CursorScreenPos[0].Y -= 1;
			TextBufferCursor.Dispose(c);
			return true;
		}
		if (iterate_cursor_with_screen_pos(&p, c) == false)
		{
			// 下の行がないのでどうしようもない
			TextBufferCursor.Dispose(c);
			return false;
		}

	}
}

static bool_t screen_scrollup()
{
	// カーソルの画面Ｙ座標が最上列なので左上座標の更新が必要
	text_buffer_cursor_t lt = TextBufferCursor.Duplicate(DisplayLeftTop[0]);
	if (TextBuffer_CursorBackward(lt)) {
		// 左上から１文字戻る＝画面上における前の行の末尾にバッファ上のカーソルを移動

		// 行頭位置を算出
		text_buffer_cursor_t c = TextBufferCursor.Duplicate(lt);
		TextBuffer_MoveToBeginningOfLine(c);

		// 行頭位置を(0,0)点としてカーソルltの画面上位置を計算
		screen_pos_t lp = calc_screen_pos_from_line_head(c, lt);

		// ltの画面位置を超えない位置の行頭をサーチ
		screen_pos_t p = { 0,0 };
		while (p.Y != lp.Y)
		{
			iterate_cursor_with_screen_pos(&p, c);
		}

		// 見つかったところが新しい左上位置
		TextBufferCursor.Copy(DisplayLeftTop[0], c);
		CursorScreenPos[0].Y += 1;
		ValidateDisplayCursorPos();
		TextBufferCursor.Dispose(c);
		TextBufferCursor.Dispose(lt);
		return true;
	}
	else
	{
		// DisplayLeftTopはバッファ先頭なので戻れない
		TextBufferCursor.Dispose(lt);
		return false;
	}
}

//カーソルを1つ前に移動
//出力：下記変数を移動先の値に変更
//Cursor[0] バッファ上のカーソル位置
//CursorScreenPos[0] 画面上のカーソル位置
//cx2 cxと同じ
//DisplayLeftTop[0] 画面左上のバッファ上の位置
void cursor_left_(void) {

	if (CursorScreenPos[0].X == 0 && CursorScreenPos[0].Y == 0)
	{
		if (screen_scrollup() == false)
		{
			// 上にスクロールできない＝先頭なので戻れない
			return;
		}
	}

	move_cursor_to_prev(&CursorScreenPos[0], Cursor[0]);
	cx2 = CursorScreenPos[0].X;
}

void cursor_left(void) {
	ValidateDisplayCursorPos();
	cursor_left_();
	ValidateDisplayCursorPos();
}

//カーソルを右に移動
void cursor_right(void) {
	ValidateDisplayCursorPos();
	if (iterate_cursor_with_screen_pos(&CursorScreenPos[0], Cursor[0])) {
		if (CursorScreenPos[0].Y == EDITOR_SCREEN_HEIGHT) {
			// カーソルを進めることに成功したらカーソル位置が画面最下段からはみ出たので
			// 画面を一行下にスクロールする
			screen_scrolldown();
		}
	}	
	cx2 = CursorScreenPos[0].X;
	ValidateDisplayCursorPos();
}

//カーソルを1つ上に移動
void cursor_up_(void) {

	if (CursorScreenPos[0].Y == 0) {
		if (screen_scrollup() == false)
		{
			// 画面を上にスクロールできない＝戻れないので終了
			return;
		}
	}
	ValidateDisplayCursorPos();

	// 左上の補正が終った
	// 左上位置から画面上で１行上の位置をサーチする
	screen_pos_t target = { cx2, CursorScreenPos[0].Y - 1 };
	screen_pos_t p = { 0,0 };
	text_buffer_cursor_t c = TextBufferCursor.Allocate();

	move_cursor_to_display_pos(target, &p, c);

	TextBufferCursor.Copy(Cursor[0], c);
	CursorScreenPos[0] = p;
	TextBufferCursor.Dispose(c);

	ValidateDisplayCursorPos();
}


void cursor_up(void) {
	ValidateDisplayCursorPos();
	cursor_up_();
	ValidateDisplayCursorPos();
}


//カーソルを1つ下に移動
//出力：下記変数を移動先の値に変更
//cursorbp,cursorix バッファ上のカーソル位置
//cx,cy 画面上のカーソル位置
//cx2 移動前のcxと同じ
//disptopbp,disptopix 画面左上のバッファ上の位置
void cursor_down_(void) {
	// 左上位置からサーチする
#if 1
	screen_pos_t target = { cx2, CursorScreenPos[0].Y + 1 };
	screen_pos_t p = { 0,0 };
	text_buffer_cursor_t c = TextBufferCursor.Allocate();

	if (move_cursor_to_display_pos(target, &p, c) == false)
	{
		if (p.Y != target.Y )
		{
			// 次の行に移動できていない＝最下段の行で下を押した
			TextBufferCursor.Dispose(c);
			return;
		}
	}

	TextBufferCursor.Copy(Cursor[0], c);
	CursorScreenPos[0] = p;
	TextBufferCursor.Dispose(c);

#else


	text_buffer_cursor_t lt = TextBufferCursor.Duplicate(DisplayLeftTop[0]);
	screen_pos_t csp = CursorScreenPos[0];
	screen_pos_t p = { 0,0 };
	screen_pos_t target = { cx2, csp.Y + 1 };
	text_buffer_cursor_t c = TextBufferCursor.Duplicate(lt);
	screen_pos_t pp = p;
	text_buffer_cursor_t pc = TextBufferCursor.Duplicate(c);
	for (;;)
	{
		if (p.Y == target.Y && p.X == target.X)
		{
			break;
		}
		if (p.Y == target.Y && p.X > target.X)
		{
			p = pp;
			TextBufferCursor.Copy(c, pc);
			break;
		}
		if (p.Y > target.Y)
		{
			p = pp;
			TextBufferCursor.Copy(c, pc);
			break;
		}
		pp = p;
		TextBufferCursor.Copy(pc, c);
		if (iterate_cursor_with_screen_pos(&p, c) == false)
		{
			// 次の行がないので移動できない
			TextBufferCursor.Dispose(lt);
			TextBufferCursor.Dispose(c);
			TextBufferCursor.Dispose(pc);
			return;
		}
	}

	TextBufferCursor.Copy(Cursor[0], c);
	CursorScreenPos[0] = p;
	TextBufferCursor.Copy(DisplayLeftTop[0], lt);

	TextBufferCursor.Dispose(lt);
	TextBufferCursor.Dispose(c);
	TextBufferCursor.Dispose(pc);

#endif

	// 画面を一行下にスクロールする
	if (CursorScreenPos[0].Y == EDITOR_SCREEN_HEIGHT)
	{
		if (screen_scrolldown() == false)
		{
			// 移動できない＝スクロールできないので終り
			return;
		}
	}

	ValidateDisplayCursorPos();


}

void cursor_down(void) {
	ValidateDisplayCursorPos();
	cursor_down_();
	ValidateDisplayCursorPos();
}


// カーソルを行頭に移動する
void cursor_home_(void) {
	move_cursor_to_display_line_head(&CursorScreenPos[0], Cursor[0]);
	cx2 = 0;
}

void cursor_home(void) {
	ValidateDisplayCursorPos();
	cursor_home_();
	ValidateDisplayCursorPos();
}



// カーソルを行末に移動する
// 横移動なので最後に行った横移動後のＸ座標を保持する変数cx2を更新する
void cursor_end_(void) {
	move_cursor_to_display_line_tail(&CursorScreenPos[0], Cursor[0]);
	cx2 = CursorScreenPos[0].X;
}

void cursor_end(void) {
	ValidateDisplayCursorPos();
	cursor_end_();
	ValidateDisplayCursorPos();
}

void cursor_pageup_(void) {
	//PageUpキー
	//最上行が最下行になるまでスクロール
	//出力：下記変数を移動先の値に変更
	//cursorbp,cursorix バッファ上のカーソル位置
	//cx,cx2
	//cy
	//disptopbp,disptopix 画面左上のバッファ上の位置

	const int cy_old = CursorScreenPos[0].Y;
	while (CursorScreenPos[0].Y > 0) {
		cursor_up(); // cy==0になるまでカーソルを上に移動
	}

	int i;
	text_buffer_cursor_t prev = TextBufferCursor.Allocate();
	for (i = 0; i < EDITOR_SCREEN_HEIGHT - 1; i++) {
		//画面行数-1行分カーソルを上に移動
		TextBufferCursor.Copy(prev, DisplayLeftTop[0]);
		cursor_up();
		if (TextBuffer_EqualCursor(prev, DisplayLeftTop[0])) {
			break; //最上行で移動できなかった場合抜ける
		}
	}
	TextBufferCursor.Dispose(prev);
	//元のY座標までカーソルを下に移動、1行も動かなかった場合は最上行に留まる
	if (i > 0) {
		while (CursorScreenPos[0].Y < cy_old) {
			cursor_down();
		}
	}
}

void cursor_pageup(void) {
	ValidateDisplayCursorPos();
	cursor_pageup_();
	ValidateDisplayCursorPos();
}

void cursor_pagedown_(void) {
	//PageDownキー
	//最下行が最上行になるまでスクロール
	//出力：下記変数を移動先の値に変更
	//cursorbp,cursorix バッファ上のカーソル位置
	//cx,cx2
	//cy
	//disptopbp,disptopix 画面左上のバッファ上の位置


	const int cy_old = CursorScreenPos[0].Y;
	while (CursorScreenPos[0].Y < EDITOR_SCREEN_HEIGHT - 1) {
		// cy==EDITWIDTH-1になるまでカーソルを下に移動
		int y = CursorScreenPos[0].Y;
		cursor_down();
		if (y == CursorScreenPos[0].Y) {
			break;// バッファ最下行で移動できなかった場合抜ける
		}
	}

	int i;
	text_buffer_cursor_t prev = TextBufferCursor.Allocate();
	for (i = 0; i < EDITOR_SCREEN_HEIGHT - 1; i++) {
		//画面行数-1行分カーソルを下に移動
		TextBufferCursor.Copy(prev, DisplayLeftTop[0]);
		cursor_down();
		if (TextBuffer_EqualCursor(prev, DisplayLeftTop[0])) {
			break; //最下行で移動できなかった場合抜ける
		}
	}
	TextBufferCursor.Dispose(prev);

	//下端からさらに移動した行数分、カーソルを上に移動、1行も動かなかった場合は最下行に留まる
	if (i > 0) {
		while (CursorScreenPos[0].Y > cy_old) {
			cursor_up();
		}
	}
}

void cursor_pagedown(void) {
	ValidateDisplayCursorPos();
	cursor_pagedown_();
	ValidateDisplayCursorPos();
}

void cursor_top(void) {
	//カーソルをテキストバッファの先頭に移動
	TextBuffer_MoveToBeginningOfDocument(Cursor[0]);

	//範囲選択モード解除
	TextBufferCursor.Dispose(SelectStart);
	SelectStart = INVALID_TEXTBUFFER_CURSOR;

	// 画面の左上位置をリセット
	TextBufferCursor.Copy(DisplayLeftTop[0], Cursor[0]);

	// 画面上カーソル位置をリセット
	CursorScreenPos[0].X = 0;
	CursorScreenPos[0].Y = 0;

	// 最後の横移動位置をリセット
	cx2 = 0;
}

static int countarea(void) {
	//テキストバッファの指定範囲の文字数をカウント
	//範囲は(cursorbp,cursorix)と(SelectStart.Buffer,SelectStart.Index)で指定
	//後ろ側の一つ前の文字までをカウント

	//選択範囲の開始位置と終了の前後を判断して開始位置と終了位置を設定
	if (CursorScreenPos[0].Y < SelectStartCursorScreenPos.Y ||
		(CursorScreenPos[0].Y == SelectStartCursorScreenPos.Y && CursorScreenPos[0].X < SelectStartCursorScreenPos.X)) {
		return TextBuffer_strlen(Cursor[0], SelectStart);
	}
	else {
		return TextBuffer_strlen(SelectStart, Cursor[0]);
	}
}

// テキストバッファの指定範囲を削除
static void deletearea(void) {
	//範囲は(cursorbp,cursorix)と(SelectStart.Buffer,SelectStart.Index)で指定
	//後ろ側の一つ前の文字までを削除
	//削除後のカーソル位置は選択範囲の先頭にし、範囲選択モード解除する

	int n = countarea(); //選択範囲の文字数カウント

	//範囲選択の開始位置と終了位置の前後を判断してカーソルを開始位置に設定
	if (CursorScreenPos[0].Y > SelectStartCursorScreenPos.Y || (CursorScreenPos[0].Y == SelectStartCursorScreenPos.Y && CursorScreenPos[0].X > SelectStartCursorScreenPos.X)) {
		TextBufferCursor.Copy(Cursor[0], SelectStart);
		CursorScreenPos[0] = SelectStartCursorScreenPos;
	}
	cx2 = CursorScreenPos[0].X;

	//範囲選択モード解除
	TextBufferCursor.Dispose(SelectStart);
	SelectStart = INVALID_TEXTBUFFER_CURSOR;

	// 始点からn文字削除
	TextBuffer_DeleteArea(Cursor[0], n);
	CursorScreenPos[0] = calc_screen_pos_from_line_head(DisplayLeftTop[0], Cursor[0]);
}

//画面の再描画
static void redraw(void) {
	uint8_t cl = COLOR_NORMALTEXT;

	DisableInterrupt();

	text_buffer_cursor_t select_start, select_end;
	if (SelectStart == INVALID_TEXTBUFFER_CURSOR) {
		//範囲選択モードでない場合
		select_start = INVALID_TEXTBUFFER_CURSOR;
		select_end = INVALID_TEXTBUFFER_CURSOR;
	}
	else {
		//範囲選択モードの場合、開始位置と終了の前後判断して
		//bp1 を開始位置、bp2 を終了位置に設定
		if (CursorScreenPos[0].Y < SelectStartCursorScreenPos.Y ||
			(CursorScreenPos[0].Y == SelectStartCursorScreenPos.Y && CursorScreenPos[0].X < SelectStartCursorScreenPos.X)) {
			select_start = TextBufferCursor.Duplicate(Cursor[0]);
			select_end = TextBufferCursor.Duplicate(SelectStart);
		}
		else {
			select_start = TextBufferCursor.Duplicate(SelectStart);
			select_end = TextBufferCursor.Duplicate(Cursor[0]);
		}
	}
	TextVRAM.SetTextColor(COLOR_NORMALTEXT);
	TextVRAM.SetBackgroundColor(COLOR_NORMALTEXT_BG);

	// テキストVRAMへの書き込み
	// 必要なら割り込み禁止とか使うこと
	TextVRAM.ClearScreen();
	text_vram_pixel_t* vp = TextVRAM.GetVramPtr();
	text_buffer_cursor_t bp = TextBufferCursor.Duplicate(DisplayLeftTop[0]);
	screen_pos_t sp = { 0,0 };
	while (sp.Y < EDITOR_SCREEN_HEIGHT) {
		// 選択範囲の始点/終点に到達してたら色設定変更
		if (SelectStart != INVALID_TEXTBUFFER_CURSOR) {
			if (TextBuffer_EqualCursor(bp, select_start)) cl = COLOR_AREASELECTTEXT;
			if (TextBuffer_EqualCursor(bp, select_end)) cl = COLOR_NORMALTEXT;
		}
		char16_t ch = TextBuffer_TakeCharacatorOnCursor(bp);
		vp[sp.Y * VWIDTH_X + sp.X].ch = ch;
		vp[sp.Y * VWIDTH_X + sp.X].color = cl;
		//vp[sp.Y * VWIDTH_X + sp.X].bgcolor = bc;
		if (iterate_cursor_with_screen_pos(&sp, bp) == false)
		{
			break;
		}
	}
	TextBufferCursor.Dispose(bp);
	TextBufferCursor.Dispose(select_start);
	TextBufferCursor.Dispose(select_end);

	EnableInterrupt();

}


//
//クリップボード
//
static struct {
	// バッファ
	char16_t buffer[EDITOR_SCREEN_WIDTH * EDITOR_SCREEN_HEIGHT];
	// 格納されている文字数
	size32_t	length;
} Clipboard;

// 選択範囲をクリップボードにコピー
void Clipboard_CopyTo(void) {
	text_buffer_cursor_t bp1 = TextBufferCursor.Allocate();
	text_buffer_cursor_t bp2 = TextBufferCursor.Allocate();

	//範囲選択モードの場合、開始位置と終了の前後判断して
	//bp1,ix1を開始位置、bp2,ix2を終了位置に設定
	if (CursorScreenPos[0].Y < SelectStartCursorScreenPos.Y ||
		(CursorScreenPos[0].Y == SelectStartCursorScreenPos.Y && CursorScreenPos[0].X < SelectStartCursorScreenPos.X)) {
		TextBufferCursor.Copy(bp1, Cursor[0]);
		TextBufferCursor.Copy(bp2, SelectStart);
	}
	else {
		TextBufferCursor.Copy(bp1, SelectStart);
		TextBufferCursor.Copy(bp2, Cursor[0]);
	}

	char16_t *pd = Clipboard.buffer;
	while (TextBuffer_EqualCursor(bp1, bp2) == false) {
		*pd++ = TextBuffer_TakeCharacatorOnCursor(bp1);
		TextBuffer_CursorForward(bp1);
	}
	Clipboard.length = pd - Clipboard.buffer;
	TextBufferCursor.Dispose(bp1);
	TextBufferCursor.Dispose(bp2);
}

void Clipboard_PasteFrom(void) {
	// クリップボードから貼り付け

	char16_t *p = Clipboard.buffer;
	for (int n = Clipboard.length; n > 0; n--) {
		if (InsertCharactor(*p++) == false) {
			break;
		}
		cursor_right();//画面上、バッファ上のカーソル位置を1つ後ろに移動
	}
}

//範囲選択モード開始時のカーソル開始位置グローバル変数設定
void set_areamode() {
	TextBufferCursor.Dispose(SelectStart);
	SelectStart = TextBufferCursor.Duplicate(Cursor[0]);
	SelectStartCursorScreenPos = CursorScreenPos[0];
}

//カーソル関連グローバル変数を一時避難
void save_cursor(void) {
	TextBufferCursor.Copy(Cursor[1], Cursor[0]);
	TextBufferCursor.Copy(DisplayLeftTop[1], DisplayLeftTop[0]);
	CursorScreenPos[1] = CursorScreenPos[0];
}

//カーソル関連グローバル変数を一時避難場所から戻す
void restore_cursor(void) {
	TextBufferCursor.Copy(Cursor[0], Cursor[1]);
	TextBufferCursor.Copy(DisplayLeftTop[0], DisplayLeftTop[1]);
	CursorScreenPos[0] = CursorScreenPos[1];
}

void inittextbuf(void) {
	// テキストバッファの初期化
	TextBuffer_Initialize();

	// カーソルをリセット
	TextBufferCursor.Dispose(Cursor[0]);
	TextBufferCursor.Dispose(Cursor[1]);
	TextBufferCursor.Dispose(DisplayLeftTop[0]);
	TextBufferCursor.Dispose(DisplayLeftTop[1]);
	TextBufferCursor.Dispose(SelectStart);

	Cursor[0] = TextBufferCursor.Allocate();
	Cursor[1] = TextBufferCursor.Allocate();
	DisplayLeftTop[0] = TextBufferCursor.Allocate();
	DisplayLeftTop[1] = TextBufferCursor.Allocate();
	SelectStart = INVALID_TEXTBUFFER_CURSOR;

	// カーソル位置を先頭に
	cursor_top();

	//編集済みフラグクリア
	edited = false;
}

int savetextfile(char16_t *filename) {
	// テキストバッファをテキストファイルに書き込み
	// 書き込み成功で0、失敗でエラーコード（負数）を返す

	ptr_t fp = File.Open(filename, FileMode_CreateNew, FileAccess_Write);
	if (fp == NULL) {
		return ERR_CANTFILEOPEN;
	}

	text_buffer_cursor_t bp = TextBufferCursor.Allocate();

	int er = 0;//エラーコード
	do {
		char16_t *pd = filebuf;
		int n = 0;
		while (n < FILEBUFSIZE - 1) {
			char16_t ch = TextBuffer_TakeCharacatorOnCursor(bp);
			if (ch == 0x0000)
			{
				break;
			}
			if (ch == L'\n')
			{
				// 改行文字を\n->\r\nに変更
				*pd++ = L'\r';
			}
			*pd++ = ch;
			TextBuffer_CursorForward(bp);
			n++;
		}
		if (n > 0) {
			int i = File.Write(fp, (uint8_t*)filebuf, sizeof(char16_t) * n) / sizeof(char16_t);
			if (i != n) {
				er = ERR_CANTWRITEFILE;
			}
		}
	} while (TextBuffer_EndOfString(bp) == false);

	File.Close(fp);
	TextBufferCursor.Dispose(bp);
	return er;
}

// テキストファイルをテキストバッファに読み込み
// 読み込み成功で0、失敗でエラーコード（負数）を返す
int LoadTextFile(char16_t *filename) {
#if defined(_MSC_VER)
	ptr_t fp = File.Open(filename, FileMode_Open, FileAccess_Read);
	if (fp == NULL) {
		return ERR_CANTFILEOPEN;
	}

	inittextbuf();

	text_buffer_cursor_t bp = TextBufferCursor.Allocate();

	int n;
	int er = 0;
	do {
		n = File.Read(fp, (uint8_t*)filebuf, sizeof(char16_t) * FILEBUFSIZE) / sizeof(char16_t);
		char16_t *ps = filebuf;
		for (int i = 0; i < n; i++) {
			if (*ps == L'\r') {
				//改行コード\r\n→\rを読み飛ばす
				ps++;
			}
			else {
				if (TextBuffer_InsertCharacatorOnCursor(bp, *ps++) != 0) {
					er = ERR_FILETOOBIG;
					break;
				}
				TextBuffer_CursorForward(bp);

			}
		}
	} while (n == FILEBUFSIZE && er == 0);
	File.Close(fp);
	TextBufferCursor.Dispose(bp);
	if (er) {
		inittextbuf();//エラー発生の場合バッファクリア
	}
	return er;
#else
	return ERR_CANTFILEOPEN;
#endif
}

//

void save_as(void) {
	// 現在のテキストバッファの内容をファイル名を付けてSDカードに保存
	// ファイル名はグローバル変数currentfile[]
	// ファイル名はキーボードから変更可能
	// 成功した場合currentfileを更新

	int er;
	TextVRAM.ClearScreen();
	{
		screen_pos_t vcp = { 0,0 };
		TextVRAM.SetCursorPosition(&vcp);
		TextVRAM.SetTextColor(COLOR_NORMALTEXT);
	}
	TextVRAM.puts(L"Save To SD Card\n");

	//currentfileからtempfileにコピー
	char16_t tempfile[FILE_NAME_BUF_LEN];
	Memory.Copy(tempfile, CurrentFileName, sizeof(CurrentFileName));

	while (1) {
		// ファイル名入力のプロンプト表示
		TextVRAM.puts(L"File Name + [Enter] / [ESC]\n");
		if (KeyInput.gets(tempfile, sizeof(tempfile) / sizeof(tempfile[0])) == false) {
			return; //ESCキーが押された
		}

		if (tempfile[0] == 0) continue; //NULL文字列の場合
		TextVRAM.puts(L"Writing...\n");
		er = savetextfile(tempfile); //ファイル保存、er:エラーコード
		if (er == 0) {
			TextVRAM.puts(L"OK");
			//tempfileからcurrentfileにコピーして終了
			Memory.Copy(CurrentFileName, tempfile, sizeof(tempfile));
			edited = false; //編集済みフラグクリア
			WaitMS(1000);//1秒待ち
			return;
		}
		TextVRAM.SetTextColor(COLOR_ERRORTEXT);
		if (er == ERR_CANTFILEOPEN) {
			TextVRAM.puts(L"Bad File Name or File Error\n");
		}
		else {
			TextVRAM.puts(L"Cannot Write\n");
		}
		TextVRAM.SetTextColor(COLOR_NORMALTEXT);
		TextVRAM.puts(L"Retry:[Enter] / Quit:[ESC]\n");
		while (1) {
			if (KeyInput.getch() && Keyboard_GetCurrentVKeyCode())
			{
				uint8_t vk = Keyboard_GetCurrentVKeyCode();
				if (vk == VKEY_RETURN || vk == VKEY_SEPARATOR) {
					break;
				}
				if (vk == VKEY_ESCAPE) {
					return;
				}
			}
		}
	}
}

// SDカード等からファイルを選択して読み込み
int selectfile(void) {
#if defined(_MSC_VER)
	// currenfile[]にファイル名を記憶
	// 戻り値　0：読み込みを行った　-1：読み込みなし
	int i, er;

	EnumerateFilesContext_t ctx;

	//ファイルの一覧をSDカードから読み出し
	TextVRAM.ClearScreen();
	if (edited) {
		//最終保存後に編集済みの場合、保存の確認
		TextVRAM.SetTextColor(COLOR_NORMALTEXT);
		TextVRAM.puts(L"Save Editing File?\n");
		TextVRAM.puts(L"Save:[Enter] / Not Save:[ESC]\n");
		while (1) {
			KeyInput.getch();
			uint8_t vk = Keyboard_GetCurrentVKeyCode();
			if (vk == VKEY_RETURN || vk == VKEY_SEPARATOR) {
				save_as();
				break;
			}
			else if (vk == VKEY_ESCAPE) {
				break;
			}
		}
	}
	while (Directory.EnumerateFiles.Create(&ctx, L"") == false) {
		TextVRAM.SetTextColor(COLOR_ERRORTEXT);
		TextVRAM.puts(L"No File Found\n");
		TextVRAM.puts(L"Retry:[Enter] / Quit:[ESC]\n");
		while (1) {
			KeyInput.getch();
			uint8_t vk = Keyboard_GetCurrentVKeyCode();
			if (vk == VKEY_RETURN || vk == VKEY_SEPARATOR) {
				break;
			}
			else if (vk == VKEY_ESCAPE) {
				return -1;
			}
		}
	}


	uint16_t filenum = 0;
	while (Directory.EnumerateFiles.MoveNext(&ctx) && (filenum < MAXFILENUM)) {
		//filenames[]にファイル名の一覧を読み込み
		const char16_t *filename = Directory.EnumerateFiles.GetCurrent(&ctx);
		size32_t sz = sizeof(filenames[filenum]) / sizeof(filenames[filenum][0]);
		Utf16.NCopy(filenames[filenum], filename, sz);
		filenames[filenum][sz - 1] = 0;
		filenum++;
	}

	Directory.EnumerateFiles.Dispose(&ctx);

	//ファイル一覧を画面に表示
	TextVRAM.ClearScreen();
	{
		screen_pos_t vcp = { 0,0 };
		TextVRAM.SetCursorPosition(&vcp);
		TextVRAM.SetTextColor(4);
	}
	TextVRAM.puts(L"Select File + [Enter] / [ESC]\n");
	for (i = 0; i < filenum; i++) {
		int x = (i & 1) * 15U + 1U;
		int y = i / 2U + 1U;
		{
			screen_pos_t vcp = { x, y };
			TextVRAM.SetCursorPosition(&vcp);
			TextVRAM.SetTextColor(COLOR_NORMALTEXT);
		}
		TextVRAM.puts(filenames[i]);
	}

	//ファイルの選択
	i = 0;
	{
		screen_pos_t vcp = { 0, 1 };
		TextVRAM.SetCursorPosition(&vcp);
		TextVRAM.SetTextColor(5);
	}
	TextVRAM.putch('>'); // Right Arrow
	{
		screen_pos_t vcp;
		TextVRAM.GetCursorPosition(&vcp);
		vcp.X--;
		TextVRAM.SetCursorPosition(&vcp);
	}
	while (1) {
		KeyInput.getch();
		uint8_t vk = Keyboard_GetCurrentVKeyCode();
		if (vk == 0) { continue; }
		TextVRAM.putch(' ');
		{
			screen_pos_t vcp = { 0,EDITOR_SCREEN_HEIGHT - 1 };
			TextVRAM.SetCursorPosition(&vcp);
			TextVRAM.SetTextColor(COLOR_NORMALTEXT);
		}
		for (uint8_t x = 0; x < EDITOR_SCREEN_WIDTH - 1; x++) TextVRAM.putch(' '); //最下行のステータス表示を消去
		switch (vk) {
		case VKEY_UP:
		case VKEY_NUMPAD8:
			//上矢印キー
			if (i >= 2) i -= 2;
			break;
		case VKEY_DOWN:
		case VKEY_NUMPAD2:
			//下矢印キー
			if (i + 2 < filenum) i += 2;
			break;
		case VKEY_LEFT:
		case VKEY_NUMPAD4:
			//左矢印キー
			if (i > 0) i--;
			break;
		case VKEY_RIGHT:
		case VKEY_NUMPAD6:
			//右矢印キー
			if (i + 1 < filenum) i++;
			break;
		case VKEY_RETURN: //Enterキー
		case VKEY_SEPARATOR: //テンキーのEnter
						   //ファイル名決定。読み込んで終了
			er = LoadTextFile(filenames[i]); //テキストバッファにファイル読み込み
			if (er == 0) {
				//currenfile[]変数にファイル名をコピーして終了
				Memory.Copy(CurrentFileName, filenames[i], sizeof(filenames[i]));
				return 0;
			}
			else {
				// エラー発生
				screen_pos_t vcp = { 0,EDITOR_SCREEN_HEIGHT - 1 };
				TextVRAM.SetCursorPosition(&vcp);
				TextVRAM.SetTextColor(COLOR_ERRORTEXT);
				if (er == ERR_CANTFILEOPEN) TextVRAM.puts(L"Cannot Open File");
				else if (er == ERR_FILETOOBIG) TextVRAM.puts(L"File Too Big");
			}
			break;
		case VKEY_ESCAPE:
			//ESCキー、ファイル読み込みせず終了
			return -1;
		}
		{
			screen_pos_t vcp = { (i & 1) * 15, i / 2 + 1 };
			TextVRAM.SetCursorPosition(&vcp);
			TextVRAM.SetTextColor(5);
		}

		TextVRAM.putch('>'); // Right Arrow
		{
			screen_pos_t vcp;
			TextVRAM.GetCursorPosition(&vcp);
			vcp.X--;
			TextVRAM.SetCursorPosition(&vcp);
		}
	}
#else
	return -1;
#endif
	}
void newtext(void) {
	// 新規テキスト作成
	if (edited) {
		//最終保存後に編集済みの場合、保存の確認
		TextVRAM.ClearScreen();
		TextVRAM.SetTextColor(COLOR_NORMALTEXT);
		TextVRAM.puts(L"Save Editing File?\n");
		TextVRAM.puts(L"Save:[Enter] / Not Save:[ESC]\n");
		while (1) {
			KeyInput.getch();
			uint8_t vk = Keyboard_GetCurrentVKeyCode();
			if (vk == VKEY_RETURN || vk == VKEY_SEPARATOR) {
				save_as();
				break;
			}
			else if (vk == VKEY_ESCAPE) {
				break;
			}
		}
	}
	inittextbuf(); //テキストバッファ初期化
	CurrentFileName[0] = 0; //作業中ファイル名クリア
}
void displaybottomline(void) {
	//エディター画面最下行の表示
	{
		screen_pos_t vcp = { 0, EDITOR_BOTTOM_STATUSLINE };
		TextVRAM.SetCursorPosition(&vcp);
		TextVRAM.SetTextColor(COLOR_BOTTOMLINE);
		TextVRAM.SetBackgroundColor(COLOR_BOTTOMLINE_BG);
	}
	TextVRAM.puts(L"F1:LOAD F2:SAVE   F4:NEW ");
	TextVRAM.putdigit2(TextBuffer_GetCapacity() - TextBuffer_GetTotalLength(), 5);
	TextVRAM.FillBackgroundColor(0, EDITOR_BOTTOM_STATUSLINE, EDITOR_SCREEN_WIDTH, COLOR_BOTTOMLINE_BG);
}

void normal_code_process(char16_t k) {
	// 通常文字入力処理
	// k:入力された文字コード

	edited = true; //編集済みフラグを設定

	if (KeyInput.GetInsertMode() || k == '\n' || SelectStart != INVALID_TEXTBUFFER_CURSOR) { // 挿入モードの場合
		// 選択範囲を削除
		if (SelectStart != INVALID_TEXTBUFFER_CURSOR) {
			deletearea();
		}
		if (InsertCharactor(k)) {
			cursor_right();//画面上、バッファ上のカーソル位置を1つ後ろに移動
		}
	}
	else { //上書きモード
		if (OverwriteCharactor(k)) {
			cursor_right();//画面上、バッファ上のカーソル位置を1つ後ろに移動
		}
	}
}

void control_code_process(uint8_t k, uint8_t sh) {
	// 制御文字入力処理
	// k:制御文字の仮想キーコード
	// sh:シフト関連キー状態

	save_cursor(); //カーソル関連変数退避（カーソル移動できなかった場合戻すため）

	switch (k) {
	case VKEY_LEFT:
	case VKEY_NUMPAD4:
		//シフトキー押下していなければ範囲選択モード解除（NumLock＋シフト＋テンキーでも解除）
		if ((sh & CHK_SHIFT) == 0 || (k == VKEY_NUMPAD4) && (sh & CHK_NUMLK)) {
			TextBufferCursor.Dispose(SelectStart);
			SelectStart = INVALID_TEXTBUFFER_CURSOR;
		}
		else if (SelectStart != INVALID_TEXTBUFFER_CURSOR) {
			set_areamode(); //範囲選択モードでなければ範囲選択モード開始
		}
		if (sh & CHK_CTRL) {
			//CTRL＋左矢印でHome
			cursor_home();
			break;
		}
		cursor_left();
		//if (SelectStart.Buffer != NULL && (DisplayLeftTop[0].Buffer != DisplayLeftTop[1].Buffer || DisplayLeftTop[0].Index != DisplayLeftTop[1].Index)) {
		if (SelectStart != INVALID_TEXTBUFFER_CURSOR && !TextBuffer_EqualCursor(DisplayLeftTop[0], DisplayLeftTop[1])) {
			//範囲選択モードで画面スクロールがあった場合
			if (SelectStartCursorScreenPos.Y < EDITOR_SCREEN_HEIGHT - 1) {
				SelectStartCursorScreenPos.Y++; //範囲スタート位置もスクロール
			}
			else {
				restore_cursor(); //カーソル位置を戻す（画面範囲外の範囲選択禁止）
			}
		}
		break;
	case VKEY_RIGHT:
	case VKEY_NUMPAD6:
		//シフトキー押下していなければ範囲選択モード解除（NumLock＋シフト＋テンキーでも解除）
		if ((sh & CHK_SHIFT) == 0 || (k == VKEY_NUMPAD6) && (sh & CHK_NUMLK)) {
			TextBufferCursor.Dispose(SelectStart);
			SelectStart = INVALID_TEXTBUFFER_CURSOR;
		}
		else if (SelectStart == INVALID_TEXTBUFFER_CURSOR) {
			set_areamode(); //範囲選択モードでなければ範囲選択モード開始
		}
		if (sh & CHK_CTRL) {
			//CTRL＋右矢印でEnd
			cursor_end();
			break;
		}
		cursor_right();
		if (SelectStart != INVALID_TEXTBUFFER_CURSOR && (TextBuffer_EqualCursor(DisplayLeftTop[0], DisplayLeftTop[1]) == false)) {
			//範囲選択モードで画面スクロールがあった場合
			if (SelectStartCursorScreenPos.Y > 0) SelectStartCursorScreenPos.Y--; //範囲スタート位置もスクロール
			else restore_cursor(); //カーソル位置を戻す（画面範囲外の範囲選択禁止）
		}
		break;
	case VKEY_UP:
	case VKEY_NUMPAD8:
		//シフトキー押下していなければ範囲選択モード解除（NumLock＋シフト＋テンキーでも解除）
		if ((sh & CHK_SHIFT) == 0 || (k == VKEY_NUMPAD8) && (sh & CHK_NUMLK)) {
			TextBufferCursor.Dispose(SelectStart);
			SelectStart = INVALID_TEXTBUFFER_CURSOR;
		}
		else if (SelectStart == INVALID_TEXTBUFFER_CURSOR) {
			set_areamode(); //範囲選択モードでなければ範囲選択モード開始
		}
		cursor_up();
		if (SelectStart != INVALID_TEXTBUFFER_CURSOR && (TextBuffer_EqualCursor(DisplayLeftTop[0], DisplayLeftTop[1]) == false)) {
			//範囲選択モードで画面スクロールがあった場合
			if (SelectStartCursorScreenPos.Y < EDITOR_SCREEN_HEIGHT - 1) SelectStartCursorScreenPos.Y++; //範囲スタート位置もスクロール
			else restore_cursor(); //カーソル位置を戻す（画面範囲外の範囲選択禁止）
		}
		break;
	case VKEY_DOWN:
	case VKEY_NUMPAD2:
		//シフトキー押下していなければ範囲選択モード解除（NumLock＋シフト＋テンキーでも解除）
		if ((sh & CHK_SHIFT) == 0 || (k == VKEY_NUMPAD2) && (sh & CHK_NUMLK)) {
			TextBufferCursor.Dispose(SelectStart);
			SelectStart = INVALID_TEXTBUFFER_CURSOR;
		}
		else if (SelectStart == INVALID_TEXTBUFFER_CURSOR) {
			set_areamode(); //範囲選択モードでなければ範囲選択モード開始
		}
		cursor_down();
		if (SelectStart != INVALID_TEXTBUFFER_CURSOR && (TextBuffer_EqualCursor(DisplayLeftTop[0], DisplayLeftTop[1]) == false)) {
			//範囲選択モードで画面スクロールがあった場合
			if (SelectStartCursorScreenPos.Y > 0) SelectStartCursorScreenPos.Y--; //範囲スタート位置もスクロール
			else restore_cursor(); //カーソル位置を戻す（画面範囲外の範囲選択禁止）
		}
		break;
	case VKEY_HOME:
	case VKEY_NUMPAD7:
		//シフトキー押下していなければ範囲選択モード解除（NumLock＋シフト＋テンキーでも解除）
		if ((sh & CHK_SHIFT) == 0 || (k == VKEY_NUMPAD7) && (sh & CHK_NUMLK)) {
			TextBufferCursor.Dispose(SelectStart);
			SelectStart = INVALID_TEXTBUFFER_CURSOR;
		}
		else if (SelectStart == INVALID_TEXTBUFFER_CURSOR) {
			set_areamode(); //範囲選択モードでなければ範囲選択モード開始
		}
		cursor_home();
		break;
	case VKEY_END:
	case VKEY_NUMPAD1:
		//シフトキー押下していなければ範囲選択モード解除（NumLock＋シフト＋テンキーでも解除）
		if ((sh & CHK_SHIFT) == 0 || (k == VKEY_NUMPAD1) && (sh & CHK_NUMLK)) {
			TextBufferCursor.Dispose(SelectStart);
			SelectStart = INVALID_TEXTBUFFER_CURSOR;
		}
		else if (SelectStart == INVALID_TEXTBUFFER_CURSOR) {
			set_areamode(); //範囲選択モードでなければ範囲選択モード開始
		}
		cursor_end();
		break;
	case VKEY_PRIOR: // PageUpキー
	case VKEY_NUMPAD9:
		//シフト＋PageUpは無効（NumLock＋シフト＋「9」除く）
		if ((sh & CHK_SHIFT) && ((k != VKEY_NUMPAD9) || ((sh & CHK_NUMLK) == 0))) {
			break;
		}
		//範囲選択モード解除
		TextBufferCursor.Dispose(SelectStart);
		SelectStart = INVALID_TEXTBUFFER_CURSOR;
		cursor_pageup();
		break;
	case VKEY_NEXT: // PageDownキー
	case VKEY_NUMPAD3:
		//シフト＋PageDownは無効（NumLock＋シフト＋「3」除く）
		if ((sh & CHK_SHIFT) && ((k != VKEY_NUMPAD3) || ((sh & CHK_NUMLK) == 0))) {
			break;
		}
		//範囲選択モード解除
		TextBufferCursor.Dispose(SelectStart);
		SelectStart = INVALID_TEXTBUFFER_CURSOR;
		cursor_pagedown();
		break;
	case VKEY_DELETE: //Deleteキー
	case VKEY_DECIMAL: //テンキーの「.」
		edited = true; //編集済みフラグ
		if (SelectStart != INVALID_TEXTBUFFER_CURSOR) {
			deletearea();//選択範囲を削除
		}
		else {
			TextBuffer_DeleteCharacatorOnCursor(Cursor[0]);
			CursorScreenPos[0] = calc_screen_pos_from_line_head(DisplayLeftTop[0], Cursor[0]);
		}
		break;
	case VKEY_BACK: //BackSpaceキー
		edited = true; //編集済みフラグ
		if (SelectStart != INVALID_TEXTBUFFER_CURSOR) {
			deletearea();//選択範囲を削除
			break;
		}
		if (TextBuffer_StartOfString(Cursor[0])) {
			break; //バッファ先頭では無視
		}
		cursor_left();
		TextBuffer_DeleteCharacatorOnCursor(Cursor[0]);
		CursorScreenPos[0] = calc_screen_pos_from_line_head(DisplayLeftTop[0], Cursor[0]);
		break;
	case VKEY_INSERT:
	case VKEY_NUMPAD0:
		KeyInput.SetInsertMode(!KeyInput.GetInsertMode()); //挿入モード、上書きモードを切り替え
		break;
	case 'C':
		//CTRL+C、クリップボードにコピー
		if (SelectStart != INVALID_TEXTBUFFER_CURSOR && (sh & CHK_CTRL)) {
			Clipboard_CopyTo();
		}
		break;
	case 'X':
		//CTRL+X、クリップボードに切り取り
		if (SelectStart != INVALID_TEXTBUFFER_CURSOR && (sh & CHK_CTRL)) {
			Clipboard_CopyTo();
			deletearea(); //選択範囲の削除
			edited = true; //編集済みフラグ
		}
		break;
	case 'V':
		//CTRL+V、クリップボードから貼り付け
		if ((sh & CHK_CTRL) == 0) break;
		if (Clipboard.length == 0) break;
		edited = true; //編集済みフラグ
		if (SelectStart != INVALID_TEXTBUFFER_CURSOR) {
			//範囲選択している時は削除してから貼り付け
			if (TextBuffer_GetTotalLength() - countarea() + Clipboard.length <= TextBuffer_GetCapacity()) { //バッファ空き容量チェック
				deletearea();//選択範囲を削除
				Clipboard_PasteFrom();//クリップボード貼り付け
			}
		}
		else {
			if (TextBuffer_GetTotalLength() + Clipboard.length <= TextBuffer_GetCapacity()) { //バッファ空き容量チェック
				Clipboard_PasteFrom();//クリップボード貼り付け
			}
		}
		break;
	case 'S':
		//CTRL+S、SDカードに保存
		if ((sh & CHK_CTRL) == 0) break;
	case VKEY_F2: //F2キー
		save_as(); //ファイル名を付けて保存
		break;
	case 'O':
		//CTRL+O、ファイル読み込み
		if ((sh & CHK_CTRL) == 0) break;
	case VKEY_F1: //F1キー
				//F1キー、ファイル読み込み
		selectfile();	//ファイルを選択して読み込み
		break;
	case 'N':
		//CTRL+N、新規作成
		if ((sh & CHK_CTRL) == 0) break;
	case VKEY_F4: //F4キー
		newtext(); //新規作成
		break;
	}
}

#ifdef EXPERIMENTAL
static IME_Context context = { &roma2kana,{ 0 },{ 0 }, 0 };
static int ime = 0;

bool_t hook_ime(uint8_t ascii, uint8_t vkey, CTRLKEY_FLAG ctrlkey)
{
	if ((vkey == L' ') && (ctrlkey & CHK_CTRL)) {
		// トグル
		ime = (ime == 0) ? 1 : 0;
		IME_Reset(&context);
		return true;
	}
ReEval:
	switch (ime) {
	case 0:
		return false;
	case 1:
		if (vkey == L' ')
		{
			char16_t *o = context.pobox_output;
			context.pobox_search_next = 0;
			pobox_search(context.current, &o, 1, IME_BUF_MAX, context.pobox_search_next);
			ime = 2;
			goto ReEval;
		}
		if (vkey == VKEY_BACK)
		{
			if (context.currentlen > 0)
			{
				context.currentlen--;
				context.current[context.currentlen] = 0x0000;
				return true;
			}
			return false;
		}
		if (vkey == VKEY_ESCAPE)
		{
			context.currentlen = 0;
			context.current[0] = 0x0000;
			return true;
		}
		if (vkey == VKEY_RETURN || vkey == VKEY_SEPARATOR) {
			if (context.currentlen == 0)
			{
				return false;
			}
			// 変換バッファの内容を挿入

			char16_t *p = context.current;
			for (int n = context.currentlen; n > 0; n--) {
				if (InsertCharactor(*p) == false) {
					break;
				}
				cursor_right();//画面上、バッファ上のカーソル位置を1つ後ろに移動
				p++;
			}

			IME_Reset(&context);
			return true;
		}
		if (vkey == VKEY_F7)
		{
			// カタカナ化
			char16_t *p = context.current;
			for (int n = context.currentlen; n > 0; n--) {
				*p = (0x3040 <= *p && *p <= 0x309F) ? *p + (0x30A0 - 0x3040) : *p;
				p++;
			}
			return true;
		}
		if (vkey == VKEY_F6)
		{
			// ひらがな化
			char16_t *p = context.current;
			for (int n = context.currentlen; n > 0; n--) {
				*p = (0x30A0 <= *p && *p <= 0x30FF) ? *p -= (0x30A0 - 0x3040) : *p;
				p++;
			}
			return true;
		}

		if (ascii != 0x0000U)
		{
			IME_Update(&context, ascii);
			return true;
		}

		if (context.currentlen > 0)
		{
			// 入力中は握りつぶす
			return true;
		}
		return false;
	case 2:
		if (vkey == L' ')
		{
			char16_t *o = context.pobox_output;
			context.pobox_search_next++;
			if (pobox_search(context.current, &o, 1, IME_BUF_MAX, context.pobox_search_next) == 0)
			{
				context.pobox_search_next = 0;
				pobox_search(context.current, &o, 1, IME_BUF_MAX, context.pobox_search_next);
			}
			return true;
		}
		if (vkey == VKEY_ESCAPE)
		{
			ime = 1;
			return true;
		}
		if (vkey == VKEY_RETURN || vkey == VKEY_SEPARATOR) {
			// 変換バッファの内容を挿入
			if (context.pobox_output[0] == L'\0')
			{
				TextVRAM.puts(context.current);
			}
			else
			{
				TextVRAM.puts(context.pobox_output);
			}

			for (char16_t *p = context.pobox_output[0] != L'\0' ? context.pobox_output : context.current; *p != L'\0'; p++) {
				if (InsertCharactor(*p) == false) {
					break;
				}
				cursor_right();//画面上、バッファ上のカーソル位置を1つ後ろに移動
			}
			ime = 1;
			IME_Reset(&context);
			return true;
		}
		if (vkey == VKEY_BACK)
		{
			// 変換バッファの内容を挿入
			if (context.pobox_output[0] == L'\0')
			{
				TextVRAM.puts(context.current);
			}
			else
			{
				TextVRAM.puts(context.pobox_output);
			}

			for (char16_t *p = context.pobox_output[0] != L'\0' ? context.pobox_output : context.current; *p != L'\0' && *(p + 1) != L'\0'; p++) {
				if (InsertCharactor(*p) == false) {
					break;
				}
				cursor_right();//画面上、バッファ上のカーソル位置を1つ後ろに移動
			}
			ime = 1;
			IME_Reset(&context);
			return true;
		}

		return true;

	default:
		return false;
	}
}
#endif

void texteditor(void) {
	Memory.Initialize();

	// ビデオメモリクリア
	TextVRAM.Initialize();

	{
		screen_pos_t vcp = { 0,0 };
		TextVRAM.SetCursorPosition(&vcp);
		TextVRAM.SetTextColor(COLOR_NORMALTEXT);
		TextVRAM.SetBackgroundColor(COLOR_NORMALTEXT_BG);
	}
	// キーボード初期化	
	Keyboard_Initialize();

	// IME初期化
	IME_Initialize();


	inittextbuf(); //テキストバッファ初期化
	TextVRAM.SetPaletteColor(COLOR_BOTTOMLINE_BG, 0x00, 0x20, 0x80);

	CurrentFileName[0] = 0; //作業中ファイル名クリア

	KeyInput.SetInsertMode(true); //0:上書き、1:挿入
	Clipboard.length = 0; //クリップボードクリア


	text_buffer_cursor_t c = TextBufferCursor.Allocate();
	uint8_t ch[] = { 0x54, 0x00, 0x69, 0x00, 0x6e, 0x00, 0x79, 0x00, 0x54, 0x00, 0x65, 0x00, 0x78, 0x00, 0x74, 0x00, 0x45, 0x00, 0x64, 0x00, 0x69, 0x00, 0x74, 0x00, 0x6f, 0x00, 0x72, 0x00, 0xe5, 0x65, 0x2C, 0x67, 0x9e, 0x8a, 0xfe, 0x5b, 0xdc, 0x5f, 0x48, 0x72, 0x00, 0x00 };
	for (char16_t *p = (char16_t *)ch; *p != 0x0000; ++p) {
		TextBuffer_InsertCharacatorOnCursor(c, *p);
		TextBuffer_CursorForward(c);
	}

	while (1) {
		redraw();//画面再描画
		displaybottomline(); //画面最下行にファンクションキー機能表示

		screen_pos_t vcp = { CursorScreenPos[0].X, CursorScreenPos[0].Y };
		TextVRAM.SetCursorPosition(&vcp);

#ifdef EXPERIMENTAL
		switch (ime)
		{
		case 0:
			break;
		case 1:
			TextVRAM.SetTextColor(COLOR_BOTTOMLINE);
			TextVRAM.SetBackgroundColor(COLOR_NORMALTEXT_BG);
			TextVRAM.puts(context.current);
			break;
		case 2:
			TextVRAM.SetTextColor(COLOR_BOTTOMLINE);
			TextVRAM.SetBackgroundColor(COLOR_NORMALTEXT_BG);
			if (context.pobox_output[0] == '\0')
			{
				TextVRAM.puts(context.current);
			}
			else
			{
				TextVRAM.puts(context.pobox_output);
			}
			break;
		default:
			break;

		}

		//		TextVRAM.SetCursorPosition(&vcp);
#endif

		TextVRAM.SetTextColor(COLOR_NORMALTEXT);
		TextVRAM.SetBackgroundColor(COLOR_NORMALTEXT_BG);



		while (1) {
			//キー入力待ちループ
			WaitMS(1000 / 60);  //60分の1秒ウェイト
			if (Keyboard_ReadKey() && Keyboard_GetCurrentVKeyCode()) {
				break;  // 仮想キーが取得できたらループから抜ける
			}
			//if (SelectStart.Buffer == NULL) {
			//	GCTextBufferOne(); //1バイトガベージコレクション（範囲選択時はしない）
			//}
		}

		uint8_t k1 = Keyboard_GetCurrentAsciiCode();
		uint8_t k2 = Keyboard_GetCurrentVKeyCode();
		CTRLKEY_FLAG sh = Keyboard_GetCurrentCtrlKeys();             //sh:シフト関連キー状態

#ifdef EXPERIMENTAL
		if (hook_ime(k1, k2, sh) == false) {

			//Enter押下は単純に改行文字を入力とする
			if (k2 == VKEY_RETURN || k2 == VKEY_SEPARATOR) {
				k1 = '\n';
			}
			if (k2 == VKEY_TAB) {
				k1 = '\t';
			}
			if (k1) {
				//通常文字が入力された場合
				normal_code_process(k1);
			}
			else {
				//制御文字が入力された場合
				control_code_process(k2, sh);
			}
		}
#else

		//Enter押下は単純に改行文字を入力とする
		if (k2 == VKEY_RETURN || k2 == VKEY_SEPARATOR) {
			k1 = '\n';
		}
		if (k1) {
			//通常文字が入力された場合
			normal_code_process(k1);
		}
		else {
			//制御文字が入力された場合
			control_code_process(k2, sh);
		}
#endif

		if (SelectStart != INVALID_TEXTBUFFER_CURSOR &&
			CursorScreenPos[0].X == SelectStartCursorScreenPos.X &&
			CursorScreenPos[0].Y == SelectStartCursorScreenPos.Y) {
			//選択範囲の開始と終了が重なったら範囲選択モード解除
			TextBufferCursor.Dispose(SelectStart);
			SelectStart = INVALID_TEXTBUFFER_CURSOR;
		}
	}
}


