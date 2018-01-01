extern int printf(char*, ...);
extern int getchar(void);
extern int putchar(int);

int main(int argc, char *argv[])
{
	unsigned long c = ((1UL << 32) - 1);
	printf("%x\n",c);
    return 0;
}
