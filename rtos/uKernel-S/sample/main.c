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
 * @brief   テストケース兼サンプルプログラム
 * @author  whelp
 * @date    2010/09/02 07:21:22
 */

#include "../src/kernel.h"
#include "../src/syscall.h"

#if defined(TARGET_ARCH)
#if TARGET_ARCH == ARCH_AVR
#include <avr/io.h>
#include <avr/interrupt.h>
#include <util/delay.h>
#include <avr/pgmspace.h>
#define couts(x) {\
	char *p = x;\
	disableInterrupt();\
	for (p = x; *p != '\0'; p++) { PORTB = *p; }\
	enableInterrupt();\
}
#elif TARGET_ARCH == ARCH_WIN32
#include <stdio.h>
#define couts(x) {		\
	disableInterrupt();	\
	puts(x);			\
	enableInterrupt();	\
}
#elif (TARGET_ARCH == ARCH_M16C_TBIRD)
#define DEBUG_PORT (*(volatile uint8_t*)0x4000)
#define couts(x) {\
	const char *p = x;\
	disableInterrupt();\
	for (p = x; *p != '\0'; p++) { DEBUG_PORT = *p; }\
	enableInterrupt();\
}
#elif (TARGET_ARCH == ARCH_LPC1343)
#define couts(x) puts(x)
#elif (TARGET_ARCH == ARCH_RX63N)

unsigned long PARM;
unsigned char IO_BUF;

#pragma inline_asm charput
void charput(char ch) {
	MOV.L       #_IO_BUF,R2     ; 出力バッファ先頭アドレスをR2に設定
	MOV.B       R1,[R2]         ; R1（出力文字）を出力バッファに設定
	MOV.L       #1220000h,R1    ; PUTCの機能コードをR1に設定
	MOV.L       #_PARM,R3       ; パラメータ・ブロックのアドレスをR3に設定
	MOV.L       R2,[R3]         ; 出力バッファ先頭アドレスを出力バッファに設定
	MOV.L       R3,R2           ; R3（パラメータ・ブロックのアドレス）をR2に設定
	MOV.L       #1000000h,R3      ; システム・コールのアドレスをR3に設定
	JSR         R3              ; システム・コール
}

#define couts(x) {\
	char *p = x;\
	disableInterrupt();\
	for (p = x; *p != '\0'; p++) { charput(*p); }\
	charput('\n'); \
	enableInterrupt();\
}

#else
#error "Unknown Archtecture."
#endif
#else
#error "Architecture undefined."
#endif

extern void TASKPROC init_task(ptr_t arg);
extern void TASKPROC task1(ptr_t arg);
extern void TASKPROC task2(ptr_t arg);
extern void TASKPROC task3(ptr_t arg);
//extern void TASKPROC task4(ptr_t arg);
//extern void TASKPROC task5(ptr_t arg);
//extern void TASKPROC task6(ptr_t arg);
extern void TASKPROC diag_task(ptr_t arg);

#define TESTCASE 0x0107

/**
 * タスクテーブル
 */
const taskproc_t TaskTable[] = {
	init_task,	/** 最高優先度の初期化タスク */
	task1,		/** ユーザータスク１ */
	task2,		/** ユーザータスク２ */
	task3,		/** ユーザータスク３ */
//	task4,		/** ユーザータスク４ */
//	task5,		/** ユーザータスク５ */
//	task6,		/** ユーザータスク５ */
	diag_task	/** 最低優先度のアイドルタスク */
};

#if (TESTCASE == 0x0101)
/**
 * テストケース１－１：二つのタスクの場合（１）
 */
void TASKPROC init_task(ptr_t arg) {
	(void)arg;
	couts("初期化タスク：タスクＡを生成");
	API_startTASK((taskid_t)1,(ptr_t)NULL);
	couts("初期化タスク：タスクＢを生成");
	API_startTASK((taskid_t)2,(ptr_t)NULL);
	couts("初期化タスク：終了");
}

void TASKPROC task1(ptr_t arg) {
	(void)arg;
	couts("タスクＡ：開始");
	couts("タスクＡ：セマフォ獲得を試みる");
	if (API_takeSEMA(1) != SUCCESS) {
		couts("タスクＡ：セマフォ獲得に失敗");
		return;
	} else {
		couts("タスクＡ：セマフォ獲得に成功");
	}

	couts("タスクＡ：スリープに入る");
	API_pauseTASK(10);

	couts("タスクＡ：スリープから復帰");
	couts("タスクＡ：セマフォを開放");
	if (API_giveSEMA(1) != SUCCESS) {
		return;
	}

	couts("タスクＡ：終了");
}

