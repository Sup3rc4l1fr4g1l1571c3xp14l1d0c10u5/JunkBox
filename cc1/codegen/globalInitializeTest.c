extern int printf(char *str, ...);

short x[8] = {1,2,3,4,5};
short *y = x+1;

int main(void) {
	printf("%d\n", y[3]);
    return 0;
}

