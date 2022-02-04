using JBSnorro;


void Evolve(IEnumerable<ICistronSpec> orders, Func<object[], float> scoreFunction)
{

}

public class Genome
{
    public static Genome CreateRandom(IReadOnlyList<ICistronSpec> specs)
    {
        var defaultOrders = ICistron.DefaultCistrons;
        int minimalBitCount = specs.Aggregate(0, (s, order) => s + order.MinBitCount);

    }
    private static Dictionary<ICistronSpec, int> SpecToIndices(IEnumerable<ICistronSpec> specs)
    {
        return specs.Zip(Enumerable.Range(0, int.MaxValue)).ToDictionary(t => t.First, t => t.Second);
    }
    private Genome(IReadOnlyList<Chromosome> chromosomes, IEnumerable<ICistronSpec> specs) : this(chromosomes, SpecToIndices(specs))
    {
    }
    private Genome(IReadOnlyList<Chromosome> chromosomes, Dictionary<ICistronSpec, int> specIndices)
    {
        this.StartCodonUniques = GenerateUniqueRandomNumbers(drawCount: specIndices.Count, max: 254);
        this.Chromosomes = chromosomes;
        this.CodonCollection = new ReadOnlyStartCodonCollection<ICistronSpec>(specIndices.Keys, this.StartCodonUniques);
        // this dictionary is a O(1) way of finding the index of a spec in this.Specs
    }
    private Dictionary<ICistronSpec, int> SpecIndices { get; }
    public IReadOnlyCollection<ICistronSpec> Specs => SpecIndices.Keys;
    public IReadOnlyList<Chromosome> Chromosomes { get; }
    internal ReadOnlyStartCodonCollection<ICistronSpec> CodonCollection { get; }
    private readonly int[] StartCodonUniques;  // for serialiation
    public object[] Interpret()
    {
        var interpreters = new Func<object>[this.Specs.Count];
        foreach (var (spec, interpret) in Chromosomes.SelectMany(c => c.FindCistrons(this.CodonCollection)))
        {
            int specIndex = this.SpecIndices[spec]; // must be present, otherwise throw
            bool alreadyPresent = interpreters[specIndex] != null;
            if (alreadyPresent)
            {
                if (spec.Merger == null)
                    throw new Exception($"Multiple of the same unmergable cistron type detected ({spec})");
                interpreters[specIndex] = spec.Merger.Merge(interpreters[specIndex], interpret);
            }
            else
            {
                interpreters[specIndex] = interpret;
            }
        }

        var result = new object[this.Specs.Count];
        for (int i = 0; i < this.Specs.Count; i++)
        {
            result[i] = interpreters[i]();
        }
        return result;
    }
}

internal class Codon
{
    public ICistron Cistron { get; }

    public Codon(ICistron cistron)
    {
        Cistron = cistron;
    }
}
public class ReadOnlyStartCodonCollection<T>
{
    public ReadOnlyStartCodonCollection(IReadOnlyCollection<T> objects, IReadOnlyList<int> keys)
    {
        Assert(objects.Count < 255);
        CodonBitLength = 8; // = (int)Math.Log2(cistronCount) + 1;


        var dict = new Dictionary<int, T>(objects.Count);
        int i = 0;
        foreach (var obj in objects)
        {
            var key = keys[i] + 1; // + 1 to skip StopCodon
            dict.Add(key, obj);
            i++;
        }
        this.Objects = dict;
        this.StopCodonByteLength = 1;
        this.StopCodon = 0;
        Assert(!dict.ContainsKey(this.StopCodon));
    }
    public int CodonBitLength { get; }
    public int CodonByteLength => (CodonBitLength + 7) / 8;
    public int StopCodon { get; }
    public int StopCodonByteLength { get; }
    public IReadOnlyDictionary<int, T> Objects { get; }


    public bool TryFindNext(byte[] data, int startIndex, out T? value)
    {
        return TryFindNext(data, startIndex, out value, out var _);
    }
    public bool TryFindNext(byte[] data, int startIndex, out T? value, out int index)
    {
        Assert(CodonByteLength == 1);
        Assert(CodonBitLength == 8);
        for (int i = startIndex; i < data.Length; i++)
        {
            if (this.Objects.TryGetValue(data[i], out var codon))
            {
                index = i;
                value = codon;
                return true;
            }
        }
        index = -1;
        value  = default;
        return false;
    }
    public Index FindStopCodon(byte[] data, int startIndex)
    {
        for (int i = startIndex; i < data.Length; i++)
        {
            if (i == StopCodon)
                return i;
        }
        return Index.End;
    }

    public IEnumerable<KeyValuePair<T, Range>> Split(byte[] data)
    {
        int index = 0;
        while (TryFindNext(data, index, out var value, out var codonIndex))
        {
            int cistronStartIndex = codonIndex + this.CodonByteLength;
            var stopCodonIndex = FindStopCodon(data, codonIndex);

            if (Index.End.Equals(stopCodonIndex))
            {
                yield return KeyValuePair.Create(value!, new Range(cistronStartIndex, Index.End));
                break;
            }
            else
            {
                yield return KeyValuePair.Create(value!, new Range(cistronStartIndex, stopCodonIndex));
                index = stopCodonIndex.Value + StopCodonByteLength;
            }
        }
    }
}

