#include <windows.h>
#include <mmsystem.h>
#pragma comment(lib,"winmm.lib")
//#include <inttypes.h>
//#include <stdbool.h>
#include <tchar.h>
#include <process.h>
#include "../../editor/TextVRAM.h"
#include "../../editor/Keyboard.h"
#include "../../editor/font.h"
#include "../../editor/TextEditor.h"

#define CLASSNAME _T("SingleChipComputer")

static HBITMAP hBackBuffer;
static LPVOID lpBackBufferPixels;
static HDC hdcSrc, hdcOld;
static HWND hVideoWindow;
static CRITICAL_SECTION cs;
void *back_buffer;

#define SCALE 1
//#define VIDEO_WIDTH_PIXEL 320
//#define VIDEO_HEIGHT_PIXEL 240
#define VIDEO_WIDTH_PIXEL (WIDTH_X * FONT_WIDTH)
#define VIDEO_HEIGHT_PIXEL (WIDTH_Y * FONT_HEIGHT)

static uint8_t heap[1024 * 1024 * 4];

void *__heap_start = (void *)(heap);
void *__heap_end = (void *)(heap+sizeof(heap));

void WaitMS(uint32_t n) {
	Sleep(n);
}

void DisableInterrupt()
{
	EnterCriticalSection(&cs);
}

void EnableInterrupt()
{
	LeaveCriticalSection(&cs);
}

