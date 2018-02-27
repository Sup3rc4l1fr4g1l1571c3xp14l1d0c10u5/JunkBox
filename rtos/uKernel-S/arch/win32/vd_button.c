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
 * @brief	Win32環境でのボタンの仮想デバイス
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	作成
 */

#include "./arch.h"
#include "./virtualhw.h"
#include "./vd_kerneltimer.h"
#include "../../src/kernel.h"
#include <Windows.h>
#include <MMSystem.h>
#pragma comment(lib,"winmm.lib")

HWND		g_hwnd;

/* タッチスクリーンのタッチ位置を保持するレジスタもどき */
volatile int touch_x;
volatile int touch_y;

typedef struct {
	int width, height;
	LPBYTE pixels;
} Image;

/* DIB用変数 */
Image dib;
LPDWORD lpdwPixel;
HBITMAP hbmpBMP, hbmpOld;
HDC hdcBMP;

/* DIB操作関数 */
bool_t CreateFrameBuffer(HWND hwnd) {
	BITMAPINFO biDIB;
	HDC hdc;
	
	/* DIB用BITMAPINFOをクリア */
	ZeroMemory(&biDIB, sizeof(biDIB));

	/* DIB用BITMAPINFO設定 */
	biDIB.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
	biDIB.bmiHeader.biWidth = 240;
	biDIB.bmiHeader.biHeight = -320;
	biDIB.bmiHeader.biPlanes = 1;
	biDIB.bmiHeader.biBitCount = 32;
	biDIB.bmiHeader.biCompression = BI_RGB;

	/* DIBSection作成 */
	hbmpBMP = CreateDIBSection(NULL, &biDIB, DIB_RGB_COLORS, (void **)(&lpdwPixel), NULL, 0);

	/* ウインドウのHDC取得 */
	hdc = GetDC(hwnd);

	/* DIBSection用メモリデバイスコンテキスト作成 */
	hdcBMP = CreateCompatibleDC(hdc);

	/* ウインドウのHDC解放 */
	ReleaseDC(hwnd, hdc);

	/* DIBSectionのHBITMAPをメモリデバイスコンテキストに選択 */
	hbmpOld = (HBITMAP)SelectObject(hdcBMP, hbmpBMP);

	dib.width  = 240;
	dib.height = 320;
	dib.pixels = (LPBYTE)lpdwPixel;

	return TRUE;
}

void DestroyFrameBuffer() {
	/* DIBSectionをメモリデバイスコンテキストの選択から外す */
	SelectObject(hdcBMP, hbmpOld);
	/* DIBSectionを削除 */
	DeleteObject(hbmpBMP);
	/* メモリデバイスコンテキストを削除 */
	DeleteDC(hdcBMP);
}

/* 画像読み込み */
bool_t LoadBMP( Image *img, const char *path ) {
	HANDLE fh;
	HGLOBAL hBuf;
	LPBYTE lpBuf;
	DWORD dummy;
	LPBITMAPFILEHEADER lpfh;
	LPBITMAPINFO lpInfo;
	DWORD offset;
	LPBYTE lpBMP;
	int i, j;
	bool_t ret;

	fh = CreateFile(path,GENERIC_READ,0,NULL,OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL,NULL);

	hBuf = GlobalAlloc(GPTR,GetFileSize(fh,NULL)); // バッファ確保
	if (hBuf == NULL) {
		return FALSE;
	}
	lpBuf = (LPBYTE)GlobalLock(hBuf);
	if (lpBuf == NULL) {
		return FALSE;
	}
	if (ReadFile(fh,lpBuf,GetFileSize(fh,NULL),&dummy,NULL) == FALSE) {
		return FALSE;
	}
	lpfh = (LPBITMAPFILEHEADER)lpBuf;
	lpInfo = (LPBITMAPINFO)(lpBuf+sizeof(BITMAPFILEHEADER));
	offset = *(LPDWORD)(lpBuf+10);
	lpBMP = lpBuf+offset;
	CloseHandle(fh);

	ret = FALSE;
	if ((lpBuf[0] == 'B') && (lpBuf[1] == 'M') && (lpInfo->bmiHeader.biBitCount == 24)) {
		img->width  = lpInfo->bmiHeader.biWidth;
		img->height = lpInfo->bmiHeader.biHeight;
		img->pixels = (LPBYTE)malloc(img->width * img->height * 4);
		for (j=0; j<img->height; j++) {
			for (i=0; i<img->width; i++) {
				img->pixels[(i*4+0)+((319-j)*240*4)] = lpBMP[(i*3+0)+(j*240*3)];
				img->pixels[(i*4+1)+((319-j)*240*4)] = lpBMP[(i*3+1)+(j*240*3)];
				img->pixels[(i*4+2)+((319-j)*240*4)] = lpBMP[(i*3+2)+(j*240*3)];
				img->pixels[(i*4+3)+((319-j)*240*4)] = 0x00;
			}
		}
		ret = TRUE;
	}
	GlobalUnlock(lpBuf);
	GlobalFree(hBuf);
	return ret;

}