public class Chromosome
{
    private readonly byte[] _data;

    public IEnumerable<(ICistronSpec, Func<object>)> FindCistrons(ReadOnlyStartCodonCollection<ICistronSpec> codonCollection)
    {
        foreach (var (cistronSpec, range) in codonCollection.Split(this._data))
        {
            if (Index.End.Equals(range.End))
            {
                if (!cistronSpec.ImplicitStopCodonAllowed)
                    throw new Exception("Infeasible");
                if (range.GetOffsetAndLength(this._data.Length).Length < cistronSpec.MinByteCount)
                    throw new Exception("Implicit stop codon sequence too short");
            }
            yield return (cistronSpec, () => cistronSpec.Interpreter.Create(this._data.AsSpan(range)));
        }
    }
}

public interface ICistronSpec
{
    int MinBitCount { get; }
    int MaxBitCount { get; }
    int MinByteCount => (MinBitCount + 7)/ 8;
    int MaxByteCount => (MaxBitCount + 7)/ 8;
    bool Meta => false;
    bool ImplicitStopCodonAllowed => true;
    IMultiCistronMerger? Merger => null;
    ICistronInterpreter Interpreter => ((ICistronSpec<object>)this).Interpreter;
}
public interface IMultiCistronMerger  // decides which is recessive, dominant, or merges
{
    Func<object> Merge(Func<object> previous, Func<object> current);
}
public interface ICistronSpec<out T> : ICistronSpec
{
    new ICistronInterpreter<T> Interpreter { get; }
}
public interface ICistronInterpreter
{
    object Create(Span<byte> cistron) => ((ICistronInterpreter<object>)this).Create(cistron);
}
public interface ICistronInterpreter<out T> : ICistronInterpreter
{
    /// <summary>
    /// The purpose of this function is to transform the data in a <typeparamref name="T"/> 
    /// where similar input corresponding to similar output, is a approximately continuous fashion.
    /// </summary>
    /// <returns> An <see cref="ICistron"/> or an <see cref="ICistron"/>-like.</returns>
    new T Create(Span<byte> cistron);
}

public interface ICistron
{
    public static IReadOnlyCollection<ICistron> DefaultCistrons { get; } = new List<ICistron>();
}

public class Number : ICistron
{
    public static ICistronSpec<byte> ByteFactory { get; } = new ByteFactoryImpl();
    public static ICistronSpec<float> CreateUniformFloatFactory(float min, float max)
    {
        return new UniformFloatFactoryImpl(min, max);
    }
    class ByteFactoryImpl : ICistronSpec<byte>
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
        public int MinBitCount { get; } = 1;
        public int MaxBitCount { get; } = 1;

        public byte Create(byte[] data)
        {
            Assert(data.Length == 1);
            return proximityOrder[data[0]];
        }

        public ICistronInterpreter<byte> Interpreter { get; } = new ByteCistronInterpreter();
        internal class ByteCistronInterpreter : ICistronInterpreter<byte>
        {
            public static byte Create(Span<byte> cistron)
            {
                Assert(cistron.Length == 1);
                return cistron[0];
            }
            byte ICistronInterpreter<byte>.Create(Span<byte> cistron)
            {
                return ByteCistronInterpreter.Create(cistron);
            }
        }

    }
    class UniformFloatFactoryImpl : ICistronSpec<float>
    {
        private readonly ByteFactoryImpl byteFactory = new();
        public int MinBitCount => byteFactory.MinBitCount;
        public int MaxBitCount => byteFactory.MaxBitCount;
        public float Min { get; }
        public float Max { get; }


        public UniformFloatFactoryImpl(float min, float max)
        {
            Assert(float.IsNormal(min));
            Assert(float.IsNormal(max));
            Assert(min < max);

            this.Min = min;
            this.Max = max;
            this.Interpreter = new FloatCistronInterpreter(this);
        }

        public ICistronInterpreter<float> Interpreter { get; }

        internal class FloatCistronInterpreter : ICistronInterpreter<float>
        {
            private readonly UniformFloatFactoryImpl spec;
            public FloatCistronInterpreter(UniformFloatFactoryImpl spec)
            {
                this.spec = spec;
            }
            public static float Create(Span<byte> cistron, float min, float max)
            {
                Assert(cistron.Length == 1);
                byte b = ByteFactoryImpl.ByteCistronInterpreter.Create(cistron);

                float result = min + (max - min) * b / 255f;
                return result;
            }
            float ICistronInterpreter<float>.Create(Span<byte> cistron)
            {
                return FloatCistronInterpreter.Create(cistron, this.spec.Min, this.spec.Max);
            }
        }

    }
}

public class Correlation : ICistron
{

}
public class Array : ICistron
{

}
public class Selection : ICistron
{

}
public class Function : ICistron
{

}
public class Distribution : Function
{

}
public class MutationRate : Function
{

}
