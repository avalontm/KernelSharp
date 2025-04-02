# KernelSharp

KernelSharp is a minimalist kernel implementation in C# aimed at 64-bit systems. This project aims to create a modern kernel that leverages C# capabilities while maintaining efficient performance.

## Features

The project includes a CoreLib implementation that provides the basic functionalities required for the operating system:

| Category               | Components                                                             | Status        |
|------------------------|------------------------------------------------------------------------|---------------|
| **Primitive Types**     | Int32, Int64, UInt32, UInt64, Byte, SByte, Boolean, Char, Double, Single | ✅ Implemented |
| **Fundamental Types**   | String, Array, Object                                                  | ✅ Implemented |
| **Memory**              | Buffer, SpanHelpers, Unsafe, Allocator                                 | ✅ Implemented |
| **Collections**         | List (basic)                                                           | ✅ Partial     |
| **System Structures**   | DateTime, TimeSpan, Random                                             | ✅ Implemented |
| **Runtime**             | EEType, RuntimeType                                                    | ✅ Implemented |
| **Native Support**      | Native interoperability for memory management                          | ✅ Implemented |
| **Exception Handling**  | Basic exception handling                                               | ⚠️ Basic      |
| **Memory Management**   | Static GC, bootstrapping, initialization                               | ✅ Implemented |
| **Multiboot**           | Support for Multiboot (GRUB)                                           | ✅ Implemented |

## Architecture

The project is structured into several layers:

1. **Low-Level Layer (Assembly)**:
   - Initial hardware setup
   - SSE initialization
   - Basic memory management

2. **CoreLib (.NET Implementation)**:
   - Primitive and fundamental types
   - System structures
   - Runtime support

3. **Kernel**:
   - System initialization
   - Console management
   - Paged memory management

## Loader

The system uses a custom loader that:

- Initializes the heap
- Handles Multiboot information
- Configures CoreLib modules
- Enables SSE for floating-point operations

## Type Implementation

CoreLib implements fundamental types necessary for kernel operation:

- **String**: Complete implementation with string manipulation methods
- **Array**: Support for one-dimensional arrays with manipulation methods
- **Object**: Base for the .NET type system
- **DateTime**: Date and time handling
- **Numeric Structures**: Implementation of integer and floating-point types

## Runtime Features

- **EEType**: Runtime type system
- **Unsafe**: Low-level memory operations
- **StartupCodeHelpers**: Helpers for initialization
- **ThrowHelpers**: Exception handling support

## Memory and Allocation

- Implementation of a simple heap
- Paging system
- Aligned memory allocation

## Console

Basic console implementation with:
- Color support
- Print functions
- Screen buffer manipulation

## Project Status

This project is under active development. The current implementation provides basic functionality for executing C# code in a bare-metal environment.

## Build Requirements

To compile the project, you need:

- .NET SDK 8.0 or higher
- NASM for assembly code
- System build tool (cmoos)
- Essential tools (nasm, ld, objcopy)
- Microsoft.DotNet.ILCompiler 8.0.0
- Qemu
- Visual Studio Code

## Usage

To build the project, use the following command:

```bash
cmoos.exe build --debug
