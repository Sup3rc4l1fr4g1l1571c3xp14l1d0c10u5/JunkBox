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
 * @brief   �e�X�g�P�[�X���T���v���v���O����
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
	MOV.L       #_IO_BUF,R2     ; �o�̓o�b�t�@�擪�A�h���X��R2�ɐݒ�
	MOV.B       R1,[R2]         ; R1�i�o�͕����j���o�̓o�b�t�@�ɐݒ�
	MOV.L       #1220000h,R1    ; PUTC�̋@�\�R�[�h��R1�ɐݒ�
	MOV.L       #_PARM,R3       ; �p�����[�^�E�u���b�N�̃A�h���X��R3�ɐݒ�
	MOV.L       R2,[R3]         ; �o�̓o�b�t�@�擪�A�h���X���o�̓o�b�t�@�ɐݒ�
	MOV.L       R3,R2           ; R3�i�p�����[�^�E�u���b�N�̃A�h���X�j��R2�ɐݒ�
	MOV.L       #1000000h,R3      ; �V�X�e���E�R�[���̃A�h���X��R3�ɐݒ�
	JSR         R3              ; �V�X�e���E�R�[��
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
 * �^�X�N�e�[�u��
 */
const taskproc_t TaskTable[] = {
	init_task,	/** �ō��D��x�̏������^�X�N */
	task1,		/** ���[�U�[�^�X�N�P */
	task2,		/** ���[�U�[�^�X�N�Q */
	task3,		/** ���[�U�[�^�X�N�R */
//	task4,		/** ���[�U�[�^�X�N�S */
//	task5,		/** ���[�U�[�^�X�N�T */
//	task6,		/** ���[�U�[�^�X�N�T */
	diag_task	/** �Œ�D��x�̃A�C�h���^�X�N */
};

#if (TESTCASE == 0x0101)
/**
 * �e�X�g�P�[�X�P�|�P�F��̃^�X�N�̏ꍇ�i�P�j
 */
void TASKPROC init_task(ptr_t arg) {
	(void)arg;
	couts("�������^�X�N�F�^�X�N�`�𐶐�");
	API_startTASK((taskid_t)1,(ptr_t)NULL);
	couts("�������^�X�N�F�^�X�N�a�𐶐�");
	API_startTASK((taskid_t)2,(ptr_t)NULL);
	couts("�������^�X�N�F�I��");
}

void TASKPROC task1(ptr_t arg) {
	(void)arg;
	couts("�^�X�N�`�F�J�n");
	couts("�^�X�N�`�F�Z�}�t�H�l�������݂�");
	if (API_takeSEMA(1) != SUCCESS) {
		couts("�^�X�N�`�F�Z�}�t�H�l���Ɏ��s");
		return;
	} else {
		couts("�^�X�N�`�F�Z�}�t�H�l���ɐ���");
	}

	couts("�^�X�N�`�F�X���[�v�ɓ���");
	API_pauseTASK(10);

	couts("�^�X�N�`�F�X���[�v���畜�A");
	couts("�^�X�N�`�F�Z�}�t�H���J��");
	if (API_giveSEMA(1) != SUCCESS) {
		return;
	}

	couts("�^�X�N�`�F�I��");
}

void TASKPROC task2(ptr_t arg) {
	(void)arg;
	couts("�^�X�N�a�F�J�n");
	couts("�^�X�N�a�F�Z�}�t�H�l�������݂邪�A\n"
		 "�@�@�@�@�@�^�X�N�`���Z�}�t�H���l�����Ă���̂Ŏ��s�B\n"
		 "�@�@�@�@�@�Z�}�t�H�҂��ɂȂ�B");
	if (API_takeSEMA(1) != SUCCESS) {
		return;
	}

	couts("�^�X�N�a�F�Z�}�t�H���l�����ĕ��A");
	couts("�^�X�N�a�F�Z�}�t�H���J��");
	if (API_giveSEMA(1) != SUCCESS) {
		return;
	}
	couts("�^�X�N�a�F�I��");
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
 * �e�X�g�P�[�X�P�|�Q�F��̃^�X�N�̏ꍇ�i�Q�j
 */

void TASKPROC init_task(ptr_t arg) {
	(void)arg;
	couts("�������^�X�N�F�^�X�N�`�𐶐�");
	API_startTASK((taskid_t)1,(ptr_t)NULL);
	couts("�������^�X�N�F�^�X�N�a�𐶐�");
	API_startTASK((taskid_t)2,(ptr_t)NULL);
	couts("�������^�X�N�F�I��");
}

void TASKPROC task1(ptr_t arg) {
	(void)arg;
	couts("�^�X�N�`�F�J�n");
	couts("�^�X�N�`�F�X���[�v�ɓ���");
	API_pauseTASK(10);

	couts("�^�X�N�`�F�Z�}�t�H�l�������݂邪�A\n"
		 "�@�@�@�@�@�^�X�N�a���Z�}�t�H���l�����Ă���̂Ŏ��s�B\n"
		 "�@�@�@�@�@�Z�}�t�H�҂��ɂȂ�B");
	if (API_takeSEMA(1) != SUCCESS) {
		return;
	}

	couts("�^�X�N�`�F�Z�}�t�H���l�����ĕ��A");
	if (API_giveSEMA(1) != SUCCESS) {
		return;
	}

	couts("�^�X�N�`�F�I��");
	API_exitTASK();

}

void TASKPROC task2(ptr_t arg) {
	(void)arg;
	couts("�^�X�N�a�F�J�n");
	couts("�^�X�N�a�F�Z�}�t�H���l������");
	if (API_takeSEMA(1) != SUCCESS) {
		couts("�^�X�N�a�F�Z�}�t�H�̊l���Ɏ��s");
		return;
	}

	couts("�^�X�N�a�F�X���[�v�ɓ���");
	API_pauseTASK(10);

	couts("�^�X�N�a�F�X���[�v���畜�A");
	couts("�^�X�N�a�F�Z�}�t�H���J��");
	if (API_giveSEMA(1) != SUCCESS) {
		couts("�^�X�N�a�F�Z�}�t�H�̉���Ɏ��s");
		return;
	}

	couts("�^�X�N�a�F�I��");
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
 * @brief �ō��D��x�̏������^�X�N
 */
void TASKPROC init_task(ptr_t arg) {
	(void)arg;
	couts("�������^�X�N�F�������J�n");
	couts("�������^�X�N�F�^�X�N�`�𐶐�");
	API_startTASK((taskid_t)1,(ptr_t)0x1234);
	couts("�������^�X�N�F�^�X�N�a�𐶐�");
	API_startTASK((taskid_t)2,NULL);
	couts("�������^�X�N�F�^�X�N�b�𐶐�");
	API_startTASK((taskid_t)3,NULL);
	couts("�������^�X�N�F�������I��");
}

void TASKPROC task1(ptr_t arg) {
	(void)arg;
	for( ;; ) {
		couts("�^�X�N�`�F�ғ�");
		API_pauseTASK(30);
	}
}

void TASKPROC task2(ptr_t arg) {
	(void)arg;
	for( ;; ) {
		couts("�^�X�N�a�F�ғ�");
		API_pauseTASK(40);
	}
}

void TASKPROC task3(ptr_t arg) {
	(void)arg;
	for( ;; ) {
		couts("�^�X�N�b�F�ғ�");
		API_pauseTASK(50);
	}
}

void TASKPROC diag_task(ptr_t arg) {
	(void)arg;
	for( ;; ) { }
}

#elif (TESTCASE == 0x0104)

/**
 * @brief �ō��D��x�̏������^�X�N
 */
void TASKPROC init_task(ptr_t arg) {
	(void)arg;
	couts("�������^�X�N�F�������J�n");
	couts("�������^�X�N�F�^�X�N�`�𐶐�");
	API_startTASK((taskid_t)1,(ptr_t)0x1234);
	couts("�������^�X�N�F�^�X�N�a�𐶐�");
	API_startTASK((taskid_t)2,NULL);
	couts("�������^�X�N�F�^�X�N�b�𐶐�");
	API_startTASK((taskid_t)3,NULL);
	couts("�������^�X�N�F�������I��");
}

void TASKPROC task1(ptr_t arg) {
	(void)arg;
	couts("�^�X�N�`�F�J�n");
	for( ;; ) {
		/* ���b�Z�[�W�𑗐M */
		couts("�^�X�N�`�F���b�Z�[�W�𑗐M�i�P�j");
		API_sendMSG(2, "hello, ");
		API_pauseTASK(10);
		couts("�^�X�N�`�F���b�Z�[�W�𑗐M�i�Q�j");
		API_sendMSG(2, "world! ");
		API_pauseTASK(10);
	}
}

void TASKPROC task2(ptr_t arg) {
	(void)arg;
	couts("�^�X�N�a�F�J�n");
	for( ;; ) {
		taskid_t id;
		char* data = NULL;

		/* ���b�Z�[�W����M */
		couts("�^�X�N�a�F���b�Z�[�W�̎�M�҂�");
		API_waitMSG();
		if (API_recvMSG(&id, (ptr_t*)&data) == SUCCESS) {
			couts("�^�X�N�a�F���b�Z�[�W����M�����̂ŕ\��");
			couts(data);
		}
	}
}

void TASKPROC task3(ptr_t arg) {
	(void)arg;
	couts("�^�X�N�b�F�J�n");
	for( ;; ) {
		/* ���̃^�X�N�Ɩ��֌W�ɉғ� */
		couts("�^�X�N�b�F�ғ�");
		API_pauseTASK(50);
	}
}

void TASKPROC diag_task(ptr_t arg) {
	(void)arg;
	for( ;; ) { }
}

#elif (TESTCASE == 0x0105)

/**
 * @brief �ō��D��x�̏������^�X�N
 */
void TASKPROC init_task(ptr_t arg) {
	(void)arg;
	couts("�������^�X�N�F�������J�n");
	couts("�������^�X�N�F�^�X�N�`�𐶐�");
	API_startTASK((taskid_t)1,(ptr_t)0x1234);
	couts("�������^�X�N�F�^�X�N�a�𐶐�");
	API_startTASK((taskid_t)2,NULL);
	couts("�������^�X�N�F�^�X�N�b�𐶐�");
	API_startTASK((taskid_t)3,NULL);
	couts("�������^�X�N�F�������I��");
}

void TASKPROC task1(ptr_t arg) {
	taskid_t id;
	char* data = NULL;

	(void)arg;
	couts("�^�X�N�`�F�J�n");
	
	/* ���b�Z�[�W����M */
	couts("�^�X�N�`�F���b�Z�[�W�̎�M�҂�");
	API_waitMSG();
	if (API_recvMSG(&id, (ptr_t*)&data) == SUCCESS) {
		couts("�^�X�N�`�F���b�Z�[�W����M�����̂ŕ\��");
		couts(data);
	}
	API_restartTASK(10);
}

void TASKPROC task2(ptr_t arg) {
	(void)arg;
	couts("�^�X�N�`�F�J�n");
	for( ;; ) {
		/* ���b�Z�[�W�𑗐M */
		couts("�^�X�N�a�F���b�Z�[�W�𑗐M�i�P�j");
		API_sendMSG(1, "hello, ");
		API_pauseTASK(10);
		couts("�^�X�N�a�F���b�Z�[�W�𑗐M�i�Q�j");
		API_sendMSG(1, "world! ");
		API_pauseTASK(10);
	}
}


void TASKPROC task3(ptr_t arg) {
	(void)arg;
	couts("�^�X�N�b�F�J�n");
	/* ���̃^�X�N�Ɩ��֌W�ɉғ� */
	API_pauseTASK(50);
	couts("�^�X�N�b�F�ғ�");
	API_restartTASK(50);
}

void TASKPROC diag_task(ptr_t arg) {
	(void)arg;
	for( ;; ) {
	}
}

#elif (TESTCASE == 0x0106)

/**
 * @brief �ō��D��x�̏������^�X�N
 */
void TASKPROC init_task(ptr_t arg) {
	(void)arg;
	couts("�������^�X�N�F�������J�n");
	couts("�������^�X�N�F�^�X�N�`�𐶐�");
	API_startTASK((taskid_t)1,(ptr_t)0x1234);
	couts("�������^�X�N�F�^�X�N�a�𐶐�");
	API_startTASK((taskid_t)2,(ptr_t)NULL);
	couts("�������^�X�N�F�^�X�N�b�𐶐�");
	API_startTASK((taskid_t)3,(ptr_t)NULL);
	couts("�������^�X�N�F�������I��");
}

static int x = 0;

void TASKPROC task1(ptr_t arg) {
	(void)arg;
	for (;;) {
		if (x != 1) {
			couts("�^�X�N�`");
			x = 1;
		}
	}
}

void TASKPROC task2(ptr_t arg) {
	(void)arg;
	for (;;) {
		if (x != 2) {
			couts("�^�X�N�a");
			x = 2;
			API_pauseTASK(10);
			couts("�^�X�N�a");
			x = 2;
		}
	}
}


void TASKPROC task3(ptr_t arg) {
	(void)arg;
	for (;;) {
		if (x != 3) {
			couts("�^�X�N�b");
			x = 3;
			API_pauseTASK(5);
			couts("�^�X�N�b");
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
	// �d�b�ԍ���`��
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
 * @brief �ō��D��x�̏������^�X�N
 */
void TASKPROC init_task(ptr_t arg) {
	(void)arg;
	couts("�������^�X�N�F�������J�n");
	telnum[0] = '\0';
	ClearScreen(&idle);
	UpdateScreen();
	couts("�������^�X�N�F�^�X�N�`�𐶐�");
#if (SCHEDULER_TYPE == SCHEDULING_TIME_SHARING) || (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
	API_startTASK((taskid_t)1,(ptr_t)0x1234, 2);
#else
	API_startTASK((taskid_t)1,(ptr_t)0x1234);
#endif
	couts("�������^�X�N�F�^�X�N�a�𐶐�");
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
	couts("�������^�X�N�F�������I��");
}

void TASKPROC task1(ptr_t arg) {
	(void)arg;
	for (;;) {
		taskid_t from;
		ptr_t data;
		couts("�^�X�N�`�����s���ł�");
		couts("�^�X�N�`�F�{�^���������荞�݂ɑΉ����ă^�X�N�T���N������悤�ɐݒ�");
		disableInterrupt();
		ClearScreen(&idle);
		UpdateScreen();
		enableInterrupt();
		API_hookInterrupt(2,5);
		couts("���b�Z�[�W�҂��J�n");
		if (API_waitMSG() != SUCCESS) {
			continue;
		}
		if ((API_recvMSG(&from, &data) != SUCCESS) || (from != 5)) {
			continue;
		}
		couts("�^�X�N�`�F�{�^���������荞�݂ɑΉ����ă^�X�N�Q�`�S���N������悤�ɐݒ�");
		disableInterrupt();
		telnumx = 0;
		telnum[0] = '\0';
		ClearScreen(&normal);
		UpdateScreen();
		enableInterrupt();
		API_hookInterrupt(1,2);
		API_hookInterrupt(2,3);
		API_hookInterrupt(3,4);
		couts("���b�Z�[�W�҂��J�n");
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
		couts("�^�X�N�`�F�{�^���������荞�݂ɑΉ����ă^�X�N�T���N������悤�ɐݒ�");
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
		couts("�^�X�N�a�����s���ł�");
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
		// ��ʂ��N���A
		ClearScreen(&normal);
		// �^�b�`���ꂽ�{�^����`��
		s_id  = id = get_touch_btn_id(touch_btns, Length(touch_btns), touch_x, touch_y);
		DrawTouchedButton(id);
		// �d�b�ԍ���`��
		DrawTellNumber((const char*)telnum);
		// �X�V
		UpdateScreen();
	enableInterrupt();
}

void TASKPROC task4(ptr_t arg) {
	int id;
	disableInterrupt();
		// ��ʂ��N���A
		ClearScreen(&normal);
		// �^�b�`���ꂽ�{�^����`��
		id = get_touch_btn_id(touch_btns, Length(touch_btns), touch_x, touch_y);
		DrawTouchedButton(id);
		// �d�b�ԍ���`��
		DrawTellNumber((const char*)telnum);
		// �X�V
		UpdateScreen();
	enableInterrupt();
}

void TASKPROC task3(ptr_t arg) {
	int id, sid;
	(void)arg;
	disableInterrupt();
		// ��ʂ��N���A
		ClearScreen(&normal);
//		// �^�b�`���ꂽ�{�^���͕`�悵�Ȃ��`��
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
		// �d�b�ԍ���`��
		DrawTellNumber((const char*)telnum);
		// �X�V
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
 * @brief �ō��D��x�̏������^�X�N
 */
void TASKPROC init_task(ptr_t arg) {
	/* LPC1343�{�[�h�ɐς܂�Ă���LED�̃|�[�g��_�ł�����e�X�g */
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
