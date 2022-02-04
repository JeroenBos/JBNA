global using static JBSnorro.Extensions;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace JBSnorro;

public static class Extensions
{
    public static bool HasBit(this uint flags, int index)
    {
        // Contract.Requires(0 <= index && index < 32);

        return (flags & (1 << index)) != 0;
    }
    public static BitArray ToBitArray(this uint flags, int capacity = 32)
    {
        // Contract.Requires(0 <= capacity);
        // Contract.Requires(capacity <= 32);

        var result = new BitArray(capacity);
        for (int i = 0; i < capacity; i++)
        {
            result[i] = HasBit(flags, i);
        }
        return result;
    }
    public static IComparer<T> ToComparer<T>(this Func<T?, T?, int> comparer)
    {
        return new SimpleIComparer<T>(comparer);
    }

    /// <summary> Wraps around a <code>Func&lt;T, T&gt;</code> to represent an IComparer&lt;T&gt;. </summary>
    /// <typeparam name="T"> The type of the elements to compare. </typeparam>
    private sealed class SimpleIComparer<T> : IComparer<T>
    {
        /// <summary> The underlying comparing delegate. </summary>
        [NotNull]
        private readonly Func<T, T, int> compare;
        /// <summary> Creates a new IComparer&lt;<typeparamref name="T"/>&gt; from the specified delegate. </summary>
        /// <param name="compare"> The function comparing two elements. </param>
        [DebuggerHidden]
        public SimpleIComparer([NotNull] Func<T, T, int> compare)
        {
            Contract.Requires(compare != null);

            this.compare = compare;
        }
        /// <summary> Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other. </summary>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        /// <returns> A signed integer that indicates the relative values of <paramref name="x"/> and <paramref name="y"/>, as shown in the following table.
        /// Value Meaning Less than zero<paramref name="x"/> is less than <paramref name="y"/>.Zero<paramref name="x"/> equals <paramref name="y"/>.Greater than zero<paramref name="x"/> is greater than <paramref name="y"/>. </returns>
        [DebuggerHidden]
        public int Compare(T x, T y)
        {
            return compare(x, y);
        }
    }

    public static void Assert(bool condition, [CallerArgumentExpression("condition")] string message = "")
    {
        if (!condition)
        {
            throw new Exception("AssertionError: " + message);
        }
    }

    public static int[] GenerateUniqueRandomNumbers(int drawCount, int max)
    {
        throw new NotImplementedException();
    }
}
