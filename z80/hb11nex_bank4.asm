
	include	hb11nex_msx_common.inc


; NEXTOR

CHGBNK equ 7FD0h

	org	7BD0h - (OFFSET_TABLE.top - OFFSET_TABLE)

OFFSET_TABLE:

	dw	INIT2_PATCH - .top
	dw	CLEAN_PATCH - .top

	dw	ORIGINAL_INIT2_ADDRESS - .top
	dw	ORIGINAL_CLEAN_ADDRESS - .top

	dw	GT_DATE_TIME_PATCH - .top
	dw	SET_DATE_PATCH - .top

	dw	CLK_START1 - .top
	dw	CLK_START2 - .top
	dw	CLK_END1 - .top

	dw	HIMEM_PATCH_DOS2 - .top
	dw	HIMEM_PATCH_DOS1 - .top

.top:

INIT2_PATCH:
	call	ROMSKIP_PATCH

	call	DETECT_MACHINES_INIT2

	;INIT2: Nextor initialize
ORIGINAL_INIT2_ADDRESS equ $+1
	ld	hl, 0

	jr	JUMPHL_NEXTOR_BANK0

CLEAN_PATCH:

	call	NO_RTC_PATCH_INIT

	call	DETECT_MACHINES_CLEAN

	;CLEAN: Nextor H.RUNC handler
ORIGINAL_CLEAN_ADDRESS equ $+1
	ld	hl, 0

JUMPHL_NEXTOR_BANK0:
	push	hl
	xor	a
JUMP_NEXTOR_CHGBNK
	jp	CHGBNK

HIMEM_PATCH_DOS2:
	call	FREE_DISKBASIC2WORK
	call	FREE_MSXMUSIC
	call	FREE_DISKBASIC2WORK

	xor	a
	jr	HIMEM_RET

HIMEM_PATCH_DOS1:
	call	FREE_DISKBASIC1WORK
	call	FREE_MSXMUSIC
	call	FREE_DISKBASIC1WORK

	ld	a, 3
HIMEM_RET:
	ld	hl, (HIMSAV)
	ld	(HIMEM), hl
	jr	JUMP_NEXTOR_CHGBNK

	include	hb11nex_machine_specific_patch.inc

DETECT_MACHINES_INIT2:
	ld	a, (IDBYT2)
	or	a

	jr	nz, PANASONIC_INIT2_PATCH
	;jr	MSX1_INIT2_PATCH

	include	hb11nex_msx1_mapper_patch.inc

	include	hb11nex_nortc_patch.inc

	include	hb11nex_unmusic_patch.inc

	include	hb11nex_romskip_patch.inc
	;assert $<=CHGBNK
