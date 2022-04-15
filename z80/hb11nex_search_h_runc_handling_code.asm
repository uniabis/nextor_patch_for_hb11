; Search code for CLEAN(H.RUNC handling)

H.RUNC equ 0FECBh

	ld	hl, H.RUNC	;set up hook for clean up procedure
	ld	(hl), 0F7h	;instruction code for "RST 30h"
	inc	hl
	ld	(hl),a
	inc	hl
	db	11h		;instruction code for "LD BC, nnnn"
