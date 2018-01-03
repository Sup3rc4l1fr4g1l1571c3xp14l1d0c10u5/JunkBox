//#define _ISOC99_SOURCE 1

//#include <stdio.h>
extern int printf(char*,...);

//#include <math.h>
extern double sin(double);
extern double cos(double);
extern double tan(double);
extern double asin(double);
extern double acos(double);
extern double atan(double);
extern double sinh(double);
extern double cosh(double);
extern double tanh(double);
extern double exp(double);
extern double fabs(double);
extern double log(double);
extern double log10(double);
extern double pow(double, double);
extern double sqrt(double);
extern double round(double);
extern double ceil(double);
extern double floor(double);

int main()
{
   printf("%f\n", sin(0.12));
   printf("%f\n", cos(0.12));
   printf("%f\n", tan(0.12));
   printf("%f\n", asin(0.12));
   printf("%f\n", acos(0.12));
   printf("%f\n", atan(0.12));
   printf("%f\n", sinh(0.12));
   printf("%f\n", cosh(0.12));
   printf("%f\n", tanh(0.12));
   printf("%f\n", exp(0.12));
   printf("%f\n", fabs(-0.12));
   printf("%f\n", log(0.12));
   printf("%f\n", log10(0.12));
   printf("%f\n", pow(0.12, 0.12));
   printf("%f\n", sqrt(0.12));
   printf("%f\n", round(12.34));
   printf("%f\n", ceil(12.34));
   printf("%f\n", floor(12.34));

   return 0;
}

/* vim: set expandtab ts=4 sw=3 sts=3 tw=80 :*/
