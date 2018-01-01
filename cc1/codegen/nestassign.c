extern int printf(char*, ...);
extern int getchar(void);
extern int putchar(int);

int main(int argc, char *argv[])
{
	int c = 4*5+3;
	printf("4*5+3 = %d\n", c);
	c = c % 4;
	printf("(4*5+3)%%4 = %d\n", c);
    return 0;
}
