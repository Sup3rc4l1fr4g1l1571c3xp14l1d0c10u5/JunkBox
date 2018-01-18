extern int printf(char*, ...);

struct header {
	int x;
	int y;
	int z;
	int w;
};

void swap_copy(struct header *x, struct header *y) {
	printf("x=%8.8x\n", y->x);
	printf("y=%8.8x\n", y->y);
	printf("z=%8.8x\n", y->z);
	printf("w=%8.8x\n", y->w);
	x->x = y->w;
	x->y = y->z;
	x->z = y->y;
	x->w = y->x;
}

void direct_copy(struct header *x, struct header *y) {
	*x = *y;
}

int main(void) {
	struct header a, b, c;
	a.x = 0x12345678;
	a.y = 0x9ABCDEF0;
	a.z = 0x13579ABE;
	a.w = 0xDEADBEEF;

	swap_copy(&b, &a);
	printf("x=%8.8x\n", b.x);
	printf("y=%8.8x\n", b.y);
	printf("z=%8.8x\n", b.z);
	printf("w=%8.8x\n", b.w);

	direct_copy(&c, &b);
	printf("x=%8.8x\n", c.x);
	printf("y=%8.8x\n", c.y);
	printf("z=%8.8x\n", c.z);
	printf("w=%8.8x\n", c.w);
return 0;
}
