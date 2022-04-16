namespace JBNA;

public static class NumberSpec
{
    public static ICistronInterpreter<byte> ByteFactory { get; } = new ByteInterpreter();
    public static ICistronInterpreter<float> CreateUniformFloatFactory(float min, float max)
    {
        return new UniformFloatInterpreter(min, max);
    }
    internal class ByteInterpreter : ICistronInterpreter<byte>
    {
        private static readonly byte[] proximityOrder = ComputeProximityOrder();
        private static byte[] ComputeProximityOrder()
        {
            var mapping = new byte[256] { 0, 1, 2, 9, 3, 10, 11, 37, 4, 12, 13, 38, 14, 39, 40, 93, 5, 15, 16, 41, 17, 42, 43, 94, 18, 44, 44, 95, 45, 96, 97, 163, 6, 19, 20, 46, 21, 47, 48, 98, 22, 49, 50, 99, 51, 100, 101, 164, 23, 52, 53, 102, 54, 103, 104, 165, 55, 105, 106, 166, 107, 166, 167, 219, 7, 24, 25, 56, 26, 57, 57, 108, 27, 58, 59, 109, 60, 110, 111, 168, 28, 61, 62, 112, 63, 113, 114, 169, 64, 115, 116, 170, 117, 171, 172, 220, 29, 65, 66, 118, 67, 119, 120, 173, 68, 121, 122, 174, 123, 175, 176, 221, 69, 124, 125, 177, 126, 178, 179, 222, 127, 180, 181, 223, 182, 224, 225, 247, 8, 30, 31, 70, 32, 71, 72, 128, 33, 73, 74, 129, 75, 130, 131, 183, 34, 76, 77, 132, 78, 133, 134, 184, 79, 135, 136, 185, 137, 186, 187, 226, 35, 80, 81, 138, 82, 139, 140, 188, 83, 141, 142, 189, 143, 190, 191, 227, 84, 144, 145, 192, 146, 193, 194, 228, 147, 195, 196, 229, 197, 230, 231, 248, 36, 85, 86, 148, 87, 149, 150, 198, 88, 151, 152, 199, 153, 200, 201, 232, 89, 154, 155, 202, 156, 203, 204, 233, 157, 205, 206, 234, 207, 235, 236, 249, 90, 158, 159, 208, 160, 209, 210, 237, 161, 211, 212, 238, 213, 239, 240, 250, 162, 214, 215, 241, 216, 242, 243, 251, 217, 244, 245, 252, 246, 253, 254, 255 };
            return mapping;
            //var onesCounts = Enumerable.Range(0, 256).Select(b => Enumerable.Range(0, 8).Count(i => Extensions.HasBit((uint)b, i))).Select(b => (byte)b).ToArray();
            //int secondThenFirstComparer((byte a, byte b) first, (byte a, byte b) second)
            //{
            //    var aComparison = first.b.CompareTo(second.b);
            //    if (aComparison != 0)
            //        return aComparison;
            //    var r = first.a.CompareTo(second.a);
            //    return r;
            //}
            //var result = Enumerable.Range(0, 256).Select(b => (byte)b)
            //                       .Zip(onesCounts)
            //                       .OrderBy(_ => _, Extensions.ToComparer<(byte, byte)>(secondThenFirstComparer))
            //                       .Select(t => t.First)
            //                       .ToArray();
            //return result;

            //result[0] = 0; // differences are some binomial coefficients
            //result[0b1] = 8;
            //result[0b11] = 36;
            //result[0b111] = 92;
            //result[0b1111] = 162;
            //result[0b11111] = 218;
            //result[0b111111] = 246;
            //result[0b1111111] = 254;
            //result[0b11111111] = 255;
            //for (int i = 1; i < result.Length; i++)
            //{
            //    if (result[i] != 0) continue;
            //    result[i] = onesCounts.Zip(result)
            //                          .Take(i)
            //                          .Reverse()
            //                          .Where(t => t.First == onesCounts[i])
            //                          .First()
            //                          .Second;
            //    result[i]++;
            //}
            //return result;
        }

