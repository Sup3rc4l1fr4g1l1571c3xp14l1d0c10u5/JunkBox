extern int printf(char *str, ...);

void bubbleSort(int numbers[], int array_size)
{
int i, j;
  for (i = 0; i < (array_size - 1); i++) {
    for (j = (array_size - 1); j > i; j--) {
      if (numbers[j-1] > numbers[j]) {
        int temp;
        temp = numbers[j-1];
        numbers[j-1] = numbers[j];
        numbers[j] = temp;
      }
    }
  }
}

int main(void) {
    int i, n[10]; // = {5,4,3,2,1,0,9,8,7,6,};
    n[0] = 5;
    n[1] = 4;
    n[2] = 3;
    n[3] = 2;
    n[4] = 1;
    n[5] = 0;
    n[6] = 9;
    n[7] = 8;
    n[8] = 7;
    n[9] = 6;
    bubbleSort(n,10);
    for (i=0;i<10;i++) {
        printf("numbers[%d] = %d\n", i, n[i]);
    }

    return 0;
}
