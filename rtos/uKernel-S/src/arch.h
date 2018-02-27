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
 * @brief   アーキテクチャ選択
 * @author  Kazuya Fukuhara
 * @date	2010/08/15 16:11:02	作成
 * @date	2010/09/01 21:36:49	WIN32アーキテクチャを追加
 *
 * 新しいアーキテクチャを追加した場合、このファイルにアーキテクチャ固有の
 * ヘッダファイルへのincludeを追加すること。
 */

#ifndef __ARCH_H__
#define __ARCH_H__

#include "./config.h"

#if defined(TARGET_ARCH)
# if (TARGET_ARCH == ARCH_DUMMY)
#  include "../arch/stub/arch.h"
#  include "../arch/stub/hw.h"
#  include "../arch/stub/context.h"
# elif (TARGET_ARCH == ARCH_AVR)
#  include "../arch/avr/arch.h"
#  include "../arch/avr/hw.h"
#  include "../arch/avr/context.h"
# elif (TARGET_ARCH == ARCH_WIN32)
#  include "../arch/win32/arch.h"
#  include "../arch/win32/hw.h"
#  include "../arch/win32/context.h"
# elif (TARGET_ARCH == ARCH_M16C_TBIRD)
#  include "../arch/m16c-tbird/arch.h"
#  include "../arch/m16c-tbird/hw.h"
#  include "../arch/m16c-tbird/context.h"
# elif (TARGET_ARCH == ARCH_LPC1343)
#  include "../arch/lpc1343/arch.h"
#  include "../arch/lpc1343/hw.h"
#  include "../arch/lpc1343/context.h"
# elif (TARGET_ARCH == ARCH_RX63N)
#  include "../arch/rx63n/arch.h"
#  include "../arch/rx63n/hw.h"
#  include "../arch/rx63n/context.h"
# else
#  error "Unknown architecture."
# endif
#else
# error "Architecture undefined."
#endif

#endif
