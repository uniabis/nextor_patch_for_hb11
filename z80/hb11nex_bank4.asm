
	include	hb11nex_msx_common.inc


; NEXTOR

CHGBNK equ 7FD0h

	org	7BD0h - (OFFSET_TABLE.top - OFFSET_TABLE)

OFFSET_TABLE:

	dw	INIT2_PATCH - .top
	dw	CLEAN_PATCH - .top

	dw	ORIGINAL_INIT2_ADDRESS + 1 - .top
	dw	ORIGINAL_CLEAN_ADDRESS + 1 - .top

	dw	GT_DATE_TIME_PATCH - .top
	dw	SET_DATE_PATCH - .top

	dw	CLK_START1 + 1 - .top
	dw	CLK_START2 + 1 - .top
	dw	CLK_END1 + 1 - .top

	dw	HIMEM_PATCH_DOS2 - .top
	dw	HIMEM_PATCH_DOS1 - .top

.top:

INIT2_PATCH:

	ld	a, (IDBYT2)
	or	a

	call	z, MSX1_INIT2_PATCH
	call	nz, FS4600F_INIT2_PATCH

	;INIT2: Nextor initialize
ORIGINAL_INIT2_ADDRESS:
	ld	hl, 0

	jr	JUMPHL_NEXTOR_BANK0

CLEAN_PATCH:

	call	NO_RTC_PATCH_INIT

	call	DETECT_MACHINES

	;CLEAN: Nextor H.RUNC handler
ORIGINAL_CLEAN_ADDRESS:
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

	include	hb11nex_msx1_mapper_patch.inc

	include	hb11nex_machine_specific_patch.inc

	include	hb11nex_nortc_patch.inc

	include	hb11nex_unmusic_patch.inc

	;assert $<=CHGBNK
