extern int printf(char *str, ...);

struct s {
    unsigned short x;
    unsigned short y;
    unsigned short z;
};

int main(void) {
    struct s s,t;
    t.x = 0xDEAD;
    t.y = 0xBEEF;
    t.z = 0xFEED;
    s = t;    
    printf("s.x=%x\n", s.x);
    printf("s.y=%x\n", s.y);
    printf("s.z=%x\n", s.z);
    return 0;
}

