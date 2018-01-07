CC=gcc
MYCC=../bin/debug/AnsiCParser.exe
AS=gcc -g
FILES=$(shell ls *.c)
OUTDIR=result

TARGETS=$(patsubst %.c,$(OUTDIR)/%.exe,$(FILES))
ASMS=$(patsubst %.c,$(OUTDIR)/%.s,$(FILES))
LOGS=$(patsubst %.c,$(OUTDIR)/%.log,$(FILES))
OUTS=$(patsubst %.c,$(OUTDIR)/%.out,$(FILES))
RESULTS=$(patsubst %.c,$(OUTDIR)/%.result,$(FILES))

GCCOUTDIR=gccout
EXPECTS=$(patsubst %.c,%.expect,$(FILES))

.phony: all clean expects

all: $(GCCOUTDIR) $(EXPECTS) $(OUTDIR) $(TARGETS)

$(TARGETS): $(OUTDIR)/%.exe: $(OUTDIR)/%.s
	-$(AS) -o $@ $< > $(OUTDIR)/$*.log 2>&1 && \
	$@ > $(OUTDIR)/$*.out 2>&1 && \
	diff -u --strip-trailing-cr $*.expect $(OUTDIR)/$*.out > $(OUTDIR)/$*.result 2>&1

$(ASMS): $(OUTDIR)/%.s: %.c
	-$(MYCC) $< > $(OUTDIR)/$*.log 2>&1 && mv $*.s $(OUTDIR)/ && mv $*.ast $(OUTDIR)/

$(OUTDIR):
	mkdir -p $(OUTDIR)

clean: 
	-$(RM) -rf $(TARGETS) $(ASMS) $(LOGS) $(OUTS) $(RESULTS)

expects: $(GCCOUTDIR) $(EXPECTS)

$(GCCOUTDIR):
	mkdir -p $(GCCOUTDIR)

$(EXPECTS): %.expect: %.c
	-$(CC) -o $(GCCOUTDIR)/$*.exe $< && \
	$(GCCOUTDIR)/$*.exe > $@


