//#include <stdio.h>

typedef struct tagFILE FILE;
typedef unsigned int size_t;

extern FILE *fopen(char*, char*);
extern size_t fread(void *, size_t, size_t, FILE *);
extern size_t fwrite(const void *, size_t, size_t, FILE *);
extern void fclose(FILE *);
extern int getc(FILE *);
extern int fgetc(FILE *);
extern char *fgets(char *, int, FILE *);
extern int printf(char*, ...);

int main()
{
   FILE *f = fopen("fred.txt", "w");
   fwrite("hello\nhello\n", 1, 12, f);
   fclose(f);
{
   char freddy[7];
   f = fopen("fred.txt", "r");
   if (fread(freddy, 1, 6, f) != 6)
      printf("couldn't read fred.txt\n");

   freddy[6] = '\0';
   fclose(f);

   printf("%s", freddy);
{
   int InChar;
   char ShowChar;
   f = fopen("fred.txt", "r");
   while ( (InChar = fgetc(f)) != -1)
   {
      ShowChar = InChar;
      if (ShowChar < ' ')
         ShowChar = '.';

      printf("ch: %d '%c'\n", InChar, ShowChar);
   }
   fclose(f);

   f = fopen("fred.txt", "r");
   while ( (InChar = getc(f)) != -1)
   {
      ShowChar = InChar;
      if (ShowChar < ' ')
         ShowChar = '.';

      printf("ch: %d '%c'\n", InChar, ShowChar);
   }
   fclose(f);
}

   f = fopen("fred.txt", "r");
   while (fgets(freddy, sizeof(freddy), f) != 0)
      printf("x: %s", freddy);

   fclose(f);
}

   return 0;
}

/* vim: set expandtab ts=4 sw=3 sts=3 tw=80 :*/
