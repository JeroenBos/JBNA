namespace JBNA;

internal class FourierFunctionCistronInterpreter : ICistronInterpreter<DimensionfulFunction>, ICistronInterpreter<DimensionfulContinuousFunction>, ICistronInterpreter<IDimensionfulContinuousFunction>, ICistronInterpreter<IDimensionfulDiscreteFunction>, ICistronInterpreter<IIntegratedDimensionfulDiscreteFunction>, ICistronInterpreter<IIntegratedDimensionfulContinuousFunction>
{
    public static FourierFunctionCistronInterpreter Singleton { get; } = new FourierFunctionCistronInterpreter();

    /// <summary>
    /// The number of bits at the beginning of the cistron that encode how many bits encode each subsequent number.
    /// </summary>
    private uint bitsPerNumberBitLength = 4;
    /// <summary>
    /// The minimum number of bits required to encode the first number (after the bitsPerNumber).
    /// </summary>
    const int bitsForFirstNumber = 3;
    public ulong MinBitCount => bitsPerNumberBitLength + bitsForFirstNumber;
    public ulong MaxBitCount => int.MaxValue;


    internal Coefficients Interpret(BitReader cistronReader)
    {
        const int minCoefficientBitCount = 2;
        Assert(cistronReader.CanRead(MinBitCount));

        ulong coefficientBitCount = minCoefficientBitCount + cistronReader.ReadUInt32(bitsForFirstNumber); // in a_0MinBitCount + [0, 15]

        ulong a_0BitCount = Math.Min(coefficientBitCount, cistronReader.RemainingLength);
        long a_0 = cistronReader.ReadInt32((int)a_0BitCount);

        var (N, rem) = Math.DivRem(cistronReader.RemainingLength, 2 * coefficientBitCount);
        bool extra_a = rem >= minCoefficientBitCount;
        bool extra_b = rem >= minCoefficientBitCount + coefficientBitCount;

        // even though we only use Half precision, computationally, floats are faster
        var a_n = new float[N + (extra_a ? 1UL : 0)];
        var b_n = new float[N + (extra_b ? 1UL : 0)];

        for (int i = 0; i < (int)N; i++)
        {
            a_n[i] = cistronReader.ReadSingle((int)coefficientBitCount);
            b_n[i] = cistronReader.ReadSingle((int)coefficientBitCount);
        }
        if (extra_a)
        {
            a_n[^1] = cistronReader.ReadSingle((int)Math.Min(coefficientBitCount, cistronReader.RemainingLength));
            if (extra_b)
            {
                b_n[^1] = cistronReader.ReadSingle((int)Math.Min(coefficientBitCount, cistronReader.RemainingLength));
            }
        }

        Assert(cistronReader.RemainingLength < 2);
        return new Coefficients(a_0, a_n, b_n);
    }
    internal sealed record Coefficients(float a_0, float[] a_n, float[] b_n) : IIntegratedDimensionfulContinuousFunction, IIntegratedDimensionfulDiscreteFunction
    {
        public DimensionfulContinuousFunction Original => Invoke;
        public float NormalizationConstant
        {
            get
            {
                // see https://math.stackexchange.com/a/3406156/8064
                var sum = a_0 * a_0 / 4;
                for (int i = 0; i < a_n.Length; i++)
                {
                    sum += a_n[i] * a_n[i];
                }
                for (int i = 0; i < b_n.Length; i++)
                {
                    sum += b_n[i] * b_n[i];
                }
                return 1 / (float)Math.Sqrt(sum);
            }
        }

        public float Invoke(OneDimensionalContinuousQuantity arg)
        {
            // see https://en.wikipedia.org/wiki/Fourier_series#Definition

            var twoPiOverP = 2 * Math.PI / arg.Length;
            double x = (double)arg.Value;
            int n;
            double result = a_0;
            for (n = 1; n < b_n.Length; n++)
            {
                result += a_n[n] * Math.Cos(twoPiOverP * n * x);
                result += b_n[n] * Math.Sin(twoPiOverP * n * x);
            }
            if (a_n.Length != b_n.Length)
                result += a_n[^1] * Math.Cos(twoPiOverP * n * x);
            return (float)result;
        }

        public float Invoke(OneDimensionalDiscreteQuantity arg)
        {
            var result = this.Invoke(arg.ToContinuous());
            return result;
        }
    }


    private FourierFunctionCistronInterpreter() { }

    object ICistronInterpreter.Interpret(BitReader cistronReader) => Interpret(cistronReader)!;
    DimensionfulFunction ICistronInterpreter<DimensionfulFunction>.Interpret(BitReader cistronReader) => this.Interpret(cistronReader).Invoke;
    DimensionfulContinuousFunction ICistronInterpreter<DimensionfulContinuousFunction>.Interpret(BitReader cistronReader) => this.Interpret(cistronReader).Invoke;
    IDimensionfulContinuousFunction ICistronInterpreter<IDimensionfulContinuousFunction>.Interpret(BitReader cistronReader) => Interpret(cistronReader);
    IDimensionfulDiscreteFunction ICistronInterpreter<IDimensionfulDiscreteFunction>.Interpret(BitReader cistronReader) => Interpret(cistronReader);
    IIntegratedDimensionfulDiscreteFunction ICistronInterpreter<IIntegratedDimensionfulDiscreteFunction>.Interpret(BitReader cistronReader) => Interpret(cistronReader);
    IIntegratedDimensionfulContinuousFunction ICistronInterpreter<IIntegratedDimensionfulContinuousFunction>.Interpret(BitReader cistronReader) => Interpret(cistronReader);
}
