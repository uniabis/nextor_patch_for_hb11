; NO-RTC NEXTOR PATCH

DEFDATE equ ((1984 - 1980) * 512 + 1 * 32 + 1)

; MSX-DOS work
TIMFLG equ 0F338h
F_SPSAVE equ 0F339h

;CURDATE equ 0F33Bh
;CURDATE is used as PATHNAM on DOS2


NO_RTC_PATCH_INIT:

	ld	a, 0Dh
	out	(0B4h), a
	in	a, (0B5h)
	and	04h
	or	08h
	out	(0B5h), a

	ld	a, 0Ah
	out	(0B4h), a
	in	a, (0B5h)
	and	0Eh

	jr	nz, NORTC

CLK_END1 equ $+1
	call	0

	; has RTC
	ld	a, -1

INIT_EXIT:
	ld	(TIMFLG), a

	ret

NORTC:
	ld	a, low DEFDATE
	ld	(F_SPSAVE + 0), a
	ld	a, high DEFDATE
	ld	(F_SPSAVE + 1), a

	xor	a
	jr	INIT_EXIT

GT_DATE_TIME_PATCH:
	ld	a, (TIMFLG)
	or	a

CLK_START1 equ $+1
	jp	nz, 0

	pop	af

	ld	a, (F_SPSAVE + 0)
	ld	h, a
	and	31
	ld	l, a	; day of month
	ld	a, (F_SPSAVE + 1)
	rra
	ld	d, a	; year from 1980
	ld	a, h
	rra
	and	0F0h
	rrca
	rrca
	rrca
	rrca
	ld	h, a	; month

	ld	bc, 0	; hours, minutes
	ld	e, b	; seconds

	ret

SET_DATE_PATCH:
	ld	a, (TIMFLG)
	or	a

CLK_START2 equ $+1
	jp	nz, 0

	pop	af

	push	de

	ld	a, h	; month
	add	a
	add	a
	add	a
	add	a
	add	a
	rl	d	; year from 1980
	or	l	; day of month
	ld	(F_SPSAVE + 0), a
	ld	a, d
	ld	(F_SPSAVE + 1), a

	pop	de

	ret
