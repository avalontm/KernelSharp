﻿using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Delegate | AttributeTargets.Interface)]
    public sealed class EditorBrowsableAttribute : Attribute
    {
        public EditorBrowsableAttribute(EditorBrowsableState state)
        {
            State = state;
        }

        public EditorBrowsableAttribute() : this(EditorBrowsableState.Always)
        {
        }

        public EditorBrowsableState State { get; }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == this)
            {
                return true;
            }

            return (obj is EditorBrowsableAttribute other) && other.State == State;
        }

        public override int GetHashCode() => base.GetHashCode();
    }
}
