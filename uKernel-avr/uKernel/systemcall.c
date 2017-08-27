#include "systemcall.h"
#include "readyqueue.h"
#include "pausequeue.h"
#include "message.h"
#include "semaphore.h"
#include "task.h"
#include "extint.h"
#include "kerneldata.h"

/**
 * @addtogroup  システムコール
 */

/*@{*/

/**
 * @brief システムコールID
 */
typedef enum syscallid_t {
    svcid_start_task = 0,
    svcid_exit_task = 1,
    svcid_pause_task = 2,
    svcid_resume_task = 3,
    SVCID_resetTASK = 4,
    svcid_restart_task = 5,
    svcid_get_taskid = 6,
    svcid_take_semaphore = 7,
    svcid_give_semaphore = 8,
    svcid_test_and_set_semaphore = 9,
    SVCID_hookIntTask = 10,
    SVCID_restart = 11,
    svcid_send_message = 12,
    svcid_recive_message = 13,
    svcid_wait_message = 14,
    svcid_hook_extint = 15,
    svcid_unhook_extint = 16,
} syscallid_t;

/**
 * @brief   システムコール呼び出し/結果で使われる共通領域。
 * @note    各システムコールの呼び出し情報構造体の先頭に置くことでこの構造体を継承したように振る舞わせている。
 */
typedef union syscall_param_t {
    syscallid_t   syscallid;    /**< 呼び出すシステムコールのＩＤ */
    svcresultid_t result;       /**< システムコールの呼び出し結果 */
} syscall_param_t;  

/**
 * @brief                   システムコール呼び出し（ユーザー側）
 * @param   param [in,out]  システムコール呼び出し用の引数(syscall_param_tを継承していること)
 * @note                    引数がレジスタ渡しであることは必須.
 */
__attribute__((naked))
static void syscall(register syscall_param_t* param) {
    (void)param;
    asm volatile("sbi   0x0B,   2");    /**< INT0(外部割り込み0番ポート＝PORTD2)にHighを出力するとソフトウェア割り込みが発生する */
    asm volatile("ret");                /**< 外部割り込みから戻ってくるとここから実行が再開 */
}

/**
 * @brief svc_start_taskシステムコール用の呼び出し情報構造体
 */
struct svc_start_task_param_t {
    syscall_param_t     syscall_param;  /**< システムコール共通領域 */
    taskid_t        taskid;             /**< 引数1: 起動するタスクのID */
    void*           argument;           /**< 引数2: タスクに渡す引数 */
};

/**
 * @brief                   指定したタスクを開始する
 * @param   [in] taskid     開始するタスクのID
 * @param   [in] argument   タスクに渡す引数
 * @retval  SUCCESS         指定したタスクの開始に成功
 * @retval  ERR1            ユーザータスク以外、もしくは呼び出し元自身が指定された
 * @retval  ERR2            DORMANT状態以外のタスクを起動しようとした
 */
svcresultid_t svc_start_task(taskid_t taskid, void* argument) {
    struct svc_start_task_param_t param = {
        .syscall_param.syscallid = svcid_start_task,
        .taskid = taskid,
        .argument = argument
    };
    syscall((syscall_param_t*)&param);
    return param.syscall_param.result;
}

/**
 * @brief               指定したタスクを開始する
 * @param   [in] par    システムコール呼び出しの引数が格納された構造体へのポインタ
 * @retval  SUCCESS     指定したタスクの開始に成功
 * @retval  ERR1        ユーザータスク以外、もしくは呼び出し元自身が指定された
 * @retval  ERR2        DORMANT状態以外のタスクを起動しようとした
 */
