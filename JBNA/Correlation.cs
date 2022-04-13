using JBSnorro;
using JBSnorro.Collections;
using JBSnorro.Diagnostics;

namespace JBNA;

/// <summary>
/// Converts a bit sequence to a 1-dimensional function
/// </summary>
class CorrelationSpec : ICistronInterpreter<Func<int, bool[]>>, ICistronInterpreter<Func<int, byte[]>>, ICistronInterpreter<Func<int, short[]>>, ICistronInterpreter<Func<int, Half[]>>
{
    private readonly Nature nature;
    private readonly SubCistronInterpreter subInterpreter;
    public CorrelationSpec(Nature nature)
    {
        var functionSpec = (MinBitCount: 0, MaxBitCount: 1UL);
        var probabilitySpec = (MinBitCount: 0, MaxBitCount: 1UL);

        this.nature = nature;
        this.subInterpreter = new SubCistronInterpreter(nature, new[] { functionSpec, probabilitySpec });
    }

    // the understanding here is that the cistron does not start with the type of function that it is
    // that is up to the parent cistron or allele to determine (preliminary design decision)
    private ICistronInterpreter<Func<int, float>> get1DProbabilityFunction(BitArrayReadOnlySegment subcistron)
    {
        throw new NotImplementedException();
    }
    private ICistronInterpreter<Func<int, int, float>> get2DProbabilityFunction(BitArrayReadOnlySegment subcistron)
    {
        throw new NotImplementedException();
    }
    private ICistronInterpreter<Func<int, int, int, float>> get3DProbabilityFunction(BitArrayReadOnlySegment subcistron)
    {
        throw new NotImplementedException();
    }
    public ulong MinBitCount => 8;
    public ulong MaxBitCount => this.nature.MaxCistronLength;

    Func<int, bool[]> ICistronInterpreter<Func<int, bool[]>>.Interpret(BitArrayReadOnlySegment cistron)
    {
        var generator = (IGenerator1D)this.Interpret(cistron);
        return generator.CreateBooleans;
    }
    Func<int, byte[]> ICistronInterpreter<Func<int, byte[]>>.Interpret(BitArrayReadOnlySegment cistron)
    {
        var generator = (IGenerator1D)this.Interpret(cistron);
        return generator.CreateBytes;
    }
    Func<int, short[]> ICistronInterpreter<Func<int, short[]>>.Interpret(BitArrayReadOnlySegment cistron)
    {
        var generator = (IGenerator1D)this.Interpret(cistron);
        return generator.CreateShorts;
    }
    Func<int, Half[]> ICistronInterpreter<Func<int, Half[]>>.Interpret(BitArrayReadOnlySegment cistron)
    {
        var generator = (IGenerator1D)this.Interpret(cistron);
        return generator.CreateHalfs;
    }
    private Generator Interpret(BitArrayReadOnlySegment cistron)
    {
        // let's say the cistrons represent a list of chunks, each chunk consisting of
        // - an index where the pattern starts
        // - a duration for which the pattern lasts
        // - a probability of the pattern applying at that index
        // - the pattern
        // in fact, we could almost see this as separate cistrons.
        // yeah, let's have the all cooperate into a single "allele"
        // each cistron can represent a pattern and associated odds of them applying
        //
        // next, how exactly is the pattern encoded?
        // given that the output is bool[]? I mean, if we had the output being float[] in the range [0, 1], then it would be trivial
        // We also want some redundancy to improve viability of paths of course, so let's just have it be 
        // represented by bytes, and every byte over 127 is a true, the rest false.
        // the representation (e.g. uniform byte, vs uniform float, or any nonlinear form) must also be encoded
        // so that makes the pattern consist of:
        // - length (or until stopcodon?)
        // - element representation great codon representation, i.e. a many-to-one map)
        // - elements
        // fun fact, then it becomes recursive, as a probability function could in itself be a CorrelationSpec

        // as a sidenote, it maybe make sense for some mechanism to duplicate cistrons then, such that it can duplicate 
        // and both copied can evolve separately, making more paths viable?
        // maybe in the form of general (partial) chromosome duplication? Yes!

        // Another question: how to have the pattern start at multiple indices?
        // the startindices, durations and likelihoods could be represented by a probability function...
        // let's say that the probability function is the likelihood the function continues
        // when the previous index was false, that means it starts. If the pattern ends, then it starts anew.

        // The stopCodon should probably be different from the real cistron-stopping codon

        if (cistron.Length > this.MaxBitCount)
            throw new GenomeInviableException("Too long pattern");

        var subcistrons = this.subInterpreter.Interpret(cistron);
        var probabilityFunctionCistron = subcistrons[0];
        var patternCistron = subcistrons[1];

        return new Generator(this.nature, probabilityFunctionCistron, patternCistron, repeats: false);
    }


