class BitReader
{
    private readonly JBSnorro.Collections.BitArray data;
    private readonly ulong[] _data;
    public bool this[int longIndex, int bitindex] => this.data[longIndex * 64 + bitIndex];

    private int current;
    private int ulongIndex => current % 64;
    private int bitIndex => current % 64;
    public int RemainingBitCount => this.data.Length - current;
    public BitReader(JBSnorro.Collections.BitArray data, int startBitIndex = 0)
    {
        this.data = data;
        this.current = startBitIndex;
    }

    private static Exception InsufficientBitsException(string elementName)
    {
        return new InvalidOperationException($"Insufficient bits remaining in stream to read '{elementName}'");
    }
    public bool ReadBit()
    {
        if (this.RemainingBitCount < 1)
            throw InsufficientBitsException("bit");

        bool result = this[ulongIndex, bitIndex];
        current++;
        return result;
    }
    public byte ReadByte(int bitCount = 8)
    {
        if (bitCount < 1 || bitCount > 8)
            throw new ArgumentException(nameof(bitCount));
        if (this.RemainingBitCount < bitCount)
            throw InsufficientBitsException("byte");

        return (byte)_Read(bitCount);
    }
    public short ReadInt16(int bitCount = 16)
    {
        if (bitCount < 1 || bitCount > 16)
            throw new ArgumentException(nameof(bitCount));
        if (this.RemainingBitCount < bitCount)
            throw InsufficientBitsException("short");

        return (short)_Read(bitCount);
    }
    
    public int ReadInt32(int bitCount = 32)
    {
        if (bitCount < 1 || bitCount > 32)
            throw new ArgumentException(nameof(bitCount));
        if (this.RemainingBitCount < bitCount)
            throw InsufficientBitsException("int");

        return (int)_Read(bitCount);
    }
    public long ReadInt64(int bitCount = 64)
    {
        if (bitCount < 1 || bitCount > 64)
            throw new ArgumentException(nameof(bitCount));
        if (this.RemainingBitCount < bitCount)
            throw InsufficientBitsException("long");

        return (long)_Read(bitCount);
    }
    public float ReadSingle()
    {
        if (this.RemainingBitCount < 32)
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
        if (this.RemainingBitCount < 16)
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
        if (bitIndex + bitCount < 64)
        {
            ulong shifted = this._data[ulongIndex] >> bitIndex;
            ulong mask = ulong.MaxValue >> (64 - bitCount);
            result = shifted & mask;
        }
        else
        {
            ulong shiftedP1 = this._data[ulongIndex] >> this.bitIndex;
            ulong maskP1 = ulong.MaxValue >> (64 - this.bitIndex);
            ulong shiftedP2 = this._data[ulongIndex + 1] << (bitCount - this.bitIndex);
            ulong maskP2 = ulong.MaxValue >> (64 - bitCount);

            result = (shiftedP1 & maskP1) | (shiftedP2 & maskP2);
        }

        current += bitCount;

        return result;
    }
}
