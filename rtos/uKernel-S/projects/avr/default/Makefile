###############################################################################
# Makefile for the project uKernel
###############################################################################

## General Flags
PROJECT = uKernel
MCU = atmega328p
TARGET = uKernel.elf
CC = avr-gcc

CPP = avr-g++

## Options common to compile, link and assembly rules
COMMON = -mmcu=$(MCU)

## Compile options common for all C compilation units.
CFLAGS = $(COMMON)
CFLAGS += -Wall -gdwarf-2 -W   -std=gnu99                            -DF_CPU=20000000UL -Os -funsigned-char -funsigned-bitfields -fpack-struct -fshort-enums
CFLAGS += -MD -MP -MT $(*F).o -MF dep/$(@F).d 

## Assembly specific flags
ASMFLAGS = $(COMMON)
ASMFLAGS += $(CFLAGS)
ASMFLAGS += -x assembler-with-cpp -Wa,-gdwarf2

## Linker flags
LDFLAGS = $(COMMON)
LDFLAGS += -nostdlib  -nodefaultlibs  -nostartfiles -Wl,-Map=uKernel.map


## Intel Hex file production flags
HEX_FLASH_FLAGS = -R .eeprom -R .fuse -R .lock -R .signature

HEX_EEPROM_FLAGS = -j .eeprom
HEX_EEPROM_FLAGS += --set-section-flags=.eeprom="alloc,load"
HEX_EEPROM_FLAGS += --change-section-lma .eeprom=0 --no-change-warnings


## Include Directories
INCLUDES = -I"C:\uKernel\projects\avr\src" 

## Objects that must be built in order to link
OBJECTS = kernel.o message.o semapho.o syscall.o hw.o startup.o main.o interrupt.o 

## Objects explicitly added by the user
LINKONLYOBJECTS = 

## Build
all: $(TARGET) uKernel.hex uKernel.eep uKernel.lss size

## Compile
kernel.o: ../../../src/kernel.c
	$(CC) $(INCLUDES) $(CFLAGS) -c  $<

message.o: ../../../src/message.c
	$(CC) $(INCLUDES) $(CFLAGS) -c  $<

semapho.o: ../../../src/semapho.c
	$(CC) $(INCLUDES) $(CFLAGS) -c  $<

syscall.o: ../../../src/syscall.c
	$(CC) $(INCLUDES) $(CFLAGS) -c  $<

hw.o: ../../../arch/avr/hw.c
	$(CC) $(INCLUDES) $(CFLAGS) -c  $<

startup.o: ../../../arch/avr/startup.S
	$(CC) $(INCLUDES) $(ASMFLAGS) -c  $<

main.o: ../../../sample/main.c
	$(CC) $(INCLUDES) $(CFLAGS) -c  $<

interrupt.o: ../../../arch/avr/interrupt.c
	$(CC) $(INCLUDES) $(CFLAGS) -c  $<

##Link
$(TARGET): $(OBJECTS)
	 $(CC) $(LDFLAGS) $(OBJECTS) $(LINKONLYOBJECTS) $(LIBDIRS) $(LIBS) -o $(TARGET)

%.hex: $(TARGET)
	avr-objcopy -O ihex $(HEX_FLASH_FLAGS)  $< $@

%.eep: $(TARGET)
	-avr-objcopy $(HEX_EEPROM_FLAGS) -O ihex $< $@ || exit 0

%.lss: $(TARGET)
	avr-objdump -h -S $< > $@

size: ${TARGET}
	@echo
	@avr-size -C --mcu=${MCU} ${TARGET}

## Clean target
.PHONY: clean
clean:
	-rm -rf $(OBJECTS) uKernel.elf dep/* uKernel.hex uKernel.eep uKernel.lss uKernel.map


## Other dependencies
-include $(shell mkdir dep 2>/dev/null) $(wildcard dep/*)

