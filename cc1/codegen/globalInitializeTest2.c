extern int printf(char *str, ...);

int x[] = {10,9,8,7,6,5,4,3,2,1,0};

int *p = ((unsigned char*)&x[5]); // ok
//int *p = ((unsigned char*)&x[10]) - ((unsigned char*)&x[5]); // ng

int main(void) {
	return printf("%d\n",*p);
}

