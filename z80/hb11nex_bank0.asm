
; NEXTOR

CHGBNK equ 7FD0h

	org	76E0h - (OFFSET_TABLE.top - OFFSET_TABLE)

OFFSET_TABLE:

	dw	BANK4_SEGMENT - .top

	dw	BANK4_HI_ADDRESS - .top

	dw	BANK0_INIT2_PATCH - .top
	dw	BANK4_INIT2_LO_ADDRESS - .top

	dw	BANK0_CLEAN_PATCH - .top
	dw	BANK4_CLEAN_LO_ADDRESS - .top

	dw	BANK0_HIMEM_PATCH - .top
	dw	BANK4_HIMEM_LO_ADDRESS - .top
.top:

BANK0_INIT2_PATCH:
BANK4_INIT2_LO_ADDRESS equ $+1
	ld	a, 0

	db	21h		;instruction code for "LD HL, nnnn"
BANK0_CLEAN_PATCH:
BANK4_CLEAN_LO_ADDRESS equ $+1
	ld	a, 0

	db	21h		;instruction code for "LD HL, nnnn"
BANK0_HIMEM_PATCH:
BANK4_HIMEM_LO_ADDRESS equ $+1
	ld	a, 0

;	db	21h		;instruction code for "LD HL, nnnn"
;4TH_PATCH:
;BANK4_4TH_LO_ADDRESS equ $+1
;	ld	a, 0

	ld	l, a
BANK4_HI_ADDRESS equ $+1
	ld	h, 0

NEXTOR_BANK4_JUMPHL:
	push	hl

BANK4_SEGMENT equ $+1
	ld	a, 4
	jp	CHGBNK
