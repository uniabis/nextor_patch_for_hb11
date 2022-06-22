; MACHINE SPECIFIC NEXTOR PATCH

DETECT_MACHINES:

	call	CHECK_HITBIT
	db	83h
	dw	5E60h, 5DB2h
	jr	z, .foundHB11

	call	CHECK_SELECT_KEY
	jr	z, .hitSelect1

	call	CHECK_HITBIT
	db	03h
	dw	4043h, 4027h
	jr	z, FOUND

	call	CHECK_HITBIT
	db	87h
	dw	4076h, 4042h
	jr	z, FOUND

	call	CHECK_HITBIT
	db	87h
	dw	4079h, 4045h
	jr	z, FOUND

	call	CHECK_HITBIT
	db	84h
	dw	4051h, 4027h
	jr	z, FOUND

	call	CHECK_HITBIT
	db	87h
	dw	405Ch, 40B8h
	jr	z, FOUND

	call	CHECK_MBH3
	jr	z, FOUND

	call	CHECK_MSXWRITE
	jr	z, FOUND

.checkDown:
	;check [DOWN] key
	ld	a, (NEWKEY+8)
	bit	6, a
	jr	z, FOUND
	ret

.hitSelect1:
	ld	a, (H.STKE)
	cp	0C9h
	jr	z, .checkDown
.hitSelect2:
	ld	hl, 6474h
	ld	(USRTAB), hl
	jr	.checkDown

.foundHB11:

	; HB-11

	ld	de, H.INIP
	;ld	hl, HB11_H_INIP
	;ld	bc, 5
	ld	c, 5
	ldir

	; work around for Word land sentence II
	ld	a, 0F3h
	ld	(0D001h), a

	call	CHECK_SELECT_KEY
	jr	z, .hitSelect2

FOUND:

	ld	hl, H.STKE
	ld	bc, 05C9h
.lp:
	ld	(hl), c
	inc	hl
	djnz .lp

	ret

CHECK_H_STEK_402E:
	ld	de, 402Eh

	; [in]
	;  a:slot
	;  de:handler
	; [out]
	;  ZF:1(match)
	;  c:slot
	; [work]
	;  a, hl
CHECK_H_STEK:
	ld	c, a
	ld	hl, H.STKE
	ld	a, (hl)
	cp	0F7h
	ret	nz
	inc	hl
	ld	a, (hl)
	cp	c
	ret	nz
	inc	hl
	ld	a, (hl)
	cp	e
	ret	nz
	inc	hl
	ld	a, (hl)
	cp	d
	ret	nz
	inc	hl
	ld	a, (hl)
	cp	0C9h
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

CHECK_HITBIT:
	pop	hl

	ld	a, (hl)
	inc	hl
	ld	e, (hl)
	inc	hl
	ld	d, (hl)
	inc	hl
	ld	c, (hl)
	inc	hl
	ld	b, (hl)
	inc	hl

	push	hl

	push	bc

	call	CHECK_H_STEK

	pop	de

	ret	nz

	ld	b, .magic_end - .magic
	call	CMP_SLOT
.magic:
	db	'HITBIT'
.magic_end:

HB11_H_INIP:
	rst	CALLF
	db	83h
	dw	5DE9h
	ret

CHECK_MBH3:
	ld	a, 83h
	call	CHECK_H_STEK_402E
	ret	nz

	ld	b, .magic_end - .magic
	ld	de, 4308h
	call	CMP_SLOT
.magic:
	db	'H3',0,'HCOPY'
.magic_end:

CHECK_MSXWRITE:
	ld	a, (H.STKE+1)
	call	CHECK_H_STEK_402E
	ret	nz

	ld	b, .magic_end - .magic
	ld	de, 9A32h
	call	CMP_SLOT
.magic:
	db	'YAMAHA-MSX-JWP-SYSTEM'
.magic_end:


CHECK_SELECT_KEY:
	;check [SELECT] key
	ld	a, (NEWKEY+7)
	and	40h
	ret

PANASONIC_INIT2_PATCH:

	call	CHECK_SELECT_KEY
	ret	z

	ld	a, 23h
	;FS-A1
	ld	(0CBD8H), a
	ld	(0C3D2H), a
	ld	(0C3CEH), a

	;FS-A1WX/ST
	ld	hl, .magic_vje80a
	ld	de, 0C010h
	ld	bc, .magic_vje80a_end - .magic_vje80a

	ldir

	call	CHECK_FS4600F
	ret	nz

	ld	hl, 7FF9h
	ld	de, 8700h
	call	.wrsltde

	ld	hl, 6000h
	ld	de, 8704h
.wrsltde:
	ld	a, d
	jp	WRSLT

.magic_vje80a:
	db	'JWrite'
.magic_vje80a_end:

CHECK_FS4600F:
	ld	a, (EXPTBL)
	add	a
	jr	nc, .noext

	ld	bc, 88h+(.magic_fs4600f_end - .magic_fs4600f)*256
	ld	de, 0B9C6h
	call	CMP_SLOT
.magic_fs4600f:
	db	'Matsushita 1985'
.magic_fs4600f_end:

.noext:
	pop	hl
	ret


