section .text
global _ReloadSegments

_ReloadSegments:
    ; Recargar CS requiere un salto lejano (far jump)
    push qword 0x08        ; Selector de código de 64 bits
    lea rax, [.reload_CS]  ; Cargar dirección de retorno en RAX
    push rax               ; Apilar dirección de retorno
    retfq                  ; Retorno lejano (far return) para cambiar CS

.reload_CS:
    ; En modo largo (64 bits), DS, ES, SS no son necesarios en la mayoría de los casos.
    ; FS y GS pueden ser utilizados para datos específicos del sistema o de hilos.
    mov ax, 0x10           ; Selector de datos (debe estar configurado correctamente en la GDT)
    mov ds, ax
    mov es, ax
    mov ss, ax
    ; FS y GS pueden ser configurados según necesidad.
    ret