static svcresultid_t svc_start_task_impl(struct svc_start_task_param_t *par) {
    if (is_user_task(par->taskid) == false) {
        /* ユーザータスク以外は起動できない */
        return ERR1;
    }

    tcb_t *tcb = &tcbs[par->taskid];

    /* 起動できるタスクは自分以外かつDORMANT状態のもの */
    if (tcb == current_tcb) {
        /* 自分を起動することはできない */
        return ERR1;
    } else if (tcb->status == DORMANT) {
        /* 起動するタスクの設定 */
        context_t* ctx = get_context_from_tcb(tcb);
        tcb->status = READY;
        tcb->next_task = NULL;
        tcb->argument = par->argument;

        /* 引数を設定 */
        set_argument_to_context(ctx, tcb->argument);

        /* readyキューに挿入 */
        add_task_to_readyqueue(tcb);

        /* スケジューリング要求 */
        request_reschedule = (current_tcb > tcb) ? true : false;

        return SUCCESS;
    } else {
        /* DORMANT状態以外のタスクを起動しようとした */
        return ERR2;
    }
}

/**
 * @brief   svc_exit_taskシステムコール用の呼び出し情報構造体
 */
struct svc_exit_task_param_t {
    syscall_param_t syscall_param;    /**< システムコール共通領域 */
};

/**
 * @brief               自タスクを終了する
 * @retval  SUCCESS     自タスクの終了に成功
 * @retval  ERR3        ready 状態でないタスクを exit させようとした
 * @retval  ERR31       DIAGタスクが par->taskid に指定されている
 */
svcresultid_t svc_exit_task(void) {
    struct svc_exit_task_param_t param = {
        .syscall_param.syscallid = svcid_exit_task
    };
    syscall((syscall_param_t*)&param);
    return param.syscall_param.result;
}

/**
 * @brief               自タスクを終了する
 * @param   [in] par    システムコール呼び出しの引数が格納された構造体へのポインタ
 * @retval  SUCCESS     自タスクの終了に成功
 * @retval  ERR3        ready 状態でないタスクを exit させようとした
 * @retval  ERR31       DIAGタスクが par->taskid に指定されている
 */
static svcresultid_t svc_exit_task_impl(struct svc_exit_task_param_t *par) {
    (void)par;
    if (DIAGTASK == current_tcb) {
        /* DIAGタスクは終了できない */
        return ERR31;
    } else if (current_tcb->status == READY) {
        /* 現在のタスクと関連付けられているセマフォをすべて開放 */
        unlink_semaphore(current_tcb);

        /* 現在のタスクと関連付けられているメッセージをすべて開放 */
        unlink_message(current_tcb);

        /* ReadyQueueから現在のタスクを取り外す */
        remove_task_from_readyqueue(current_tcb);

        /* TCBをリセット */
        reset_tcb(get_taskid_from_tcb(current_tcb));

        /* スケジューリング要求を設定 */
        request_reschedule = true;

        return SUCCESS;
    } else {
        /* ready 状態でないタスクを exit させようとした */
        return ERR3;
    }
}

/**
 * @brief    svc_pause_taskシステムコール用の呼び出し情報構造体
 */
struct svc_pause_task_param_t {
    syscall_param_t     syscall_param;  /**< システムコール共通領域 */
    msec_t              msec;           /**< 停止時間 */
};

/**
 * @brief               自タスクを一時停止する
 * @param   [in] msec   停止時間(0は無期限の停止)
 * @retval  SUCCESS     自タスクの一時停止に成功
 * @retval  ERR4        ready 状態でないタスクを pause させようとした
 * @retval  ERR31       INITタスクもしくはDIAGタスクががpar->taskidに指定されている
 */
svcresultid_t svc_pause_task(msec_t msec) {
    struct svc_pause_task_param_t param = {
        .syscall_param.syscallid = svcid_pause_task,
        .msec = msec
    };
    syscall((syscall_param_t*)&param);
    return param.syscall_param.result;
}

/**
 * @brief               自タスクを一時停止する
 * @param   [in] par    システムコール呼び出しの引数が格納された構造体へのポインタ
 * @retval  SUCCESS     自タスクの一時停止に成功
 * @retval  ERR4        ready 状態でないタスクを pause させようとした
 * @retval  ERR31       INITタスクもしくはDIAGタスクががpar->taskidに指定されている
 */
