
#include "pico.utf16.h"
#include "pico.memory.h"
#include "pico.file.h"

static size32_t _tcslen(const char16_t *src)
{
	size32_t n = 0;
	while (*src != 0x0000)
	{
		src++;
		n++;
	}
	return n;
}

static char16_t *_tcsdup(const char16_t *str)
{
	size32_t sz = _tcslen(str) + 1;
	char16_t *buf = (char16_t*)Memory.Allocate(sizeof(char16_t) * sz);
	Memory.Copy(buf, (const ptr_t)str, sizeof(char16_t) * sz);
	return buf;
}

static void _tcscpy(char16_t *dst, const char16_t *src)
{
	while (*src != 0x0000)
	{
		*dst++ = *src++;
	}
	*dst = 0x0000;
}

static void _tcsncpy(char16_t *dst, const char16_t *src, size32_t sizeInWord)
{
	while (*src != 0x0000 && sizeInWord > 0)
	{
		*dst++ = *src++;
		sizeInWord--;
	}
	if (sizeInWord > 0)
	{
		*dst = 0x0000;
	}
}

static int _tcscmp(const char16_t *s1, const char16_t *s2)
{
	for (;;)
	{
		int n = *s1 - *s2;
		if (n != 0) { return n; }
		if (*s1 == 0x0000)
		{
			return 0;
		}
		s1++;
		s2++;
	}
}

static int _tcsncmp(const char16_t *s1, const char16_t *s2, size32_t sizeInWord)
{
	while (sizeInWord > 0)
	{
		int n = *s1 - *s2;
		if (n != 0) { return n; }
		if (*s1 == 0x0000)
		{
			return 0;
		}
		s1++;
		s2++;
		sizeInWord--;
	}
	return 0;
}

static char16_t *_tcschr(char16_t *str, char16_t ch)
{
	char16_t *s;
	for (s = str; *s && *s != ch; s++)
	{
		;
	}
	return s;

}

static char16_t *_tcschrs(char16_t *str, const char16_t *subs)
{
	char16_t *s;
	for (s = str; *s; s++)
	{
		char16_t *p = _tcschr((char16_t*)subs, *s);
		if (*p != 0x0000)
		{
			break;
		}
	}
	return s;

}

static size32_t _freadln(char16_t *buf, size32_t sizeInWord, ptr_t ctx)
{
	char16_t tmp[64];
	size32_t tmp_ptr = 0, tmp_size = 0;
	size32_t n = 0;
	for (;;) {
		if (tmp_ptr == tmp_size) {
			tmp_size = File.Read(ctx, (uint8_t*)tmp, sizeof(char16_t) * 64) / 2;
			if (tmp_size == 0) {
				break;
			}
			tmp_ptr = 0;
		}
		char16_t ch = tmp[tmp_ptr++];
		if (ch == L'\n')
		{
			if (n > 0 && buf[n - 1] == L'\r') { buf[n - 1] = 0x0000; }
			File.Seek(ctx, (tmp_ptr - tmp_size) * sizeof(char16_t), SeekOrigin_Current);
			tmp_ptr = tmp_size = 0;
			break;
		}
		else
		{
			if (n < sizeInWord - 1) { buf[n++] = ch; }
		}
	}
	buf[n] = '\0';
	return n;
}

const struct PicoUtf16Api Utf16 = {
	_tcsdup,
	_tcscpy,
	_tcsncpy,
	_tcslen,
	_tcscmp,
	_tcsncmp,
	_tcschr,
	_tcschrs,

	_freadln,
};