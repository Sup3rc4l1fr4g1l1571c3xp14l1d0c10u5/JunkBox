extern int printf(char *str, ...);


int main(void) {
	int x = 10;
	int n[] = { x+0, x+1, x+2 };
	return printf("%d\n", n[1]);
}