static svcresultid_t svc_pause_task_impl(struct svc_pause_task_param_t *par) {
    if ((INITTASK == current_tcb) || (DIAGTASK == current_tcb)) {
        /* INITタスクとDIAGタスクは一時停止できない */
        return ERR31;
    } else if (current_tcb->status == READY) {
        current_tcb->status = PAUSE;
        current_tcb->pause_msec = par->msec;
        remove_task_from_readyqueue(current_tcb);
        if (par->msec != 0) {
            /* 無期限休止ではない場合、pausequeueに追加する */
            add_task_to_pausequeue(current_tcb, par->msec);
        }
        request_reschedule = true;
        return SUCCESS;
    } else {
        /* ready 状態でないタスクを pause させようとした */
        return ERR4;
    }
}

/**
 * @brief   svc_resume_taskシステムコール用の呼び出し情報構造体
 */
struct svc_resume_task_param_t {
    syscall_param_t syscall_param;  /**< システムコール共通領域 */
    taskid_t        taskid;         /**< 引数1: 起動するタスクのID */
};

/**
 * @brief               PAUSE状態のタスクを再開する
 * @param   [in] taskid システムコール呼び出しの引数が格納された構造体へのポインタ
 * @retval  SUCCESS     指定されたタスクの再開に成功
 * @retval  ERR1        不正なタスクIDがtaskidに指定されている
 * @retval  ERR5        taskid に指定されたタスクはPAUSE状態ではない
 * @retval  ERR31       DIAGタスクが taskid に指定されている
 * @note                セマフォ待ち状態などPAUSE状態ではないタスクはresumeTASKできない
 */
svcresultid_t svc_resume_task(taskid_t taskid) {
    struct svc_resume_task_param_t param = {
        .syscall_param.syscallid = svcid_resume_task,
        .taskid = taskid
    };
    syscall((syscall_param_t*)&param);
    return param.syscall_param.result;
}

/**
 * @brief               PAUSE状態のタスクを再開する
 * @param   [in] par    システムコール呼び出しの引数が格納された構造体へのポインタ
 * @retval  SUCCESS     指定されたタスクの再開に成功
 * @retval  ERR1        不正なタスクIDがpar->taskidに指定されている
 * @retval  ERR5        par->taskid に指定されたタスクはPAUSE状態ではない
 * @retval  ERR31       DIAGタスクが par->taskid に指定されている
 * @note                セマフォ待ち状態などPAUSE状態ではないタスクはresumeTASKできない
 */
static svcresultid_t svc_resume_task_impl(struct svc_resume_task_param_t *par) {
    tcb_t* tcb;

    if (par->taskid >= TASK_NUM) {
        /* 不正なタスクIDがtaskidに指定されている */
        return ERR1;
    } else if (par->taskid == TASK_NUM - 1) {
        /* DIAGTASKはresumeできない */
        return ERR31;
    } else {
        tcb = &tcbs[par->taskid];
    }

    /* 休止中のタスクか確認 */
    if (tcb->status != PAUSE) {
        /* セマフォ待ちなど、PAUSE状態ではないタスクをresumeさせた */
        request_reschedule = false;
        return ERR5;
    }

    /* 有限休眠の場合はタスクがpausequeueに登録されているので取り除く
     * 無限休眠の場合は登録されていない
     */
    (void)remove_task_from_pausequeue(tcb);

    /* タスクを再び稼動させる */
    tcb->status = READY;
    tcb->pause_msec = 0;

    add_task_to_readyqueue(current_tcb);

    /* スケジューリング要求 */
    request_reschedule = (current_tcb > tcb) ? true : false;

    return SUCCESS;
}

/**
 * @brief   svc_restart_taskシステムコール用の呼び出し情報構造体
 */
struct svc_restart_task_param_t {
    syscall_param_t syscall_param;  /**< システムコール共通領域 */
    msec_t          msec;           /**< 引数1: リスタートするまでの時間（tick単位） */
};

