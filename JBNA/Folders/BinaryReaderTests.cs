using Xunit;
using JBSnorro.Collections;
using static JBSnorro.Diagnostics.Contract;

public class BinaryReaderTests
{
    [Fact]
    public void Can_Construct()
    {
        var reader = new BitReader(new JBSnorro.Collections.BitArray());
        Assert(reader.Length == 0);
    }
    [Fact]
    public void Can_Read_FalseBit()
    {
        var reader = new BitReader(new JBSnorro.Collections.BitArray(new bool[] { false }));
        Assert(reader.ReadBit() == false);
    }
    [Fact]
    public void Can_Read_TrueBit()
    {
        var reader = new BitReader(new JBSnorro.Collections.BitArray(new bool[] { true }));
        Assert(reader.ReadBit() == true);
    }
    [Fact]
    public void Can_Read_Zero_Byte()
    {
        var reader = new BitReader(new[] { 0b0UL }, 8);
        Assert(reader.ReadByte() == 0);
    }

    [Fact]
    public void Can_Read_One_Byte()
    {
        var reader = new BitReader(new[] { 0b0000_0001UL }, 8);
        Assert(reader.ReadByte() == 1);
    }
    [Fact]
    public void Can_Read_Two_Byte()
    {
        var reader = new BitReader(new[] { 0b0000_0010UL }, 8);
        Assert(reader.ReadByte() == 2);
    }

    [Fact]
    public void Can_Read_Two_ULong()
    {
        var reader = new BitReader(new[] { 0b0000_0010UL }, 64);
        Assert(reader.ReadUInt64() == 2);
    }
    [Fact]
    public void Reading_bits_is_successive()
    {
        var reader = new BitReader(new[] { 0b0000_0110UL }, 8);
        Assert(reader.ReadBit() == false);
        Assert(reader.ReadBit() == true);
        Assert(reader.ReadBit() == true);
        Assert(reader.ReadBit() == false);
        Assert(reader.ReadBit() == false);
        Assert(reader.RemainingLength == 3);
    }
    [Fact]
    public void Reading_bytes_is_successive()
    {
        var reader = new BitReader(new[] { 0b1111_0000_0000_0110UL }, 16);
        Assert(reader.ReadByte() == 0b110);
        Assert(reader.RemainingLength == 8);
        Assert(reader.ReadByte() == 0b1111_0000);
        Assert(reader.RemainingLength == 0);
    }
    [Fact]
    public void Reading_bits_and_bytes_is_successive()
    {
        var reader = new BitReader(new[] { 0b1101_0111_0100_0000_0110UL }, 20);
        Assert(reader.ReadBit() == false);
        Assert(reader.RemainingLength == 19);
        Assert(reader.ReadByte() == 0b0000_0011);
        Assert(reader.RemainingLength == 11);
        Assert(reader.ReadBit() == false);
        Assert(reader.ReadBit() == true);
        Assert(reader.ReadBit() == false);
        Assert(reader.RemainingLength == 8);
        Assert(reader.ReadByte() == 0b1101_0111);
    }
    [Fact]
    public void Can_read_bytes_over_ulong_crossing()
    {
        var reader = new BitReader(new[] { (0b1001UL << 60) | 1234, 0b1100UL }, 100);
        var x = reader.ReadUInt64(bitCount: 60);
        Assert(x == 1234);
        var y = reader.ReadByte();
        Assert(y == 0b1100_1001);
    }
}
