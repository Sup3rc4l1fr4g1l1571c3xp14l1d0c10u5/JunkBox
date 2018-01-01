extern int printf(char *str, ...);

union s {
    unsigned char x;
    unsigned short y;
    unsigned int z;
};

int main(void) {
    union s s;
    s.x = 0xDEAD;
    printf("s.x=%x\n", s.x);
    printf("s.y=%x\n", s.y);
    printf("s.z=%x\n", s.z);
    s.y = 0xBEEF;
    printf("s.x=%x\n", s.x);
    printf("s.y=%x\n", s.y);
    printf("s.z=%x\n", s.z);
    s.z = 0xFEEDEAD;
    printf("s.x=%x\n", s.x);
    printf("s.y=%x\n", s.y);
    printf("s.z=%x\n", s.z);
    return 0;
}

