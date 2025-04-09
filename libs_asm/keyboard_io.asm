; keyboard_io.asm
; Low-level keyboard functions for 64-bit kernel
; Implements functions to check for keypress availability and read keyboard input directly from BIOS

section .text
global _CheckKeyAvailable
global _ReadKeyFromBIOS

section .data
;------------------------------------------------------------------------------
; _CheckKeyAvailable
; Checks if a key is available in the keyboard buffer
; 
; Input: None
; Output:
;   RAX = 1 if a key is available, 0 otherwise
;------------------------------------------------------------------------------
_CheckKeyAvailable:
    push rbx                    ; Save registers
    push rcx
    push rdx
    
    ; Check keyboard status register
    mov dx, 0x64                ; Keyboard controller status port
    in al, dx                   ; Read status
    and al, 0x01                ; Check if output buffer is full (bit 0)
    
    ; Convert result to boolean (0 or 1)
    movzx rax, al               ; Zero-extend AL to RAX
    
    pop rdx                     ; Restore registers
    pop rcx
    pop rbx
    ret

;------------------------------------------------------------------------------
; _ReadKeyFromBIOS
; Reads a key from the keyboard
; 
; Input: None
; Output:
;   RAX = Scan code in lower byte, ASCII value in upper byte
;         For special keys, upper byte contains 0
;------------------------------------------------------------------------------
_ReadKeyFromBIOS:
    push rbx                    ; Save registers
    push rcx
    push rdx
    
    ; Wait until a key is available
    mov dx, 0x64                ; Keyboard controller status port
.wait_for_key:
    in al, dx                   ; Read status
    and al, 0x01                ; Check if output buffer is full (bit 0)
    jz .wait_for_key            ; If not, keep waiting
    
    ; Read the scan code
    mov dx, 0x60                ; Keyboard data port
    in al, dx                   ; Read scan code
    movzx rbx, al               ; Store scan code in RBX
    
    ; Convert scan code to ASCII if possible
    ; Note: This is a simplified conversion for basic keys
    ; A complete implementation would handle all keys and modifiers
    cmp al, 0x01                ; ESC key
    je .special_key
    cmp al, 0x0E                ; Backspace
    je .backspace_key
    cmp al, 0x1C                ; Enter
    je .enter_key
    cmp al, 0x39                ; Space
    je .space_key
    
    ; Check for alphanumeric keys (simplified)
    cmp al, 0x02                ; '1' key
    jb .special_key
    cmp al, 0x0D                ; '=' key
    jbe .number_key
    
    cmp al, 0x10                ; 'Q' key
    jb .special_key
    cmp al, 0x1C                ; Enter
    jb .letter_key
    
    ; If not handled, treat as a special key
    jmp .special_key
    
.backspace_key:
    mov cx, 0x08                ; ASCII for backspace
    jmp .done
    
.enter_key:
    mov cx, 0x0D                ; ASCII for carriage return
    jmp .done
    
.space_key:
    mov cx, 0x20                ; ASCII for space
    jmp .done
    
.number_key:
    sub al, 0x02                ; Convert from scan code offset
    add al, '1'                 ; '1' ASCII value
    cmp al, '9' + 1
    jb .store_ascii
    cmp al, '9' + 1
    je .zero_key
    add al, 7                   ; Handle '-' and '=' keys
    jmp .store_ascii
    
.zero_key:
    mov al, '0'
    jmp .store_ascii
    
.letter_key:
    sub al, 0x10                ; Convert from scan code offset
    add al, 'A'                 ; 'A' ASCII value
    
.store_ascii:
    movzx cx, al                ; Store ASCII in CX
    jmp .done
    
.special_key:
    xor cx, cx                  ; Special key, no ASCII equivalent
    
.done:
    ; Combine scan code and ASCII
    mov rax, rcx                ; ASCII in RAX
    shl rax, 8                  ; Shift to upper byte
    or rax, rbx                 ; Combine with scan code

    pop rdx                     ; Restore registers
    pop rcx
    pop rbx
    ret