    /// <summary>
    /// Has the ability to repeating a pattern (provided in the form of bits) over a region using a probaility function.
    /// If the probability at a certain point is higher than the threshold (currently a constant 0.5), then the pattern start there.
    /// If the probability of the next point is also higher, then the pattern continues there.
    /// Otherwise, the pattern can start at the point again.
    /// </summary>
    private class Generator : IGenerator1D //, IGenerator2D TODO
    {
        public BitArrayReadOnlySegment ProbabilityFunctionCistron { get; }
        public BitArrayReadOnlySegment PatternCistron { get; }
        public bool Repeats { get; }// as opposed to scales
        public Nature Nature { get; }

        // function as caches for the interfaces
        public object? _probabilityFunction { get; set; }
        public object? _pattern { get; set; }

        public Generator(Nature nature, BitArrayReadOnlySegment probabilityFunctionSubCistron, BitArrayReadOnlySegment patternSubcistron, bool repeats)
        {
            this.Nature = nature;
            this.PatternCistron = patternSubcistron;
            this.ProbabilityFunctionCistron = probabilityFunctionSubCistron;
            this.Repeats = repeats;
        }
    }


    interface IGenerator1D
    {
        Nature Nature { get; }
        BitArrayReadOnlySegment ProbabilityFunctionCistron { get; }
        BitArrayReadOnlySegment PatternCistron { get; }
        bool Repeats { get; }

        bool[] CreateBooleans(int length) => Create(length, b => b > 127, bitsPerItem: 8);
        byte[] CreateBytes(int length) => Create(length, s => (byte)s, bitsPerItem: 8);
        short[] CreateShorts(int length) => Create(length, s => (short)s);
        Half[] CreateHalfs(int length) => Create(length, s => checked((short)s).BitsAsHalf());


        /// <summary>A backing field for the probability function.</summary>
        object? _probabilityFunction { get; set; }
        /// <summary>A backing field for the pattern function.</summary>
        object? _pattern { get; set; }

        DimensionfulFunction<bool> ProbabilityFunction
        {
            get
            {
                if (_probabilityFunction == null)
                {
                    _probabilityFunction = this.Nature.FunctionFactory.Interpret1DFunction(this.ProbabilityFunctionCistron).Map(@short => (@short & 255) > 127);
                }
                return (DimensionfulFunction<bool>)_probabilityFunction;
            }
        }

        /// <summary>
        /// Gets the function from value and dimension length to pattern value.
        /// </summary>
        DimensionfulDiscreteFunction getPatternValue
        {
            get
            {
                if (_pattern == null)
                {
                    var  x = this.Nature.FunctionFactory.Interpret1DPattern(this.PatternCistron);
                    _pattern = x;
                }
                return (DimensionfulDiscreteFunction)_pattern;
            }
        }

        TResult[] Create<TResult>(int length, Func<int, TResult> selectResult, int bitsPerItem = 16)
        {
            var reader = this.PatternCistron.ToBitReader();
            Contract.Assert(reader.Length >= (ulong)bitsPerItem * (ulong)length);

            int startIndex = int.MinValue;
            var result = new TResult[length];
            for (int i = 0; i < length; i++)
            {
                if (ProbabilityFunction(new OneDimensionalDiscreteQuantity(i, length)))
                {
                    if (startIndex != i - 1)
                    {
                        startIndex = i;
                    }
                    result[i] = selectResult(getPatternValue(new OneDimensionalDiscreteQuantity(i - startIndex, length)));
                }
                else
                    startIndex = int.MinValue;
            }
            return result;
        }
    }
    //private record Generator1D : GeneratorBase
    //{
    //    private readonly Func<int, float> probabilityFunction;
    //    public Generator1D(BitArrayReadOnlySegment probabilityFunctionCistron, BitArrayReadOnlySegment pattern, bool repeats) : base(probabilityFunctionCistron, pattern, repeats)
    //    {
    //        probabilityFunction = probabilityFunctionCistron.ToFunction();
    //    }