void TASKPROC task2(ptr_t arg) {
	(void)arg;
	couts("タスクＢ：開始");
	couts("タスクＢ：セマフォ獲得を試みるが、\n"
		 "　　　　　タスクＡがセマフォを獲得しているので失敗。\n"
		 "　　　　　セマフォ待ちになる。");
	if (API_takeSEMA(1) != SUCCESS) {
		return;
	}

	couts("タスクＢ：セマフォを獲得して復帰");
	couts("タスクＢ：セマフォを開放");
	if (API_giveSEMA(1) != SUCCESS) {
		return;
	}
	couts("タスクＢ：終了");
}

void TASKPROC task3(ptr_t arg) {
	(void)arg;
}

void TASKPROC diag_task(ptr_t arg) {
	(void)arg;
	for( ;; ) { }
}

#elif (TESTCASE == 0x0102)
/**
 * テストケース１－２：二つのタスクの場合（２）
 */

void TASKPROC init_task(ptr_t arg) {
	(void)arg;
	couts("初期化タスク：タスクＡを生成");
	API_startTASK((taskid_t)1,(ptr_t)NULL);
	couts("初期化タスク：タスクＢを生成");
	API_startTASK((taskid_t)2,(ptr_t)NULL);
	couts("初期化タスク：終了");
}

void TASKPROC task1(ptr_t arg) {
	(void)arg;
	couts("タスクＡ：開始");
	couts("タスクＡ：スリープに入る");
	API_pauseTASK(10);

	couts("タスクＡ：セマフォ獲得を試みるが、\n"
		 "　　　　　タスクＢがセマフォを獲得しているので失敗。\n"
		 "　　　　　セマフォ待ちになる。");
	if (API_takeSEMA(1) != SUCCESS) {
		return;
	}

	couts("タスクＡ：セマフォを獲得して復帰");
	if (API_giveSEMA(1) != SUCCESS) {
		return;
	}

	couts("タスクＡ：終了");
	API_exitTASK();

}

void TASKPROC task2(ptr_t arg) {
	(void)arg;
	couts("タスクＢ：開始");
	couts("タスクＢ：セマフォを獲得する");
	if (API_takeSEMA(1) != SUCCESS) {
		couts("タスクＢ：セマフォの獲得に失敗");
		return;
	}

	couts("タスクＢ：スリープに入る");
	API_pauseTASK(10);

	couts("タスクＢ：スリープから復帰");
	couts("タスクＢ：セマフォを開放");
	if (API_giveSEMA(1) != SUCCESS) {
		couts("タスクＢ：セマフォの解放に失敗");
		return;
	}

	couts("タスクＢ：終了");
	API_exitTASK();
	
}

void TASKPROC task3(ptr_t arg) {
	(void)arg;
}

void TASKPROC diag_task(ptr_t arg) {
	(void)arg;
	for( ;; ) { }
}

#elif (TESTCASE == 0x0103)

/**
 * @brief 最高優先度の初期化タスク
 */
void TASKPROC init_task(ptr_t arg) {
	(void)arg;
	couts("初期化タスク：初期化開始");
	couts("初期化タスク：タスクＡを生成");
	API_startTASK((taskid_t)1,(ptr_t)0x1234);
	couts("初期化タスク：タスクＢを生成");
	API_startTASK((taskid_t)2,NULL);
	couts("初期化タスク：タスクＣを生成");
	API_startTASK((taskid_t)3,NULL);
	couts("初期化タスク：初期化終了");
}

void TASKPROC task1(ptr_t arg) {
	(void)arg;
	for( ;; ) {
		couts("タスクＡ：稼働");
		API_pauseTASK(30);
	}
}

void TASKPROC task2(ptr_t arg) {
	(void)arg;
	for( ;; ) {
		couts("タスクＢ：稼働");
		API_pauseTASK(40);
	}
}

void TASKPROC task3(ptr_t arg) {
	(void)arg;
	for( ;; ) {
		couts("タスクＣ：稼働");
		API_pauseTASK(50);
	}
}

void TASKPROC diag_task(ptr_t arg) {
	(void)arg;
	for( ;; ) { }
}

#elif (TESTCASE == 0x0104)

/**
 * @brief 最高優先度の初期化タスク
 */