unsigned int wkey2vkey(WPARAM wp) {
	switch (wp) {
	case VK_LBUTTON: return VKEY_LBUTTON;
	case VK_RBUTTON: return VKEY_RBUTTON;
	case VK_CANCEL: return VKEY_CANCEL;
	case VK_MBUTTON: return VKEY_MBUTTON;
	case VK_XBUTTON1: return VKEY_XBUTTON1;
	case VK_XBUTTON2: return VKEY_XBUTTON2;
	case VK_BACK: return VKEY_BACK;
	case VK_TAB: return VKEY_TAB;
	case VK_CLEAR: return VKEY_CLEAR;
	case VK_RETURN: return VKEY_RETURN;
	case VK_SHIFT: return VKEY_SHIFT;
	case VK_CONTROL: return VKEY_CONTROL;
	case VK_MENU: return VKEY_MENU;
	case VK_PAUSE: return VKEY_PAUSE;
	case VK_CAPITAL: return VKEY_CAPITAL;
	case VK_KANA: return VKEY_KANA;
	//case VK_HANGUEL: return VKEY_HANGUEL;
	//case VK_HANGUL: return VKEY_HANGUL;
	case VK_JUNJA: return VKEY_JUNJA;
	case VK_FINAL: return VKEY_FINAL;
	case VK_HANJA: return VKEY_HANJA;
	//case VK_KANJI: return VKEY_KANJI;
	case VK_ESCAPE: return VKEY_ESCAPE;
	case VK_CONVERT: return VKEY_CONVERT;
	case VK_NONCONVERT: return VKEY_NONCONVERT;
	case VK_ACCEPT: return VKEY_ACCEPT;
	case VK_MODECHANGE: return VKEY_MODECHANGE;
	case VK_SPACE: return VKEY_SPACE;
	case VK_PRIOR: return VKEY_PRIOR;
	case VK_NEXT: return VKEY_NEXT;
	case VK_END: return VKEY_END;
	case VK_HOME: return VKEY_HOME;
	case VK_LEFT: return VKEY_LEFT;
	case VK_UP: return VKEY_UP;
	case VK_RIGHT: return VKEY_RIGHT;
	case VK_DOWN: return VKEY_DOWN;
	case VK_SELECT: return VKEY_SELECT;
	case VK_PRINT: return VKEY_PRINT;
	case VK_EXECUTE: return VKEY_EXECUTE;
	case VK_SNAPSHOT: return VKEY_SNAPSHOT;
	case VK_INSERT: return VKEY_INSERT;
	case VK_DELETE: return VKEY_DELETE;
	case VK_HELP: return VKEY_HELP;
	case VK_LWIN: return VKEY_LWIN;
	case VK_RWIN: return VKEY_RWIN;
	case VK_APPS: return VKEY_APPS;
	case VK_SLEEP: return VKEY_SLEEP;
	case VK_NUMPAD0: return VKEY_NUMPAD0;
	case VK_NUMPAD1: return VKEY_NUMPAD1;
	case VK_NUMPAD2: return VKEY_NUMPAD2;
	case VK_NUMPAD3: return VKEY_NUMPAD3;
	case VK_NUMPAD4: return VKEY_NUMPAD4;
	case VK_NUMPAD5: return VKEY_NUMPAD5;
	case VK_NUMPAD6: return VKEY_NUMPAD6;
	case VK_NUMPAD7: return VKEY_NUMPAD7;
	case VK_NUMPAD8: return VKEY_NUMPAD8;
	case VK_NUMPAD9: return VKEY_NUMPAD9;
	case VK_MULTIPLY: return VKEY_MULTIPLY;
	case VK_ADD: return VKEY_ADD;
	case VK_SEPARATOR: return VKEY_SEPARATOR;
	case VK_SUBTRACT: return VKEY_SUBTRACT;
	case VK_DECIMAL: return VKEY_DECIMAL;
	case VK_DIVIDE: return VKEY_DIVIDE;
	case VK_F1: return VKEY_F1;
	case VK_F2: return VKEY_F2;
	case VK_F3: return VKEY_F3;
	case VK_F4: return VKEY_F4;
	case VK_F5: return VKEY_F5;
	case VK_F6: return VKEY_F6;
	case VK_F7: return VKEY_F7;
	case VK_F8: return VKEY_F8;
	case VK_F9: return VKEY_F9;
	case VK_F10: return VKEY_F10;
	case VK_F11: return VKEY_F11;
	case VK_F12: return VKEY_F12;
	case VK_F13: return VKEY_F13;
	case VK_F14: return VKEY_F14;
	case VK_F15: return VKEY_F15;
	case VK_F16: return VKEY_F16;
	case VK_F17: return VKEY_F17;
	case VK_F18: return VKEY_F18;
	case VK_F19: return VKEY_F19;
	case VK_F20: return VKEY_F20;
	case VK_F21: return VKEY_F21;
	case VK_F22: return VKEY_F22;
	case VK_F23: return VKEY_F23;
	case VK_F24: return VKEY_F24;
	case VK_NUMLOCK: return VKEY_NUMLOCK;
	case VK_SCROLL: return VKEY_SCROLL;
	case VK_LSHIFT: return VKEY_LSHIFT;
	case VK_RSHIFT: return VKEY_RSHIFT;
	case VK_LCONTROL: return VKEY_LCONTROL;
	case VK_RCONTROL: return VKEY_RCONTROL;
	case VK_LMENU: return VKEY_LMENU;
	case VK_RMENU: return VKEY_RMENU;
	case VK_BROWSER_BACK: return VKEY_BROWSER_BACK;
	case VK_BROWSER_FORWARD: return VKEY_BROWSER_FORWARD;
	case VK_BROWSER_REFRESH: return VKEY_BROWSER_REFRESH;
	case VK_BROWSER_STOP: return VKEY_BROWSER_STOP;
	case VK_BROWSER_SEARCH: return VKEY_BROWSER_SEARCH;
	case VK_BROWSER_FAVORITES: return VKEY_BROWSER_FAVORITES;
	case VK_BROWSER_HOME: return VKEY_BROWSER_HOME;
	case VK_VOLUME_MUTE: return VKEY_VOLUME_MUTE;
	case VK_VOLUME_DOWN: return VKEY_VOLUME_DOWN;
	case VK_VOLUME_UP: return VKEY_VOLUME_UP;
	case VK_MEDIA_NEXT_TRACK: return VKEY_MEDIA_NEXT_TRACK;
	case VK_MEDIA_PREV_TRACK: return VKEY_MEDIA_PREV_TRACK;
	case VK_MEDIA_STOP: return VKEY_MEDIA_STOP;
	case VK_MEDIA_PLAY_PAUSE: return VKEY_MEDIA_PLAY_PAUSE;
	case VK_LAUNCH_MAIL: return VKEY_LAUNCH_MAIL;
	case VK_LAUNCH_MEDIA_SELECT: return VKEY_LAUNCH_MEDIA_SELECT;
	case VK_LAUNCH_APP1: return VKEY_LAUNCH_APP1;
	case VK_LAUNCH_APP2: return VKEY_LAUNCH_APP2;
	case VK_OEM_1: return VKEY_OEM_1;
	case VK_OEM_PLUS: return VKEY_OEM_PLUS;
	case VK_OEM_COMMA: return VKEY_OEM_COMMA;
	case VK_OEM_MINUS: return VKEY_OEM_MINUS;
	case VK_OEM_PERIOD: return VKEY_OEM_PERIOD;
	case VK_OEM_2: return VKEY_OEM_2;
	case VK_OEM_3: return VKEY_OEM_3;
	case VK_OEM_4: return VKEY_OEM_4;
	case VK_OEM_5: return VKEY_OEM_5;
	case VK_OEM_6: return VKEY_OEM_6;
	case VK_OEM_7: return VKEY_OEM_7;
	case VK_OEM_8: return VKEY_OEM_8;
	case VK_OEM_102: return VKEY_OEM_102;
	case VK_PROCESSKEY: return VKEY_PROCESSKEY;
	//case VK_PACKE: return VKEY_PACKE;
	case VK_ATTN: return VKEY_ATTN;
	case VK_CRSEL: return VKEY_CRSEL;
	case VK_EXSEL: return VKEY_EXSEL;
	case VK_EREOF: return VKEY_EREOF;
	case VK_PLAY: return VKEY_PLAY;
	case VK_ZOOM: return VKEY_ZOOM;
	case VK_NONAME: return VKEY_NONAME;
	case VK_PA1: return VKEY_PA1;
	case VK_OEM_CLEAR: return VKEY_OEM_CLEAR;
	default:
		return wp;

	}
}

