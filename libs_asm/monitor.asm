; Lock and Unlock functions for x86-64 kernel
; These functions provide basic spinlock mechanisms for synchronization

global _Lock
global _Unlock

section .data
    ; Spinlock variable (0 = unlocked, 1 = locked)
    spinlock dd 0

section .text
_Lock:
    ; Acquire spinlock with atomic test-and-set
    push rbx        ; Save rbx
.spin:
    mov eax, 1      ; Value to set
    mov ebx, 0      ; Expected value (unlocked)
    
    ; Atomic compare and exchange
    lock cmpxchg [spinlock], eax
    
    ; If ZF=0, lock was already held, so spin
    jnz .spin
    
    pop rbx         ; Restore rbx
    ret

_Unlock:
    ; Release the spinlock
    mov dword [spinlock], 0
    ret