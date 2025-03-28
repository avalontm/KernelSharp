using System;

namespace System
{
    // Define los diferentes tipos de elementos a los que se pueden aplicar atributos
    [Flags]
    public enum AttributeTargets
    {
        Assembly = 0x0001,        // Atributo puede ser aplicado a un ensamblado
        Module = 0x0002,          // Atributo puede ser aplicado a un módulo
        Class = 0x0004,           // Atributo puede ser aplicado a una clase
        Struct = 0x0008,          // Atributo puede ser aplicado a una estructura
        Enum = 0x0010,            // Atributo puede ser aplicado a un enumerado
        Constructor = 0x0020,     // Atributo puede ser aplicado a un constructor
        Method = 0x0040,          // Atributo puede ser aplicado a un método
        Property = 0x0080,        // Atributo puede ser aplicado a una propiedad
        Field = 0x0100,           // Atributo puede ser aplicado a un campo
        Event = 0x0200,           // Atributo puede ser aplicado a un evento
        Interface = 0x0400,       // Atributo puede ser aplicado a una interfaz
        Parameter = 0x0800,       // Atributo puede ser aplicado a un parámetro
        Delegate = 0x1000,        // Atributo puede ser aplicado a un delegado
        ReturnValue = 0x2000,     // Atributo puede ser aplicado a un valor de retorno
        GenericParameter = 0x4000,// Atributo puede ser aplicado a un parámetro genérico

        // Combinaciones comunes
        All = Assembly | Module | Class | Struct | Enum | Constructor | Method |
              Property | Field | Event | Interface | Parameter | Delegate | ReturnValue |
              GenericParameter,
              
        ClassMembers = Class | Struct | Enum | Constructor | Method | Property | Field |
                       Event | Delegate | Interface
    }
}