void TASKPROC init_task(ptr_t arg) {
	(void)arg;
	couts("初期化タスク：初期化開始");
	couts("初期化タスク：タスクＡを生成");
	API_startTASK((taskid_t)1,(ptr_t)0x1234);
	couts("初期化タスク：タスクＢを生成");
	API_startTASK((taskid_t)2,NULL);
	couts("初期化タスク：タスクＣを生成");
	API_startTASK((taskid_t)3,NULL);
	couts("初期化タスク：初期化終了");
}

void TASKPROC task1(ptr_t arg) {
	(void)arg;
	couts("タスクＡ：開始");
	for( ;; ) {
		/* メッセージを送信 */
		couts("タスクＡ：メッセージを送信（１）");
		API_sendMSG(2, "hello, ");
		API_pauseTASK(10);
		couts("タスクＡ：メッセージを送信（２）");
		API_sendMSG(2, "world! ");
		API_pauseTASK(10);
	}
}

void TASKPROC task2(ptr_t arg) {
	(void)arg;
	couts("タスクＢ：開始");
	for( ;; ) {
		taskid_t id;
		char* data = NULL;

		/* メッセージを受信 */
		couts("タスクＢ：メッセージの受信待ち");
		API_waitMSG();
		if (API_recvMSG(&id, (ptr_t*)&data) == SUCCESS) {
			couts("タスクＢ：メッセージを受信したので表示");
			couts(data);
		}
	}
}

void TASKPROC task3(ptr_t arg) {
	(void)arg;
	couts("タスクＣ：開始");
	for( ;; ) {
		/* 他のタスクと無関係に稼働 */
		couts("タスクＣ：稼働");
		API_pauseTASK(50);
	}
}

void TASKPROC diag_task(ptr_t arg) {
	(void)arg;
	for( ;; ) { }
}

#elif (TESTCASE == 0x0105)

/**
 * @brief 最高優先度の初期化タスク
 */
void TASKPROC init_task(ptr_t arg) {
	(void)arg;
	couts("初期化タスク：初期化開始");
	couts("初期化タスク：タスクＡを生成");
	API_startTASK((taskid_t)1,(ptr_t)0x1234);
	couts("初期化タスク：タスクＢを生成");
	API_startTASK((taskid_t)2,NULL);
	couts("初期化タスク：タスクＣを生成");
	API_startTASK((taskid_t)3,NULL);
	couts("初期化タスク：初期化終了");
}

void TASKPROC task1(ptr_t arg) {
	taskid_t id;
	char* data = NULL;

	(void)arg;
	couts("タスクＡ：開始");
	
	/* メッセージを受信 */
	couts("タスクＡ：メッセージの受信待ち");
	API_waitMSG();
	if (API_recvMSG(&id, (ptr_t*)&data) == SUCCESS) {
		couts("タスクＡ：メッセージを受信したので表示");
		couts(data);
	}
	API_restartTASK(10);
}

void TASKPROC task2(ptr_t arg) {
	(void)arg;
	couts("タスクＡ：開始");
	for( ;; ) {
		/* メッセージを送信 */
		couts("タスクＢ：メッセージを送信（１）");
		API_sendMSG(1, "hello, ");
		API_pauseTASK(10);
		couts("タスクＢ：メッセージを送信（２）");
		API_sendMSG(1, "world! ");
		API_pauseTASK(10);
	}
}


void TASKPROC task3(ptr_t arg) {
	(void)arg;
	couts("タスクＣ：開始");
	/* 他のタスクと無関係に稼働 */
	API_pauseTASK(50);
	couts("タスクＣ：稼働");
	API_restartTASK(50);
}

void TASKPROC diag_task(ptr_t arg) {
	(void)arg;
	for( ;; ) {
	}
}

#elif (TESTCASE == 0x0106)

/**
 * @brief 最高優先度の初期化タスク
 */
void TASKPROC init_task(ptr_t arg) {
	(void)arg;
	couts("初期化タスク：初期化開始");
	couts("初期化タスク：タスクＡを生成");
	API_startTASK((taskid_t)1,(ptr_t)0x1234);
	couts("初期化タスク：タスクＢを生成");
	API_startTASK((taskid_t)2,(ptr_t)NULL);
	couts("初期化タスク：タスクＣを生成");
	API_startTASK((taskid_t)3,(ptr_t)NULL);
	couts("初期化タスク：初期化終了");
}

static int x = 0;

void TASKPROC task1(ptr_t arg) {
	(void)arg;
	for (;;) {
		if (x != 1) {
			couts("タスクＡ");
			x = 1;
		}
	}
}

