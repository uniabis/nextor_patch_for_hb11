; Search code for HB-11

SRCBAS	equ	7DEEh
BASROM	equ	0FBB1h

	ld	a, (BASROM)
	or	a
	ld	ix, SRCBAS - 5	;Because BASIC cartridge is no more enabled.
