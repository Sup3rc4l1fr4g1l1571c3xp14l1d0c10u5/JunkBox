/*
 * The  information  in  this  document  is  subject  to  change
 * without  notice  and  should not be construed as a commitment
 * by Digital Equipment Corporation or by DECUS.
 *
 * Neither Digital Equipment Corporation, DECUS, nor the authors
 * assume any responsibility for the use or reliability of  this
 * document or the described software.
 *
 *      Copyright (C) 1980, DECUS
 *
 * General permission to copy or modify, but not for profit,  is
 * hereby  granted,  provided that the above copyright notice is
 * included and reference made to  the  fact  that  reproduction
 * privileges were granted by DECUS.
 */
//#include <stdio.h>
typedef unsigned long size_t;

//typedef struct tagFILE FILE;
//struct _reent {
//  int _errno;
//  FILE *_stdin, *_stdout, *_stderr;
//};
//extern struct _reent *__getreent(void);
typedef struct _iobuf FILE;
struct _iobuf {
    char *_ptr;
    int _cnt;
    char *_base;
    int _flag;
    int _file;
    int _charbuf;
    int _bufsiz;
    char *_tmpfname;
};
extern FILE (* _imp___iob)[];

extern FILE *fopen(char*, char*);
extern size_t fread(void *, size_t, size_t, FILE *);
extern size_t fwrite(const void *, size_t, size_t, FILE *);
extern void fclose(FILE *);
extern int getc(FILE *);
extern int fgetc(FILE *);
extern char *fgets(char *, int, FILE *);
extern int printf(char*, ...);
extern int fprintf(FILE *, char*, ...);

//#include <stdlib.h>
extern void exit(int);
//#include <ctype.h>	// tolower()
extern int tolower(int);

/*
 * grep
 *
 * Runs on the Decus compiler or on vms, On vms, define as:
 *      grep :== "$disk:[account]grep"      (native)
 *      grep :== "$disk:[account]grep grep" (Decus)
 * See below for more information.
 */

char *documentation[] = {
   "grep searches a file for a given pattern.  Execute by",
   "   grep [flags] regular_expression file_list\n",
   "Flags are single characters preceded by '-':",
   "   -c      Only a count of matching lines is printed",
   "   -f      Print file name for matching lines switch, see below",
   "   -n      Each line is preceded by its line number",
   "   -v      Only print non-matching lines\n",
   "The file_list is a list of files (wildcards are acceptable on RSX modes).",
   "\nThe file name is normally printed if there is a file given.",
   "The -f flag reverses this action (print name no file, not if more).\n",
   0 };

char *patdoc[] = {
   "The regular_expression defines the pattern to search for.  Upper- and",
   "lower-case are always ignored.  Blank lines never match.  The expression",
   "should be quoted to prevent file-name translation.",
   "x      An ordinary character (not mentioned below) matches that character.",
   "'\\'    The backslash quotes any character.  \"\\$\" matches a dollar-sign.",
   "'^'    A circumflex at the beginning of an expression matches the",
   "       beginning of a line.",
   "'$'    A dollar-sign at the end of an expression matches the end of a line.",
   "'.'    A period matches any character except \"new-line\".",
   "':a'   A colon matches a class of characters described by the following",
   "':d'     character.  \":a\" matches any alphabetic, \":d\" matches digits,",
   "':n'     \":n\" matches alphanumerics, \": \" matches spaces, tabs, and",
   "': '     other control characters, such as new-line.",
   "'*'    An expression followed by an asterisk matches zero or more",
   "       occurrences of that expression: \"fo*\" matches \"f\", \"fo\"",
   "       \"foo\", etc.",
   "'+'    An expression followed by a plus sign matches one or more",
   "       occurrences of that expression: \"fo+\" matches \"fo\", etc.",
   "'-'    An expression followed by a minus sign optionally matches",
   "       the expression.",
   "'[]'   A string enclosed in square brackets matches any character in",
   "       that string, but no others.  If the first character in the",
   "       string is a circumflex, the expression matches any character",
   "       except \"new-line\" and the characters in the string.  For",
   "       example, \"[xyz]\" matches \"xx\" and \"zyx\", while \"[^xyz]\"",
   "       matches \"abc\" but not \"axb\".  A range of characters may be",
   "       specified by two characters separated by \"-\".  Note that,",
   "       [a-z] matches alphabetics, while [z-a] never matches.",
   "The concatenation of regular expressions is a regular expression.",
   0};