void TASKPROC task2(ptr_t arg) {
	(void)arg;
	for (;;) {
		if (x != 2) {
			couts("タスクＢ");
			x = 2;
			API_pauseTASK(10);
			couts("タスクＢ");
			x = 2;
		}
	}
}


void TASKPROC task3(ptr_t arg) {
	(void)arg;
	for (;;) {
		if (x != 3) {
			couts("タスクＣ");
			x = 3;
			API_pauseTASK(5);
			couts("タスクＣ");
			x = 3;
		}
	}
}

void TASKPROC diag_task(ptr_t arg) {
	(void)arg;
	for( ;; ) {
	}
}
#elif (TESTCASE == 0x0107)


typedef struct {
	int width, height;
	LPBYTE pixels;
} Image;

extern volatile int touch_x;
extern volatile int touch_y;
extern Image normal, touched, number, dib, idle, talking, talkend;

#define BOUNDSRECT(x,y,w,h) { x, y, x+w, y+h }

static const RECT touch_btns[] = {
	BOUNDSRECT( 32,56,48,48),
	BOUNDSRECT( 96,56,48,48),
	BOUNDSRECT(160,56,48,48),
	BOUNDSRECT( 32,112,48,48),
	BOUNDSRECT( 96,112,48,48),
	BOUNDSRECT(160,112,48,48),
	BOUNDSRECT( 32,168,48,48),
	BOUNDSRECT( 96,168,48,48),
	BOUNDSRECT(160,168,48,48),
	BOUNDSRECT( 32,224,48,48),
	BOUNDSRECT( 96,224,48,48),
	BOUNDSRECT(160,224,48,48),

	BOUNDSRECT( 32,280,80,32),
	BOUNDSRECT(128,280,80,32),
};

#define Length(x) (sizeof(x)/sizeof(x[0]))
int get_touch_btn_id(const RECT* buttons, int cnt, int x, int y) {
	int n;
	for (n=0; n<cnt; n++) {
		if ((buttons[n].left <= x)&&(x<=buttons[n].right )&&(buttons[n].top <= y)&&(y <= buttons[n].bottom )) {
			return n;
		}
	}
	return -1;
}

static const RECT dial_numbers[] = {
	BOUNDSRECT( 0*20, 0, 20, 24),
	BOUNDSRECT( 1*20, 0, 20, 24),
	BOUNDSRECT( 2*20, 0, 20, 24),
	BOUNDSRECT( 3*20, 0, 20, 24),
	BOUNDSRECT( 4*20, 0, 20, 24),
	BOUNDSRECT( 5*20, 0, 20, 24),
	BOUNDSRECT( 6*20, 0, 20, 24),
	BOUNDSRECT( 7*20, 0, 20, 24),
	BOUNDSRECT( 8*20, 0, 20, 24),
	BOUNDSRECT( 9*20, 0, 20, 24),

	BOUNDSRECT(10*20, 0, 20, 24), // #
	BOUNDSRECT(11*20, 0, 20, 24), // *
};

extern bool_t ImgGetPixel(Image *img, int x, int y, DWORD *color);
extern bool_t ImgSetPixel(Image *img, int x, int y, DWORD color);
extern void ImgBitBlt(Image *dest, Image *src, int x, int y, const RECT *rect);
extern void UpdateScreen();

volatile char telnum[16];
volatile int telnumx = 0;

void ClearScreen(Image *img) {
	static const RECT screen = { 0,0,240,320 };
	ImgBitBlt(&dib, img, 0, 0, &screen);
}

void DrawTouchedButton(int id) {
	if (id != -1) {
		ImgBitBlt(&dib, &touched, touch_btns[id].left, touch_btns[id].top, &touch_btns[id]);
	}
}

