#include "./uKernel/kernel.h"
#include "./uKernel/systemcall.h"
#include "./uKernel/kerneldata.h"

#define TASK_ENTRYS const taskentry_t task_entries[TASK_NUM] =

#define TASK_ENTRY(id, proc) { \
    .stack_pointer      = &task_stack[(id)][TASK_STACK_SIZE-1-sizeof(context_t)+0], \
    .start_proc.address = (proc) \
}

#define TASK_DEF(id, param) void task##id##_proc(param)

#define LED_DDR  DDRB
#define LED_PORT PORTB
#define LED_PIN  PINB
#define LED      PINB5

TASK_DEF(0, void* param) {
    (void)param;
    svc_start_task(1, NULL);
    svc_start_task(2, NULL);
    svc_start_task(3, NULL);
    svc_pause_task(10);
}

enum counter_op_t {
    READ_CNT,
    WRITE_CNT,
};

struct counter_msg_t {
    enum counter_op_t   message;
    int                 value;
};

TASK_DEF(1, void* param) {
    (void)param;
    int cnt = 0;
    for (;;) {
        svc_wait_message();

        taskid_t from;
        struct counter_msg_t *msg;
        svc_recive_message(&from, (void**)&msg);

        switch (msg->message) {
            case READ_CNT:
                svc_send_message(from, (void*)cnt);
                break;
            case WRITE_CNT:
                cnt = msg->value;
                break;
            default:
                break;
        }
    }
}

TASK_DEF(2, void* param) {
    (void)param;
    for (;;) {
        svc_pause_task(100);

        struct counter_msg_t msg_read = { 
            .message    = READ_CNT 
        };
        svc_send_message(1, (void*)&msg_read);

        taskid_t from;
        int cnt;
        svc_recive_message(&from, (void*)&cnt);

        struct counter_msg_t msg_write = {
            .message    = WRITE_CNT,
            .value      = cnt + 1 
        };
        svc_send_message(1, (void*)&msg_write);

    }
}

TASK_DEF(3, void* param) {
    (void)param;
    for (;;) {
        svc_pause_task(10);
        struct counter_msg_t msg_read = { .message = READ_CNT };
        svc_send_message(1, (void*)&msg_read);
        taskid_t from;
        int cnt;
        svc_recive_message(&from, (void*)&cnt);
    }
}

TASK_ENTRYS {
    TASK_ENTRY(0, task0_proc),
    TASK_ENTRY(1, task1_proc),
    TASK_ENTRY(2, task2_proc),
    TASK_ENTRY(3, task3_proc),
};

int main(void) {
    LED_DDR |= _BV(LED);

    start_kernel();

    halt();
}
