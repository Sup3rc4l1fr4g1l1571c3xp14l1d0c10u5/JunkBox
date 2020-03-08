	.text
	.rdata
LC0:
	.ascii "hello, world\0"

	.text
	.globl	_main
	.globl ___main
	.globl _printf
_main:
	pushl	%ebp
	movl	%ebp, %esp
	andl	%esp, $-16
	subl	%esp, $16
	call	$___main
	movl	(%esp), $LC0
	call	$_printf
	movl	%eax, $0
	leave
	ret

