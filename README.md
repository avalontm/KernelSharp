# KernelSharp

KernelSharp es una implementación de un núcleo minimalista en C# orientado a sistemas de 32 bits. Este proyecto busca crear un núcleo moderno que aprovecha las capacidades de C# mientras mantiene un rendimiento eficiente.

## Características

El proyecto incluye una implementación de CoreLib que proporciona las funcionalidades básicas necesarias para el sistema operativo:

| Categoría | Componentes | Estado |
|-----------|------------|--------|
| **Tipos primitivos** | Int32, Int64, UInt32, UInt64, Byte, SByte, Boolean, Char, Double, Single | ✅ Implementado |
| **Tipos fundamentales** | String, Array, Object | ✅ Implementado |
| **Memoria** | Buffer, SpanHelpers, Unsafe, Allocator | ✅ Implementado |
| **Colecciones** | List (básica) | ✅ Parcial |
| **Estructuras de sistema** | DateTime, TimeSpan, Random | ✅ Implementado |
| **Runtime** | EEType, RuntimeType | ✅ Implementado |
| **Soporte nativo** | Interoperabilidad nativa para manejo de memoria | ✅ Implementado |
| **Gestión de excepciones** | Manejo de excepciones básico | ⚠️ Básico |
| **Gestión de memoria** | GC estático, arranque, inicialización | ✅ Implementado |
| **Multiboot** | Soporte para Multiboot (GRUB) | ✅ Implementado |

## Arquitectura

El proyecto está estructurado en varias capas:

1. **Capa de bajo nivel (Ensamblador)**
   - Configuración inicial del hardware
   - Inicialización de SSE
   - Gestión básica de memoria

2. **CoreLib (Implementación de .NET)**
   - Tipos primitivos y fundamentales
   - Estructuras de sistema
   - Soporte para runtime

3. **Kernel**
   - Inicialización del sistema
   - Manejo de consola
   - Gestión de memoria paginada

## Loader

El sistema utiliza un cargador personalizado que:

- Inicializa el heap
- Maneja la información de Multiboot
- Configura los módulos CoreLib
- Habilita SSE para operaciones de punto flotante

## Implementación de Tipos

El CoreLib implementa tipos fundamentales que son necesarios para el funcionamiento del núcleo:

- **String**: Implementación completa con métodos para manipulación de cadenas
- **Array**: Soporte para arreglos unidimensionales con métodos de manipulación
- **Object**: Base para el sistema de tipos de .NET
- **DateTime**: Manejo de fechas y horas
- **Estructuras numéricas**: Implementación de tipos enteros y de punto flotante

## Características de Runtime

- **EEType**: Sistema de tipos en tiempo de ejecución
- **Unsafe**: Operaciones de memoria de bajo nivel
- **StartupCodeHelpers**: Ayudantes para la inicialización
- **ThrowHelpers**: Soporte para manejo de excepciones

## Memoria y Asignación

- Implementación de un heap simple
- Sistema de paginación
- Asignación de memoria alineada

## Consola

Implementación de una consola básica con:
- Soporte para colores
- Funciones de impresión 
- Manipulación del buffer de pantalla

## Estado del proyecto

Este proyecto está en desarrollo activo. La implementación actual provee la funcionalidad básica para ejecutar código C# en un entorno bare-metal.

## Requisitos de compilación

Para compilar el proyecto se necesita:

- .NET SDK 7.0 o superior
- NASM para el código ensamblador
- Herramienta de construcción del sistema (cmoos)
- Herramientas esenciales (nams, ld, objcopy)

## Licencia

Este proyecto está licenciado bajo la licencia MIT.