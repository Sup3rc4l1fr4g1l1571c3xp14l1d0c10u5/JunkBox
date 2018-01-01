extern int printf(char *str, ...);

int main(void) {
	char s[] = "this is a pen";
	int i;
	for (i=0;i<sizeof(s)/sizeof(s[0]);i++) {
		printf("s[%d]=%c\n",i,s[i]);
	}
    return 0;
}

