;
; Memory mapper initializer for MSX1 NEXTOR patch version
;

RDSLT	equ	000Ch
IDBYT2	equ	002Dh

CLRPRIM	equ	0F38Ch
CLRPRI1	equ	0F398h
RS2IQ	equ	0FAF5h
EXPTBL	equ	0FCC1h
DEVICE	equ	0FD99h
H.STKE	equ	0FEDAh

INIT2	equ	0480Ch

	org	7600h

	; return if MSX2 version up adapter initilized.
	;ld	a,(EXPTBL)
	;add	a
	;jr	nz,goinit2

	; return if MSX2 or later
	ld	a,(IDBYT2)
	or	a
	jr	nz,goinit2

	; return if DOS2 initialized.
	ld	a,(DEVICE)
	or	a
	jr	nz,goinit2

	di

	call	hitbit_u

	; page2 pri slot <- page3 pri slot
	in	a,(0A8h)
	ld	d,a
	and	0C0h
	rrca
	rrca
	ld	b,a
	ld	a,d
	and	0CFh
	or	b
	out	(0A8h),a

	; page2 sec slot <- page3 sec slot
	ld	a,(0FFFFh)
	cpl
	ld	e,a
	and	0C0h
	rrca
	rrca
	ld	b,a
	ld	a,e
	and	0CFh
	or	b
	ld	(0FFFFh),a

	; Check if a mapper exists on page2 
	ld	hl,8000h

	xor	a
	out	(0FEh),a
	ld	b,(hl)

	inc	b	;org+1

	ld	(hl),b

	inc	a
	out	(0FEh),a

	inc	b	;org+2

	ld	c,(hl)
	ld	(hl),b

	dec	b	;org+1

	dec	a
	out	(0FEh),a

	ld	a,(hl)
	cp	b
	jr	nz,nomapper1

	inc	b	;org+2

	ld	a,1
	out	(0FEh),a
	
	ld	a,(hl)
	cp	b
	jr	nz,nomapper2

	ld	(hl),c

	dec	b	;org+1
	dec	b	;org+0

	xor	a
	out	(0FEh),a
	ld	(hl),b

	exx

	; copy bios work from page3 to page2 (mapper segment 0)
	ld	hl,0C000h
	ld	de,08000h
	ld	bc,03FFFh
	ldir

	exx

	jr	restoreslot

nomapper2:
	dec	b	;org+1
nomapper1:
	dec	b	;org+0
	ld	(hl),b

restoreslot:
	; restore seconday slot states
	ld	a,e
	ld	(0FFFFh),a

	; restore primary slot states
	ld	a,d
	out	(0A8h),a

resetmapper:
	xor	a
	out	(0FFh),a
	inc	a
	out	(0FEh),a
	inc	a
	out	(0FDh),a
	inc	a
	out	(0FCh),a

	ei

goinit2:
	jp	INIT2

	; workaround for HITBIT-U(HB-11)
	; skip internal software on slot3-0
hitbit_u:

	ld	a,(EXPTBL + 3)
	add	a
	ret	nc

	ld	hl,05DB2h
	ld	de,.magic
	ld	b,.patch_top - .magic

.lp:
	call	.rdslt30

	ld	c,a
	ld	a,(de)
	cp	c
	ret	nz
	inc	hl
	inc	de
	djnz	.lp

	ex	de,hl

.skip_check:

	ld	bc,.patch_end - .patch_top
	ld	de,RS2IQ
	ld	a,e
	ld	(CLRPRIM + 4),a
	ld	a,d
	ld	(CLRPRIM + 5),a

	ldir

	ret

.rdslt30:
	; read code from slot3-0
	push	bc
	push	de
	ld	a,083h
	call	RDSLT
	pop	de
	pop	bc
	ret

.magic:
	db	'HITBIT'


.patch_top:
	push	af
	push	ix
	ex	(sp),hl

	ld	a,05Eh
	cp	h
	jr	nz,.patch_skip
	ld	a,026h
	cp	l
	jr	nz,.patch_skip

	; remove patch
	ld	a,CLRPRI1 & 255
	ld	(CLRPRIM + 4),a
	ld	a,CLRPRI1 / 256
	ld	(CLRPRIM + 5),a

	; skip internal software on slot3-0
	pop	hl
	pop	af
	ret

.patch_skip:
	pop	hl
	pop	af
	jp	(ix)

.patch_end:

