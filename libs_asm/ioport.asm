; 64-bit I/O operations section
section .text
global _In8
global _Out8
global _In16
global _Out16
global _In32
global _Out32

section .data
; Read a single byte from an I/O port
; Input: Port in RCX (32 bits)
; Output: Byte read in AL
_In8:
    mov edx, ecx   ; Move port to EDX
    xor eax, eax   ; Clear EAX (ensure upper bits are zero)
    in al, dx      ; Read byte from port to AL
    ret

; Write a single byte to an I/O port
; Input: Port in RCX (32 bits), value in RDX (8 bits)
_Out8:
    mov eax, edx   ; Move value to EAX (ensure upper bits are cleared)
    mov edx, ecx   ; Move port to EDX
    out dx, al     ; Write byte (AL) to port
    ret

; Read a 16-bit word from an I/O port
; Input: Port in RCX (32 bits)
; Output: Word read in AX
_In16:
    mov edx, ecx   ; Move port to EDX
    xor eax, eax   ; Clear EAX (ensure upper bits are zero)
    in ax, dx      ; Read word from port to AX
    ret

; Write a 16-bit word to an I/O port
; Input: Port in RCX (32 bits), value in RDX (16 bits)
_Out16:
    mov eax, edx   ; Move value to EAX
    mov edx, ecx   ; Move port to EDX
    out dx, ax     ; Write word (AX) to port
    ret

; Read 32 bits from an I/O port
; Input: Port in CX (16 bits)
; Output: 32-bit value in EAX
_In32:
    mov dx, cx     ; Move port to DX (16 bits)
    in eax, dx     ; Read 32 bits from port directly to EAX
    ret
    
; Write 32 bits to an I/O port
; Input: Port in CX (16 bits), value in EDX (32 bits)
_Out32:
    mov dx, cx     ; Move port to DX (16 bits)
    mov eax, edx   ; Move value to EAX for output operation
    out dx, eax    ; Write 32 bits to port
    ret