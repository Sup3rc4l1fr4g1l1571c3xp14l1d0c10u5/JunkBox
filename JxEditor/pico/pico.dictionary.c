#include "./pico.dictionary.h"

static bool_t EnumerateFiles_Create(EnumerateFilesContext_t *ctx, const char16_t *path) {
#ifdef _WIN32
	ctx->h = FindFirstFileW(L"*.*", &ctx->sr);
	ctx->first = true;
	return true;
#else
	return false;
#endif
}

static void EnumerateFiles_Dispose(EnumerateFilesContext_t *ctx) {
#ifdef _WIN32
	if (ctx != INVALID_HANDLE_VALUE) {
		FindClose(ctx->h);
		ctx->h = INVALID_HANDLE_VALUE;
	}
#else
#endif
}

static bool_t EnumerateFiles_MoveNext(EnumerateFilesContext_t *ctx)
{
#ifdef _WIN32
	if (ctx->h == INVALID_HANDLE_VALUE)
	{
		return false;
	}
	if (ctx->first)
	{
		ctx->first = false;
		return true;
	}
	if (FindNextFileW(ctx->h, &ctx->sr) == FALSE)
	{
		EnumerateFiles_Dispose(ctx);
		return false;
	}
	return true;
#else
	return false;
#endif
}

static const char16_t *EnumerateFiles_GetCurrent(EnumerateFilesContext_t *ctx)
{
#ifdef _WIN32
	if (ctx->h == INVALID_HANDLE_VALUE)
	{
		return NULL;
	}
	else {
		return ctx->sr.cFileName;
	}
#else
	return NULL;
#endif
}

const struct PicoDirectoryApi Directory =
{
	{
		EnumerateFiles_Create,
		EnumerateFiles_Dispose,
		EnumerateFiles_MoveNext,
		EnumerateFiles_GetCurrent,
	}
};

