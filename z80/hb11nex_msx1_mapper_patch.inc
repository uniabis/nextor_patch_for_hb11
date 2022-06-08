; NEXTOR PATCH for MAPPER on MSX1

CHKCD1 equ 05Ah
CHKCD2 equ 0A5h

MSX1_INIT2_PATCH:

	; set primary slot of page 2 to the same as page 3

	in	a, (PSLOT)
	ld	d, a
	rrca
	rrca
	and	030h
	ld	b, a
	ld	a, d
	and	0CFh
	or	b
	out	(PSLOT), a

	; set secondary slot of page 2 to the same as page 3

	ld	hl, SSLOT
	ld	a, (hl)
	ld	e, a

	; invert secondary slot of page 0 temporaly for check
	xor 0FCh
	ld	(hl), a	

	cpl
	cp	(hl)
	ld	a, e
	jr	nz, .notextended

	cpl
	ld	e, a

	rrca
	rrca
	and	030h
	ld	b, a
	ld	a, e
	and	0CFh
	or	b

.notextended:
	ld	(hl), a

	; check page2

	push	de

	call	checkmapper
	jr	nc, mappernotfound

	; page2 and page3 are mapper

	; set segment of page 2 to initial value of page3
	xor	a
	out	(P2_REG), a

	; copy contents of page 3 to page 2
	ex	de, hl
	ld	hl, 0C000h
	ld	bc,  3FFFh

	push	de

	call	copymapper

	; fix basic work
	pop	hl
	ld	(BOTTOM), hl
	ld	(hl), l
	inc	l
	ld	(TXTTAB), hl

	scf
mappernotfound:
	call	nc, resetmapper

	pop	de

	; restore primary slot
	ld	a, d
	out	(PSLOT), a

	; restore secondary slot
	ld	a, e
	ld	(SSLOT), a

	; set ZF
	xor	a

	ret

copymapper:
	ldir
resetmapper:
	; initialize segments of all pages
	xor	a
	out	(P3_REG),a
	inc	a
	out	(P2_REG),a
	inc	a
	out	(P1_REG),a
	inc	a
	out	(P0_REG),a
	ret


checkmapper:

	ld	hl, 08000h

	ld	a, 1
	out	(P2_REG), a

	ld	b, (hl)
	ld	(hl), CHKCD2

	xor	a
	out	(P2_REG), a

	ld	c, (hl)
	ld	(hl), CHKCD1

	inc	a
	out	(P2_REG), a
	ld	e, (hl)

	xor	a
	out	(P2_REG), a

	ld	d, (hl)
	ld	(hl), c

	inc	a
	out	(P2_REG), a
	ld	(hl), b

	ld	a, CHKCD2
	cp	e
	jr	nz, .nomapper

	ld	a, CHKCD1
	cp	d
	jr	nz, .nomapper

	scf

	; RAM with mapper
	; cf = 1, zf = 1
	ret



.nomapper:

  IFNDEF CHECK_NOMAPPERRAM
	; RAM without mapper or no RAM
	; cf = 0, zf = 0
	or	a
	ret
  ELSE
	ld	a, CHKCD2
	cp	c
	jr	nz, .noram

	ld	a, CHKCD1
	cp	e

.noram:

	; RAM without mapper
	; cf = 0, zf = 1
	; no RAM
	; cf = 0, zf = 0

	ret	nc
	ccf
	ret
  ENDIF
