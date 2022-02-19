public static class JBNAExtensions
{
    public static Index FindCodon(this IReadOnlyList<byte> data, byte codon, int startIndex = 0)
    {
        int i = 0;
        foreach (var element in data)
        {
            if (element == codon)
                return i;
            i++;
        }
        return Index.End;
    }
    public static Index FindCodon(this byte[] data, byte codon, int startIndex = 0)
    {
        return JBNAExtensions.FindCodon(data.AsSpan(), codon, startIndex);
    }
    public static Index FindCodon(this ReadOnlySpan<byte> data, byte codon, int startIndex = 0)
    {
        for (int i = startIndex; i < data.Length; i++)
        {
            if (data[i] == codon)
                return i;
        }
        return Index.End;
    }
}
