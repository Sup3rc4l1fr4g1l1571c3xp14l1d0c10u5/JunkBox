extern int printf(char*, ...);

int main(int argc, char *argv[])
{
	int x = 0;
	printf("++x=%d\t",++x);	printf("x=%d\n",x);
	printf("x++=%d\t",x++);	printf("x=%d\n",x);
    return 0;
}
