global using static JBSnorro.Extensions;
using System;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace JBSnorro;

public static class Extensions
{
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
    [DebuggerHidden]
    public static void Assert(bool condition, [CallerArgumentExpression("condition")] string message = "")
    {
        if (!condition)
        {
            throw new Exception("AssertionError: " + message);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="drawCount"></param>
    /// <param name="max"> Exlusive. </param>
    /// <exception cref="NotImplementedException"></exception>
    public static TCodon[] GenerateUniqueRandomNumbers(this Random random, int drawCount, int max)
    {
        var list = new int[max];
        for (int i = 0; i < max; i++)
            list[i] = i;
        random.Shuffle(list);

        return list.Take(drawCount).Select(i => (byte)i).ToArray();
    }
    public static float Normal(this Random random, float average, float standardDevation)
    {
        // from https://stackoverflow.com/a/218600/308451
        double u1 = 1.0 - random.NextDouble(); // uniform(0,1] random doubles
        double u2 = 1.0 - random.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); // random normal(0,1)
        double result = average + standardDevation * randStdNormal; // random normal(mean,stdDev^2)
        return (float)result;
    }
    public static int[] Many(this Random random, int count, int min, int max)
    {
        int[] result = new int[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = random.Next(min, max);
        }
        return result;
    }
    public static int[] ManySorted(this Random random, int count, int min, int max)
    {
        var result = random.Many(count, min, max);
        Array.Sort(result);
        return result;
    }

    public static void Shuffle<T>(this Random random, IList<T> list)
    {
        for (int n = list.Count - 1; n > 1; n--)
        {
            int k = random.Next(n + 1);
            T temp = list[k];
            list[k] = list[n];
            list[n] = temp;
        }
    }

    public static TResult DeferToUpcast<TResult>(this object obj, [CallerMemberName] string callerMemberName = "")
    {
        var type = obj.GetType();
        var propInfo = type.GetProperty(callerMemberName);
        if (propInfo == null)
        {
            propInfo = type.GetInterfaces().Select(i => i.GetProperty(callerMemberName)).FirstOrDefault(pi => pi != null);
            if (propInfo == null)
                throw new Exception($"Property '{callerMemberName}' not found");
        }
        var result = propInfo.GetValue(obj);
        return (TResult)result!;
    }
    public static TResult DeferToUpcast<TArg0, TResult>(this object obj, TArg0 arg0, [CallerMemberName] string callerMemberName = "")
    {
        var mi = obj.GetType().GetMethod(callerMemberName, new[] { typeof(TArg0) });
        var result = mi!.Invoke(obj, new object?[] { arg0 });
        return (TResult)result!;
    }
    public static TResult DeferToUpcast<TArg0, TArg1, TResult>(this object obj, TArg0 arg0, TArg1 arg1, [CallerMemberName] string callerMemberName = "")
    {
        var mi = obj.GetType().GetMethod(callerMemberName, new[] { typeof(TArg0), typeof(TArg1) });
        var result = mi!.Invoke(obj, new object?[] { arg0, arg1 });
        return (TResult)result!;
    }
    public static float StandardDeviation(this IEnumerable<float> numbers, float? average = null)
    {
        float μ = average ?? numbers.Average();

        int count = 0;
        float sum = 0;
        foreach (var number in numbers)
        {
            sum += (number - μ) * (number - μ);
            count++;
        }
        if (count == 0)
            return 0;
        return (float)Math.Sqrt(sum / count);
    }
}
