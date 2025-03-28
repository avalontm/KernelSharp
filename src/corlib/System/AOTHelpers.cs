namespace System
{
    public static class AOTHelpers
    {
        // Implementa funciones con nombres exactos que espera el enlazador

        // Los nombres incluyen el prefijo Object__
        public static bool Object__Equals(object obj1, object obj2)
        {
            return obj1.Equals(obj2);
        }

        public static int Object__GetHashCode(object obj)
        {
            return obj.GetHashCode();
        }

        public static string Object__ToString(object obj)
        {
            return obj.ToString();
        }

        public static void Object__Dispose(object obj)
        {
            if (obj is IDisposable disposable)
                disposable.Dispose();
        }
    }
}