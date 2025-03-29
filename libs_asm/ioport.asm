section .text
global _OutByte
global _InByte
global _OutWord
global _InWord
global _OutDWord
global _InDWord

; void OutByte(ushort port, byte value)
_OutByte:
    mov dx, [esp + 4]    ; port
    mov al, [esp + 8]    ; value
    out dx, al
    ret

; byte InByte(ushort port)
_InByte:
    mov dx, [esp + 4]    ; port
    xor eax, eax
    in al, dx
    ret

; void OutWord(ushort port, ushort value)
_OutWord:
    mov dx, [esp + 4]    ; port
    mov ax, [esp + 8]    ; value
    out dx, ax
    ret

; ushort InWord(ushort port)
_InWord:
    mov dx, [esp + 4]    ; port
    xor eax, eax
    in ax, dx
    ret

; void OutDWord(ushort port, uint value)
_OutDWord:
    mov dx, [esp + 4]    ; port
    mov eax, [esp + 8]   ; value
    out dx, eax
    ret

; uint InDWord(ushort port)
_InDWord:
    mov dx, [esp + 4]    ; port
    in eax, dx
    ret