
CKCMD2	equ	59D6h
NOTCMD	equ	59EDh

	org	59D7h

	cp	(hl)		;compare with my statement
	jr	nz,NOTCMD	;failed, try next statement
	inc	de
	inc	hl
	and	a		;end of name?
	jr	nz,CKCMD2	;check more characters