void DrawTellNumber(const char *ch) {
	// 電話番号を描画
	int n,s;
	int l = strlen(ch);
	s = 0;
	if (l > 11) { s = l-11; }

	n = 0;
	while (ch[s] != '\0') {
		switch (ch[s]) {
			case '0': ImgBitBlt(&dib, &number, 8+n*20, 21, &dial_numbers[0]); break;
			case '1': ImgBitBlt(&dib, &number, 8+n*20, 21, &dial_numbers[1]); break;
			case '2': ImgBitBlt(&dib, &number, 8+n*20, 21, &dial_numbers[2]); break;
			case '3': ImgBitBlt(&dib, &number, 8+n*20, 21, &dial_numbers[3]); break;
			case '4': ImgBitBlt(&dib, &number, 8+n*20, 21, &dial_numbers[4]); break;
			case '5': ImgBitBlt(&dib, &number, 8+n*20, 21, &dial_numbers[5]); break;
			case '6': ImgBitBlt(&dib, &number, 8+n*20, 21, &dial_numbers[6]); break;
			case '7': ImgBitBlt(&dib, &number, 8+n*20, 21, &dial_numbers[7]); break;
			case '8': ImgBitBlt(&dib, &number, 8+n*20, 21, &dial_numbers[8]); break;
			case '9': ImgBitBlt(&dib, &number, 8+n*20, 21, &dial_numbers[9]); break;
			case '#': ImgBitBlt(&dib, &number, 8+n*20, 21, &dial_numbers[10]); break;
			case '*': ImgBitBlt(&dib, &number, 8+n*20, 21, &dial_numbers[11]); break;
		}
		s++; 
		n++;
	}
}

/**
 * @brief 最高優先度の初期化タスク
 */
void TASKPROC init_task(ptr_t arg) {
	(void)arg;
	couts("初期化タスク：初期化開始");
	telnum[0] = '\0';
	ClearScreen(&idle);
	UpdateScreen();
	couts("初期化タスク：タスクＡを生成");
#if (SCHEDULER_TYPE == SCHEDULING_TIME_SHARING) || (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
	API_startTASK((taskid_t)1,(ptr_t)0x1234, 2);
#else
	API_startTASK((taskid_t)1,(ptr_t)0x1234);
#endif
	couts("初期化タスク：タスクＢを生成");
#if (SCHEDULER_TYPE == SCHEDULING_TIME_SHARING) || (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
	API_startTASK((taskid_t)6,(ptr_t)NULL, 2);
#else
	API_startTASK((taskid_t)6,(ptr_t)NULL);
#endif
#if (SCHEDULER_TYPE == SCHEDULING_TIME_SHARING) || (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
	API_hookInterrupt(1,2);
	API_hookInterrupt(2,3);
	API_hookInterrupt(3,4);
#else
//	API_hookInterrupt(1,2);
//	API_hookInterrupt(2,3);
//	API_hookInterrupt(3,4);
#endif
	couts("初期化タスク：初期化終了");
}

void TASKPROC task1(ptr_t arg) {
	(void)arg;
	for (;;) {
		taskid_t from;
		ptr_t data;
		couts("タスクＡが実行中です");
		couts("タスクＡ：ボタン押下割り込みに対応してタスク５を起動するように設定");
		disableInterrupt();
		ClearScreen(&idle);
		UpdateScreen();
		enableInterrupt();
		API_hookInterrupt(2,5);
		couts("メッセージ待ち開始");
		if (API_waitMSG() != SUCCESS) {
			continue;
		}
		if ((API_recvMSG(&from, &data) != SUCCESS) || (from != 5)) {
			continue;
		}
		couts("タスクＡ：ボタン押下割り込みに対応してタスク２～４を起動するように設定");
		disableInterrupt();
		telnumx = 0;
		telnum[0] = '\0';
		ClearScreen(&normal);
		UpdateScreen();
		enableInterrupt();
		API_hookInterrupt(1,2);
		API_hookInterrupt(2,3);
		API_hookInterrupt(3,4);
		couts("メッセージ待ち開始");
		if (API_waitMSG() != SUCCESS) {
			continue;
		}
		if ((API_recvMSG(&from, &data) != SUCCESS) || (from != 3)) {
			continue;
		}
		if ((int)data == 0) {
			disableInterrupt();
			enableInterrupt();
			API_hookInterrupt(1,0);
			API_hookInterrupt(2,0);
			API_hookInterrupt(3,0);
			continue;
		}
		couts("タスクＡ：ボタン押下割り込みに対応してタスク５を起動するように設定");
		API_hookInterrupt(1,0);
		API_hookInterrupt(2,5);
		API_hookInterrupt(3,0);
		disableInterrupt();
		ClearScreen(&talking);
		UpdateScreen();
		enableInterrupt();

		if (API_waitMSG() != SUCCESS) {
			continue;
		}
		if ((API_recvMSG(&from, &data) != SUCCESS) || (from != 5)) {
			continue;
		}
		disableInterrupt();
		ClearScreen(&talkend);
		UpdateScreen();
		enableInterrupt();
		API_hookInterrupt(2,0);
		API_pauseTASK(100);
	}
}

void TASKPROC task6(ptr_t arg) {
	(void)arg;
	for (;;) {
		couts("タスクＢが実行中です");
		API_pauseTASK(50);
	}
}

