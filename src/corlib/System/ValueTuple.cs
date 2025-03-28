using System.Runtime.InteropServices;

namespace System
{
    internal interface IValueTupleInternal : ITuple
    {
    }

    public struct ValueTuple : IValueTupleInternal, ITuple
    {
        int ITuple.Length => 0;

#nullable enable
        object? ITuple.this[int index] => null;
#nullable disable

        public static ValueTuple Create()
        {
            return default;
        }

        public static ValueTuple<T1> Create<T1>(T1 item1)
        {
            return new(item1);
        }

        public static ValueTuple<T1, T2> Create<T1, T2>(T1 item1, T2 item2)
        {
            return new(item1, item2);
        }

        public static ValueTuple<T1, T2, T3> Create<T1, T2, T3>(T1 item1, T2 item2, T3 item3)
        {
            return new(item1, item2, item3);
        }

        public static ValueTuple<T1, T2, T3, T4> Create<T1, T2, T3, T4>(T1 item1, T2 item2, T3 item3, T4 item4)
        {
            return new(item1, item2, item3, item4);
        }

        public static ValueTuple<T1, T2, T3, T4, T5> Create<T1, T2, T3, T4, T5>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
        {
            return new(item1, item2, item3, item4, item5);
        }

        public static ValueTuple<T1, T2, T3, T4, T5, T6> Create<T1, T2, T3, T4, T5, T6>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6)
        {
            return new ValueTuple<T1, T2, T3, T4, T5, T6>(item1, item2, item3, item4, item5, item6);
        }

        public static ValueTuple<T1, T2, T3, T4, T5, T6, T7> Create<T1, T2, T3, T4, T5, T6, T7>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7)
        {
            return new(item1, item2, item3, item4, item5, item6, item7);
        }

        public static ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8>> Create<T1, T2, T3, T4, T5, T6, T7, T8>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8)
        {
            return new(item1, item2, item3, item4, item5, item6, item7, Create(item8));
        }
    }

    public struct ValueTuple<T1> : IValueTupleInternal, ITuple
    {
        public T1 Item1;

        public ValueTuple(T1 item1)
        {
            Item1 = item1;
        }
        int ITuple.Length => 1;

#nullable enable
        object? ITuple.this[int index] => index != 0 ? null : Item1;
#nullable disable
    }

    [StructLayout(LayoutKind.Auto)]
    public struct ValueTuple<T1, T2> : IValueTupleInternal, ITuple
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            Item1 = item1;
            Item2 = item2;
        }

        int ITuple.Length => 2;

#nullable enable
        object? ITuple.this[int index] =>
#nullable disable

        index switch
        {
            0 => Item1,
            1 => Item2,
            _ => null,
        };
    }

    public struct ValueTuple<T1, T2, T3> : IValueTupleInternal, ITuple
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;

        public ValueTuple(T1 item1, T2 item2, T3 item3)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
        }

        int ITuple.Length => 3;

#nullable enable
        object? ITuple.this[int index] =>
#nullable disable

        index switch
        {
            0 => Item1,
            1 => Item2,
            2 => Item3,
            _ => null,
        };
    }

    public struct ValueTuple<T1, T2, T3, T4> : IValueTupleInternal, ITuple
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;

        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
        }

        int ITuple.Length => 4;

#nullable enable
        object? ITuple.this[int index] =>
#nullable disable

        index switch
        {
            0 => Item1,
            1 => Item2,
            2 => Item3,
            3 => Item4,
            _ => null,
        };
    }

    public struct ValueTuple<T1, T2, T3, T4, T5> : IValueTupleInternal, ITuple
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;

        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
            Item5 = item5;
        }

        int ITuple.Length => 5;

#nullable enable
        object? ITuple.this[int index] =>
#nullable disable

        index switch
        {
            0 => Item1,
            1 => Item2,
            2 => Item3,
            3 => Item4,
            4 => Item5,
            _ => null,
        };
    }

    public struct ValueTuple<T1, T2, T3, T4, T5, T6> : IValueTupleInternal, ITuple
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;
        public T6 Item6;

        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
            Item5 = item5;
            Item6 = item6;
        }

        int ITuple.Length => 6;

#nullable enable
        object? ITuple.this[int index] =>
#nullable disable

        index switch
        {
            0 => Item1,
            1 => Item2,
            2 => Item3,
            3 => Item4,
            4 => Item5,
            5 => Item6,
            _ => null,
        };
    }

    public struct ValueTuple<T1, T2, T3, T4, T5, T6, T7> : IValueTupleInternal, ITuple
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;
        public T6 Item6;
        public T7 Item7;

        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
            Item5 = item5;
            Item6 = item6;
            Item7 = item7;
        }

        int ITuple.Length => 7;

#nullable enable
        object? ITuple.this[int index] =>
#nullable disable

        index switch
        {
            0 => Item1,
            1 => Item2,
            2 => Item3,
            3 => Item4,
            4 => Item5,
            5 => Item6,
            6 => Item7,
            _ => null,
        };
    }

    public struct ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> : IValueTupleInternal, ITuple
    where TRest : struct
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;
        public T6 Item6;
        public T7 Item7;
        public TRest Rest;

        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, TRest rest)
        {
            if (rest is not IValueTupleInternal)
            {
                rest = (TRest)(ITupleInternal)rest;
            }

            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
            Item5 = item5;
            Item6 = item6;
            Item7 = item7;
            Rest = rest;
        }

        int ITuple.Length => Rest is IValueTupleInternal @internal ? 7 + @internal.Length : 8;

#nullable enable
        object? ITuple.this[int index]
#nullable disable
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return Item1;
                    case 1:
                        return Item2;
                    case 2:
                        return Item3;
                    case 3:
                        return Item4;
                    case 4:
                        return Item5;
                    case 5:
                        return Item6;
                    case 6:
                        return Item7;
                    default:
                        break;
                }

                return Rest is IValueTupleInternal @internal ? @internal[index - 7] : index == 7 ? Rest : null;
            }
        }
    }
}