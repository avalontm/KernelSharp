namespace System
{
    // Este atributo indica que un enumerado puede ser tratado como una combinación de bits
    // Es decir, que se puede usar con operaciones bitwise (OR, AND, etc.)
    [AttributeUsage(AttributeTargets.Enum, Inherited = false)]
    public sealed class FlagsAttribute : Attribute
    {
        // Constructor sin parámetros
        public FlagsAttribute()
        {
            // No requiere inicialización especial
        }
    }
}