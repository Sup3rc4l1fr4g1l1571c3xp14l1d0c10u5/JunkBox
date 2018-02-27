
void SystemInit(void) {
	// 特に行うことはない
}

enum regname_t {
	R8=0, R9, R10, R11, R4, R5, R6, R7, R0, R1, R2, R3, R12, LR, PC, xPSR, MAX_REG
};

struct tcb_t {
	unsigned int regs[MAX_REG];
};
struct stack_t {
	unsigned char stack[128];
};

struct task_t{
	// tcb
	struct tcb_t *tcb;

	// タスクチェーン
	struct task_t *next;
	
	// タスクスタック
	struct stack_t stack;
};

struct task_t t[2];
struct task_t *current;

__svc(0) void start(int code);
__svc(1) void swich(int code);

// software interrupt

void SVC_HandlerBody(void) {
	current = current->next;
}

__asm void SVC_Handler (int code) {
	// SVC割り込みが呼び出された時点で R0, R1, R2, R3, R12, LR, PC, xPSR はスタック上に保存されている

	// PSPが0＝一つもタスクが走っていない場合はコンテキスト保存を行わずにタスク起動だけ行う
	// 本当ならBOTTOMタスクを用意しておくといい
	MRS R1, PSP
	cbz r1, START
	
	IMPORT current
	IMPORT SVC_HandlerBody
	
	// save context to stack
	STMDB R1!, { R4, R5, R6, R7 }
	MOV R4, R8
	MOV R5, R9
	MOV R6, R10
	MOV R7, R11
	STMDB R1!, { R4, R5, R6, R7 }
	ldr R4, =current
	ldr R4, [R4]
	STR R1, [R4]

	MRS R1, MSP
	mov r1, lr
	push {r1}

	// context switch
	LDR R1, =SVC_HandlerBody
	BLX R1
	
	pop {r1}
	mov lr, r1

	// load context to stack	
START
	
	// r1 = current->stack_top
	ldr r1, =current					
	ldr r1, [r1]						
	ldr r1, [r1]						
	LDM r1!, { R4,R5,R6,R7 }
	MOV R8, R4	
	MOV R9, R5
	MOV R10, R6
	MOV R11, R7
	LDM r1!, { R4,R5,R6,R7 }
	MSR PSP, r1

	ORR lr, lr, #4
	BX	lr	
}	

void task1(void) {
	volatile int n=0;
	for (;;) {
		n++;
		swich(0x1234);
	}
}

void task2(void) {
	volatile int n=10;
	for (;;) {
		n++;
		swich(0x5678);
	}
}


/*----------------------------------------------------------------------------
  MAIN function
 *----------------------------------------------------------------------------*/
int main (void) {
	
	t[0].tcb->regs[R12] = (unsigned int)&(t[0].stack.stack[128-4]);
	t[0].tcb->regs[LR]  = (unsigned int)(task1);
	t[0].tcb->regs[PC]  = (unsigned int)(task1);
	t[0].tcb->regs[xPSR]  = 0x21000000U;
	t[0].next = &t[1];

	t[1].tcb->regs[R12] = (unsigned int)&(t[1].stack.stack[128-4]);
	t[1].tcb->regs[LR]  = (unsigned int)(task2);
	t[1].tcb->regs[PC]  = (unsigned int)(task2);
	t[1].tcb->regs[xPSR]  = 0x21000000U;
	t[1].next = &t[0];

	current = &t[0];
	
	start(0x9ABC);
	for (;;) {}

}
