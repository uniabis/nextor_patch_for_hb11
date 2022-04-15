; Search code for RTC

CLK_END:

	ld	a, 0Dh
	out	(0B4h), a
	in	a, (0B5h)
	or	08h
	out	(0B5h), a
	ret

CLK_START:

	ld	a, 0Dh
	out	(0B4h), a
	in	a, (0B5h)
	and	04h
	out	(0B5h), a
	ret

GT_DATE_TIME:

	db	0CDh	; opcode CALL
