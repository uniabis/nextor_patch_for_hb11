; HB-11/101/201 NEXTOR PATCH

; MSX BIOS

RDSLT equ 000Ch
CALLF equ 0030h
USRTAB equ 0F39AH

; MSX WORK

H.INIP equ 0FDC7h
H.STKE equ 0FEDAh

; NEXTOR

CHGBNK equ 7FD0h

; SONY

HB11_FIRMWARE_SLOT equ 83h
HB11_ID_ADDRESS equ 5DB2h

HB101_FIRMWARE_SLOT equ 03h
HB101_ID_ADDRESS equ 4027h

DETECT_HB11:

	ld	hl,HB11_H_STKE
	call	CMP_HSTKE
	jr	nz, DETECT_HB101

	ld	de,HB11_ID_ADDRESS
	ld	c, HB11_FIRMWARE_SLOT
	call	CMP_HITBIT
	jr	nz, NOTFOUND

	ld	de, H.INIP
	;ld	hl, HB11_H_INIP
	;ld	bc, 5
	ld	c, 5
	ldir

	; work around for Word land sentence II
	ld	a, 0F3h
	ld	(0D001h), a

FOUND:

	ld	hl, H.STKE
	ld	(hl), 0C9h

NOTFOUND:
	push	hl

	;CLEAN: Nextor H.RUNC handler
ORIGINAL_CLEAN_ADDRESS:
	ld	hl, 0

JUMP_NEXTOR_BANK0:
	ex	(sp), hl

	xor	a
	jp	CHGBNK

DETECT_HB101:
	ld	hl, HB101_H_STKE
	call	CMP_HSTKE
	jr	nz, NOTFOUND

	ld	de, HB101_ID_ADDRESS
	ld	c, HB101_FIRMWARE_SLOT
	call	CMP_HITBIT
	jr	nz, NOTFOUND
	jr	FOUND

CMP_HSTKE:
	ld	de, H.STKE
	ld	b, 5
.lp:
	ld	a, (de)
	inc	de

	cp	(hl)
	inc	hl
	ret	nz

	djnz	.lp

	;xor	a
	ret

CMP_HITBIT:
	ld	hl, .magic
	ld	b, .magic_end - .magic

.lp:
	ex	de, hl
	push	bc
	push	de
	ld	a, c
	call	RDSLT
	pop	de
	pop	bc
	ex	de, hl
	inc	de

	cp	(hl)
	inc	hl
	ret	nz

	djnz	.lp

	;xor	a
	ret

.magic:
	db	'HITBIT'
.magic_end:

HB11_H_INIP:
	rst	CALLF
	db	HB11_FIRMWARE_SLOT
	dw	5DE9h
	ret

HB11_H_STKE:
	rst	CALLF
	db	HB11_FIRMWARE_SLOT
	dw	5E60h
	ret

HB101_H_STKE:
	rst	CALLF
	db	HB101_FIRMWARE_SLOT
	dw	4043h
	ret
