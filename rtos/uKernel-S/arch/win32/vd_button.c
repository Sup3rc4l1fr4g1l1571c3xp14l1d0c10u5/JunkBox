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
 * @brief	Win32���ł̃{�^���̉��z�f�o�C�X
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	�쐬
 */

#include "./arch.h"
#include "./virtualhw.h"
#include "./vd_kerneltimer.h"
#include "../../src/kernel.h"
#include <Windows.h>
#include <MMSystem.h>
#pragma comment(lib,"winmm.lib")

HWND		g_hwnd;

/* �^�b�`�X�N���[���̃^�b�`�ʒu��ێ����郌�W�X�^���ǂ� */
volatile int touch_x;
volatile int touch_y;

typedef struct {
	int width, height;
	LPBYTE pixels;
} Image;

/* DIB�p�ϐ� */
Image dib;
LPDWORD lpdwPixel;
HBITMAP hbmpBMP, hbmpOld;
HDC hdcBMP;

/* DIB����֐� */
bool_t CreateFrameBuffer(HWND hwnd) {
	BITMAPINFO biDIB;
	HDC hdc;
	
	/* DIB�pBITMAPINFO���N���A */
	ZeroMemory(&biDIB, sizeof(biDIB));

	/* DIB�pBITMAPINFO�ݒ� */
	biDIB.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
	biDIB.bmiHeader.biWidth = 240;
	biDIB.bmiHeader.biHeight = -320;
	biDIB.bmiHeader.biPlanes = 1;
	biDIB.bmiHeader.biBitCount = 32;
	biDIB.bmiHeader.biCompression = BI_RGB;

	/* DIBSection�쐬 */
	hbmpBMP = CreateDIBSection(NULL, &biDIB, DIB_RGB_COLORS, (void **)(&lpdwPixel), NULL, 0);

	/* �E�C���h�E��HDC�擾 */
	hdc = GetDC(hwnd);

	/* DIBSection�p�������f�o�C�X�R���e�L�X�g�쐬 */
	hdcBMP = CreateCompatibleDC(hdc);

	/* �E�C���h�E��HDC��� */
	ReleaseDC(hwnd, hdc);

	/* DIBSection��HBITMAP���������f�o�C�X�R���e�L�X�g�ɑI�� */
	hbmpOld = (HBITMAP)SelectObject(hdcBMP, hbmpBMP);

	dib.width  = 240;
	dib.height = 320;
	dib.pixels = (LPBYTE)lpdwPixel;

	return TRUE;
}

void DestroyFrameBuffer() {
	/* DIBSection���������f�o�C�X�R���e�L�X�g�̑I������O�� */
	SelectObject(hdcBMP, hbmpOld);
	/* DIBSection���폜 */
	DeleteObject(hbmpBMP);
	/* �������f�o�C�X�R���e�L�X�g���폜 */
	DeleteDC(hdcBMP);
}

/* �摜�ǂݍ��� */
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

	hBuf = GlobalAlloc(GPTR,GetFileSize(fh,NULL)); // �o�b�t�@�m��
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
 * @brief	�^�b�`�X�N���[���̃E�B���h�E�v���V�[�W��
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	�쐬
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
			// �^�b�`�ʒu���擾����
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
			// �^�b�`�ʒu���擾����
			touch_x = LOWORD( lParam );
			touch_y = HIWORD( lParam );
			still_down = FALSE;
			VHW_onInterrupt(INTID_BUTTON_UP);
			return 0;
		}
		case WM_PAINT: {
			PAINTSTRUCT ps;
			HDC hdc = BeginPaint(hwnd,&ps);
			/* DIBSection��\�� */
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
 * @brief	�^�b�`�X�N���[���̃��C������
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
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
 * @brief	�^�b�`�X�N���[�����荞�݃n���h��
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */
void NAKED ButtonDownInterruptServiceRoutine(void) {
	/* ���̎��_�ł͂ǂ̃R���e�L�X�g���g���Ă��邩�s���Ȃ̂ŃX�^�b�N���g���Ȃ� */

	/* ���݂̃^�X�N�̃R���e�L�X�g��ۑ� */
	SAVE_CONTEXT();

	/* �J�[�l���X�^�b�N�ɐ؂�ւ� */
	SET_KERNEL_STACKPOINTER();

	/* �ȍ~�̓J�[�l���X�^�b�N�̗��p���\�ɂȂ����̂ŁA�֐��Ăяo���Ȃǂ��\�ƂȂ� */
	ExtIntID = INTID_BUTTON_DOWN;

	external_interrupt_handler();
}

/**
 * @brief	�^�b�`�X�N���[�����荞�݃n���h��
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */
void NAKED ButtonUpInterruptServiceRoutine(void) {
	/* ���̎��_�ł͂ǂ̃R���e�L�X�g���g���Ă��邩�s���Ȃ̂ŃX�^�b�N���g���Ȃ� */

	/* ���݂̃^�X�N�̃R���e�L�X�g��ۑ� */
	SAVE_CONTEXT();

	/* �J�[�l���X�^�b�N�ɐ؂�ւ� */
	SET_KERNEL_STACKPOINTER();

	/* �ȍ~�̓J�[�l���X�^�b�N�̗��p���\�ɂȂ����̂ŁA�֐��Ăяo���Ȃǂ��\�ƂȂ� */
	ExtIntID = INTID_BUTTON_UP;

	external_interrupt_handler();
}

/**
 * @brief	�^�b�`�X�N���[�����荞�݃n���h��
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */
void NAKED ButtonDragInterruptServiceRoutine(void) {
	/* ���̎��_�ł͂ǂ̃R���e�L�X�g���g���Ă��邩�s���Ȃ̂ŃX�^�b�N���g���Ȃ� */

	/* ���݂̃^�X�N�̃R���e�L�X�g��ۑ� */
	SAVE_CONTEXT();

	/* �J�[�l���X�^�b�N�ɐ؂�ւ� */
	SET_KERNEL_STACKPOINTER();

	/* �ȍ~�̓J�[�l���X�^�b�N�̗��p���\�ɂȂ����̂ŁA�֐��Ăяo���Ȃǂ��\�ƂȂ� */
	ExtIntID = INTID_BUTTON_DRAG;

	external_interrupt_handler();
}

/**
 * @brief	�^�b�`�X�N���[���̏�����
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 * 
 * �{�^���̉��z�f�o�C�X�Ɗ��荞�݃��[�`����ݒ�
 */
void initButtonDevice(void) {
	/* �^�b�`�X�N���[���̉��z�f�o�C�X�Ƃ��ăC���X�g�[�� */
	virtualdevice_t vd;
	vd.name = "TouchScreen";
	vd.vdsr = ButtonProc;

	VHW_installVirtualDevice(&vd);
	/* INTID_BUTTON�C�x���g�̊��荞�݃��[�`�����w�� */
	VHW_setInterruputServiceRoutine(INTID_BUTTON_DOWN, ButtonDownInterruptServiceRoutine); 
	VHW_setInterruputServiceRoutine(INTID_BUTTON_UP  , ButtonUpInterruptServiceRoutine); 
	VHW_setInterruputServiceRoutine(INTID_BUTTON_DRAG, ButtonDragInterruptServiceRoutine); 
}