/**
 * @brief               自タスクをリスタートする
 * @param   [in] msec   再開するまでの時間（tick単位）
 * @retval  SUCCESS     リスタート成功
 * @retval  ERR6        msecに0が指定された
 * @note                msecを0に設定すること（無限休眠）はできない
 * @note                長期間休眠(65536ms以上)には割り込みなど別の要素を使うこと。
 */
svcresultid_t svc_restart_task(msec_t msec) {
    struct svc_restart_task_param_t par = {
        .syscall_param.syscallid = svcid_restart_task,
        .msec = msec
    };
    syscall((syscall_param_t*)&par);
    return par.syscall_param.result;
}

/**
 * @brief               自タスクをリスタートする
 * @param   [in] param  システムコール呼び出しの引数が格納された構造体へのポインタ
 * @retval  SUCCESS     リスタート成功
 * @retval  ERR6        param->msecに0が指定された
 * @note                param->msecを0に設定すること（無限休眠）はできない
 * @note                長期間休眠(param->msec > 255)には割り込みなど別の要素を使うこと。
 */
static svcresultid_t svc_restart_task_impl(struct svc_restart_task_param_t *param) {
    tcb_t* tcb = current_tcb;

    if (param->msec == 0) {
        /* 無期限休止は認めない */
        return ERR6;
    } else {
        /* ok */
    }

    /*
     * 通常のタスク終了と同様に単純にexitTASK()で終了させると、messageが開放されてしまうため、
     * 一端messageを退避した後に、タスクをexitTASK()で終了させ、その後リスタート待ちに遷移させてから
     * 再度messageを設定することでリスタート時もメッセージを保持することを可能とする
     */

    /* messageを退避 */
    message_t *message = tcb->message;
    tcb->message = NULL;

    /* タスクをexitTASK()で終了する。 */
    struct svc_exit_task_param_t dummy;
    svc_exit_task_impl(&dummy);

    /* 再度messageを設定 */
    tcb->message = message;

    /* この時点でタスクは DORMANT 状態になっているので、WAIT_RESTART状態へ遷移させた後、pausequeueに投入する */
    tcb->status = WAIT_RESTART;
    add_task_to_pausequeue(tcb, param->msec);

    /* タスクの引数を再度設定 */
    context_t* ctx = get_context_from_tcb(tcb);
    set_argument_to_context(ctx, tcb->argument);

    request_reschedule = true;

    return SUCCESS;
}

/**
 * @brief   svc_get_taskidシステムコール用の呼び出し情報構造体
 */
struct svc_get_taskid_param_t {
    syscall_param_t syscall_param;  /**< システムコール共通領域 */
    taskid_t        taskid;         /**< 戻り値: システムコールによって自タスクのタスクＩＤが格納される */
};

/**
 * @brief               自タスクのタスクＩＤを取得する
 * @param   [out] ptid  自タスクのタスクＩＤが格納される領域
 * @retval  SUCCESS     タスクＩＤ取得獲得成功
 */
svcresultid_t svc_get_taskid(taskid_t *ptid) {
    struct svc_get_taskid_param_t par = {
        .syscall_param.syscallid = svcid_get_taskid
    };
    syscall((syscall_param_t*)&par);
    *ptid = par.taskid;
    return par.syscall_param.result;
}

/**
 * @brief               自タスクのタスクＩＤを取得する
 * @param   [in] param  システムコール呼び出しの引数が格納された構造体へのポインタ
 * @retval  SUCCESS     タスクＩＤ取得獲得成功
 */
static svcresultid_t svc_get_taskid_impl(struct svc_get_taskid_param_t *param) {
    param->taskid = get_taskid_from_tcb(current_tcb);
    return SUCCESS;
}

/**
 * @brief   svc_take_semaphoreシステムコール用の呼び出し情報構造体
 */
struct take_semaphore_param_t {
    syscall_param_t     syscall_param;  /**< システムコール共通領域 */
    semaphoreid_t       sid;            /**< 獲得するセマフォのＩＤ */
};

