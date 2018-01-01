extern int printf(char*, ...);
extern int scanf(char*, ...);

int fibonacci(int n)
{
    switch (n) {
        case 0: return 0;
        case 1: return 1;
        default: return fibonacci(n - 2) + fibonacci(n - 1);
    }
}

int main(void)
{
    int n;
    printf("n = ");
    scanf("%d", &n);
    printf("F(%d) = %d\n", n, fibonacci(n));
    return 0;
}
