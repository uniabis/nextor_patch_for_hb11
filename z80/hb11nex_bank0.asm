
CHGBNK equ 7FD0h

	org	76E0h - 2 * 5

OFFSET_TABLE:

	dw	BANK4_SEGMENT + 1 - .top

	dw	BANK0_INIT2_PATCH - .top
	dw	BANK0_CLEAN_PATCH - .top

	dw	BANK4_INIT2_ADDRESS + 1 - .top
	dw	BANK4_CLEAN_ADDRESS + 1 - .top
.top:

BANK0_INIT2_PATCH:
	push	hl

BANK4_INIT2_ADDRESS:
	ld	hl, 0
	jr	NEXTOR_BANK4_JUMP

BANK0_CLEAN_PATCH:
	push	hl

BANK4_CLEAN_ADDRESS:
	ld	hl, 0

NEXTOR_BANK4_JUMP:
	ex	(sp), hl

BANK4_SEGMENT:
	ld	a, 4
	jp	CHGBNK