# 119 "46_grep.c"
int cflag=0, fflag=0, nflag=0, vflag=0, nfile=0, debug=0;

char *pp, lbuf[512], pbuf[256];

char *cclass();
char *pmatch();
void store(int);
void error(char *);
void badpat(char *, char *, char *);
int match(void);


/*** Display a file name *******************************/
void file(char *s)
{
   printf("File %s:\n", s);
}

/*** Report unopenable file ****************************/
void cant(char *s)
{
   fprintf((&(* _imp___iob)[2]), "%s: cannot open\n", s);
}

/*** Give good help ************************************/
void help(char **hp)
{
   char **dp;

   for (dp = hp; *dp; ++dp)
      printf("%s\n", *dp);
}

/*** Display usage summary *****************************/
void usage(char *s)
{
   fprintf((&(* _imp___iob)[2]), "?GREP-E-%s\n", s);
   fprintf((&(* _imp___iob)[2]),
         "Usage: grep [-cfnv] pattern [file ...].  grep ? for help\n");
   exit(1);
}

/*** Compile the pattern into global pbuf[] ************/
void compile(char *source)
{
   char *s; /* Source string pointer     */
   char *lp; /* Last pattern pointer      */
   int c; /* Current character         */
   int o; /* Temp                      */
   char *spp; /* Save beginning of pattern */

   s = source;
   if (debug)
      printf("Pattern = \"%s\"\n", s);
   pp = pbuf;
   while (c = *s++) {
      /*
       * STAR, PLUS and MINUS are special.
       */
      if (c == '*' || c == '+' || c == '-') {
         if (pp == pbuf ||
               (o=pp[-1]) == 2 ||
               o == 3 ||
               o == 7 ||
               o == 8 ||
               o == 9)
            badpat("Illegal occurrence op.", source, s);
         store(15);
         store(15);
         spp = pp; /* Save pattern end     */
         while (--pp > lp) /* Move pattern down    */
            *pp = pp[-1]; /* one byte             */
         *pp = (c == '*') ? 7 :
            (c == '-') ? 9 : 8;
         pp = spp; /* Restore pattern end  */
         continue;
      }
      /*
       * All the rest.
       */
      lp = pp; /* Remember start       */
      switch(c) {

         case '^':
            store(2);
            break;

         case '$':
            store(3);
            break;

         case '.':
            store(4);
            break;

         case '[':
            s = cclass(source, s);
            break;

         case ':':
            if (*s) {
               switch(tolower(c = *s++)) {

                  case 'a':
                  case 'A':
                     store(10);
                     break;

                  case 'd':
                  case 'D':
                     store(11);
                     break;

                  case 'n':
                  case 'N':
                     store(12);
                     break;

                  case ' ':
                     store(13);
                     break;

                  default:
                     badpat("Unknown : type", source, s);

               }
               break;
            }
            else badpat("No : type", source, s);

         case '\\':
            if (*s)
               c = *s++;

         default:
            store(1);
            store(tolower(c));
      }
   }
   store(15);
   store(0); /* Terminate string     */
   if (debug) {
      for (lp = pbuf; lp < pp;) {
         if ((c = (*lp++ & 0377)) < ' ')
            printf("\\%o ", c);
         else printf("%c ", c);
      }
      printf("\n");
   }
}

