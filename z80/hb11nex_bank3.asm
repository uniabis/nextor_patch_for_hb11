
; NEXTOR

CHGBNK equ 7FD0h

	org	76F7h - (OFFSET_TABLE.top - OFFSET_TABLE)

OFFSET_TABLE:

	dw	BANK4_SEGMENT + 1 - .top

	dw	BANK3_HIMEM_PATCH - .top

	dw	BANK4_HIMEM_ADDRESS + 1 - .top
.top:

BANK3_HIMEM_PATCH:
BANK4_HIMEM_ADDRESS:
	ld	hl, 0

NEXTOR_BANK4_JUMPHL:
	push	hl

BANK4_SEGMENT:
	ld	a, 4
	jp	CHGBNK