static LRESULT CALLBACK UIWindowProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam) {
	switch (msg) {
	case WM_CREATE: {
		BITMAPINFO bmi;
		ZeroMemory(&bmi, sizeof(bmi));
		bmi.bmiHeader.biWidth = VIDEO_WIDTH_PIXEL * SCALE;
		bmi.bmiHeader.biHeight = -VIDEO_HEIGHT_PIXEL * SCALE;
		bmi.bmiHeader.biBitCount = 32;
		bmi.bmiHeader.biPlanes = 1;
		bmi.bmiHeader.biSize = sizeof(bmi.bmiHeader);
		bmi.bmiHeader.biSizeImage = bmi.bmiHeader.biWidth * abs(bmi.bmiHeader.biHeight) * bmi.bmiHeader.biBitCount / 8;

		hBackBuffer = CreateDIBSection(0, &bmi, DIB_RGB_COLORS, &lpBackBufferPixels, NULL, 0);
		if (hBackBuffer == NULL) {
			return 0;
		}
		hdcSrc = CreateCompatibleDC(NULL);
		if (hdcSrc == NULL) {
			return 0;
		}
		hdcOld = SelectObject(hdcSrc, hBackBuffer);
		if (hdcOld == NULL) {
			return 0;
		}
		memset(lpBackBufferPixels, 0x00, bmi.bmiHeader.biSizeImage);

		back_buffer = lpBackBufferPixels;
		return 0;
	}
	case WM_DESTROY: {
		back_buffer = NULL;
		if ((hdcOld != NULL) && (hBackBuffer != NULL)) {
			SelectObject((HDC)hdcOld, hBackBuffer);
			hdcOld = NULL;
		}
		if (hdcSrc != NULL) {
			DeleteDC(hdcSrc);
			hdcSrc = NULL;
		}
		if (hBackBuffer != NULL) {
			DeleteObject(hBackBuffer);
			hBackBuffer = NULL;
		}

		PostQuitMessage(0);
		return 0;
	}
	case WM_PAINT: {
		PAINTSTRUCT ps;
		HDC hdc = BeginPaint(hwnd, &ps);
		if (hdc != NULL) {
			RECT rt;
			if (GetClientRect(hwnd, &rt) != 0) {
				BitBlt(hdc, 0, 0, VIDEO_WIDTH_PIXEL*SCALE, VIDEO_HEIGHT_PIXEL*SCALE, hdcSrc, 0, 0, SRCCOPY);
			}
			EndPaint(hwnd, &ps);
		}
		return 0;
	}
	case WM_LBUTTONDOWN:
		PostMessage(hwnd, WM_NCLBUTTONDOWN, HTCAPTION, lParam);
		return 0;
	case WM_KEYDOWN:
		if (!(lParam & (1 << 30))) {
			Keyboard_PushKeyStatus(wkey2vkey(wParam), 0);
		}
		return 0;
	case WM_KEYUP:
		Keyboard_PushKeyStatus(wkey2vkey(wParam), 1);
		return 0;
	default:
		return (DefWindowProc(hwnd, msg, wParam, lParam));
	}
	return DefWindowProc(hwnd, msg, wParam, lParam);
}