/**
 * @brief               セマフォを獲得する。獲得できない場合はwait状態になる。
 * @param   [in] sid    獲得するセマフォのＩＤ
 * @retval  SUCCESS     セマフォ獲得成功
 * @retval  ERR10       エラー：獲得したいセマフォのＩＤが範囲外
 * @retval  ERR31       エラー：INIT, DIAGがセマフォを獲得しようとした
 */
svcresultid_t svc_take_semaphore(semaphoreid_t sid) {
    struct take_semaphore_param_t par = {
        .syscall_param.syscallid = svcid_take_semaphore,
        .sid = sid
    };
    syscall((syscall_param_t*)&par);
    return par.syscall_param.result;
}

/**
 * @brief               セマフォを獲得する。獲得できない場合はwait状態になる。
 * @param   [in] param  システムコール呼び出しの引数が格納された構造体へのポインタ
 * @retval  SUCCESS     セマフォ獲得成功
 * @retval  ERR10       エラー：獲得したいセマフォのＩＤが範囲外
 * @retval  ERR31       エラー：INIT, DIAGがセマフォを獲得しようとした
 */
static svcresultid_t svc_take_semaphore_impl(struct take_semaphore_param_t *param) {
    return take_semaphore(param->sid);
}

/**
 * @brief   svc_give_semaphoreシステムコール用の呼び出し情報構造体
 */
struct svc_give_semaphore_param_t {
    syscall_param_t     syscall_param;  /**< システムコール共通領域 */
    semaphoreid_t       sid;            /**< 放棄するセマフォのＩＤ */
};

/**
 * @brief               獲得したセマフォを放棄する
 * @param   [in] sid    放棄するセマフォのＩＤ
 * @retval  SUCCESS     セマフォ放棄成功
 * @retval  ERR10       エラー：放棄したいセマフォのＩＤが範囲外
 * @retval  ERR12       エラー：獲得していないセマフォを放棄しようとした
 */
svcresultid_t svc_give_semaphore(semaphoreid_t sid) {
    struct svc_give_semaphore_param_t par = {
        .syscall_param.syscallid = svcid_give_semaphore,
        .sid = sid
    };
    syscall((syscall_param_t*)&par);
    return par.syscall_param.result;
}

/**
 * @brief               獲得したセマフォを放棄する
 * @param   [in] param  システムコール呼び出しの引数が格納された構造体へのポインタ
 * @retval  SUCCESS     セマフォ放棄成功
 * @retval  ERR10       エラー：放棄したいセマフォのＩＤが範囲外
 * @retval  ERR12       エラー：獲得していないセマフォを放棄しようとした
 */
static svcresultid_t svc_give_semaphore_impl(struct svc_give_semaphore_param_t *param) {
    return give_semaphore(param->sid);
}

/**
 * @brief   svc_test_and_set_semaphoreシステムコール用の呼び出し情報構造体
 */
struct svc_test_and_set_semaphore_param_t {
    syscall_param_t syscall_param;  /**< システムコール共通領域 */
    semaphoreid_t   sid;            /**< 獲得を試みるセマフォのＩＤ */
};

/**
 * @brief               セマフォの獲得を試みる。
 * @param   [in] sid    獲得を試みるセマフォのＩＤ
 * @retval  SUCCESS     セマフォ獲得成功
 * @retval  ERR10       エラー：獲得したいセマフォのＩＤが範囲外
 * @retval  ERR11       エラー：セマフォを獲得できなかった
 * @retval  ERR31       エラー：INIT, DIAGがセマフォを獲得しようとした
 * @note                takeSEMAと違い獲得に失敗した場合は制御が戻る
 */
svcresultid_t svc_test_and_set_semaphore(semaphoreid_t sid) {
    struct svc_test_and_set_semaphore_param_t par = {
        .syscall_param.syscallid = svcid_test_and_set_semaphore,
        .sid = sid
    };
    syscall((syscall_param_t*)&par);
    return par.syscall_param.result;
}

