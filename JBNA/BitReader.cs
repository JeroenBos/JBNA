using BitArray = JBSnorro.Collections.BitArray;

namespace JBSnorro;

public interface IBitReader
{
    int Length { get; }
    bool ReadBit();
    byte ReadByte(int bitCount);
    short ReadInt16(int bitCount);
    int ReadInt32(int bitCount);
    long ReadInt64(int bitCount);
    ulong ReadUInt64(int bitCount);

    Half ReadHalf();
    float ReadSingle();
    double ReadDouble();
    void Seek(int bitIndex);
}
public class BitReader : IBitReader
{
    private readonly ulong[] _data;
    private readonly BitArray data;
    /// <summary> In bits. </summary>
    public int Length => this.data.Length;
    public bool this[int longIndex, int bitindex] => this.data[longIndex * 64 + bitIndex];

    /// <summary> In bits. </summary>
    private int current;
    private int ulongIndex => current / 64;
    private int bitIndex => current % 64;
    /// <summary> In bits. </summary>
    public int RemainingLength => this.Length - current;
    public BitReader(BitArray data, int startBitIndex = 0)
    {
        this.data = data;
        this.current = startBitIndex;

        // HACK until next version of library 
        this._data = (ulong[])typeof(BitArray).GetField("data", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(this.data)!;
    }
    public BitReader(ulong[] data, int length, int startBitIndex = 0)
    {
        this._data = data;
        this.data = new JBSnorro.Collections.BitArray();
        // HACK until next version of library 
        typeof(BitArray).GetField("data", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(this.data, this._data);
        typeof(BitArray).GetField(_getBackingFieldName("Length"), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(this.data, length);
        string _getBackingFieldName(string propertyName) => string.Format("<{0}>k__BackingField", propertyName);

        this.current = startBitIndex;
    }

    private static Exception InsufficientBitsException(string elementName)
    {
        return new InvalidOperationException($"Insufficient bits remaining in stream to read '{elementName}'");
    }
    public bool ReadBit()
    {
        if (this.RemainingLength < 1)
            throw InsufficientBitsException("bit");

        bool result = this[ulongIndex, bitIndex];
        current++;
        return result;
    }
    public byte ReadByte(int bitCount = 8)
    {
        if (bitCount < 1 || bitCount > 8)
            throw new ArgumentException(nameof(bitCount));
        if (this.RemainingLength < bitCount)
            throw InsufficientBitsException("byte");

        return (byte)ReadUInt64(bitCount);
    }
    public short ReadInt16(int bitCount = 16)
    {
        if (bitCount < 1 || bitCount > 16)
            throw new ArgumentException(nameof(bitCount));
        if (this.RemainingLength < bitCount)
            throw InsufficientBitsException("short");

        return (short)ReadUInt64(bitCount);
    }

    public int ReadInt32(int bitCount = 32)
    {
        if (bitCount < 1 || bitCount > 32)
            throw new ArgumentException(nameof(bitCount));
        if (this.RemainingLength < bitCount)
            throw InsufficientBitsException("int");

        return (int)ReadUInt64(bitCount);
    }
    public long ReadInt64(int bitCount = 64)
    {
        if (bitCount < 1 || bitCount > 64)
            throw new ArgumentException(nameof(bitCount));
        if (this.RemainingLength < bitCount)
            throw InsufficientBitsException("long");

        return (long)ReadUInt64(bitCount);
    }
    public Half ReadHalf()
    {
        if (this.RemainingLength < 16)
            throw InsufficientBitsException("Half");
        short i = ReadInt16();
        unsafe
        {
            short* pointer = &i;
            Half* halfPointer = (Half*)pointer;
            Half result = *halfPointer;
            return result;
        }
    }
    public float ReadSingle()
    {
        if (this.RemainingLength < 32)
            throw InsufficientBitsException("Single");
        int i = ReadInt32();
        unsafe
        {
            int* pointer = &i;
            float* floatPointer = (float*)pointer;
            float result = *floatPointer;
            return result;
        }
    }
    public double ReadDouble()
    {
        if (this.RemainingLength < 64)
            throw InsufficientBitsException("Double");
        long i = ReadInt64();
        unsafe
        {
            long* pointer = &i;
            double* doublePointer = (double*)pointer;
            double result = *doublePointer;
            return result;
        }
    }


    public ulong ReadUInt64(int bitCount)
    {
        if (bitCount < 1 || bitCount > 64)
            throw new ArgumentException(nameof(bitCount));

        ulong result;
        if (bitIndex + bitCount <= 64)
        {
            ulong shifted = this._data[ulongIndex] >> bitIndex;
            ulong mask = LowerBitsMask(bitCount);
            result = shifted & mask;
        }
        else
        {
            ulong shiftedP1 = this._data[ulongIndex] >> this.bitIndex;
            ulong maskP1 = LowerBitsMask(this.bitIndex);
            ulong shiftedP2 = this._data[ulongIndex + 1] << ((this.bitIndex + bitCount) % 64);
            ulong maskP2 = LowerBitsMask(bitCount);

            result = (shiftedP1 & maskP1) | (shiftedP2 & maskP2);
        }

        current += bitCount;

        return result;
    }

    public void Seek(int bitIndex)
    {
        if (bitIndex < 0 || bitIndex > this.Length)
            throw new ArgumentOutOfRangeException(nameof(bitIndex));
        this.current = bitIndex;
    }
    /// <summary>
    /// Gets the index in the stream the pattern occurs at.
    /// </summary>
    public int Find(ulong pattern, int patternLength)
    {
        if (patternLength < 1)
            throw new ArgumentOutOfRangeException(nameof(patternLength));
        if (patternLength > 64)
            throw new NotSupportedException("patternLength > 64");

        ulong mask = LowerBitsMask(patternLength);

        for (; this.current + patternLength < this.Length; this.current -= (patternLength - 1))
        {
            ulong current = this.ReadUInt64(patternLength);
            if (((current ^ pattern) & mask) == mask)
            {
                return this.current - patternLength;
            }
        }
        this.current = this.Length;
        return -1;
    }
    /// <summary>
    /// Gets the index in the stream the pattern occurs at.
    /// </summary>
    public int Find(ulong pattern, int patternLength, int startBitIndex)
    {
        if (startBitIndex < 0 || startBitIndex > this.Length)
            throw new ArgumentOutOfRangeException(nameof(startBitIndex));
        if (patternLength < 1)
            throw new ArgumentOutOfRangeException(nameof(patternLength));
        if (patternLength > 64)
            throw new ArgumentOutOfRangeException(nameof(patternLength), "> 64");

        this.Seek(startBitIndex);
        return this.Find(pattern, patternLength, startBitIndex);
    }

    /// <param name="length"> In bits. </param>
    public void CopyTo(ulong[] dest, int startBitIndex, int length, int destBitIndex)
    {
        if (dest is null)
            throw new ArgumentNullException(nameof(dest));
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length));
        if (startBitIndex < 0 || startBitIndex + length > this.Length)
            throw new ArgumentOutOfRangeException(nameof(length));
        if (destBitIndex < 0 || destBitIndex + length > dest.Length * 64)
            throw new ArgumentOutOfRangeException(nameof(destBitIndex));
        
        this.data.CopyTo(dest, destBitIndex);

    }

    /// <summary> Returns <see param="bitCount"/> ones followed by zeroes (least significant to most). </summary>
    private static ulong LowerBitsMask(int bitCount)
    {
        return ulong.MaxValue >> (64 - bitCount);
    }
    /// <summary> Returns zeroes ending on <see param="bitCount"/> ones (least significant to most). </summary>
    private static ulong UpperBitsMask(int bitCount)
    {
        return ~UpperBitsUnmask(bitCount);
    }
    /// <summary> Returns <see param="bitCount"/> zeroes followed by ones (least significant to most). </summary>
    private static ulong LowerBitsUnmask(int bitCount)
    {
        return ~LowerBitsMask(bitCount);
    }
    /// <summary> Returns ones ending on <see param="bitCount"/> zeroes (least significant to most). </summary>
    private static ulong UpperBitsUnmask(int bitCount)
    {
        return ulong.MaxValue << bitCount;
    }
}
