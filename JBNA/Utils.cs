//public static class JBNAExtensions
//{
//    /// <summary>
//    /// Gets the data index (as opposed to bit index) at which the specified codon (i.e. all 8 bits) appears.
//    /// </summary>
//    internal static Index FindCodon(this IReadOnlyList<byte> data, byte codon, int dataStartIndex = 0)
//    {
//        int i = 0;
//        foreach (var element in data.Skip(dataStartIndex))
//        {
//            if (element == codon)
//                return i;
//            i++;
//        }
//        return Index.End;
//    }
//    /// <summary>
//    /// Gets the data index (as opposed to bit index) at which the specified codon (i.e. all 8 bits) appears.
//    /// </summary>
//    internal static Index FindCodon(this byte[] data, byte codon, int dataStartIndex = 0)
//    {
//        return JBNAExtensions.FindCodon(data.AsSpan(), codon, dataStartIndex);
//    }
//    /// <summary>
//    /// Gets the data index (as opposed to bit index) at which the specified codon (i.e. all 8 bits) appears.
//    /// </summary>
//    internal static Index FindCodon(this ReadOnlySpan<byte> data, byte codon, int dataStartIndex = 0)
//    {
//        for (int i = dataStartIndex; i < data.Length; i++)
//        {
//            if (data[i] == codon)
//                return i;
//        }
//        return Index.End;
//    }

//    /// <summary>
//    /// Gets the data index (as opposed to bit index) at which the specified codon (i.e. all 64 bits) appears.
//    /// </summary>
//    internal static Index FindCodon(this IReadOnlyList<ulong> data, ulong codon, int dataStartIndex = 0)
//    {
//        int i = 0;
//        foreach (var element in data.Skip(dataStartIndex))
//        {
//            if (element == codon)
//                return i;
//            i++;
//        }
//        return Index.End;
//    }
//    /// <summary>
//    /// Gets the data index (as opposed to bit index) at which the specified codon (i.e. all 64 bits) appears.
//    /// </summary>
//    internal static Index FindCodon(this ulong[] data, ulong codon, int dataStartIndex = 0)
//    {
//        return JBNAExtensions.FindCodon(data.AsSpan(), codon, dataStartIndex);
//    }
//    /// <summary>
//    /// Gets the data index (as opposed to bit index) at which the specified codon (i.e. all 64 bits) appears.
//    /// </summary>
//    internal static Index FindCodon(this ReadOnlySpan<ulong> data, ulong codon, int dataStartIndex = 0)
//    {
//        for (int i = dataStartIndex; i < data.Length; i++)
//        {
//            if (data[i] == codon)
//                return i;
//        }
//        return Index.End;
//    }
//}