void TASKPROC task5(ptr_t arg) {
	(void)arg;
	API_sendMSG(1,NULL);
}

static int s_id = -1;

void TASKPROC task2(ptr_t arg) {
	int id;
	(void)arg;
	disableInterrupt();
		// 画面をクリア
		ClearScreen(&normal);
		// タッチされたボタンを描画
		s_id  = id = get_touch_btn_id(touch_btns, Length(touch_btns), touch_x, touch_y);
		DrawTouchedButton(id);
		// 電話番号を描画
		DrawTellNumber((const char*)telnum);
		// 更新
		UpdateScreen();
	enableInterrupt();
}

void TASKPROC task4(ptr_t arg) {
	int id;
	disableInterrupt();
		// 画面をクリア
		ClearScreen(&normal);
		// タッチされたボタンを描画
		id = get_touch_btn_id(touch_btns, Length(touch_btns), touch_x, touch_y);
		DrawTouchedButton(id);
		// 電話番号を描画
		DrawTellNumber((const char*)telnum);
		// 更新
		UpdateScreen();
	enableInterrupt();
}

void TASKPROC task3(ptr_t arg) {
	int id, sid;
	(void)arg;
	disableInterrupt();
		// 画面をクリア
		ClearScreen(&normal);
//		// タッチされたボタンは描画しない描画
		id = get_touch_btn_id(touch_btns, Length(touch_btns), touch_x, touch_y);
		sid = s_id;
		if (sid == id) {
			if (telnumx+1 < Length(telnum)) {
				switch (id) {
					case 0 : telnum[telnumx] = '1'; telnumx++; break;
					case 1 : telnum[telnumx] = '2'; telnumx++; break;
					case 2 : telnum[telnumx] = '3'; telnumx++; break;
					case 3 : telnum[telnumx] = '4'; telnumx++; break;
					case 4 : telnum[telnumx] = '5'; telnumx++; break;
					case 5 : telnum[telnumx] = '6'; telnumx++; break;
					case 6 : telnum[telnumx] = '7'; telnumx++; break;
					case 7 : telnum[telnumx] = '8'; telnumx++; break;
					case 8 : telnum[telnumx] = '9'; telnumx++; break;
					case 9 : telnum[telnumx] = '*'; telnumx++; break;
					case 10 : telnum[telnumx] = '0'; telnumx++; break;
					case 11 : telnum[telnumx] = '#'; telnumx++; break;
					default: break;
				}
				telnum[telnumx] = '\0';
			}
		}
		// 電話番号を描画
		DrawTellNumber((const char*)telnum);
		// 更新
		UpdateScreen();
	enableInterrupt();
	if ( (sid == id) && ((id == 12) || (id == 13)) ) {
		API_sendMSG(1, (ptr_t)(id==12?1:0));
	}
}

void TASKPROC diag_task(ptr_t arg) {
	(void)arg;
	for( ;; ) {
	}
}

#elif (TESTCASE==0x0108)

#include "../arch/lpc1343/hw/gpio.h"
#define CFG_LED_PORT                (0)
#define CFG_LED_PIN                 (7)
#define CFG_LED_ON                  (0)
#define CFG_LED_OFF                 (1)


/**
 * @brief 最高優先度の初期化タスク
 */
void TASKPROC init_task(ptr_t arg) {
	/* LPC1343ボードに積まれているLEDのポートを点滅させるテスト */
	GPIO_SetDir(CFG_LED_PORT, CFG_LED_PIN, 1);
	GPIO_SetValue(CFG_LED_PORT, CFG_LED_PIN, CFG_LED_OFF);
	API_startTASK((taskid_t)1,NULL);
	API_startTASK((taskid_t)2,NULL);
}

void TASKPROC task1(ptr_t arg) {
	for (;;) {
		GPIO_SetValue(CFG_LED_PORT, CFG_LED_PIN, CFG_LED_ON);
		API_pauseTASK(200);
	}
}

void TASKPROC task2(ptr_t arg) {
	for (;;) {
		API_pauseTASK(100);
		GPIO_SetValue(CFG_LED_PORT, CFG_LED_PIN, CFG_LED_OFF);
		API_pauseTASK(100);
	}
}

void TASKPROC task3(ptr_t arg) {
	for (;;) {
	}
}

void TASKPROC task4(ptr_t arg) {
	for (;;) {
	}
}

void TASKPROC diag_task(ptr_t arg) {
	(void)arg;
	for( ;; ) {
	}
}

#else




#endif
