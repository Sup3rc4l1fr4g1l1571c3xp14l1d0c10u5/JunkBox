
#include "pico.file.h"
#ifdef _WIN32
#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#endif

static ptr_t Open(const char16_t *path, FileMode mode, FileAccess access)
{
#ifdef _WIN32
	FILE *fp;
	switch (access)
	{
	case FileAccess_Read:
		switch (mode)
		{
		case FileMode_Open:
			fp = _wfopen(path, L"rb");
			return fp;
		case FileMode_OpenOrCreate:
			fp = _wfopen(path, L"ab+");
			return fp;
		default:
			return NULL;
		}
	case FileAccess_Write:
		switch (mode)
		{
		case FileMode_Append:
			fp = _wfopen(path, L"ab");
			return fp;
		case FileMode_Create:
			fp = _wfopen(path, L"w+");
			return fp;
		case FileMode_CreateNew:
			fp = _wfopen(path, L"wb");
			return fp;
		case FileMode_Open:
			fp = _wfopen(path, L"ab");
			fseek(fp, 0, SEEK_SET);
			return fp;
		case FileMode_OpenOrCreate:
			fp = _wfopen(path, L"ab+");
			fseek(fp, 0, SEEK_SET);
			return fp;
		case FileMode_Truncate:
			fp = _wfopen(path, L"rb");
			if (fp != NULL)
			{
				fp = _wfreopen(path, L"wb", fp);
			}
			return fp;
		default:
			return NULL;
		}
	case FileAccess_ReadWrite:
		switch (mode)
		{
		case FileMode_Append:
			fp = _wfopen(path, L"ab");
			return fp;
		case FileMode_Create:
			fp = _wfopen(path, L"w+");
			return fp;
		case FileMode_CreateNew:
			fp = _wfopen(path, L"wb");
			return fp;
		case FileMode_Open:
			fp = _wfopen(path, L"ab");
			fseek(fp, 0, SEEK_SET);
			return fp;
		case FileMode_OpenOrCreate:
			fp = _wfopen(path, L"ab+");
			fseek(fp, 0, SEEK_SET);
			return fp;
		case FileMode_Truncate:
			fp = _wfopen(path, L"rb");
			if (fp != NULL)
			{
				fp = _wfreopen(path, L"wb+", fp);
			}
			return fp;
		default:
			return NULL;
		}
	default:
		return NULL;

	}
#else
	return NULL;
#endif
}

static void Close(ptr_t file)
{
#ifdef _WIN32
	fclose((FILE*)file);
#else
#endif
}

static size32_t Read(ptr_t file, uint8_t *buf, size32_t bufSize) {
#ifdef _WIN32
	return fread(buf, 1, bufSize, file);
#else
	return 0;
#endif
}

static size32_t Write(ptr_t file, const uint8_t *buf, size32_t bufSize) {
#ifdef _WIN32
	return fwrite(buf, 1, bufSize, file);
#else
	return 0;
#endif
}

static bool_t Seek(ptr_t file, size32_t offset, SeekOrigin origin) {
#ifdef _WIN32
	int org;
	switch (origin)
	{
	case SeekOrigin_Begin:
		org = SEEK_SET;
		break;
	case SeekOrigin_Current:
		org = SEEK_CUR;
		break;
	case SeekOrigin_End:
		org = SEEK_END;
		break;
	default:
		return false;
	}
	return (fseek((FILE*)file, offset, origin) == 0) ? true : false;
#else
	return false;
#endif
}

static bool_t Position(ptr_t file, size32_t *pos) {
#ifdef _WIN32
	long n = ftell((FILE*)file);
	if (n < 0L)
	{
		*pos = 0;
		return false;
	}
	else {
		*pos = (size32_t)n;
		return true;
	}
#else
#endif
}

static bool_t EoF(ptr_t file) {
#ifdef _WIN32
	return (feof((FILE*)file) == 0) ? false : true;
#else
#endif
}

const struct PicoFileApi File =
{
	Open,
	Close,
	Read,
	Write,
	Seek,
	Position,
	EoF,
};