/**
 * @brief               セマフォの獲得を試みる。
 * @param   [in] param  システムコール呼び出しの引数が格納された構造体へのポインタ
 * @retval  SUCCESS     セマフォ獲得成功
 * @retval  ERR10       エラー：獲得したいセマフォのＩＤが範囲外
 * @retval  ERR11       エラー：セマフォを獲得できなかった
 * @retval  ERR31       エラー：INIT, DIAGがセマフォを獲得しようとした
 * @note                takeSEMAと違い獲得に失敗した場合は制御が戻る
 */
static svcresultid_t svc_test_and_set_semaphore_impl(struct svc_test_and_set_semaphore_param_t *param) {
    return test_and_set_semaphore(param->sid);
}

/**
 * @brief   svc_recive_messageシステムコール用の呼び出し情報構造体
 */
struct svc_recive_message_param_t {
    syscall_param_t syscall_param;  /**< システムコール共通領域 */
    taskid_t        from;           /**< 送信元のタスクのＩＤが格納される */
    void*           data;           /**< 受信したメッセージの本文が格納される */
};

/**
 * @brief               タスクのメッセージキューから受信したメッセージを取り出す
 * @param   [out] from  送信元のタスクのＩＤ
 * @param   [out] data  送信されたメッセージの本文
 * @retval  SUCCESS     受信成功
 * @retval  ERR31       エラー：INITTASKとDIAGTASKがrecvMSGを呼び出した
 * @retval  ERR42       エラー：自分宛てのメッセージはなかった（正常終了）
 */
svcresultid_t svc_recive_message(taskid_t* from, void ** data) {
    struct svc_recive_message_param_t par = {
        .syscall_param.syscallid = svcid_recive_message
    };
    syscall((syscall_param_t*)&par);
    *from = par.from;
    *data = par.data;
    return par.syscall_param.result;
}

/**
 * @brief               タスクのメッセージキューから受信したメッセージを取り出す
 * @param   [in] param  システムコール呼び出しの引数が格納された構造体へのポインタ
 * @retval  SUCCESS     受信成功
 * @retval  ERR31       エラー：INITTASKとDIAGTASKがrecvMSGを呼び出した
 * @retval  ERR42       エラー：自分宛てのメッセージはなかった（正常終了）
 */
static svcresultid_t svc_recive_message_impl(struct svc_recive_message_param_t *param) {
    return recive_message(&param->from, &param->data);
}

/**
 * @brief   svc_send_messageシステムコール用の呼び出し情報構造体
 */
struct svc_send_message_param_t {
    syscall_param_t syscall_param;  /**< システムコール共通領域 */
    taskid_t        to;             /**< 送信先のタスクのＩＤ */
    void*           data;           /**< 送信するメッセージの本文 */
} PAR_sendMSG;

/**
 * @brief                   指定したタスクのメッセージキューにメッセージを送信する
 * @param   [in]    to      送信先のタスクのＩＤ
 * @param   [in]    data    送信するメッセージの本文
 * @retval  SUCCESS         受信成功
 * @retval  ERR1            エラー：不正なタスクIDがtoに指定されている
 * @retval  ERR31           エラー：INITTASKとDIAGTASKがsendMSGを呼び出した
 * @retval  ERR40           エラー：dormant状態のタスクへのメッセージ送信
 * @retval  ERR41           エラー：メッセージの空きスロットが無かった
 * @retval  ERR42           エラー：自分宛てのメッセージはなかった（正常終了）
 */
svcresultid_t svc_send_message(taskid_t to, void *data) {
    struct svc_send_message_param_t par = {
        .syscall_param.syscallid = svcid_send_message,
        .to = to,
        .data = data
    };
    syscall((syscall_param_t*)&par);
    return par.syscall_param.result;
}

