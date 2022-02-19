using BitArray = JBSnorro.Collections.BitArray;

public class BitReader
{
    private readonly BitArray data;
    private readonly ulong[] _data;
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

        return (byte)_Read(bitCount);
    }
    public short ReadInt16(int bitCount = 16)
    {
        if (bitCount < 1 || bitCount > 16)
            throw new ArgumentException(nameof(bitCount));
        if (this.RemainingLength < bitCount)
            throw InsufficientBitsException("short");

        return (short)_Read(bitCount);
    }

    public int ReadInt32(int bitCount = 32)
    {
        if (bitCount < 1 || bitCount > 32)
            throw new ArgumentException(nameof(bitCount));
        if (this.RemainingLength < bitCount)
            throw InsufficientBitsException("int");

        return (int)_Read(bitCount);
    }
    public long ReadInt64(int bitCount = 64)
    {
        if (bitCount < 1 || bitCount > 64)
            throw new ArgumentException(nameof(bitCount));
        if (this.RemainingLength < bitCount)
            throw InsufficientBitsException("long");

        return (long)_Read(bitCount);
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

    private ulong _Read(int bitCount)
    {
        if (bitCount < 1 || bitCount > 64)
            throw new ArgumentException(nameof(bitCount));

        ulong result;
        if (bitIndex + bitCount <= 64)
        {
            ulong shifted = this._data[ulongIndex] >> bitIndex;
            ulong mask = ulong.MaxValue >> (64 - bitCount);
            result = shifted & mask;
        }
        else
        {
            ulong shiftedP1 = this._data[ulongIndex] >> this.bitIndex;
            ulong maskP1 = ulong.MaxValue >> (64 - this.bitIndex);
            ulong shiftedP2 = this._data[ulongIndex + 1] << ((this.bitIndex + bitCount) % 64);
            ulong maskP2 = ulong.MaxValue >> (64 - bitCount);

            result = (shiftedP1 & maskP1) | (shiftedP2 & maskP2);
        }

        current += bitCount;

        return result;
    }

    public void Seek(int bitIndex)
    {
        if (bitIndex > this.Length)
            throw new ArgumentOutOfRangeException(nameof(bitIndex));
        this.current = bitIndex;
    }
}
