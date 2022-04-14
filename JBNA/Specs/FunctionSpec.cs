namespace JBNA;

public class FunctionSpecFactory
{
    private static readonly ImmutableArray<ICistronInterpreter<DimensionfulDiscreteFunction>> functionTypes = new ICistronInterpreter<DimensionfulDiscreteFunction>[] { FourierFunctionCistronInterpreter.Singleton }.ToImmutableArray();
    public static ICistronInterpreter<DimensionfulDiscreteFunction> FourierFunctionInterpreter => FourierFunctionCistronInterpreter.Singleton;
    public static int FunctionTypeCount => functionTypes.Length;

    public Nature Nature { get; }
    public ICistronInterpreter<DimensionfulDiscreteFunction> DiscretePatternInterpreter { get; }
    public ICistronInterpreter<DimensionfulContinuousFunction> ContinuousPatternInterpreter { get; }
    public FunctionSpecFactory(Nature nature)
    {
        this.Nature = nature ?? throw new ArgumentNullException(nameof(nature));
        var patternInterpreter = new Pattern1DInterpreter(this.Nature);
        this.DiscretePatternInterpreter = patternInterpreter;
        this.ContinuousPatternInterpreter = patternInterpreter;
    }
    public Func<int, int> InterpretImplicitlyRanged1DFunction(BitArrayReadOnlySegment cistron)
    {
        const int minimumRangeDimensionality = 256; // to require less bits to represent the range, because the dimensionality will probably never be under this
        
        // read the range of the function
        var reader = cistron.ToBitReader();
        var extremum1 = reader.ReadInt32(Nature.FunctionRangeBitCount) * minimumRangeDimensionality;
        var extremum2 = reader.ReadInt32(Nature.FunctionRangeBitCount) * minimumRangeDimensionality;
        var min = Math.Min(extremum1, extremum2);
        var max = Math.Max(extremum1, extremum2);
        var range = new Range(min, max - min);
        
        // get the function
        var f = Interpret1DFunction(cistron);
        return g;

        int g(int x)
        {
            return f(new OneDimensionalDiscreteQuantity(x, max - min, min));
        }
    }
   
    /// <summary>
    /// Converts the cistron into a function.
    /// </summary>
    /// <param name="cistron">The cistron is expected to start with an indeterminate few bits describing the 'type of function'.</param>
    /// <param name="selectResult">A function that interprets bits. </param>
    public DimensionfulDiscreteFunction Interpret1DFunction(BitArrayReadOnlySegment cistron)
    {
        // the implementation of this should just be reading a few bits
        // + a switch statement over the various implemented function types.
        // then that would make this a rangeless function?
        // Oh no wait, we didn't do that distinction anymore. the initial mapping will just be to short, and that's that. The rest can map how they see fit. Scaling will have to be done with the range [short.MinValue, short.MaxValue] in mind.

        var reader = cistron.ToBitReader();
        int functionType = reader.ReadInt32(Nature.FunctionTypeBitCount);

        return functionTypes[functionType % functionTypes.Length].Interpret(reader.RemainingSegment);
    }
    /// <summary>
    /// Converts the cistron into a pattern function.
    /// </summary>
    /// <param name="cistron">
    /// The cistron is expected to start with a caller-responsible number of bits describing the 'type of pattern'.
    /// Also configuration, e.g. whether the pattern repeats or is stretched to a particular size, could be at the start of the cistron.
    /// </param>
    /// <returns>a function that gets the value of the pattern given the position and length of the (only) dimension. </returns>
    public DimensionfulDiscreteFunction Interpret1DPattern(BitArrayReadOnlySegment cistron)
    {
        // in essence a pattern is just a particular function.
        // Q: unsure about how the type T plays a role, if any. and interpolation? Could be done at the short level, maybe should be done at the TResult level, but that then requires another parameter
        // A: we solved that by simply returning the short. Caller is allowed to map and compose as pleased.

        var patternFunction = Nature.FunctionFactory.DiscretePatternInterpreter.Interpret(cistron);
        // This patternFunction takes a quantity, which is a "tuple" that can convey simultaneously the value in the dimension, and its length.
        // its return value is the pattern value.
        return patternFunction;
    }

    class Pattern1DInterpreter : ICistronInterpreter<DimensionfulDiscreteFunction>, ICistronInterpreter<DimensionfulContinuousFunction>
    {
        public Nature Nature { get; }
        public Pattern1DInterpreter(Nature nature)
        {
            this.Nature = nature;
        }
        public ulong MinBitCount => 1UL + (ulong)Nature.PatternLengthBitCount;
        public ulong MaxBitCount => Nature.MaxCistronLength;

