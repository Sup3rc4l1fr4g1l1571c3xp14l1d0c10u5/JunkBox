//#include <stdio.h>
typedef struct _iobuf FILE;
struct _reent {
  int _errno;
  FILE *_stdin, *_stdout, *_stderr;
};

extern struct _reent *__getreent(void);
/*
struct _iobuf {
    char *_ptr;
    int _cnt;
    char *_base;
    int _flag;
    int _file;
    int _charbuf;
    int _bufsiz;
    char *_tmpfname;
};
extern FILE (* _imp___iob)[];
*/
extern int printf(char*, ...);
extern int fprintf(FILE *, char*, ...);

int fred(int p)
{
   printf("yo %d\n", p);
   return 42;
}

int (*f)(int) = &fred;

/* To test what this is supposed to test the destination function
   (fprint here) must not be called directly anywhere in the test.  */
int (*fprintfptr)(FILE *, const char *, ...) = &fprintf;

int main()
{
   fprintfptr((__getreent()->_stdout), "%d\n", (*f)(24));

   return 0;
}

/* vim: set expandtab ts=4 sw=3 sts=3 tw=80 :*/
