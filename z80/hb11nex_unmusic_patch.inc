; NEXTOR PATCH to release work of MSX-MUSIC 

; MSX-DOS

HIMSAV	equ	0F349h

DOS1SIZFCB	equ	25h
DOS1NUMFCB	equ	0F345h
DOS1FCB	equ	0F353h
DOS1SIZBLD	equ	19h
DOS1BLOADCDDE	equ	0F377h

; MSX-DOS2

_DOSVER	equ	6Fh

DOS2MFLAGS	equ	0F2EFh
DOS2BLDCHK	equ	0F2F1h
DOS2PATHNAM	equ	0F33Bh
DOS2SECBUF	equ	0F34Dh
DOS2MAXSEC	equ	0F34Fh
DOS2DIRBUF	equ	0F351h
DOS2SIZBLD	equ	0Dh

; Nextor

NEXMFLAGKILL	equ	4

; MSX-MUSIC

MUSENA	equ	07FF6h
MUSSLT	equ	0F97Ch
MUSWRK	equ	0F97Dh
MUSHSV	equ	0F9BBh

MUSWSZ	equ	12*6+12*32+9*39 ; 327h(807)

FREE_MSXMUSIC:

	call	CHECK_HIMEM
	dw	MUSWRK

	call	GET_MUSWRK
	ret	z

	bit	0, (hl)
	ret	z

	call	CHECK_FSA1GT

	ld	a, (MUSSLT)

	ld	hl, H.TIMI + 1
	jr	nc, .nomidi

	; A1GT

	ld	hl, H.MDTM + 1

	;ld	a, (MUSSLT)
	cp	(hl)
	ret	nz

	; stop timer
	ld	b, a

	xor	a
	out	(0E9h), a
	out	(0EAh), a

	ld	a, b

.nomidi:

	;ld	a, (MUSSLT)
	cp	(hl)
	ret	nz

	; restore hook

	dec	hl

	ex	de, hl
	ld	hl, MUSHSV
	ld	bc, 5

	di

	ldir

	; free MSX-MUSIC user work

	ld	de, MUSWSZ
	call	ADD_HIMEM

	; dettach FM-PAC I/O

	push	af

	;ld	a, (MUSSLT)
	ld	hl, MUSENA
	call	RDSLT

	ld	e, a
	res	0, e

	pop	af

	;ld	a, (MUSSLT)
	;ld	hl, MUSENA
	call	WRSLT

	; clear MSX-MUSIC slot work

	call	GET_MUSWRK

	res	0, (hl)

	; clear MSX-MUSIC bios work

	ld	a, 0C9h
	ld	(MUSHSV), a

	xor	a
	ld	(MUSSLT), a
	ld	(MUSWRK+0), a
	ld	(MUSWRK+1), a

	ei

	ret



FREE_DISKBASIC2WORK:
	call	CHECK_HIMEM
	dw	DOS2BLDCHK

	ld	de, DOS2SIZBLD
	call	ADD_HIMEM

	ld	a, (DOS2MFLAGS)
	bit	NEXMFLAGKILL, a
	ret	nz

	call	CHECK_HIMEM
	dw	DOS2DIRBUF

	call	CHECK_HIMEM
	dw	DOS2PATHNAM

	ld	hl, (DOS2SECBUF)
	ld	(DOS2DIRBUF), hl
	ld	(DOS2PATHNAM), hl

	ld	de, (DOS2MAXSEC)

ADD_HIMEM:
	ld	hl, (HIMSAV)
	add	hl, de
SET_HIMEM:
	ld	(HIMSAV), hl
	ret



FREE_DISKBASIC1WORK:
	call	CHECK_HIMEM
	dw	DOS1BLOADCDDE+1

	ld	de, DOS1SIZBLD
	call	ADD_HIMEM

	call	CHECK_HIMEM
	dw	DOS1FCB

	;ld	hl, 0
	ld	(DOS1FCB), hl

	ex	de, hl
	;ld d, 0
	;ld	hl, (HIMSAV)

	ld	a, (DOS1NUMFCB)
	ld	b, a
	ld	e, DOS1SIZFCB
.lpfcb:
	add	hl, de
	djnz	.lpfcb

	jr	SET_HIMEM



CHECK_HIMEM:
	pop	hl
	ld	e, (hl)
	inc	hl
	ld	d, (hl)
	inc	hl
	push	hl

	ex	de, hl
	ld	e, (hl)
	inc	hl
	ld	d, (hl)

	ld	hl, (HIMSAV)
	push	hl
	or	a
	sbc	hl, de
	pop	de

	ret	z

	pop	de
	ret



GET_MUSWRK:

	ld	a, (MUSSLT)
	or	a
	ret	z

	; get slot work of page 1

	; x000SSPP
	and	8Fh
	add	a
	jr	c, .extended
	and	06h
.extended:
	; 000SSPP0
	ld	l, a
	and	06h
	; 00000PP0
	add	a
	add	a
	add	a
	add	a
	; 0PP00000
	or	l
	; 0PPSSPP0
	and	78h
	; 0PPSS000
	ld	hl, SLTWRK + 2
	add l
	ld	l, a
	;ret	nc
	;inc	h
	ret

