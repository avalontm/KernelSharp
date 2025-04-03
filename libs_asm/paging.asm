; paging64.asm
; Low-level functions for paging and CPU control in x86-64
; To be compiled with NASM

section .text
global _LoadIDT              ; Load IDT
global _ReadCR0              ; Read CR0 register
global _WriteCR0             ; Write to CR0 register
global _ReadCR2              ; Read CR2 register (page fault address)
global _ReadCR3              ; Read CR3 register (page directory base)
global _WriteCR3             ; Write to CR3 register
global _ReadCR4              ; Read CR4 register
global _WriteCR4             ; Write to CR4 register
global _Invlpg               ; Invalidate a TLB entry
global _EnableInterrupts     ; Enable interrupts
global _DisableInterrupts    ; Disable interrupts
global _Hlt                  ; HLT instruction
global _Reset                ; System reset
global _GetRFlags            ; Get RFLAGS
global _SetRFlags            ; Set RFLAGS

; Load IDT
; void _LoadIDT(void* idtPointer)
_LoadIDT:
    push rbp                 ; Save base pointer
    mov rbp, rsp             ; Set up new base pointer
    
    ; Save critical registers that might be affected
    push rax
    
    ; Disable interrupts while loading IDT
    cli
    
    ; Load the IDT using LIDT instruction
    ; RDI already contains the address of the IDTR structure
    lidt [rdi]
    
    ; Brief delay to ensure IDT is properly loaded
    ; This can help with some hardware that might need time to process
    nop
    nop
    nop
    
    ; Re-enable interrupts
    sti
    
    ; Restore registers
    pop rax
    
    ; Restore stack frame
    pop rbp
    
    ; Return
    ret

; Read the value of CR0 register
; uint64_t _ReadCR0()
_ReadCR0:
    mov rax, cr0            ; Move CR0 to RAX (return value)
    ret

; Write a value to CR0 register
; void _WriteCR0(uint64_t value)
_WriteCR0:
    push rbx
    mov rbx, [rsp+16]       ; Get value parameter
    mov cr0, rbx            ; Move value to CR0
    pop rbx
    ret

; Read the value of CR2 register (address that caused a page fault)
; uint64_t _ReadCR2()
_ReadCR2:
    mov rax, cr2            ; Move CR2 to RAX (return value)
    ret

; Read the value of CR3 register (page directory base)
; uint64_t _ReadCR3()
_ReadCR3:
    mov rax, cr3            ; Move CR3 to RAX (return value)
    ret

; Write a value to CR3 register (load a new page directory)
; void _WriteCR3(uint64_t value)
_WriteCR3:
    push rbx
    mov rbx, [rsp+16]       ; Get value parameter
    mov cr3, rbx            ; Move value to CR3
    pop rbx
    ret

; Read the value of CR4 register
; uint64_t _ReadCR4()
_ReadCR4:
    mov rax, cr4            ; Move CR4 to RAX (return value)
    ret

; Write a value to CR4 register
; void _WriteCR4(uint64_t value)
_WriteCR4:
    push rbx
    mov rbx, [rsp+16]       ; Get value parameter
    mov cr4, rbx            ; Move value to CR4
    pop rbx
    ret

; Invalidate a TLB entry for a specific address
; void _Invlpg(void* address)
_Invlpg:
    push rbx
    mov rbx, [rsp+16]       ; Get address parameter
    invlpg [rbx]            ; Invalidate TLB entry for the address
    pop rbx
    ret

; Enable interrupts
; void _EnableInterrupts()
_EnableInterrupts:
    sti                     ; Set interrupt flag
    ret

; Disable interrupts
; void _DisableInterrupts()
_DisableInterrupts:
    cli                     ; Clear interrupt flag
    ret

; Halt CPU execution until an interrupt occurs
; void _Hlt()
_Hlt:
    hlt                     ; Halt instruction
    ret

; Reset the system using the keyboard controller
; void _Reset()
_Reset:
    ; Method 1: Using the 8042 keyboard controller
    mov al, 0xFE            ; System reset command
    out 0x64, al            ; Send to keyboard controller command port

    ; If the above method fails, try an infinite loop
    jmp _Reset

; Get the current state of the flags (RFLAGS)
; uint64_t _GetRFlags()
_GetRFlags:
    pushfq                  ; Push RFLAGS to stack
    pop rax                 ; Get the value in RAX
    ret

; Set the state of the flags (RFLAGS)
; void _SetRFlags(uint64_t flags)
_SetRFlags:
    push rbx
    mov rbx, [rsp+16]       ; Get flags parameter
    push rbx                ; Put the value on the stack
    popfq                   ; Load RFLAGS from the stack
    pop rbx
    ret