#include "pico.assert.h"

static void Halt(void) {
	volatile bool_t block = true;
	while (block) {
		// halt
	}
}

static void Assert(bool_t cond) {
	if (cond == false) {
		Halt();
	}
}

const struct PicoDiagnoticsApi Diagnotics = 
{
	Halt,
	Assert,
} ;


