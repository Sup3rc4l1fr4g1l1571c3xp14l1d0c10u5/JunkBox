.memory 0x10000
.code  0x0010
.data  0x8000
.rdata 0xC000

.data
buf:
	.u8	0x01,0x02,0x03,0x04,0x05,0x06,0x07,0x08,0x09,0x0A

.code
	movi.u32	%r2, 0
	movi.u32	%r3, 0
	mov			%r4, %r0

loop:
	cmpi.u32	%r3, 10
	jeq			end
	load.u8		%r4, %r3, buf
	add.u32		%r2, %r2, %r4
	addi.u32	%r3, %r3, 1
	jmp			loop
end:
	halt

