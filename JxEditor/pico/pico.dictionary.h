#pragma once

#include "pico.types.h"
#include "pico.utf16.h"

#ifdef _WIN32
#include <windows.h>
#endif

typedef struct
{
#ifdef _WIN32
	WIN32_FIND_DATAW sr;
	HANDLE h;
	bool_t first;
#else
	bool_t dummy;
#endif
} EnumerateFilesContext_t;

extern const struct PicoDirectoryApi
{
	struct {
		bool_t	(*Create)(EnumerateFilesContext_t *ctx, const char16_t *path);
		void	(*Dispose)(EnumerateFilesContext_t *ctx);
		bool_t	(*MoveNext)(EnumerateFilesContext_t *ctx);
		const char16_t *(*GetCurrent)(EnumerateFilesContext_t *ctx);
	} EnumerateFiles;
} Directory;


