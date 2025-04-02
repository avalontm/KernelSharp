section .text
global _OutByte
global _InByte
global _OutWord
global _InWord
global _OutDWord
global _InDWord

; void _OutByte(ushort port, byte value)
; RCX = port, RDX = value
_OutByte:
    mov rax, rdx        ; Valor a RAX
    mov rdx, rcx        ; Puerto a RDX
    out dx, al          ; Escribir byte (AL) a puerto DX
    ret

; byte _InByte(ushort port)
; RCX = port, retorno en RAX (AL)
_InByte:
    mov rdx, rcx        ; Puerto a RDX
    xor rax, rax        ; Limpiar RAX
    in al, dx           ; Leer byte de puerto DX a AL
    ret

; void _OutWord(ushort port, ushort value)
; RCX = port, RDX = value
_OutWord:
    mov rax, rdx        ; Valor a RAX
    mov rdx, rcx        ; Puerto a RDX
    out dx, ax          ; Escribir word (AX) a puerto DX
    ret

; ushort _InWord(ushort port)
; RCX = port, retorno en RAX (AX)
_InWord:
    mov rdx, rcx        ; Puerto a RDX
    xor rax, rax        ; Limpiar RAX
    in ax, dx           ; Leer word de puerto DX a AX
    ret

; void _OutDWord(ushort port, uint value)
; RCX = port, RDX = value
_OutDWord:
    mov rax, rdx        ; Valor a RAX
    mov rdx, rcx        ; Puerto a RDX
    out dx, eax         ; Escribir dword (EAX) a puerto DX
    ret

; uint _InDWord(ushort port)
; RCX = port, retorno en RAX (EAX)
_InDWord:
    mov rdx, rcx        ; Puerto a RDX
    xor rax, rax        ; Limpiar RAX
    in eax, dx          ; Leer dword de puerto DX a EAX
    ret