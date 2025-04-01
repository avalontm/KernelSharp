using System;
using System.Reflection;

namespace System.Runtime.InteropServices
{
    public interface IType
    {
        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.MemberType.
        //
        // Devuelve:
        //     Valor de System.Reflection.MemberTypes que indica que este miembro es un tipo
        //     o un tipo anidado.
        // MemberTypes MemberType { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Reflection.MemberInfo.Name.
        //
        // Devuelve:
        //     Nombre del objeto System.Type.
        public string Name { get; set; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.DeclaringType.
        //
        // Devuelve:
        //     Objeto System.Type de la clase que declara este miembro. Si el tipo es un tipo
        //     anidado, esta propiedad devuelve el tipo envolvente.
        public Type DeclaringType { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.ReflectedType.
        //
        // Devuelve:
        //     Objeto System.Type a través del cual se obtuvo este objeto System.Reflection.MemberInfo.
        public Type ReflectedType { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.GUID.
        //
        // Devuelve:
        //     GUID asociado al objeto System.Type.
        //Guid GUID { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.Module.
        //
        // Devuelve:
        //     Nombre del módulo donde está definido el objeto System.Type actual.
        //Module Module { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.Assembly.
        //
        // Devuelve:
        //     Instancia de System.Reflection.Assembly que describe el ensamblado que contiene
        //     el tipo actual.
        public Assembly Assembly { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.TypeHandle.
        //
        // Devuelve:
        //     Identificador del objeto System.Type actual.
        public RuntimeTypeHandle TypeHandle { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.FullName.
        //
        // Devuelve:
        //     Cadena que contiene el nombre completo del objeto System.Type; incluye el espacio
        //     de nombres del objeto System.Type pero no el ensamblado.
        string FullName { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.Namespace.
        //
        // Devuelve:
        //     Espacio de nombres de System.Type.
        public string Namespace { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.AssemblyQualifiedName.
        //
        // Devuelve:
        //     Nombre calificado con el ensamblado del objeto System.Type, incluido el nombre
        //     del ensamblado a partir del que se cargó System.Type.
        public string AssemblyQualifiedName { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.BaseType.
        //
        // Devuelve:
        //     System.Type desde el cual el System.Type actual hereda directamente o null si
        //     el Type actual representa a la clase System.Object.
        public Type BaseType { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.UnderlyingSystemType.
        //
        // Devuelve:
        //     Tipo de sistema subyacente para el objeto System.Type.
        Type UnderlyingSystemType { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.TypeInitializer.
        //
        // Devuelve:
        //     System.Reflection.ConstructorInfo que contiene el nombre del constructor de clase
        //     para System.Type.
        //ConstructorInfo TypeInitializer { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.Attributes.
        //
        // Devuelve:
        //     Objeto System.Reflection.TypeAttributes que representa el conjunto de atributos
        //     del objeto System.Type, a menos que el objeto System.Type represente un parámetro
        //     de tipo genérico, en cuyo caso el valor no se especifica.
        // TypeAttributes Attributes { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.IsNotPublic.
        //
        // Devuelve:
        //     Es true si el objeto System.Type de nivel superior no se ha declarado público;
        //     de lo contrario, es false.
        bool IsNotPublic { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.IsPublic.
        //
        // Devuelve:
        //     Es true si el objeto System.Type de nivel superior se ha declarado público; de
        //     lo contrario, es false.
        bool IsPublic { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.IsNestedPublic.
        //
        // Devuelve:
        //     Es true si la clase está anidada y se ha declarado pública; en caso contrario,
        //     es false.
        bool IsNestedPublic { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.IsNestedPrivate.
        //
        // Devuelve:
        //     Es true si System.Type está anidado y se ha declarado privado; en caso contrario,
        //     es false.
        bool IsNestedPrivate { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.IsNestedFamily.
        //
        // Devuelve:
        //     Es true si System.Type está anidado y solo se ve dentro de su propia familia;
        //     en caso contrario, es false.
        bool IsNestedFamily { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.IsNestedAssembly.
        //
        // Devuelve:
        //     Es true si System.Type está anidado y solo se ve dentro de su propio ensamblado;
        //     en caso contrario, es false.
        bool IsNestedAssembly { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.IsNestedFamANDAssem.
        //
        // Devuelve:
        //     Es true si System.Type está anidado y solo está visible para las clases que pertenezcan
        //     a su propia familia y a su propio ensamblado; de lo contrario, es false.
        bool IsNestedFamANDAssem { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.IsNestedFamORAssem.
        //
        // Devuelve:
        //     Es true si System.Type está anidado y solo está visible para las clases que pertenezcan
        //     a su propia familia o a su propio ensamblado; en caso contrario, es false.
        bool IsNestedFamORAssem { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.IsAutoLayout.
        //
        // Devuelve:
        //     Es true si se selecciona el atributo de diseño de clase AutoLayout para el objeto
        //     System.Type; de lo contrario es false.
        bool IsAutoLayout { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.IsLayoutSequential.
        //
        // Devuelve:
        //     Es true si se selecciona el atributo de diseño de clase SequentialLayout para
        //     el objeto System.Type; de lo contrario es false.
        bool IsLayoutSequential { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.IsExplicitLayout.
        //
        // Devuelve:
        //     Es true si se selecciona el atributo de diseño de clase ExplicitLayout para el
        //     objeto System.Type; de lo contrario es false.
        bool IsExplicitLayout { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.IsClass.
        //
        // Devuelve:
        //     Es true si System.Type es una clase; de lo contrario, es false.
        bool IsClass { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.IsInterface.
        //
        // Devuelve:
        //     Es true si System.Type es una interfaz; en caso contrario, es false.
        bool IsInterface { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.IsValueType.
        //
        // Devuelve:
        //     Es true si System.Type es un tipo de valor; en caso contrario, es false.
        bool IsValueType { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.IsAbstract.
        //
        // Devuelve:
        //     Es true si System.Type es abstracto; de lo contrario, es false.
        bool IsAbstract { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.IsSealed.
        //
        // Devuelve:
        //     Es true si System.Type se declara "sealed"; en caso contrario, es false.
        bool IsSealed { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.IsEnum.
        //
        // Devuelve:
        //     Es true si el objeto System.Type actual representa una enumeración; en caso contrario,
        //     es false.
        bool IsEnum { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.IsSpecialName.
        //
        // Devuelve:
        //     Es true si System.Type tiene un nombre que requiere un tratamiento especial;
        //     de lo contrario, es false.
        bool IsSpecialName { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.IsImport.
        //
        // Devuelve:
        //     Es true si System.Type tiene una clase System.Runtime.InteropServices.ComImportAttribute;
        //     en caso contrario, es false.
        bool IsImport { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.IsSerializable.
        //
        // Devuelve:
        //     Es true si System.Type es serializable; de lo contrario, es false.
        bool IsSerializable { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.IsAnsiClass.
        //
        // Devuelve:
        //     Es true si se selecciona el atributo de formato de cadena AnsiClass para System.Type;
        //     en caso contrario, es false.
        bool IsAnsiClass { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.IsUnicodeClass.
        //
        // Devuelve:
        //     Es true si se selecciona el atributo de formato de cadena UnicodeClass para System.Type;
        //     en caso contrario, es false.
        bool IsUnicodeClass { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.IsAutoClass.
        //
        // Devuelve:
        //     Es true si se selecciona el atributo de formato de cadena AutoClass para System.Type;
        //     en caso contrario, es false.
        bool IsAutoClass { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.IsArray.
        //
        // Devuelve:
        //     Es true si System.Type es una matriz; en caso contrario, es false.
        bool IsArray { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.IsByRef.
        //
        // Devuelve:
        //     Es true si System.Type se pasa por referencia; de lo contrario, es false.
        bool IsByRef { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.IsPointer.
        //
        // Devuelve:
        //     Es true si System.Type es un puntero; de lo contrario, es false.
        bool IsPointer { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.IsPrimitive.
        //
        // Devuelve:
        //     Es true si System.Type es uno de los tipos primitivos; en caso contrario, es
        //     false.
        bool IsPrimitive { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.IsCOMObject.
        //
        // Devuelve:
        //     Es true si System.Type es un objeto COM; en caso contrario, es false.
        bool IsCOMObject { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.HasElementType.
        //
        // Devuelve:
        //     Es true si System.Type es una matriz o un puntero, o si se pasa por referencia;
        //     en caso contrario, es false.
        bool HasElementType { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.IsContextful.
        //
        // Devuelve:
        //     Es true si System.Type puede estar hospedado en un contexto; de lo contrario,
        //     es false.
        bool IsContextful { get; }

        //
        // Resumen:
        //     Proporciona el acceso independiente de la versión de los objetos COM a la propiedad
        //     System.Type.IsMarshalByRef.
        //
        // Devuelve:
        //     Es true si System.Type se calcula por referencia; en caso contrario, es false.
        bool IsMarshalByRef { get; }


    }
}
