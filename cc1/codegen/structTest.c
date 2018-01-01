extern int printf(char *str, ...);

struct s {
    unsigned char x;
    unsigned short y;
    unsigned int z;
};

int main(void) {
    struct s s;
    s.x = 0xDEAD;
    s.y = 0xBEEF;
    s.z = 0xFEEDEAD;
    printf("s.x=%x\n", s.x);
    printf("s.y=%x\n", s.y);
    printf("s.z=%x\n", s.z);
    return 0;
}

