; HB-11 NEXTOR PATCH

	DEFINE SKIP_MENU 1

RDSLT equ 000Ch
USRTAB equ 0F39AH

H.INIP equ 0FDC7h
H.STKE equ 0FEDAh

CLEAN equ 4A04h

IDHB11 equ 5DB2h

	org 76A0h

DETECTHB11:
	;CLEAN: Nextor H.RUNC handler
	ld	hl,CLEAN
	push	hl

	ld	de,H.STKE
	ld	hl,hookcode1
	ld	b,hookcode1_end - hookcode1

.lp1:
	ld	a,(de)
	inc	de

	cp	(hl)
	inc	hl
	ret	nz

	djnz	.lp1


	ld	de,IDHB11
	;ld	hl,magic
	ld	b,magic_end - magic

.lp2:
	ex	de,hl
	push	bc
	push	de
	ld	a,083h
	call	RDSLT
	pop	de
	pop	bc
	ex	de,hl
	inc	de

	cp	(hl)
	inc	hl
	ret	nz

	djnz	.lp2

	IF SKIP_MENU

	ld	de,H.INIP
	;ld	hl,hookcode2
	ld	c,hookcode2_end - hookcode2
	ldir

	ld	hl,H.STKE
	ld	(hl),0C9h

	ELSE

	ld	hl,6474h
	ld	(USRTAB),hl

	ENDIF

	ret

hookcode1:
	db	0F7h,83h,60h,5Eh,0C9h
hookcode1_end:

magic:
	db	'HITBIT'
magic_end:

	IF SKIP_MENU

hookcode2:
	db	0F7h,83h,0E9h,5Dh,0C9h
hookcode2_end:

	ENDIF
