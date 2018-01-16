//#include <stdio.h>
extern int printf(char*,...);

int main()
{
   char a;
   int b;
   double c;
   char d[sizeof(int)];

   printf("%d\n", sizeof(a));
   printf("%d\n", sizeof(b));
   printf("%d\n", sizeof(c));
   printf("%d\n", sizeof(d));

   printf("%d\n", sizeof(!a));

   return 0;
}

/* vim: set expandtab ts=4 sw=3 sts=3 tw=80 :*/