/*** Compile a class (within []) ***********************/
char *cclass(char *source, char *src)
   /* char       *source;   // Pattern start -- for error msg. */
   /* char       *src;      // Class start */
{
   char *s; /* Source pointer    */
   char *cp; /* Pattern start     */
   int c; /* Current character */
   int o; /* Temp              */

   s = src;
   o = 5;
   if (*s == '^') {
      ++s;
      o = 6;
   }
   store(o);
   cp = pp;
   store(0); /* Byte count      */
   while ((c = *s++) && c!=']') {
      if (c == '\\') { /* Store quoted char    */
         if ((c = *s++) == '\0') /* Gotta get something  */
            badpat("Class terminates badly", source, s);
         else store(tolower(c));
      }
      else if (c == '-' &&
            (pp - cp) > 1 && *s != ']' && *s != '\0') {
         c = pp[-1]; /* Range start     */
         pp[-1] = 14; /* Range signal    */
         store(c); /* Re-store start  */
         c = *s++; /* Get end char and*/
         store(tolower(c)); /* Store it        */
      }
      else {
         store(tolower(c)); /* Store normal char */
      }
   }
   if (c != ']')
      badpat("Unterminated class", source, s);
   if ((c = (pp - cp)) >= 256)
      badpat("Class too large", source, s);
   if (c == 0)
      badpat("Empty class", source, s);
   *cp = c;
   return(s);
}

/*** Store an entry in the pattern buffer **************/
void store(int op)
{
   if (pp >= &pbuf[256])
      error("Pattern too complex\n");
   *pp++ = op;
}

/*** Report a bad pattern specification ****************/
void badpat(char *message, char *source, char *stop)
   /* char  *message;       // Error message */
   /* char  *source;        // Pattern start */
   /* char  *stop;          // Pattern end   */
{
   fprintf((&(* _imp___iob)[2]), "-GREP-E-%s, pattern is\"%s\"\n", message, source);
   fprintf((&(* _imp___iob)[2]), "-GREP-E-Stopped at byte %ld, '%c'\n",
         stop-source, stop[-1]);
   error("?GREP-E-Bad pattern\n");
}

/*** Scan the file for the pattern in pbuf[] ***********/
void grep(FILE *fp, char *fn)
   /* FILE       *fp;       // File to process            */
   /* char       *fn;       // File name (for -f option)  */
{
   int lno, count, m;

   lno = 0;
   count = 0;
   while (fgets(lbuf, 512, fp)) {
      ++lno;
      m = match();
      if ((m && !vflag) || (!m && vflag)) {
         ++count;
         if (!cflag) {
            if (fflag && fn) {
               file(fn);
               fn = 0;
            }
            if (nflag)
               printf("%d\t", lno);
            printf("%s\n", lbuf);
         }
      }
   }
   if (cflag) {
      if (fflag && fn)
         file(fn);
      printf("%d\n", count);
   }
}

/*** Match line (lbuf) with pattern (pbuf) return 1 if match ***/
int match()
{
   char *l; /* Line pointer       */

   for (l = lbuf; *l; ++l) {
      if (pmatch(l, pbuf))
         return(1);
   }
   return(0);
}