        /// <returns>a function taking a value plus the size of its dimension, and returns the value of the pattern at that position. </returns>
        DimensionfulDiscreteFunction ICistronInterpreter<DimensionfulDiscreteFunction>.Interpret(BitArrayReadOnlySegment cistron)
        {
            var reader = cistron.ToBitReader();
            bool repeats = reader.ReadBit();
            int patternLength = reader.ReadInt32(bitCount: Nature.PatternLengthBitCount);

            var function = Nature.FunctionFactory.Interpret1DFunction(reader.RemainingSegment); // .Map(impl, domainLength: patternLength);
            return impl;
            int impl(OneDimensionalDiscreteQuantity arg)
            {
                // let's say there's a lattice of length L
                // and a pattern of length P
                // then the incoming arg.Value is L

                // and intermediate quantity's Value is P
                // L and P are in the same units

                int L = arg.Length;
                int L_value = arg.Value;
                int L_offset = arg.Start;
                int P = patternLength;
                int P_offset = 0;
                int P_value;

                if (repeats)
                {
                    P_value = (L_value - L_offset) % P + P_offset;
                }
                else
                {
                    P_value = (int)Math.Round((double)((L_value - L_offset) * P) / L) + P_offset;
                }

                return new OneDimensionalDiscreteQuantity(P_value, P, P_offset).Value;
            }
        }
        DimensionfulContinuousFunction ICistronInterpreter<DimensionfulContinuousFunction>.Interpret(BitArrayReadOnlySegment cistron)
        {
            throw new NotImplementedException();
        }
    }

    internal class FourierFunctionCistronInterpreter : ICistronInterpreter<DimensionfulDiscreteFunction>, ICistronInterpreter<DimensionfulContinuousFunction>
    {
        public static FourierFunctionCistronInterpreter Singleton { get; } = new FourierFunctionCistronInterpreter();

        /// <summary>
        /// The number of bits at the beginning of the cistron that encode how many bits encode each subsequent number.
        /// </summary>
        private uint bitsPerNumberBitLength = 5;
        /// <summary>
        /// The minimum number of bits required to encode the first number (after the bitsPerNumber).
        /// </summary>
        const int bitsForFirstNumber = 4;
        public ulong MinBitCount => bitsPerNumberBitLength + bitsForFirstNumber;
        public ulong MaxBitCount { get; } = int.MaxValue;


        public DimensionfulContinuousFunction Interpret(BitArrayReadOnlySegment cistron)
        {
            const int minCoefficientBitCount = 2;
            Assert(cistron.Length >= this.MinBitCount);
            var reader = cistron.ToBitReader();
            Assert(reader.CanRead(MinBitCount));

            ulong coefficientBitCount = minCoefficientBitCount + reader.ReadUInt32(bitsForFirstNumber); // in a_0MinBitCount + [0, 31]

            ulong a_0BitCount = Math.Min(coefficientBitCount, reader.RemainingLength);
            long a_0 = reader.ReadInt32((int)a_0BitCount);

            var (N, rem) = Math.DivRem(reader.RemainingLength, 2 * coefficientBitCount);
            bool extra_a = rem >= minCoefficientBitCount;
            bool extra_b = rem >= minCoefficientBitCount + coefficientBitCount;
            var a_n = new int[N + (extra_a ? 1UL : 0)];
            var b_n = new int[N + (extra_b ? 1UL : 0)];

            for (int i = 0; i < (int)N; i++)
            {
                a_n[i] = reader.ReadInt32((int)coefficientBitCount);
                b_n[i] = reader.ReadInt32((int)coefficientBitCount);
            }
            if (extra_a)
            {
                a_n[^1] = reader.ReadInt32((int)Math.Min(coefficientBitCount, reader.RemainingLength));
                if (extra_b)
                {
                    b_n[^1] = reader.ReadInt32((int)Math.Min(coefficientBitCount, reader.RemainingLength));
                }
            }

            Assert(reader.RemainingLength < 2);
            return f;

            float f(OneDimensionalContinuousQuantity arg)
            {
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
        }


        private FourierFunctionCistronInterpreter() { }
        object ICistronInterpreter.Interpret(BitArrayReadOnlySegment cistron) => Interpret(cistron)!;
        DimensionfulDiscreteFunction ICistronInterpreter<DimensionfulDiscreteFunction>.Interpret(BitArrayReadOnlySegment cistron)
        {
            throw new NotImplementedException();
        }
    }

}
//internal class Distribution : FunctionSpecFactory
//{
//    public Distribution(ICistronInterpreter2<T> interpreter) : base( interpreter)
//    {
//    }
//}
//internal class MutationRate : FunctionSpecFactory
//{
//    public MutationRate(ICistronInterpreter2<T> interpreter) : base( interpreter)
//    {
//    }
//}
