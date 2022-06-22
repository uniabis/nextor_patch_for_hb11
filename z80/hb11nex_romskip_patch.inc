; NEXTOR PATCH to skip initialization of later slots

; Nextor entries
GSLOT1	equ	402Dh
;CALLB0	equ	403Fh
CALBNK	equ	4042h

ROMSKIP_PATCH:
	;check [ESC]+[BS] key
	ld	a, (NEWKEY+7)
	and	24h
	ret	nz

	; get Nextor kernel slot
	xor	a
	ld	ix, GSLOT1
	call	CALBNK
	ld	e, a

	; calculate spbase 
	ld	hl, 06h + 2
	bit	7, e
	jr	z, .noext1
	ld	l, 0Eh + 2
.noext1:

	add	hl, sp
	; hl=spbase

	; check current slot
	cpi	; (spbase+0)
	ret	nz

	; check value of SLTATR
	xor	a
	cpi	; (spbase+1)
	ret	nz

	; check INIT address in ROM header of page1(4004h)
	ld	a, 04h
	cpi	; (spbase+2)
	ret	nz
	ld	a, 40h
	cpi	; (spbase+3)
	ret	nz

	; get pointer of SLTATR
	ld	c, (hl)	; (spbase+4)
	inc	hl
	ld	b, (hl)	; (spbase+5)
	inc	hl
	inc	hl

	; check high address of EXPTBL
	ld	a, high EXPTBL
	cp	(hl)	; (spbase+7)
	ret	nz

	; set value of SLTATR
	ld	a, 60h ; SLTATR value : STATEMENT + DEVICE
	ld	(bc), a

	dec	hl
	; set low address of EXPTBL for slot 3(0FCC4h)
	ld	(hl), low (EXPTBL+3)	; (spbase+6)
	dec	hl
	; set dummy pointer of SLTATR
	ld	(hl), high (SLTWRK+127)	; (spbase+5)
	dec	hl
	ld	(hl), low (SLTWRK+127)	; (spbase+4)
	dec	hl

	; set INIT high address in ROM header of page2(80h)
	ld	(hl), 080h	; (spbase+3)

	dec	hl
	dec	hl
	dec	hl

	; set current slot to slot 3
	ld	(hl),03h	; (spbase+0)
	bit	7, e
	ret	z
	; set current slot to slot 3-3
	ld	(hl), 8Fh	; (spbase+0)
	ret