/*** Match partial line with pattern *******************/
char *pmatch(char *line, char *pattern)
   /* char               *line;     // (partial) line to match      */
   /* char               *pattern;  // (partial) pattern to match   */
{
   char *l; /* Current line pointer         */
   char *p; /* Current pattern pointer      */
   char c; /* Current character            */
   char *e; /* End for STAR and PLUS match  */
   int op; /* Pattern operation            */
   int n; /* Class counter                */
   char *are; /* Start of STAR match          */

   l = line;
   if (debug > 1)
      printf("pmatch(\"%s\")\n", line);
   p = pattern;
   while ((op = *p++) != 15) {
      if (debug > 1)
         printf("byte[%ld] = 0%o, '%c', op = 0%o\n",
               l-line, *l, *l, op);
      switch(op) {

         case 1:
            if (tolower(*l++) != *p++)
               return(0);
            break;

         case 2:
            if (l != lbuf)
               return(0);
            break;

         case 3:
            if (*l != '\0')
               return(0);
            break;

         case 4:
            if (*l++ == '\0')
               return(0);
            break;

         case 11:
            if ((c = *l++) < '0' || (c > '9'))
               return(0);
            break;

         case 10:
            c = tolower(*l++);
            if (c < 'a' || c > 'z')
               return(0);
            break;

         case 12:
            c = tolower(*l++);
            if (c >= 'a' && c <= 'z')
               break;
            else if (c < '0' || c > '9')
               return(0);
            break;

         case 13:
            c = *l++;
            if (c == 0 || c > ' ')
               return(0);
            break;

         case 5:
         case 6:
            c = tolower(*l++);
            n = *p++ & 0377;
            do {
               if (*p == 14) {
                  p += 3;
                  n -= 2;
                  if (c >= p[-2] && c <= p[-1])
                     break;
               }
               else if (c == *p++)
                  break;
            } while (--n > 1);
            if ((op == 5) == (n <= 1))
               return(0);
            if (op == 5)
               p += n - 2;
            break;

         case 9:
            e = pmatch(l, p); /* Look for a match    */
            while (*p++ != 15); /* Skip over pattern   */
            if (e) /* Got a match?        */
               l = e; /* Yes, update string  */
            break; /* Always succeeds     */

         case 8: /* One or more ...     */
            if ((l = pmatch(l, p)) == 0)
               return(0); /* Gotta have a match  */
         case 7: /* Zero or more ...    */
            are = l; /* Remember line start */
            while (*l && (e = pmatch(l, p)))
               l = e; /* Get longest match   */
            while (*p++ != 15); /* Skip over pattern   */
            while (l >= are) { /* Try to match rest   */
               if (e = pmatch(l, p))
                  return(e);
               --l; /* Nope, try earlier   */
            }
            return(0); /* Nothing else worked */

         default:
            printf("Bad op code %d\n", op);
            error("Cannot happen -- match\n");
      }
   }
   return(l);
}

/*** Report an error ***********************************/
void error(char *s)
{
   fprintf((&(* _imp___iob)[2]), "%s", s);
   exit(1);
}

/*** Main program - parse arguments & grep *************/
int main(int argc, char **argv)
{
   char *p;
   int c, i;
   int gotpattern;

   FILE *f;

   if (argc <= 1)
      usage("No arguments");
   if (argc == 2 && argv[1][0] == '?' && argv[1][1] == 0) {
      help(documentation);
      help(patdoc);
      return 0;
   }
   nfile = argc-1;
   gotpattern = 0;
   for (i=1; i < argc; ++i) {
      p = argv[i];
      if (*p == '-') {
         ++p;
         while (c = *p++) {
            switch(tolower(c)) {

               case '?':
                  help(documentation);
                  break;

               case 'C':
               case 'c':
                  ++cflag;
                  break;

               case 'D':
               case 'd':
                  ++debug;
                  break;

               case 'F':
               case 'f':
                  ++fflag;
                  break;

               case 'n':
               case 'N':
                  ++nflag;
                  break;

               case 'v':
               case 'V':
                  ++vflag;
                  break;

               default:
                  usage("Unknown flag");
            }
         }
         argv[i] = 0;
         --nfile;
      } else if (!gotpattern) {
         compile(p);
         argv[i] = 0;
         ++gotpattern;
         --nfile;
      }
   }
   if (!gotpattern)
      usage("No pattern");
   if (nfile == 0)
      grep((&(* _imp___iob)[0]), 0);
   else {
      fflag = fflag ^ (nfile > 0);
      for (i=1; i < argc; ++i) {
         if (p = argv[i]) {
            if ((f=fopen(p, "r")) == 0)
               cant(p);
            else {
               grep(f, p);
               fclose(f);
            }
         }
      }
   }
   return 0;
}

/* vim: set expandtab ts=4 sw=3 sts=3 tw=80 :*/
