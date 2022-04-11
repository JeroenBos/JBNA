//# pragma warning disable CS0437
//global using static JBSnorro.Extensions;
//using System;
//using System.Collections;
//using System.Diagnostics;
//using System.Diagnostics.CodeAnalysis;
//using System.Diagnostics.Contracts;
//using System.Linq.Expressions;
//using System.Runtime.CompilerServices;

//namespace JBSnorro;

//public static class Extensions
//{
    //public static bool HasBit(this uint flags, int index)
    //{
    //    // Contract.Requires(0 <= index && index < 32);

    //    return (flags & (1 << index)) != 0;
    //}
    //public static BitArray ToBitArray(this uint flags, int capacity = 32)
    //{
    //    // Contract.Requires(0 <= capacity);
    //    // Contract.Requires(capacity <= 32);

    //    var result = new BitArray(capacity);
    //    for (int i = 0; i < capacity; i++)
    //    {
    //        result[i] = HasBit(flags, i);
    //    }
    //    return result;
    //}
    //public static IComparer<T> ToComparer<T>(this Func<T?, T?, int> comparer)
    //{
    //    return new SimpleIComparer<T>(comparer);
    //}

    ///// <summary> Wraps around a <code>Func&lt;T, T&gt;</code> to represent an IComparer&lt;T&gt;. </summary>
    ///// <typeparam name="T"> The type of the elements to compare. </typeparam>
    //private sealed class SimpleIComparer<T> : IComparer<T>
    //{
    //    /// <summary> The underlying comparing delegate. </summary>
    //    [NotNull]
    //    private readonly Func<T?, T?, int> compare;
    //    /// <summary> Creates a new IComparer&lt;<typeparamref name="T"/>&gt; from the specified delegate. </summary>
    //    /// <param name="compare"> The function comparing two elements. </param>
    //    [DebuggerHidden]
    //    public SimpleIComparer([NotNull] Func<T?, T?, int> compare)
    //    {
    //        Contract.Requires(compare != null);

    //        this.compare = compare;
    //    }
    //    /// <summary> Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other. </summary>
    //    /// <param name="x">The first object to compare.</param>
    //    /// <param name="y">The second object to compare.</param>
    //    /// <returns> A signed integer that indicates the relative values of <paramref name="x"/> and <paramref name="y"/>, as shown in the following table.
    //    /// Value Meaning Less than zero<paramref name="x"/> is less than <paramref name="y"/>.Zero<paramref name="x"/> equals <paramref name="y"/>.Greater than zero<paramref name="x"/> is greater than <paramref name="y"/>. </returns>
    //    [DebuggerHidden]
    //    public int Compare(T? x, T? y)
    //    {
    //        return compare(x, y);
    //    }
    //}


//    public static TResult DeferToUpcast<TResult>(this object obj, [CallerMemberName] string callerMemberName = "")
//    {
//        var type = obj.GetType();
//        var propInfo = type.GetProperty(callerMemberName);
//        if (propInfo == null)
//        {
//            propInfo = type.GetInterfaces().Select(i => i.GetProperty(callerMemberName)).FirstOrDefault(pi => pi != null);
//            if (propInfo == null)
//                throw new Exception($"Property '{callerMemberName}' not found");
//        }
//        var result = propInfo.GetValue(obj);
//        return (TResult)result!;
//    }
//    public static TResult DeferToUpcast<TArg0, TResult>(this object obj, TArg0 arg0, [CallerMemberName] string callerMemberName = "")
//    {
//        var mi = obj.GetType().GetMethod(callerMemberName, new[] { typeof(TArg0) });
//        var result = mi!.Invoke(obj, new object?[] { arg0 });
//        return (TResult)result!;
//    }
//    public static TResult DeferToUpcast<TArg0, TArg1, TResult>(this object obj, TArg0 arg0, TArg1 arg1, [CallerMemberName] string callerMemberName = "")
//    {
//        var mi = obj.GetType().GetMethod(callerMemberName, new[] { typeof(TArg0), typeof(TArg1) });
//        var result = mi!.Invoke(obj, new object?[] { arg0, arg1 });
//        return (TResult)result!;
//    }

//    public static IEnumerable<byte> InsertBits(this IEnumerable<byte> bytes, int[] sortedBitIndices, bool[] values)
//    {
//        if (sortedBitIndices.Length == 0)
//            return bytes;

//        const int nLength = 8;
//        byte[] source = bytes as byte[] ?? bytes.ToArray();
//        var newLength = source.Length + ((values.Length + (nLength - 1)) / nLength);
//        byte[] dest = new byte[newLength];
//        int shift = 0;
//        foreach (var (startBitIndex, endBitIndex) in sortedBitIndices.Windowed2())
//        {
//            // bit will be inserted at endBitIndex
//            int startByteIndex = (startBitIndex + (nLength - 1)) / nLength;
//            int endByteIndex = (endBitIndex + (nLength - 1)) / nLength;
//            if (shift % nLength == 0)
//            {
//                int length = endByteIndex - startByteIndex;
//                Array.Copy(source, startByteIndex, dest, startByteIndex + (shift / nLength), length);
//            }
//            else
//            {
//                int i = startByteIndex;
//                int previous = i == 0 ? 0 : (source[i - 1] >> PositiveRemainder(nLength - shift - 1, nLength));
//                int next = source[i] << PositiveRemainder(shift, nLength);
//                dest[i] = (byte)(previous | next);

//                for (i++; i < endByteIndex; i++)
//                {
//                    int b = (source[i - 1] >> PositiveRemainder(nLength - shift, nLength)) | (source[i] << PositiveRemainder(shift, nLength));
//                    dest[i + (shift / nLength)] = (byte)b;
//                }
//                previous = source[i] >> PositiveRemainder(nLength - shift, nLength);
//                int middle = 1 << PositiveRemainder(shift, nLength);
//                next = source[i] << PositiveRemainder(shift + 1, nLength);

//                dest[i] = (byte)(previous | middle | next);
//            }

//            shift++;
//        }
//        return dest;

//        // I want a remainder that returns the positive remainder
//        static int PositiveRemainder(int dividend, int divisor)
//        {
//            if (divisor < 0)
//                throw new NotImplementedException();
//            int remainder = dividend % divisor;
//            if (dividend >= 0)
//                return remainder;
//            var result = remainder + divisor;
//            return result;
//        }
//    }

//    public static ArraySegment<T> AsArraySegment<T>(this IReadOnlyList<T> list)
//    {
//        if (list is ArraySegment<T> arraySegment)
//            return arraySegment;

//        throw new NotImplementedException("AsArraySegment");
//    }
//    public static ArraySegment<T> SelectSegment<T>(this T[] array, Range range)
//    {
//        var (offset, length) = range.GetOffsetAndLength(array.Length);
//        return new ArraySegment<T>(array, offset, length);
//    }
//}
