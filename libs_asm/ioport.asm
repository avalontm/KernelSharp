section .text
global _OutByte
global _InByte
global _OutWord
global _InWord
global _OutDword
global _InDword

; void _OutByte(ushort port, byte value)
; RCX = port, RDX = value
_OutByte:
    mov rax, rdx        ; Value to RAX
    mov rdx, rcx        ; Port to RDX
    out dx, al          ; Write byte (AL) to port DX
    ret

; byte _InByte(ushort port)
; RCX = port, return in RAX (AL)
_InByte:
    mov rdx, rcx        ; Port to RDX
    xor rax, rax        ; Clear RAX
    in al, dx           ; Read byte from port DX to AL
    ret

; void _OutWord(ushort port, ushort value)
; RCX = port, RDX = value
_OutWord:
    mov rax, rdx        ; Value to RAX
    mov rdx, rcx        ; Port to RDX
    out dx, ax          ; Write word (AX) to port DX
    ret

; ushort _InWord(ushort port)
; RCX = port, return in RAX (AX)
_InWord:
    mov rdx, rcx        ; Port to RDX
    xor rax, rax        ; Clear RAX
    in ax, dx           ; Read word from port DX to AX
    ret

; void _OutDword(ushort port, uint value)
; RCX = port, RDX = value
_OutDword:
    mov rax, rdx        ; Value to RAX
    mov rdx, rcx        ; Port to RDX
    out dx, eax         ; Write dword (EAX) to port DX
    ret

; uint _InDword(ushort port)
; RCX = port, return in RAX (EAX)
_InDword:
    mov rdx, rcx        ; Port to RDX
    xor rax, rax        ; Clear RAX
    in eax, dx          ; Read dword from port DX to EAX
    ret