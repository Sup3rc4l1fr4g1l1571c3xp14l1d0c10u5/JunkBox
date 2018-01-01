CC=../bin/debug/AnsiCParser.exe
AS=gcc -g
FILES=$(shell ls *.c)
OUTDIR=result

TARGETS=$(patsubst %.c,$(OUTDIR)/%.exe,$(FILES))
ASMS=$(patsubst %.c,$(OUTDIR)/%.s,$(FILES))
LOGS=$(patsubst %.c,$(OUTDIR)/%.log,$(FILES))

.phony: all clean

all: $(OUTDIR) $(TARGETS)

$(TARGETS): $(OUTDIR)/%.exe: $(OUTDIR)/%.s
	-$(AS) -o $@ $< > $(OUTDIR)/$*.log 2>&1 && $@ >> $(OUTDIR)/$*.log 2>&1 

$(ASMS): $(OUTDIR)/%.s: %.c
	-$(CC) $< > $(OUTDIR)/$*.log 2>&1 && mv $*.s $(OUTDIR)/ && mv $*.ast $(OUTDIR)/

$(OUTDIR):
	mkdir -p $(OUTDIR)

clean: 
	-$(RM) -rf $(TARGETS) $(ASMS) $(LOGS)



