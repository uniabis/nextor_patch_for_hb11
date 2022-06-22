
CKCMD2	equ	59D6h
NOTCMD	equ	59EDh

	;cp	(hl)		;compare with my statement
	jp	nz,NOTCMD	;failed, try next statement
	inc	de
	inc	hl
	and	a		;end of name?
	jp	nz,CKCMD2	;check more characters
	jp	59DFh