    //    private byte getPattern(int x, int length)
    //    {
    //        if (repeats)
    //        {
    //            return pattern[x % pattern.Length];
    //        }
    //        else
    //        {
    //            return pattern[(x * pattern.Length) / length];
    //        }
    //    }


    //    public bool[] CreateBooleans(int length)
    //    {
    //        // this is not allowed to throw GenomeInviableExceptions
    //        return Create<bool>(length, i => (i & 255) > 127);
    //    }
    //    public byte[] CreateBytes(int length)
    //    {
    //        // this is not allowed to throw GenomeInviableExceptions
    //        return Create<byte>(length, i => (byte)(i & 255)); 
    //    }
    //    public short[] CreateShorts(int length)
    //    {
    //        // this is not allowed to throw GenomeInviableExceptions
    //        return Create<short>(length, i => i);
    //    }
    //    public Half[] CreateHalfs(int length)
    //    {
    //        // this is not allowed to throw GenomeInviableExceptions
    //        return Create<Half>(length, i => i.BitsAsHalf());
    //    }
    //    protected override Array Create<TResult>(int[] lengths, Func<short, TResult> selectResult) => Create(lengths.Single(), selectResult);
    //    public override Array CreateBooleans(int[] lengths) => CreateBooleans(lengths.Single());
    //    public override Array CreateBytes(int[] lengths) => CreateBytes(lengths.Single());
    //    public override Array CreateShorts(int[] lengths) => CreateShorts(lengths.Single());
    //    public override Array CreateHalfs(int[] lengths) => CreateHalfs(lengths.Single());
    //}
    //private class Generator2D
    //{
    //    private readonly Func<int, int, float> probabilityFunction;
    //    private readonly ArraySegment<byte> encodedPattern;
    //    private readonly bool repeats; // as opposed to scales

    //    public Generator2D(
    //        Func<int, int, /*the dimensions*/float> probabilityFunction,
    //        ArraySegment<byte> encodedPattern,
    //        bool repeats = false)
    //    {
    //        this.encodedPattern = encodedPattern;
    //        this.probabilityFunction = probabilityFunction;
    //        this.repeats = repeats;
    //    }
    //    public static Generator2D Create(Func<int, int, /*the dimensions*/float> probabilityFunction, ArraySegment<byte> encodedPattern) => new Generator2D(probabilityFunction, encodedPattern);

    //    private TResult[,] Create<TResult>(int width, int height, Func<byte, TResult> selectResult)
    //    {
    //        var boundingRectangles = Floodfill.DivideMapInAreas(this.isArea, width, height);
    //        var result = new TResult[width, height];
    //        foreach (var boundingRectangle in boundingRectangles)
    //        {
    //            Assert(!this.repeats); // else notimplemented
    //            var bytesToResult = new TwoDimensionalScalingFunction<TResult>(this.encodedPattern, boundingRectangle.Width, boundingRectangle.Height, selectResult);
    //            for (int x = 0; x < boundingRectangle.Width; x++)
    //            {
    //                for (int y = 0; y < boundingRectangle.Height; y++)
    //                {
    //                    Point p = boundingRectangle.TopLeft + new Point(x, y);
    //                    if (this.isArea(p))
    //                    {
    //                        result[p.X, p.Y] = bytesToResult.GetValue(new Point(x, y), boundingRectangle.Width, boundingRectangle.Height);
    //                    }
    //                }
    //            }
    //        }

    //        return result;
    //    }
    //    private bool isArea(Point p)
    //    {
    //        return probabilityFunction(p.X, p.Y) >= 0.5;
    //    }
    //    public bool[,] CreateBooleans(int width, int height)
    //    {
    //        // this is not allowed to throw GenomeInviableExceptions
    //        return Create<bool>(width, height, b => b > 127);
    //    }
    //    public byte[,] CreateBytes(int width, int height)
    //    {
    //        // this is not allowed to throw GenomeInviableExceptions
    //        return Create<byte>(width, height, b => b);
    //    }
    //}
}