static unsigned int __stdcall UiThreadProc(void* arg) {
	// 描画用のウィンドウを作成
	WNDCLASSEX  wc;
	ZeroMemory(&wc, sizeof wc);
	wc.cbSize = sizeof(WNDCLASSEX);
	wc.lpfnWndProc = UIWindowProc;
	wc.hInstance = GetModuleHandle(0);
	wc.hIcon = LoadIcon(NULL, IDI_APPLICATION);
	wc.hCursor = LoadCursor(NULL, IDC_ARROW);
	wc.hbrBackground = (HBRUSH)GetStockObject(WHITE_BRUSH);
	wc.lpszMenuName = NULL;
	wc.lpszClassName = CLASSNAME;
	if (RegisterClassEx(&wc) == 0) {
		MessageBox(0, _T("ウィンドウクラスの登録に失敗しました。"), _T("RegisterClassEx失敗"), MB_OK | MB_ICONERROR);
		return 0;
	}

	// ウィンドウサイズを指定せずにウィンドウを生成
	hVideoWindow = CreateWindowEx(
		WS_EX_LEFT,
		CLASSNAME,
		CLASSNAME,
		WS_POPUP | WS_CAPTION,
		CW_USEDEFAULT,
		CW_USEDEFAULT,
		CW_USEDEFAULT,
		CW_USEDEFAULT,
		NULL,
		NULL,
		GetModuleHandle(0),
		NULL);
	if (hVideoWindow == NULL) {
		MessageBox(0, _T("ウィンドウの作成に失敗しました。"), _T("CreateWindowEx失敗"), MB_OK | MB_ICONERROR);
		return 0;
	}

	// ウィンドウスタイルを取得
	DWORD dwStyle = (DWORD)GetWindowLongPtr(hVideoWindow, GWL_STYLE);
	if (dwStyle == 0) {
		MessageBox(0, _T("ウィンドウスタイルの取得に失敗しました。"), _T("GetWindowLongPtr失敗"), MB_OK | MB_ICONERROR);
		return 0;
	}

	// 拡張ウィンドウスタイルを取得
	DWORD dwExStyle = (DWORD)GetWindowLongPtr(hVideoWindow, GWL_EXSTYLE);
	if (dwExStyle == 0) {
		MessageBox(0, _T("拡張ウィンドウスタイルの取得に失敗しました。"), _T("GetWindowLongPtr失敗"), MB_OK | MB_ICONERROR);
		return 0;
	}

	//	dwStyle &= ~(WS_THICKFRAME | WS_CAPTION);	// 縁成分を除去
	//	dwExStyle &= ~(WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE);	// 縁成分を除去

	(void)SetWindowLongPtr(hVideoWindow, GWL_STYLE, (LONG_PTR)0);	// 最終エラー情報をクリア
	if (SetWindowLongPtr(hVideoWindow, GWL_STYLE, (LONG_PTR)dwStyle) == 0) {
		if (GetLastError() != 0) {
			MessageBox(0, _T("ウィンドウスタイルの変更に失敗しました。"), _T("SetWindowLongPtr失敗"), MB_OK | MB_ICONERROR);
			//				return false;
		}
	}

	(void)SetWindowLongPtr(hVideoWindow, GWL_EXSTYLE, (LONG_PTR)0);	// 最終エラー情報をクリア
	if (SetWindowLongPtr(hVideoWindow, GWL_EXSTYLE, (LONG_PTR)dwExStyle) == 0) {
		if (GetLastError() != 0) {
			MessageBox(0, _T("拡張ウィンドウスタイルの変更に失敗しました。"), _T("SetWindowLongPtr失敗"), MB_OK | MB_ICONERROR);
			return 0;
		}
	}

	// 変更後のウィンドウスタイルを使ってクライアント領域の大きさからウィンドウサイズを算出
	RECT r = { 0, 0, VIDEO_WIDTH_PIXEL*SCALE, VIDEO_HEIGHT_PIXEL*SCALE };
	AdjustWindowRectEx(&r, dwStyle, FALSE, dwExStyle);

	// ウィンドウサイズを変更
	SetWindowPos(hVideoWindow, NULL, 0, 0, r.right - r.left, r.bottom - r.top, SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOZORDER);

	ShowWindow(hVideoWindow, SW_SHOW);
	if (UpdateWindow(hVideoWindow) == 0) {
		MessageBox(0, _T("ウィンドウの更新要求発行に失敗しました。"), _T("UpdateWindow失敗"), MB_OK | MB_ICONERROR);
		return 0;
	}

	ShowWindow(hVideoWindow, SW_NORMAL);
	UpdateWindow(hVideoWindow);

	MSG msg;
	while (GetMessage(&msg, NULL, 0, 0) > 0) {
		TranslateMessage(&msg);
		DispatchMessage(&msg);
	}
	return 1;
}

