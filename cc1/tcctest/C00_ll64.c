#include<stdio.h>
#include<stdint.h>
#include<stdlib.h>


int main(void) {
    	static int8_t x613 = -1;
	uint16_t x614 = 0U;
	uint64_t x615 = UINT64_MAX;
	int16_t x616 = -4236;
	volatile int32_t t143 = -885764;

	printf("(x613+x614)       = %d\n", (x613+x614));
	printf("(x615             = %llu\n", (x613+x614));
	printf("(x613+x614)==x615 = %d\n", (x613+x614)==x615);

	t143 = (((x613+x614)==x615)*x616);


	return 0;
}
