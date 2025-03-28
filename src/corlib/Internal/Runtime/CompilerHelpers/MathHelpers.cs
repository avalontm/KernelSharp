using System.Runtime;

namespace Internal.Runtime.CompilerHelpers
{
    internal class MathHelpers
    {
        public static ulong ULDiv(ulong dividend, ulong divisor)
        {
            if (divisor == 0)
                ThrowHelpers.ThrowDivideByZeroException();

            return dividend / divisor;
        }

        [RuntimeExport("ULMod")]
        public static ulong ULMod(ulong dividend, ulong divisor)
        {
            if (divisor == 0)
                ThrowHelpers.ThrowDivideByZeroException();

            return dividend % divisor;
        }

        [RuntimeExport("LMod")]
        public static long LMod(long dividend, long divisor)
        {
            if (divisor == 0)
                ThrowHelpers.ThrowDivideByZeroException();

            return dividend % divisor;
        }

        [RuntimeExport("LDiv")]
        public static long LDiv(long dividend, long divisor)
        {
            if (divisor == 0)
                ThrowHelpers.ThrowDivideByZeroException();

            return dividend / divisor;
        }
    }
}