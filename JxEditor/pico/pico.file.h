#pragma once

#include "pico.types.h"
#include "pico.utf16.h"

typedef enum {
	FileMode_Append,
	FileMode_Create,
	FileMode_CreateNew,
	FileMode_Open,
	FileMode_OpenOrCreate,
	FileMode_Truncate,
} FileMode;

typedef enum {
	FileAccess_Read,
	FileAccess_Write,
	FileAccess_ReadWrite,
} FileAccess;

typedef enum
{
	SeekOrigin_Begin,
	SeekOrigin_Current,
	SeekOrigin_End,
} SeekOrigin;

extern const struct PicoFileApi
{
	ptr_t		(*Open)(const char16_t *path, FileMode mode, FileAccess access);
	void		(*Close)(ptr_t file);
	size32_t	(*Read)(ptr_t file, uint8_t *buf, size32_t bufSize);
	size32_t	(*Write)(ptr_t file, const uint8_t *buf, size32_t bufSize);
	bool_t		(*Seek)(ptr_t file, size32_t offset, SeekOrigin origin);
	bool_t		(*Position)(ptr_t file, size32_t *pos);
	bool_t		(*EoF)(ptr_t file);
} File;


