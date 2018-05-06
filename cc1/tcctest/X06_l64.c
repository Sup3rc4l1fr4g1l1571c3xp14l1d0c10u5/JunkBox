extern int printf(char *, ...);

int main(void) {
	long long x = 0x9ABCDEF0LL;
	long long y = 0x12345678LL;
	printf("x: %lld, %llu\n", x, x);
	printf("y: %lld, %llu\n", y, y);
	printf("x+y: %lld, %llu\n", x + y, x + y);
	printf("x-y: %lld, %llu\n", x - y, x - y);
	printf("x*y: %lld, %llu\n", x * y, x * y);
	printf("x/y: %lld, %llu\n", x / y, x / y);
	return 0;
}
