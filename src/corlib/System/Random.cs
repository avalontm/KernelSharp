namespace System
{
    public class Random
    {
        // Cambiar de campo estático a propiedad estática
        private static Random s_shared;

        public static Random Shared
        {
            get
            {
                // Inicialización bajo demanda
                if (s_shared == null)
                    s_shared = new Random();
                return s_shared;
            }
            set { s_shared = value; }
        }

        // Método auxiliar para obtener una instancia compartida
        public static Random GetShared()
        {
            return Shared;
        }

        private uint x;
        private uint y;
        private uint z;
        private uint c;

        public Random() : this(123456789)
        {
        }

        public Random(int Seed)
        {
            x = (uint)Seed;
            y = 987654321;
            z = 43219876;
            c = 6543217;
        }

        private uint JKiss()
        {
            x = 314527869 * x + 1234567;
            y ^= y << 5;
            y ^= y >> 7;
            y ^= y << 22;
            ulong t = ((ulong)4294584393 * z + c);
            c = (uint)(t >> 32);
            z = (uint)t;
            return (x + y + z);
        }

        public virtual int Next(int minValue, int maxValue)
        {
            if (minValue > maxValue)
                return 0;
            // special case: a difference of one (or less) will always return the minimum
            // e.g. -1,-1 or -1,0 will always return -1
            uint diff = (uint)(maxValue - minValue);
            if (diff <= 1)
                return minValue;
            return minValue + ((int)(JKiss() % diff));
        }

        public virtual int Next(int maxValue)
        {
            if (maxValue < 0)
                return 0;
            return maxValue > 0 ? (int)(JKiss() % maxValue) : 0;
        }

        public virtual int Next()
        {
            // returns a non-negative, [0 - Int32.MacValue], random number
            // but we want to avoid calls to Math.Abs (call cost and branching cost it requires)
            // and the fact it would throw for Int32.MinValue (so roughly 1 time out of 2^32)
            int random = (int)JKiss();
            while (random == int.MinValue)
                random = (int)JKiss();
            int mask = random >> 31;
            random ^= mask;
            return random + (mask & 1);
        }

        public virtual void NextBytes(byte[] buffer)
        {
            if (buffer == null)
                return;
            // each random `int` can fill 4 bytes
            int p = 0;
            uint random;
            for (int i = 0; i < (buffer.Length >> 2); i++)
            {
                random = JKiss();
                buffer[p++] = (byte)(random >> 24);
                buffer[p++] = (byte)(random >> 16);
                buffer[p++] = (byte)(random >> 8);
                buffer[p++] = (byte)random;
            }
            if (p == buffer.Length)
                return;
            // complete the array
            random = JKiss();
            while (p < buffer.Length)
            {
                buffer[p++] = (byte)random;
                random >>= 8;
            }
        }

        public virtual double NextDouble()
        {
            // return a double value between [0,1]
            return Sample();
        }

        protected virtual double Sample()
        {
            // a single 32 bits random value is not enough to create a random double value
            uint a = JKiss() >> 6;  // Upper 26 bits
            uint b = JKiss() >> 5;  // Upper 27 bits
            return (a * 134217728.0 + b) / 9007199254740992.0;
        }
    }
}