/**
 * @brief               指定したタスクのメッセージキューにメッセージを送信する
 * @param   [in] param  システムコール呼び出しの引数が格納された構造体へのポインタ
 * @retval  SUCCESS     受信成功
 * @retval  ERR1        エラー：不正なタスクIDがparam->toに指定されている
 * @retval  ERR31       エラー：INITTASKとDIAGTASKがsendMSGを呼び出した
 * @retval  ERR40       エラー：dormant状態のタスクへのメッセージ送信
 * @retval  ERR41       エラー：メッセージの空きスロットが無かった
 * @retval  ERR42       エラー：自分宛てのメッセージはなかった（正常終了）
 */
static svcresultid_t svc_send_message_impl(struct svc_send_message_param_t *param) {
    return send_message(param->to, param->data);
}

/**
 * @brief   svc_wait_messageシステムコール用の呼び出し情報構造体
 */
struct svc_wait_message_param_t {
    syscall_param_t     syscall_param;  /**< システムコール共通領域 */
};

/**
 * @brief               メッセージを受信するまで待ち状態となる
 * @retval  SUCCESS     メッセージ受信成功
 * @retval  ERR31       エラー：INITTASKとDIAGTASKがsvc_wait_messageを呼び出した
 */
svcresultid_t svc_wait_message(void) {
    struct svc_wait_message_param_t par = {
        .syscall_param.syscallid = svcid_wait_message
    };
    syscall((syscall_param_t*)&par);
    return par.syscall_param.result;
}

/**
 * @brief               メッセージを受信するまで待ち状態となる
 * @param   [in] param  システムコール呼び出しの引数が格納された構造体へのポインタ
 * @retval  SUCCESS     メッセージ受信成功
 * @retval  ERR31       エラー：INITTASKとDIAGTASKがsvc_wait_message_implを呼び出した
 */
static svcresultid_t svc_wait_message_impl(struct svc_wait_message_param_t *param) {
    (void)param;
    return wait_message();
}

/**
 * @brief   svc_hook_extintシステムコール用の呼び出し情報構造体
 */
struct svc_hook_extint_param_t {
    syscall_param_t syscall_param;  /**< システムコール共通領域 */
    extintid_t      interrupt_id;   /**< 外部割り込み番号 */
    taskid_t        taskid;         /**< 割り込みで起動するタスクのID */
    void*           argument;       /**< 割り込みで起動するタスクのパラメータ */
};

/**
 * @brief                       外部割り込み発生時に指定したタスクを起動する
 * @param   [in] interrupt_id   割り込み番号
 * @param   [in] taskid         割り込みで起動するタスクのID
 * @param   [in] argument       割り込みで起動するタスクのパラメータ
 * @retval  SUCCESS             指定した割り込みのフックに成功
 * @retval  ERR1                ユーザータスク以外が指定された
 * @retval  ERR27               利用できない外部割り込みが指定された
 */
svcresultid_t svc_hook_extint(extintid_t interrupt_id, taskid_t taskid, void *argument) {
    struct svc_hook_extint_param_t par = {
        .syscall_param.syscallid = svcid_hook_extint,
        .interrupt_id = interrupt_id,
        .taskid = taskid,
        .argument = argument
    };
    syscall((syscall_param_t*)&par);
    return par.syscall_param.result;
}

/**
 * @brief               外部割り込み発生時に指定したタスクを起動する
 * @param   [in] par    システムコール呼び出しの引数が格納された構造体へのポインタ
 * @retval  SUCCESS     指定した割り込みのフックに成功
 * @retval  ERR1        ユーザータスク以外が指定された
 * @retval  ERR27       利用できない外部割り込みが指定された
 */
static svcresultid_t svc_hook_extint_impl(struct svc_hook_extint_param_t *par) {
    if (is_user_task(par->taskid) == false) {
        /* ユーザータスク以外は起動できない */
        return ERR1;
    }
    if (is_hookable_extint(par->interrupt_id) == false) {
        /* 利用できない外部割り込みが指定された */
        return ERR27;
    }

    hook_extint(par->interrupt_id, par->taskid, par->argument);

    return SUCCESS;
}

