#include<stdio.h>

extern unsigned int magic(void);

int main(void) {
    unsigned int m = magic();
    printf("magic=%x\n",m);
    return 0;
}

