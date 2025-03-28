using Internal.Runtime.CompilerHelpers;

namespace System
{
    public class IntArray
    {
        private int _length;
        private int[] _data;

        public IntArray(int length)
        {
            if (length < 0)
                ThrowHelpers.ThrowArgumentOutOfRangeException("length");

            _length = length;
            _data = new int[length];
        }

        public int Length => _length;

        public int this[int index]
        {
            get
            {
                if (index < 0 || index >= _length)
                    ThrowHelpers.ThrowIndexOutOfRangeException();
                return _data[index];
            }
            set
            {
                if (index < 0 || index >= _length)
                    ThrowHelpers.ThrowIndexOutOfRangeException();
                _data[index] = value;
            }
        }
    }
}