/**
 * @brief   svc_unhook_extintシステムコール用の呼び出し情報構造体
 */
struct svc_unhook_extint_param_t {
    syscall_param_t syscall_param;  /**< システムコール共通領域 */
    extintid_t      interrupt_id;   /**< 外部割り込み番号 */
};

/**
 * @brief                       外部割り込み発生時のタスク起動を解除する
 * @param   [in] interrupt_id   割り込み番号
 * @retval  SUCCESS             指定した割り込みのタスク起動解除に成功
 * @retval  ERR27               利用できない外部割り込みが指定された
 */
svcresultid_t svc_unhook_extint(extintid_t interrupt_id) {
    struct svc_unhook_extint_param_t par = {
        .syscall_param.syscallid = svcid_unhook_extint,
        .interrupt_id = interrupt_id,
    };
    syscall((syscall_param_t*)&par);
    return par.syscall_param.result;
}

/**
 * @brief               外部割り込み発生時のタスク起動を解除する
 * @param   [in] par    システムコール呼び出しの引数が格納された構造体へのポインタ
 * @retval  SUCCESS     指定した割り込みのタスク起動解除に成功
 * @retval  ERR27       利用できない外部割り込みが指定された
 */
static svcresultid_t svc_unhook_extint_impl(struct svc_unhook_extint_param_t *par) {
    if (is_hookable_extint(par->interrupt_id) == false) {
        /* 利用できない外部割り込みが指定された */
        return ERR27;
    }

    hook_extint(par->interrupt_id, (taskid_t)0, NULL);

    return SUCCESS;
}

/**
 * @brief システムコール処理
 */
__attribute__((noreturn))
void syscall_entry(void) {
    extern void schedule(void);

    /* 呼び出し元のコンテキストからシステムコールの引数を得る */
    context_t       *ctx = get_context_from_tcb(current_tcb);
    syscall_param_t *par = get_argument_from_context(ctx);

    /* システムコールのIDに応じた処理 */
    switch (par->syscallid) {
        case svcid_start_task               : par->result = svc_start_task_impl((struct svc_start_task_param_t*)par); break;
        case svcid_exit_task                : par->result = svc_exit_task_impl((struct svc_exit_task_param_t*)par); break;
        case svcid_pause_task               : par->result = svc_pause_task_impl((struct svc_pause_task_param_t*)par); break;
        case svcid_resume_task              : par->result = svc_resume_task_impl((struct svc_resume_task_param_t*)par); break;
        case svcid_restart_task             : par->result = svc_restart_task_impl((struct svc_restart_task_param_t*)par); break;

        case svcid_take_semaphore           : par->result = svc_take_semaphore_impl((struct take_semaphore_param_t*)par); break;
        case svcid_give_semaphore           : par->result = svc_give_semaphore_impl((struct svc_give_semaphore_param_t*)par); break;
        case svcid_test_and_set_semaphore   : par->result = svc_test_and_set_semaphore_impl((struct svc_test_and_set_semaphore_param_t*)par); break;

        case svcid_send_message             : par->result = svc_send_message_impl((struct svc_send_message_param_t*)par); break;
        case svcid_recive_message           : par->result = svc_recive_message_impl((struct svc_recive_message_param_t*)par); break;
        case svcid_wait_message             : par->result = svc_wait_message_impl((struct svc_wait_message_param_t*)par); break;

        case svcid_get_taskid               : par->result = svc_get_taskid_impl((struct svc_get_taskid_param_t*)par); break;

        case svcid_hook_extint              : par->result = svc_hook_extint_impl((struct svc_hook_extint_param_t*)par); break;
        case svcid_unhook_extint            : par->result = svc_unhook_extint_impl((struct svc_unhook_extint_param_t*)par); break;

        default                             : par->result = ERR30; break;
    }

    /* スケジューリングを実行して次のタスクを選択 */
    schedule();
}

/*@}*/
