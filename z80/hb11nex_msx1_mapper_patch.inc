; NEXTOR PATCH for MAPPER on MSX1

; MSX work

TXTTAB equ 0F676h
BOTTOM equ 0FC48h

; MSX I/O

PSLOT equ 0A8h
P0_REG equ 0FCh
P1_REG equ 0FDh
P2_REG equ 0FEh
P3_REG equ 0FFh

; MSX memory mapped I/O

SSLOT equ 0FFFFh


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
	ld	a, (SSLOT)
	cpl
	ld	e, a
	rrca
	rrca
	and	030h
	ld	b, a
	ld	a, e
	and	0CFh
	or	b
	ld	(SSLOT), a

	push	de

	call	.checkmapper
	jr	nc, .restoreslot


	; set segment of page 2 to initial value
	xor	a
	out	(P2_REG), a

	; copy contents of page 3 to page 2
	ld	hl, 0C000h
	ld	de,  8000h
	ld	bc,  3FFFh
	ldir

	; initialize segments of all pages
	xor	a
	out	(P3_REG),a
	inc	a
	out	(P2_REG),a
	inc	a
	out	(P1_REG),a
	inc	a
	out	(P0_REG),a

	ld	hl, 08000h
	ld	(BOTTOM), hl
	xor	a
	ld	(hl), a
	inc	hl
	ld	(TXTTAB), hl

.restoreslot:

	pop	de

	; restore secondary slot
	ld	a, e
	ld	(SSLOT), a

	; restore primary slot
	ld	a, d
	out	(PSLOT), a

	; set ZF
	xor	a

	ret

.checkmapper:

	ld	hl, 08000h

	ld	a, 1
	out	(P2_REG), a

	ld	b, (hl)
	ld	(hl), 0AAh

	xor	a
	out	(P2_REG), a

	ld	c, (hl)
	ld	(hl), 055h

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

	ld	a, e
	cp	0AAh
	jr	nz, .nomapper

	ld	a, d
	cp	055h
	jr	nz, .nomapper

	scf

	; RAM with mapper
	; cf = 1, zf = 1
	ret

.nomapper:

	ld	a, c
	cp	0AAh
	jr	nz, .noram

	ld	a, e
	cp	055h

.noram:
	scf
	ccf

	; RAM without mapper
	; cf = 0, zf = 1

	; no RAM
	; cf = 0, zf = 0

	ret

