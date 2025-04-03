; Lock and Unlock implementation for Monitor class
; For x86-64 architecture

section .data
    lock_var: dq 0           ; Global lock variable (quadword - 64 bits)

section .text
    global _Lock
    global _Unlock

; _Lock function - implements a spin lock
_Lock:
    push rbp                 ; Save base pointer
    mov rbp, rsp             ; Set new base pointer

    ; Try to acquire the lock using atomic compare and exchange
.retry:
    mov rax, 0               ; Expected value (0 = unlocked)
    mov rdx, 1               ; New value (1 = locked)
    lock cmpxchg [lock_var], rdx  ; Atomic compare and exchange
    jnz .spin                ; Jump if we didn't get the lock

    ; Lock acquired
    pop rbp                  ; Restore base pointer
    ret                      ; Return

.spin:
    ; Optional: Add a pause instruction for better performance in spin loops
    pause                    ; Hint to processor that this is a spin loop
    
    ; Optional: Add a short delay or yield to other threads
    ; For a more sophisticated implementation, you might consider:
    ; - Using PAUSE instruction (already added)
    ; - Yielding to the OS scheduler after some attempts
    ; - Implementing exponential backoff

    jmp .retry               ; Try again

; _Unlock function
_Unlock:
    push rbp                 ; Save base pointer
    mov rbp, rsp             ; Set new base pointer
    
    ; Release the lock
    mov QWORD [lock_var], 0  ; Set lock to 0 (unlocked)
    
    ; Optional: Add memory barrier to ensure visibility of changes
    ; before releasing the lock
    mfence                   ; Memory fence instruction
    
    pop rbp                  ; Restore base pointer
    ret                      ; Return