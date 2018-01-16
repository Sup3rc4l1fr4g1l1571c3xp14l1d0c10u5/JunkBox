extern int printf(char*, ...);

struct header {
	int x;
	int y;
	int z;
	int w;
};

struct header fill(struct header x) {
	struct header ret;
	printf("x=%8.8x\n", x.x);
	printf("y=%8.8x\n", x.y);
	printf("z=%8.8x\n", x.z);
	printf("w=%8.8x\n", x.w);
	ret.x = x.w;
	ret.y = x.z;
	ret.z = x.y;
	ret.w = x.x;
	return ret;
}

int main(void) {
	struct header a, b;
	a.x = 0x12345678;
	a.y = 0x9ABCDEF0;
	a.z = 0x13579ABE;
	a.w = 0xDEADBEEF;

	b = fill(a);
	printf("x=%8.8x\n", b.x);
	printf("y=%8.8x\n", b.y);
	printf("z=%8.8x\n", b.z);
	printf("w=%8.8x\n", b.w);
	return 0;
}