Image normal, touched, number, idle, talking, talkend;
#define Range(n,s,e) (((s) <= (n)) && ((n) < (e)))

bool_t ImgGetPixel(Image *img, int x, int y, DWORD *color) {
	if (Range(x,0,img->width)&&Range(y,0,img->height)) {
		*color = ((LPDWORD)img->pixels)[y * img->width + x];
		return TRUE;
	} else {
		return FALSE;
	}
}
bool_t ImgSetPixel(Image *img, int x, int y, DWORD color) {
	if (Range(x,0,img->width)&&Range(y,0,img->height)) {
		((LPDWORD)img->pixels)[y * img->width + x] = (color);
		return TRUE;
	} else {
		return FALSE;
	}
}

void ImgBitBlt(Image *dest, Image *src, int x, int y, const RECT *rect) {
	int i, j, width, height;
	DWORD color;
	
	width  = rect->right - rect->left;
	height = rect->bottom - rect->top;
	for (j=0; j<height; j++) {
		for (i=0; i<width; i++) {
			if (ImgGetPixel(src,rect->left+i,rect->top+j,&color) == TRUE) {
				ImgSetPixel(dest,x+i,y+j,color);
			}
		}
	}
}

void UpdateScreen(void) {
	InvalidateRect( g_hwnd, NULL, FALSE );
}

/**
 * @brief	タッチスクリーンのウィンドウプロシージャ
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	作成
 * 
 */
static LRESULT CALLBACK WindowProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam) {
	static bool_t still_down = FALSE;
	switch (uMsg) {
		case WM_CREATE:
			return 0;
		case WM_DESTROY:
			DestroyFrameBuffer();
			PostQuitMessage(0);
			return 0;
		case WM_LBUTTONDOWN: {
			int id, n;
			// タッチ位置を取得する
			touch_x = LOWORD( lParam );
			touch_y = HIWORD( lParam );
			still_down = TRUE;
			VHW_onInterrupt(INTID_BUTTON_DOWN);
			return 0;
		}
		case WM_MOUSEMOVE: {
			if (still_down == TRUE) {
				touch_x = LOWORD( lParam );
				touch_y = HIWORD( lParam );
				VHW_onInterrupt(INTID_BUTTON_DRAG);
			}
			return 0;
		}
		case WM_LBUTTONUP: {
			int id, n;
			// タッチ位置を取得する
			touch_x = LOWORD( lParam );
			touch_y = HIWORD( lParam );
			still_down = FALSE;
			VHW_onInterrupt(INTID_BUTTON_UP);
			return 0;
		}
		case WM_PAINT: {
			PAINTSTRUCT ps;
			HDC hdc = BeginPaint(hwnd,&ps);
			/* DIBSectionを表示 */
			BitBlt(hdc, 0, 0, 240, 320, hdcBMP, 0, 0, SRCCOPY);
			EndPaint(hwnd, &ps);
			return 0;
		}
		default:
			break;
	}

	return DefWindowProc(hwnd, uMsg, wParam, lParam);
}