void Redraw(void) {
	InvalidateRect(hVideoWindow, 0, FALSE);
}

/* ピクセル操作 */
static void set_pixel(uint16_t x, uint16_t y, uint32_t c) {
	int yy = (int)y * SCALE;
	for (int i = 0; i < SCALE; i++) {
		if (yy >= VIDEO_HEIGHT_PIXEL*SCALE) { break; }
		int xx = (int)x * SCALE;
		for (int j = 0; j < SCALE; j++) {
			if (xx >= VIDEO_WIDTH_PIXEL*SCALE) { break; }
			((uint32_t*)back_buffer)[xx + (yy*VIDEO_WIDTH_PIXEL*SCALE)] = c;
			xx++;
		}
		yy++;
	}
}


static void Update(void) {
	/**
	* ピクセル上の出力行番号を求める
	*/
	if (back_buffer == NULL) {
		return;
	}

	const text_vram_pixel_t* cursor = TextVRAM.GetCursorPtr();
	text_vram_pixel_t* tvram  = TextVRAM.GetVramPtr();
	const color_t *ClTable = TextVRAM.GetPalettePtr();

	for (int y = 0; y < WIDTH_Y; y++) {
		text_vram_pixel_t *line = &tvram[y*VWIDTH_X];

		for (int x = 0; x < VWIDTH_X;) {
			uint16_t id = Uni2Id(line[x].ch).id;
			int sz = 1;
			color_t c = ClTable[line[x].color];
			color_t b = ClTable[line[x].bgcolor];
			unsigned int inv = (&line[x] == cursor) ? 0x00FFFFFF : 0x00000000;
			if (id == 0xFFFFU) {
				for (int j = 0; j < FONT_HEIGHT; j++) {
					for (int i = 0; i < FONT_WIDTH/2; i++) {
					set_pixel(x*FONT_WIDTH / 2 + i, y*FONT_HEIGHT + j, b.bgra ^ inv);
					}
				}
			}else {
					for (int j = 0; j < FONT_HEIGHT; j++) {
					const uint8_t *p = FontData[id];
					sz = p[0];
					for (int i = 0; i < FONT_WIDTH/2* sz; i++) {
						set_pixel(x*FONT_WIDTH / 2 + i, y*FONT_HEIGHT + j, ((p[i / 8 + j * 2 + 1] >> (i % 8)) & 0x01 ? c.bgra : b.bgra) ^ inv);
					}
				}
			}
			x += sz;
		}
	}
	Redraw();
}

static unsigned int __stdcall VideoThreadProc(void* arg) {
	for (;;) {
		if (TryEnterCriticalSection(&cs)) {
			Update();
			LeaveCriticalSection(&cs);
		}
		Sleep(20);	// 大体50FPS
	}
}

static uintptr_t hVideoThread;

void VideoInit(void) {
	hVideoThread = _beginthreadex(NULL, 0, VideoThreadProc, NULL, 0, NULL);
}

static uintptr_t hUiThread;

extern int main(void);

int WINAPI WinMain(
	_In_ HINSTANCE hInstance,
	_In_opt_ HINSTANCE hPrevInstance,
	_In_ LPSTR lpCmdLine,
	_In_ int nShowCmd) {
	timeBeginPeriod(1);
	InitializeCriticalSection(&cs);
	hUiThread = _beginthreadex(NULL, 0, UiThreadProc, NULL, 0, NULL);
	VideoInit();
	texteditor();
	DeleteCriticalSection(&cs);
	return 0;
}
