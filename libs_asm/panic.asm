; kernel_functions64.asm - Basic Kernel Functions in x86_64 Assembly

section .text
global _panic
global _kernel_print

; Constants for text video buffer
%define VIDEO_MEMORY 0xB8000
%define SCREEN_WIDTH 80
%define SCREEN_HEIGHT 25
%define DEFAULT_ATTR 0x0F      ; Bright white on black background

; Variables to track cursor position
section .bss
cursor_x resq 1                ; X position of cursor (column)
cursor_y resq 1                ; Y position of cursor (row)

; Panic function - stops the system
_panic:
    cli                        ; Disable interrupts

.hang:
    hlt                        ; Halt processor until next interrupt
    jmp .hang                  ; Infinite loop

; Function to print text on screen
; Parameters:
;   - rdi: pointer to text (char*)
;   - rsi: text length (int)
_kernel_print:
    push rbx
    push rsi
    push rdi
    push rbp
    mov rbp, rsp
    
    ; If length is zero, exit
    test rsi, rsi
    jz .done

    ; Calculate base address of video buffer
    mov rdi, VIDEO_MEMORY
    
    ; Calculate initial position based on cursor_x and cursor_y
    mov rax, [cursor_y]
    imul rax, SCREEN_WIDTH
    add rax, [cursor_x]
    shl rax, 1                ; Multiply by 2 (each character is 2 bytes)
    add rdi, rax              ; Adjust write pointer
    
.print_loop:
    lodsb                      ; Load byte from [rsi] into al, increment rsi
    
    ; Check for special characters
    cmp al, 10                 ; Check if it's '\n' (newline)
    je .newline
    
    ; Normal character, write to screen
    mov [rdi], al              ; Store the character
    mov byte [rdi+1], DEFAULT_ATTR ; Set color attribute
    add rdi, 2                 ; Move forward in video buffer
    
    ; Update X position
    inc qword [cursor_x]
    cmp qword [cursor_x], SCREEN_WIDTH
    jl .next_char
    
    ; If we reach end of line, perform newline
.newline:
    mov qword [cursor_x], 0    ; Reset cursor_x
    inc qword [cursor_y]       ; Advance cursor_y
    
    ; If we reach bottom of screen, scroll
    cmp qword [cursor_y], SCREEN_HEIGHT
    jl .calc_position
    
    ; Shift content upwards (scroll)
    call .scroll_screen
    dec qword [cursor_y]       ; Adjust cursor_y after scroll
    
.calc_position:
    ; Recalculate position in video buffer
    mov rdi, VIDEO_MEMORY
    mov rax, [cursor_y]
    imul rax, SCREEN_WIDTH
    add rax, [cursor_x]
    shl rax, 1
    add rdi, rax
    
.next_char:
    dec rsi
    jnz .print_loop

.done:
    pop rbp
    pop rdi
    pop rsi
    pop rbx
    ret

; Function to scroll screen upwards
.scroll_screen:
    push rsi
    push rdi
    push rcx
    
    ; Copy lines 1-24 to lines 0-23
    mov rsi, VIDEO_MEMORY + (SCREEN_WIDTH * 2) ; Source: second line
    mov rdi, VIDEO_MEMORY                      ; Destination: first line
    mov rcx, (SCREEN_HEIGHT - 1) * SCREEN_WIDTH
    rep movsw                                  ; Copy word by word
    
    ; Clear the last line
    mov rdi, VIDEO_MEMORY + ((SCREEN_HEIGHT - 1) * SCREEN_WIDTH * 2)
    mov rcx, SCREEN_WIDTH

.clear_last_line:
    mov word [rdi], (DEFAULT_ATTR << 8) | ' '  ; Space with color attribute
    add rdi, 2
    loop .clear_last_line
    
    pop rcx
    pop rdi
    pop rsi
    ret