        public byte Interpret(BitReader cistronReader)
        {
            var bits = cistronReader.ReadByte();
            var result = proximityOrder[bits];
            return result;
        }
        public ulong MinBitCount => 8;
        public ulong MaxBitCount => 8;

        //public byte[] ReverseEngineer(TCodon startCodon, byte value, TCodon stopCodon)
        //{
        //    Assert(typeof(TCodon) == typeof(byte));
        //    return new byte[] { startCodon, value, stopCodon };
        //}

        object ICistronInterpreter.Interpret(BitReader cistron) => Interpret(cistron);
    }
    internal class UniformFloatInterpreter : ICistronInterpreter<float>
    {
        private static readonly ICistronInterpreter<byte> byteInterpreter = NumberSpec.ByteFactory;

        public ulong MinBitCount => byteInterpreter.MinBitCount;
        public ulong MaxBitCount => byteInterpreter.MaxBitCount;

        public float Min { get; }
        public float Max { get; }
        public UniformFloatInterpreter(float min, float max)
        {
            this.Min = min;
            this.Max = max;
        }
        public float Interpret(BitReader cistron)
        {
            Assert(cistron.Length == 8);
            byte b = byteInterpreter.Interpret(cistron);

            float result = this.Min + (this.Max - this.Min) * b / 255f;
            return result;
        }

        //public byte[] ReverseEngineer(byte startCodon, float value, byte stopCodon)
        //{
        //    byte encoded = Enumerable.Range(0, 255)
        //                             .Select(i => (byte)i)
        //                             .MinBy(b => Math.Abs(value - Interpret(new byte[] { b })));

        //    return new byte[] { startCodon, encoded, stopCodon };
        //}

        [DebuggerHidden] object ICistronInterpreter.Interpret(BitReader cistronReader) => Interpret(cistronReader);
    }
}

// TODO:
//public class Correlation : ICistron
//{

//}
//public class Array : ICistron
//{

//}
//public class Selection : ICistron
//{

//}

internal static class ConstantInterpreter
{
    [DebuggerHidden]
    public static ConstantInterpreter<T> Create<T>(T value)
    {
        return new ConstantInterpreter<T>(value);
    }
    [DebuggerHidden]
    public static ConstantInterpreter<T> CreateConstantInterpreter<T>(this T value) 
    {
        return Create(value);
    }
}
internal class ConstantInterpreter<T> : ICistronInterpreter<T>
{
    private readonly T value;

    public ConstantInterpreter(T value)
    {
        this.value = value;
    }

    public ulong MinBitCount => 0;
    public ulong MaxBitCount => 0;
    public T Interpret(BitReader cistronReader)
    {
        return value;
    }
}
internal class BooleanInterpreter : ICistronInterpreter<bool>
{
    public static ICistronInterpreter<bool> Instance { get; } = new BooleanInterpreter();


    public ulong MinBitCount => 1;
    public ulong MaxBitCount => 1;
    public bool Interpret(BitReader cistronReader)
    {
        return cistronReader.ReadBit();
    }

    private BooleanInterpreter() { }
}
internal class Int32Interpreter : ICistronInterpreter<int>
{
    private static readonly Int32Interpreter[] instances = Enumerable.Range(0, 32).Select(i => new Int32Interpreter((ulong)i)).ToArray();
    public static ICistronInterpreter<int> Create(int bitCount)
    {
        Requires<ArgumentException>(0 < bitCount && bitCount <= 32);

        return instances[bitCount];
    }

    public ulong MinBitCount { get; }
    public ulong MaxBitCount => MinBitCount;
    public int Interpret(BitReader cistronReader)
    {
        return cistronReader.ReadInt32((int)this.MinBitCount);
    }

    private Int32Interpreter(ulong bitCount)
    {
        MinBitCount = bitCount;
    }
}
