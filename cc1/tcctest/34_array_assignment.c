//#include <stdio.h>
extern int printf(char*, ...);

int main()
{
   int a[4];

   a[0] = 12;
   a[1] = 23;
   a[2] = 34;
   a[3] = 45;

   printf("%d %d %d %d\n", a[0], a[1], a[2], a[3]);

   return 0;
}

/* vim: set expandtab ts=4 sw=3 sts=3 tw=80 :*/