namespace JBNA;

/// <summary>
/// Converts a linear byte array to a 1-dimensional function
/// </summary>
class CorrelationSpec : ICistronInterpreter<Func<int, bool[]>>, ICistronInterpreter<Func<int, byte[]>>
{
    internal const byte stopCodon = 0b_01010101;

    // the understanding here is that the cistron does not start with the type of function that it is
    // that is up to the parent cistron or allele to determine (preliminary design decision)
    private ICistronInterpreter<Func<int, float>> get1DProbabilityFunction(byte type)
    {
        throw new NotImplementedException();
    }
    private ICistronInterpreter<Func<int, int, float>> get2DProbabilityFunction(byte type)
    {
        throw new NotImplementedException();
    }
    private ICistronInterpreter<Func<int, int, int, float>> get3DProbabilityFunction(byte type)
    {
        throw new NotImplementedException();
    }
    public int MinBitCount => 8;
    public int MaxBitCount => int.MaxValue;
    public int MaxByteCount => ((ICistronInterpreter)this).MaxByteCount;
    Func<int, bool[]> ICistronInterpreter<Func<int, bool[]>>.Interpret(IReadOnlyList<byte> cistron)
    {
        return this.Interpret(cistron).CreateBooleans;
    }
    Func<int, byte[]> ICistronInterpreter<Func<int, byte[]>>.Interpret(IReadOnlyList<byte> cistron)
    {
        return this.Interpret(cistron).CreateBytes;
    }
    private Generator Interpret(IReadOnlyList<byte> cistron)
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

        if (cistron.Count > this.MaxByteCount)
            throw new GenomeInviableException("Too long pattern");
        return Interpret<Generator, Func<int, float>>(cistron.AsArraySegment(), this.get1DProbabilityFunction, Generator.Create);
    }
    private Generator2D Interpret2D(ArraySegment<byte> cistron)
    {
        if (cistron.Count > this.MaxByteCount)
            throw new GenomeInviableException("Too long pattern");
        return Interpret<Generator2D, Func<int, int, float>>(cistron, this.get2DProbabilityFunction, Generator2D.Create);
    }
    private static TGenerator Interpret<TGenerator, FProbability>(
        ArraySegment<byte> cistron,
        Func<byte /*functionType*/, ICistronInterpreter<FProbability>> getProbabilityInterpreter,
        Func<FProbability, ArraySegment<byte>, TGenerator> createGenerator)
    {
        Index rangeEnd = cistron.FindCodon(stopCodon);
        if (rangeEnd.IsFromEnd)
        {
            // we could choose to interpret the range differently now
            throw new GenomeInviableException($"{nameof(cistron)} does not contain a stopCodon");
        }
        if (rangeEnd.GetOffset(cistron.Count) == 0)
        {
            throw new GenomeInviableException($"{nameof(cistron)} starts with a stopCodon");
        }


        var functionType = cistron[0];
        var probabilityFunctionCistron = cistron[1..rangeEnd];
        var pattern = cistron[rangeEnd..];

        var probabilityFunctionInterpreter = getProbabilityInterpreter(functionType);
        var probabilityFunction = probabilityFunctionInterpreter.Interpret(probabilityFunctionCistron);

        return createGenerator(probabilityFunction, pattern);
    }

    private class Generator
    {
        private readonly Func<int, float> probabilityFunction;
        private readonly ArraySegment<byte> pattern;
        private readonly bool repeats;// as opposed to scales
        private byte getPattern(int x, int length)
        {
            if (repeats)
            {
                return pattern[x % pattern.Count];
            }
            else
            {
                return pattern[(x * pattern.Count) / length];
            }
        }

        public Generator(Func<int, float> probabilityFunction, ArraySegment<byte> pattern, bool repeats = false)
        {
            this.pattern = pattern;
            this.probabilityFunction = probabilityFunction;
            this.repeats = repeats;
        }
        public static Generator Create(Func<int, float> probabilityFunction, ArraySegment<byte> pattern) => new(probabilityFunction, pattern);

        private TResult[] Create<TResult>(int length, Func<byte, TResult> selectResult)
        {
            int startIndex = int.MinValue;
            var result = new TResult[length];
            for (int i = 0; i < length; i++)
            {
                if (probabilityFunction(i) >= 0.5)
                {
                    if (startIndex != i - 1)
                    {
                        startIndex = i;
                    }
                    result[i] = selectResult(pattern[i - startIndex]);
                }
                else
                    startIndex = int.MinValue;
            }
            return result;
        }
        public bool[] CreateBooleans(int length)
        {
            // this is not allowed to throw GenomeInviableExceptions
            return Create<bool>(length, b => b > 127);
        }
        public byte[] CreateBytes(int length)
        {
            // this is not allowed to throw GenomeInviableExceptions
            return Create<byte>(length, b => b);
        }
    }
    private class Generator2D
    {
        private readonly Func<int, int, float> probabilityFunction;
        private readonly ArraySegment<byte> encodedPattern;
        private readonly bool repeats; // as opposed to scales

        public Generator2D(
            Func<int, int, /*the dimensions*/float> probabilityFunction,
            ArraySegment<byte> encodedPattern,
            bool repeats = false)
        {
            this.encodedPattern = encodedPattern;
            this.probabilityFunction = probabilityFunction;
            this.repeats = repeats;
        }
        public static Generator2D Create(Func<int, int, /*the dimensions*/float> probabilityFunction, ArraySegment<byte> encodedPattern) => new Generator2D(probabilityFunction, encodedPattern);

        private TResult[,] Create<TResult>(int width, int height, Func<byte, TResult> selectResult)
        {
            var boundingRectangles = Floodfill.DivideMapInAreas(this.isArea, width, height);
            var result = new TResult[width, height];
            foreach (var boundingRectangle in boundingRectangles)
            {
                Assert(!this.repeats); // else notimplemented
                var bytesToResult = new TwoDimensionalScalingFunction<TResult>(this.encodedPattern, boundingRectangle.Width, boundingRectangle.Height, selectResult);
                for (int x = 0; x < boundingRectangle.Width; x++)
                {
                    for (int y = 0; y < boundingRectangle.Height; y++)
                    {
                        Point p = boundingRectangle.TopLeft + new Point(x, y);
                        if (this.isArea(p))
                        {
                            result[p.X, p.Y] = bytesToResult.GetValue(new Point(x, y), boundingRectangle.Width, boundingRectangle.Height);
                        }
                    }
                }
            }

            return result;
        }
        private bool isArea(Point p)
        {
            return probabilityFunction(p.X, p.Y) >= 0.5;
        }
        public bool[,] CreateBooleans(int width, int height)
        {
            // this is not allowed to throw GenomeInviableExceptions
            return Create<bool>(width, height, b => b > 127);
        }
        public byte[,] CreateBytes(int width, int height)
        {
            // this is not allowed to throw GenomeInviableExceptions
            return Create<byte>(width, height, b => b);
        }
    }
}
