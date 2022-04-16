; Search code for HIMEM

HIMSAV	equ	0F349h
DOSHIM	equ	0F34Bh
HIMEM	equ	0FC4Ah

	ld	hl, (HIMSAV)
	ld	(DOSHIM), hl
