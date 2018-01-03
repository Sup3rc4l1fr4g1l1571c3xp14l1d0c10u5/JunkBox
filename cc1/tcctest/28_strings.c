//#include <stdio.h>
extern int printf(char*,...);
//#include <string.h>
extern int strcpy(char*,char*);
extern int strncpy(char*,char*,int);
extern int strcmp(char*,char*);
extern int strncmp(char*,char*,int);
extern int strlen(char*);
extern int strcat(char*,char*);
extern int strncmp(char*,char*,int);
extern int strchr(char*,int);
extern int strrchr(char*,int);
extern int memset(char*,int,int);
extern int memcpy(char*,char*,int);
extern int memcmp(char*,char*,int);

int main()
{
   char a[10];

   strcpy(a, "hello");
   printf("%s\n", a);

   strncpy(a, "gosh", 2);
   printf("%s\n", a);

   printf("%d\n", strcmp(a, "apple") > 0);
   printf("%d\n", strcmp(a, "goere") > 0);
   printf("%d\n", strcmp(a, "zebra") < 0);

   printf("%d\n", strlen(a));

   strcat(a, "!");
   printf("%s\n", a);

   printf("%d\n", strncmp(a, "apple", 2) > 0);
   printf("%d\n", strncmp(a, "goere", 2) == 0);
   printf("%d\n", strncmp(a, "goerg", 2) == 0);
   printf("%d\n", strncmp(a, "zebra", 2) < 0);

   printf("%s\n", strchr(a, 'o'));
   printf("%s\n", strrchr(a, 'l'));
   printf("%d\n", strrchr(a, 'x') == /*NULL*/0);

   memset(&a[1], 'r', 4);
   printf("%s\n", a);

   memcpy(&a[2], a, 2);
   printf("%s\n", a);

   printf("%d\n", memcmp(a, "apple", 4) > 0);
   printf("%d\n", memcmp(a, "grgr", 4) == 0);
   printf("%d\n", memcmp(a, "zebra", 4) < 0);

   return 0;
}

/* vim: set expandtab ts=4 sw=3 sts=3 tw=80 :*/
