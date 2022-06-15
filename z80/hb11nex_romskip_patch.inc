; ROMSKIP

; Nextor entries
GSLOT1	equ	402Dh
;CALLB0	equ	403Fh
CALBNK	equ	4042h

ROMSKIP_PATCH:
	;check [ESC]+[BS] key
	ld	a, (NEWKEY+7)
	and	24h
	ret	nz

	xor	a
	ld	ix, GSLOT1
	call	CALBNK

	ld	hl, 06h + 2
	add	a
	jr	nc, .noext1
	ld	l, 0Eh + 2
.noext1:

	add	hl, sp

	push	hl
	pop	ix

	ld	a, (ix + 1)
	or	a
	ret	nz

	ld	a, (ix + 2)
	cp	04h
	ret	nz

	ld	a, (ix + 3)
	cp	40h
	ret	nz

	ld	a, (ix + 7)
	cp	0FCh
	ret	nz

	ld	l, (ix + 4)
	ld	h, (ix + 5)

	ld	(hl), 60h ; SLTATR : STATEMENT + DEVICE

	bit	7, (ix + 0)

	ld	a, 03h ; slot 3
	jr	z, .noext2
	ld	a, 8Fh ; slot 3-3
.noext2:

	ld	(ix + 0), a
	ld	(ix + 3), 80h ; page 2
	ld	(ix + 4), 000h
	ld	(ix + 5), 000h
	ld	(ix + 6), 0C4h ; 0FCC4h : EXPTBL for slot 3

	ret
