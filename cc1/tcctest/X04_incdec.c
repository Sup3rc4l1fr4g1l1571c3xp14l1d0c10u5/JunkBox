char *pp, pbuf[256];
extern int printf(char*, ...);

int main(void) {
	pp = pbuf;
	printf("pbuf=%p\n",pbuf);
	
	printf("pp=%p\n",pp);
	printf("--pp=%p\n",--pp);
	printf("pp=%p\n",pp);
	printf("pp--=%p\n",pp--);
	printf("pp=%p\n",pp);
	printf("++pp=%p\n",++pp);
	printf("pp=%p\n",pp);
	printf("pp++=%p\n",pp++);
	printf("pp=%p\n",pp);

	return 0;
}

