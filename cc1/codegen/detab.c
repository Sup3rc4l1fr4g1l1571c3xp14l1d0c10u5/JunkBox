extern int getchar(void);
extern int putchar(int);

int main(int argc, char *argv[])
{
    int c, i;

    i = 0;
    while ((c = getchar()) != (/*EOF*/-1) && c != 26) {
        if (c == '\t') {
            do {
                putchar(' ');
            } while (++i % (/*TABSTEP*/4) != 0);
        } else {
            putchar(c);
            i++;
            if (c == '\n' || c == '\r') i = 0;
        }
    }
    return 0;
}
