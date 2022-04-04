
; MSX BIOS

IDBYT2 equ 002Dh

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


INIT2_PATCH:

	ld	a, (IDBYT2)
	or	a

	call	z, MSX1_INIT2_PATCH

	push	hl

	;INIT2: Nextor initialize
ORIGINAL_INIT2_ADDRESS:
	ld	hl, 0

	jp	JUMP_NEXTOR_BANK0

MSX1_INIT2_PATCH:

	; ページ2のプライマリースロットがページ3と同じになるようにする
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

	; ページ2のセカンダリースロットがページ3と同じになるようにする
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


	; ページ2をページ3初期セグメント(0)に設定
	xor	a
	out	(P2_REG), a

	; ページ3の内容をページ2にコピー
	ld	hl, 0C000h
	ld	de,  8000h
	ld	bc,  3FFFh
	ldir

	; マッパーのセグメント選択状況を初期化(3-2-1-0)
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

	; ページ3のスロットのセカンダリースロットを復元
	ld	a, e
	ld	(SSLOT), a

	; プライマリースロットを復元
	ld	a, d
	out	(PSLOT), a

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

	; マッパーRAM
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

	; マッパーではないRAM
	; cf = 0, zf = 1

	; RAMではない
	; cf = 0, zf = 0

	ret