/**
 * @brief	タッチスクリーンのメイン処理
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */
static void ButtonProc(void) {
	TCHAR		szAppName[] = TEXT("sample");
	MSG			msg;
	WNDCLASSEX	wc;
	RECT		client_rect;
	HINSTANCE hinst = GetModuleHandle(NULL);

	wc.cbSize        = sizeof(WNDCLASSEX);
	wc.style         = 0;
	wc.lpfnWndProc   = WindowProc;
	wc.cbClsExtra    = 0;
	wc.cbWndExtra    = 0;
	wc.hInstance     = hinst;
	wc.hIcon         = (HICON)LoadImage(NULL, IDI_APPLICATION, IMAGE_ICON, 0, 0, LR_SHARED);
	wc.hCursor       = (HCURSOR)LoadImage(NULL, IDC_ARROW, IMAGE_CURSOR, 0, 0, LR_SHARED);
	wc.hbrBackground = (HBRUSH)GetStockObject(WHITE_BRUSH);
	wc.lpszMenuName  = NULL;
	wc.lpszClassName = szAppName;
	wc.hIconSm       = (HICON)LoadImage(NULL, IDI_APPLICATION, IMAGE_ICON, 0, 0, LR_SHARED);
	
	if (RegisterClassEx(&wc) == 0)
		return;

	client_rect.left = client_rect.top = 0;
	client_rect.right = 240;
	client_rect.bottom = 320;
	AdjustWindowRectEx(&client_rect, WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME, FALSE, WS_EX_TOPMOST);

	g_hwnd = CreateWindowEx(WS_EX_TOPMOST, szAppName, szAppName, WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME, CW_USEDEFAULT, CW_USEDEFAULT, client_rect.right-client_rect.left, client_rect.bottom-client_rect.top, NULL, NULL, hinst, NULL);
	if (g_hwnd == NULL)
		return;

	CreateFrameBuffer(g_hwnd);
	if (LoadBMP(&normal, TEXT("normal.bmp")) == FALSE) { return; }
	if (LoadBMP(&touched, TEXT("touched.bmp")) == FALSE) { return; }
	if (LoadBMP(&number, TEXT("number.bmp")) == FALSE) { return; }
	if (LoadBMP(&idle, TEXT("idle.bmp")) == FALSE) { return; }
	if (LoadBMP(&talking, TEXT("talking.bmp")) == FALSE) { return; }
	if (LoadBMP(&talkend, TEXT("talkend.bmp")) == FALSE) { return; }

	ShowWindow(g_hwnd, SW_SHOW);
	UpdateWindow(g_hwnd);
	
	while (GetMessage(&msg, NULL, 0, 0) > 0) {
		TranslateMessage(&msg);
		DispatchMessage(&msg);
	}

	return;
}

/**
 * @brief	タッチスクリーン割り込みハンドラ
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */
void NAKED ButtonDownInterruptServiceRoutine(void) {
	/* この時点ではどのコンテキストを使っているか不明なのでスタックを使えない */

	/* 現在のタスクのコンテキストを保存 */
	SAVE_CONTEXT();

	/* カーネルスタックに切り替え */
	SET_KERNEL_STACKPOINTER();

	/* 以降はカーネルスタックの利用が可能になったので、関数呼び出しなどが可能となる */
	ExtIntID = INTID_BUTTON_DOWN;

	external_interrupt_handler();
}

/**
 * @brief	タッチスクリーン割り込みハンドラ
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */
void NAKED ButtonUpInterruptServiceRoutine(void) {
	/* この時点ではどのコンテキストを使っているか不明なのでスタックを使えない */

	/* 現在のタスクのコンテキストを保存 */
	SAVE_CONTEXT();

	/* カーネルスタックに切り替え */
	SET_KERNEL_STACKPOINTER();

	/* 以降はカーネルスタックの利用が可能になったので、関数呼び出しなどが可能となる */
	ExtIntID = INTID_BUTTON_UP;

	external_interrupt_handler();
}

/**
 * @brief	タッチスクリーン割り込みハンドラ
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */
void NAKED ButtonDragInterruptServiceRoutine(void) {
	/* この時点ではどのコンテキストを使っているか不明なのでスタックを使えない */

	/* 現在のタスクのコンテキストを保存 */
	SAVE_CONTEXT();

	/* カーネルスタックに切り替え */
	SET_KERNEL_STACKPOINTER();

	/* 以降はカーネルスタックの利用が可能になったので、関数呼び出しなどが可能となる */
	ExtIntID = INTID_BUTTON_DRAG;

	external_interrupt_handler();
}

/**
 * @brief	タッチスクリーンの初期化
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 * 
 * ボタンの仮想デバイスと割り込みルーチンを設定
 */
void initButtonDevice(void) {
	/* タッチスクリーンの仮想デバイスとしてインストール */
	virtualdevice_t vd;
	vd.name = "TouchScreen";
	vd.vdsr = ButtonProc;

	VHW_installVirtualDevice(&vd);
	/* INTID_BUTTONイベントの割り込みルーチンを指定 */
	VHW_setInterruputServiceRoutine(INTID_BUTTON_DOWN, ButtonDownInterruptServiceRoutine); 
	VHW_setInterruputServiceRoutine(INTID_BUTTON_UP  , ButtonUpInterruptServiceRoutine); 
	VHW_setInterruputServiceRoutine(INTID_BUTTON_DRAG, ButtonDragInterruptServiceRoutine); 
}

