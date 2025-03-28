using System.Runtime.InteropServices;

namespace System.Collections
{
    public interface IEnumerable
    {
        // Returns an IEnumerator for this enumerable Object.  The enumerator provides
        // a simple way to access all the contents of a collection.
        IEnumerator GetEnumerator();
    }
}