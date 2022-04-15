namespace JBNA;

public class FunctionSpecFactory
{
    private static readonly ImmutableArray<ICistronInterpreter<DimensionfulDiscreteFunction>> functionTypes = new ICistronInterpreter<DimensionfulDiscreteFunction>[] { FourierFunctionCistronInterpreter.Singleton }.ToImmutableArray();
    public static ICistronInterpreter<DimensionfulDiscreteFunction> FourierFunctionInterpreter => FourierFunctionCistronInterpreter.Singleton;
    public static int FunctionTypeCount => functionTypes.Length;

    public Nature Nature { get; }
    // public ICistronInterpreter<DimensionfulDiscreteFunction> DiscretePatternInterpreter { get; } as opposed to the function
    public ICistronInterpreter<DimensionfulDiscreteFunction> DiscretePatternInterpreter { get; }
    public ICistronInterpreter<DimensionfulContinuousFunction> ContinuousPatternInterpreter { get; }
    public ICistronInterpreter<DimensionfulContinuousFunction> ContinuousNormalizedFunctionInterpreter { get; }

    public FunctionSpecFactory(Nature nature)
    {
        this.Nature = nature ?? throw new ArgumentNullException(nameof(nature));
        var patternInterpreter = new Pattern1DInterpreter(this.Nature);
        this.DiscretePatternInterpreter = patternInterpreter;
        //this.ContinuousPatternInterpreter = patternInterpreter;
        this.OneDFunctionInterpreter = ChoiceCistronInterpreter.Create(functionTypes, reader => reader.ReadInt32(Nature.FunctionTypeBitCount) % functionTypes.Length);
        this.ContinuousNormalizedFunctionInterpreter = ContinuousPatternInterpreter.
    }
    public Func<int, int> InterpretImplicitlyRanged1DFunction(BitReader cistronReader)
    {
        const int minimumRangeDimensionality = 256; // to require less bits to represent the range, because the dimensionality will probably never be under this

        // read the range of the function
        var extremum1 = cistronReader.ReadInt32(Nature.FunctionRangeBitCount) * minimumRangeDimensionality;
        var extremum2 = cistronReader.ReadInt32(Nature.FunctionRangeBitCount) * minimumRangeDimensionality;
        var min = Math.Min(extremum1, extremum2);
        var max = Math.Max(extremum1, extremum2);
        var range = new Range(min, max - min);

        // get the function
        var f = Interpret1DFunction(cistronReader);
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
    public DimensionfulDiscreteFunction Interpret1DFunction(BitReader cistronReader)
    {
        // the implementation of this should just be reading a few bits
        // + a switch statement over the various implemented function types.
        // then that would make this a rangeless function?
        // Oh no wait, we didn't do that distinction anymore. the initial mapping will just be to short, and that's that. The rest can map how they see fit. Scaling will have to be done with the range [short.MinValue, short.MaxValue] in mind.

        return OneDFunctionInterpreter.Interpret(cistronReader);
    }
    public ICistronInterpreter<DimensionfulDiscreteFunction> OneDFunctionInterpreter { get; }


    /// <summary>
    /// Converts the cistron into a pattern function.
    /// </summary>
    /// <param name="cistronReader">
    /// The cistron is expected to start with a caller-responsible number of bits describing the 'type of pattern'.
    /// Also configuration, e.g. whether the pattern repeats or is stretched to a particular size, could be at the start of the cistron.
    /// </param>
    /// <returns>a function that gets the value of the pattern given the position and length of the (only) dimension. </returns>
    public DimensionfulDiscreteFunction Interpret1DPattern(BitReader cistronReader)
    {
        // in essence a pattern is just a particular function.
        // Q: unsure about how the type T plays a role, if any. and interpolation? Could be done at the short level, maybe should be done at the TResult level, but that then requires another parameter
        // A: we solved that by simply returning the short. Caller is allowed to map and compose as pleased.

        var patternFunction = Nature.FunctionFactory.DiscretePatternInterpreter.Interpret(cistronReader);
        // This patternFunction takes a quantity, which is a "tuple" that can convey simultaneously the value in the dimension, and its length.
        // its return value is the pattern value.
        return patternFunction;
    }
    /// <summary>
    /// A one-dimensional pattern is a sequence that is either repeated or scaled onto the dimension.
    /// </summary>
    class Pattern1DInterpreter : CompositeCistronInterpreter<bool, int, DimensionfulDiscreteFunction, DimensionfulDiscreteFunction>
    {
        public Pattern1DInterpreter(Nature nature) : base(BooleanInterpreter.Instance,
                                                          Int32Interpreter.Create(nature.PatternLengthBitCount),
                                                          nature.FunctionFactory.OneDFunctionInterpreter)
        {
        }
        /// <returns>a function taking a value plus the size of its dimension, and returns the value of the pattern at that position. </returns>
        protected override DimensionfulDiscreteFunction Combine(bool repeats, int patternLength, DimensionfulDiscreteFunction pattern)
        {
            return impl;
            int impl(OneDimensionalDiscreteQuantity arg)
            {
                // let's say there's a lattice of length L
                // and a pattern of length P
                // then the incoming arg.Value is L

                // and intermediate quantity's Value is P
                // L and P are in the same units
                var (L_value, L, L_offset) = arg;

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
                var patternArg = new OneDimensionalDiscreteQuantity(P_value, P, P_offset);
                var result = pattern(patternArg);
                return result;
            }
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


        public DimensionfulContinuousFunction Interpret(BitReader cistronReader)
        {
            const int minCoefficientBitCount = 2;
            Assert(cistronReader.CanRead(MinBitCount));

            ulong coefficientBitCount = minCoefficientBitCount + cistronReader.ReadUInt32(bitsForFirstNumber); // in a_0MinBitCount + [0, 31]

            ulong a_0BitCount = Math.Min(coefficientBitCount, cistronReader.RemainingLength);
            long a_0 = cistronReader.ReadInt32((int)a_0BitCount);

            var (N, rem) = Math.DivRem(cistronReader.RemainingLength, 2 * coefficientBitCount);
            bool extra_a = rem >= minCoefficientBitCount;
            bool extra_b = rem >= minCoefficientBitCount + coefficientBitCount;
            var a_n = new int[N + (extra_a ? 1UL : 0)];
            var b_n = new int[N + (extra_b ? 1UL : 0)];

            for (int i = 0; i < (int)N; i++)
            {
                a_n[i] = cistronReader.ReadInt32((int)coefficientBitCount);
                b_n[i] = cistronReader.ReadInt32((int)coefficientBitCount);
            }
            if (extra_a)
            {
                a_n[^1] = cistronReader.ReadInt32((int)Math.Min(coefficientBitCount, cistronReader.RemainingLength));
                if (extra_b)
                {
                    b_n[^1] = cistronReader.ReadInt32((int)Math.Min(coefficientBitCount, cistronReader.RemainingLength));
                }
            }

            Assert(cistronReader.RemainingLength < 2);
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
        object ICistronInterpreter.Interpret(BitReader cistronReader) => Interpret(cistronReader)!;
        DimensionfulDiscreteFunction ICistronInterpreter<DimensionfulDiscreteFunction>.Interpret(BitReader cistronReader)
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
