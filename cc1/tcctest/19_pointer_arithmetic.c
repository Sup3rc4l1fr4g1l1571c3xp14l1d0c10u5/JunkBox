//#include <stdio.h>
extern int printf(char*,...);

int main()
{
   int a;
   int *b;
   int *c;

   a = 42;
   b = &a;
	c = 0;

   printf("%d\n", *b);

   if (b == 0)
      printf("b is NULL\n");
   else
      printf("b is not NULL\n");

   if (c == 0)
      printf("c is NULL\n");
   else
      printf("c is not NULL\n");

   return 0;
}

/* vim: set expandtab ts=4 sw=3 sts=3 tw=80 :*/
