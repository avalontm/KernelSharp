; Native x86_64 assembly functions for GDT operations
global _StoreGDT
global _LoadGDT
global _SetDataSegments
global _SetStackSegment
global _ReloadCodeSegment
global _LoadTSS

section .text

; void _StoreGDT(GDTPointer* gdtPtr)
; Stores the current GDTR value to the provided pointer
_StoreGDT:
    sgdt [rcx]  ; Store GDTR to pointer in RCX (Windows x64 calling convention)
    ret

; void _LoadGDT(GDTPointer* gdtPtr)
; Loads a new GDT
_LoadGDT:
    lgdt [rcx]  ; Load GDTR from pointer in RCX
    ret

; void _SetDataSegments(ushort selector)
; Sets the DS and ES segment registers
_SetDataSegments:
    mov ax, cx   ; CX contains the selector
    mov ds, ax   ; DS = selector
    mov es, ax   ; ES = selector
    ret

; void _SetStackSegment(ushort selector)
; Sets the SS segment register
_SetStackSegment:
    mov ax, cx   ; CX contains the selector
    mov ss, ax   ; SS = selector
    ret

; void _ReloadCodeSegment(ushort selector)
; Reloads the CS segment register using a far return
_ReloadCodeSegment:
    ; Prepare for far return
    lea rax, [rel .reload_cs]  ; Get address of return point
    push rcx                   ; Push new CS selector
    push rax                   ; Push return address
    
    ; Execute far return to change CS
    o64 retf                   ; 64-bit far return
    
.reload_cs:
    ; Now using the new code segment
    ret

; void _LoadTSS(ushort selector)
; Loads the Task State Segment
_LoadTSS:
    ; Ensure selector has the correct format
    and cx, 0xFFF8          ; Clear RPL and TI bits
    or cx, 0                ; Set RPL to 0 (privilege level 0)
    
    mov ax, cx              ; CX contains the selector
    
    ; Try to load the TSS
    ltr ax
    
    ; Return success if no exception occurred
    ret