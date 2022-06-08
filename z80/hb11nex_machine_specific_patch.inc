; MACHINE SPECIFIC NEXTOR PATCH

; SONY

HB11_FIRMWARE_SLOT equ 83h
HB11_ID_ADDRESS equ 5DB2h

HB101_FIRMWARE_SLOT equ 03h
HB101_ID_ADDRESS equ 4027h

; Hitachi

MBH3_FIRMWARE_SLOT equ 83h
MBH3_ID_ADDRESS equ 4308h

; Matsushita

FS4600F_FIRMWARE1_SLOT equ 88h
FS4600F_ID_ADDRESS equ 0B9C6h
FS4600F_FIRMWARE2_SLOT equ 87h

; ASCII MSX-Write

MSXWRTIE_ID_ADDRESS equ 9A32h

DETECT_MACHINES:

DETECT_HB11:

	ld	hl, HB11_H_STKE
	call	CMP_HSTKE
	jr	nz, DETECT_HB101

	ld	de, HB11_ID_ADDRESS
	ld	c, HB11_FIRMWARE_SLOT
	call	CMP_HITBIT
	jr	z, FOUND_HB11

DETECT_HB101:
	ld	hl, HB101_H_STKE
	call	CMP_HSTKE
	jr	nz, DETECT_MBH3

	ld	de, HB101_ID_ADDRESS
	ld	c, HB101_FIRMWARE_SLOT
	call	CMP_HITBIT
	jr	z, FOUND

DETECT_MBH3:
	ld	hl, MBH3_H_STKE
	call	CMP_HSTKE
	jr	nz, DETECT_MSXWRITE

	call	CMP_MBH3
	jr	z, FOUND

DETECT_MSXWRITE:
	call	CHECK_MSXWRITE
	jr	z, FOUND

	;check [DOWN] key
	ld	a, (NEWKEY+8)
	bit	6, a
	jr	z, FOUND
	ret

FOUND_HB11:

	; HB-11

	ld	de, H.INIP
	;ld	hl, HB11_H_INIP
	;ld	bc, 5
	ld	c, 5
	ldir

	; work around for Word land sentence II
	ld	a, 0F3h
	ld	(0D001h), a

FOUND:

	ld	a, 0C9h
	ld	(H.STKE), a

	ret

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

	ret

CMP_SLOT:
	pop	hl
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

	ret

CMP_HITBIT:
	ld	b, .magic_end - .magic
	call	CMP_SLOT
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

CMP_MBH3:
	ld	bc, MBH3_FIRMWARE_SLOT+(.magic_end - .magic)*256
	ld	de, MBH3_ID_ADDRESS
	call	CMP_SLOT
.magic:
	db	'H3',0,'HCOPY'
.magic_end:

MBH3_H_STKE:
MSXWRITE_H_STKE:
	rst	CALLF
	db	MBH3_FIRMWARE_SLOT
	dw	402Eh
	ret

CHECK_MSXWRITE:
	ld	hl, H.STKE
	ld	a, 0F7h
	cpi
	ret	nz

	ld	c, (hl)
	inc	hl

	ld	b, 3
	ld	de, MSXWRITE_H_STKE + 2
	call	CMP_HSTKE.lp
	ret	nz

	ld	b, .magic_end - .magic
	ld	de, MSXWRTIE_ID_ADDRESS
	call	CMP_SLOT
.magic:
	db	'YAMAHA-MSX-JWP-SYSTEM'
.magic_end:


FS4600F_INIT2_PATCH:
	call	.check
	ret	nz

	ld	hl, 7FF9h
	ld	de, 0 + FS4600F_FIRMWARE2_SLOT*256
	call	.wrsltde

	ld	hl, 6000h
	ld	de, 4 + FS4600F_FIRMWARE2_SLOT*256
.wrsltde:
	ld	a, d
	jp	WRSLT

.check:
	ld	bc, FS4600F_FIRMWARE1_SLOT+(.magic_end - .magic)*256
	ld	de, FS4600F_ID_ADDRESS
	call	CMP_SLOT
.magic:
	db	'Matsushita 1985'
.magic_end:

