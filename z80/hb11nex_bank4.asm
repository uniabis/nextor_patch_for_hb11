
	org	7BD0h - 2 * 9

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
.top:

	include	hb11nex_msx1_patch.inc

CLEAN_PATCH:

	call	NO_RTC_PATCH_INIT

	include	hb11nex_hb11_patch.inc

	include	hb11nex_rtc_patch.inc

