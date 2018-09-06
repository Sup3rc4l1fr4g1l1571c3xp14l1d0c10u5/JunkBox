#pragma once

#include "pico.types.h"

extern const struct PicoDiagnoticsApi
{
	void	(*Halt)(void);
	void	(*Assert)(bool_t cond);
} Diagnotics;


