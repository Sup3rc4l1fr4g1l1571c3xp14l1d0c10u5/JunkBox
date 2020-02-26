int buf1 = 12345;
int buf2 = 12345;
int zbuf1 = 0;
int zbuf2 = 0;
static int sbuf1 = -1;
static int sbuf2 = -1;

int sum(int x, int y) {
	return x + y + buf1 + zbuf2 + zbuf2;
}

char *message() {
	return "hello";
}
