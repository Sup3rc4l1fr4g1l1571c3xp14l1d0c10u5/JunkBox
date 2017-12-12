/**@
 spec: Block scope linkage C standard
 assertion: 
 note: https://stackoverflow.com/questions/7239911/block-scope-linkage-c-standard
@**/

extern int printf(const char *, ...);

int a = 10;  /* External linkage */

void main(void)
{
	static int a = 5;  /* No linkage(staticの有無は関係ない) */

    printf("%d\n", a);    /* Prints 5 */

    {
        extern int a;  /* External linkage */

        printf("%d\n", a);    /* Prints 10 */
    }
}
