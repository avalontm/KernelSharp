﻿using Internal.Runtime.CompilerHelpers;
using Kernel;
using System;
using System.Runtime;

/// <summary>
/// Nifanfa's Super Fast Memory Allocation Lib
/// </summary>
internal static unsafe class Allocator
{
    [RuntimeExport("_malloc")]
    internal static void* malloc(ulong size)
    {
        return (void*)Allocate(size);
    }

    [RuntimeExport("_free")]
    internal static void free(void* ptr)
    {
        Free((IntPtr)ptr);
    }

    [RuntimeExport("_realloc")]
    internal static void* realloc(void* ptr, ulong size)
    {
        return (void*)Reallocate((System.IntPtr)ptr, size);
    }


    [RuntimeExport("_calloc")]
    internal static void* calloc(ulong num, ulong size)
    {
        void* ptr = (void*)Allocate(num * size);
        Native.Stosb(ptr, 0, num * size);
        return ptr;
    }

    [RuntimeExport("_kmalloc")]
    internal static void* kmalloc(ulong size)
    {
        return (void*)Allocate(size);
    }

    [RuntimeExport("_kfree")]
    internal static void kfree(void* ptr)
    {
        Free((IntPtr)ptr);
    }

    [RuntimeExport("_krealloc")]
    internal static void* krealloc(void* ptr, ulong size)
    {
        return (void*)Reallocate((IntPtr)ptr, size);
    }


    [RuntimeExport("kcalloc")]
    internal static void* kcalloc(ulong num, ulong size)
    {
        void* ptr = (void*)Allocate(num * size);
        Native.Stosb(ptr, 0, num * size);
        return ptr;
    }

    internal static unsafe void ZeroFill(IntPtr data, ulong size)
    {
        Native.Stosb((void*)data, 0, size);
    }

    static long GetPageIndexStart(IntPtr ptr)
    {
        ulong p = (ulong)ptr;
        if (p < (ulong)_Info.Start) return -1;
        p -= (ulong)_Info.Start;
        if ((p % PageSize) != 0) return -1;
        /*
         * This will get wrong if the size is larger than PageSize
         * and however the allocated address should be aligned
         */
        //p &= ~PageSize; 
        p /= PageSize;
        return (long)p;
    }

    internal static ulong Free(IntPtr intPtr)
    {
        //You can use lock(null) in Moos
        lock (null)
        {
            long p = GetPageIndexStart(intPtr);
            if (p == -1) return 0;
            ulong pages = _Info.Pages[p];
            if (pages != 0 && pages != PageSignature)
            {
                _Info.PageInUse -= pages;
                Native.Stosb((void*)intPtr, 0, pages * PageSize);
                for (ulong i = 0; i < pages; i++)
                {
                    _Info.Pages[(ulong)p + i] = 0;
                }

                _Info.Pages[p] = 0;
                return pages * PageSize;
            }
            return 0;
        }
    }

    internal static ulong MemoryInUse
    {
        get
        {
            return _Info.PageInUse * PageSize;
        }
    }

    internal static ulong MemorySize
    {
        get
        {
            return NumPages * PageSize;
        }
    }

    internal const ulong PageSignature = 0x2E61666E6166696E;

    /*
     * NumPages = Memory Size / PageSize
     * This should be a const because there will be allocations during initializing modules 👇_Info
     */
    internal const int NumPages = 131072;
    internal const ulong PageSize = 4096;

    internal struct Info
    {
        public IntPtr Start;
        public UInt64 PageInUse;
        public fixed ulong Pages[NumPages]; //Max 512MiB
    }

    internal static Info _Info;

    internal static void Initialize(IntPtr Start)
    {
        fixed (Info* pInfo = &_Info)
            Native.Stosb(pInfo, 0, (ulong)sizeof(Info));
        _Info.Start = Start;
        _Info.PageInUse = 0;
    }

    /// <summary>
    /// Returns a 4KB aligned address
    /// </summary>
    /// <param name="size"></param>
    /// <returns></returns>
    static unsafe IntPtr Allocate(ulong size)
    {
        //You can use lock(null) in Moos
        lock (null)
        {
            ulong pages = 1;

            if (size > PageSize)
            {
                pages = (size / PageSize) + ((size % 4096) != 0 ? 1UL : 0);
            }

            ulong i = 0;
            bool found = false;

            for (i = 0; i < NumPages; i++)
            {
                if (_Info.Pages[i] == 0)
                {
                    found = true;
                    for (ulong k = 0; k < pages; k++)
                    {
                        if (_Info.Pages[i + k] != 0)
                        {
                            found = false;
                            break;
                        }
                    }
                    if (found) break;
                }
                else if (_Info.Pages[i] != PageSignature)
                {
                    i += _Info.Pages[i];
                }
            }
            if (!found)
            {
                ThrowHelpers.Panic("Memory leak");
                return IntPtr.Zero;
            }

            for (ulong k = 0; k < pages; k++)
            {
                _Info.Pages[i + k] = PageSignature;
            }
            _Info.Pages[i] = pages;
            _Info.PageInUse += pages;

            IntPtr ptr = _Info.Start + (i * PageSize);
            return ptr;
        }
    }

    internal static IntPtr Reallocate(IntPtr intPtr, ulong size)
    {
        if (intPtr == IntPtr.Zero)
            return Allocate(size);
        if (size == 0)
        {
            Free(intPtr);
            return IntPtr.Zero;
        }

        long p = GetPageIndexStart(intPtr);
        if (p == -1) return intPtr;

        ulong pages = 1;

        if (size > PageSize)
        {
            pages = (size / PageSize) + ((size % 4096) != 0 ? 1UL : 0);
        }

        if (_Info.Pages[p] == pages) return intPtr;

        IntPtr newptr = Allocate(size);
        MemoryCopy(newptr, intPtr, size);
        Free(intPtr);
        return newptr;
    }

    internal static unsafe void MemoryCopy(IntPtr dst, IntPtr src, ulong size)
    {
        Native.Movsb((void*)dst, (void*)src, size);
    }
}