extern int printf(char *str, ...);

int main(void) {
    int x = sizeof("foo bar buz");
    int y = 0xDEADBEEF;
    printf("x=%x\n", x);
    printf("y=%x\n", y);
    return 0;
}

