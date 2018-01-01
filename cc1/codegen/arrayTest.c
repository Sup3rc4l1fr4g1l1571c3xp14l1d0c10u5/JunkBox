
extern int printf(char *str, ...);

int main(void) {
	int n[16];
    printf("n=%p",n);
    printf("&n[0]=%p",&n[0]);
	return 0;
}

