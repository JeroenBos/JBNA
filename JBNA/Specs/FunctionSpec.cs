using JBSnorro;
using JBSnorro.Collections;
using JBSnorro.Diagnostics;
using System.Linq;
using T = System.Func<float, float>;

namespace JBNA;


public class FunctionSpecFactory
{
    public Nature Nature { get; }
    public FunctionSpecFactory(Nature nature)
    {
        this.Nature = nature ?? throw new ArgumentNullException(nameof(nature));
    }
    /// <summary>
    /// Converts the cistron into a function.
    /// </summary>
    /// <param name="cistron">The cistron is expected to start with an indeterminate few bits describing the 'type of function'.</param>
    /// <param name="selectResult">A function that interprets bits. </param>
    public Func<int, short> Interpret1DFunction(BitArrayReadOnlySegment cistron)
    {
        // the implementation of this should just be reading a few bits
        // + a switch statement over the various implemented function types.
    }
    /// <summary>
    /// Converts the cistron into a pattern function.
    /// </summary>
    /// <param name="cistron">
    /// The cistron is expected to start with a caller-responsible number of bits describing the 'type of pattern'.
    /// Also configuration, e.g. whether the pattern repeats or is stretched to a particular size, could be at the start of the cistron.
    /// </param>
    /// <returns>a function that gets the value of the pattern given the position and length of the one dimension. </returns>
    public Func<int, int, short> Interpret1DPattern(BitArrayReadOnlySegment cistron)
    {
        // in essence a pattern is just a particular function.
        // Q: unsure about how the type T plays a role, if any. and interpolation? Could be done at the short level, maybe should be done at the TResult level, but that then requires another parameter
        // A: we solved that by simply returning the short. Caller is allowed to map and compose as pleased.

        ICistronInterpreter<Func<int, int, short>> interpreter = new Pattern1DInterpreter(Nature);
        var patternFunction = interpreter.Interpret(cistron);
        return patternFunction;
    }
    class Pattern1DInterpreter : ICistronInterpreter<Func<int, int, short>>, ICistronInterpreter<Func<float, float, short>>
    {
        public Nature Nature { get; }
        public Pattern1DInterpreter(Nature nature)
        {
            this.Nature = nature;
        }
        public ulong MinBitCount => 1UL + (ulong)Nature.PatternLengthBitCount;
        public ulong MaxBitCount => Nature.MaxCistronLength;

        Func<int /* position */, int /* dimension*/, short> ICistronInterpreter<Func<int, int, short>>.Interpret(BitArrayReadOnlySegment cistron)
        {
            var reader = cistron.ToBitReader();
            bool repeats = reader.ReadBit();
            int patternLength = reader.ReadInt32(bitCount: Nature.PatternLengthBitCount);

            var function = Nature.FunctionFactory.Interpret1DFunction(cistron);
            return impl;
            short impl(int val, int dimension)
            {
                if (repeats)
                {
                    return function!(val % dimension);
                }
                else
                {
                    return function!((val * dimension) / patternLength);
                }
            }
        }
        Func<float, float, short> ICistronInterpreter<Func<float, float, short>>.Interpret(BitArrayReadOnlySegment cistron)
        {
            throw new NotImplementedException();
        }

        public static ICistronInterpreter<T> CreateFourierFunction(float minDomain = -1, float maxDomain = 1)
        {
            return new FourierFunctionCistronInterpreter(minDomain, maxDomain);
        }

        

        internal class FourierFunctionCistronInterpreter : ICistronInterpreter<T>
        {
            /// <summary>
            /// The number of bits at the beginning of the cistron that encode how many bits encode each subsequent number.
            /// </summary>
            private uint bitsPerNumberBitLength;
            /// <summary>
            /// The minimum number of bits required to encode the first number (after the bitsPerNumber).
            /// </summary>
            const int bitsForFirstNumber = 5;
            public ulong MinBitCount => bitsPerNumberBitLength + bitsForFirstNumber;
            public ulong MaxBitCount { get; } = int.MaxValue;


            private readonly float domainMin;
            private readonly float domainMax;
            public FourierFunctionCistronInterpreter(float domainMin, float domainMax)
            {
                this.domainMin = domainMin;
                this.domainMax = domainMax;
            }

            public T Interpret(BitArrayReadOnlySegment cistron)
            {
                Assert(cistron.Length >= this.MinBitCount);
                var reader = cistron.ToBitReader();

                int nBits = reader.ReadInt32(bitsForFirstNumber); // in [0, 31]
                Assert(reader.CanRead(nBits));


                float twoPiOverP = 2 * (float)Math.PI / (this.domainMax - this.domainMin);
                var (N, rem) = Math.DivRem(checked((int)reader.RemainingLength), 2 * nBits);
                bool ab_odd = rem >= nBits;
                var a_n = new int[N + (ab_odd ? 1 : 0)];
                var b_n = new int[N];

                long a_0 = reader.ReadInt32(nBits);
                for (int i = 1; i < N; i++)
                {
                    a_n[i] = reader.ReadInt32(nBits);
                    b_n[i] = reader.ReadInt32(nBits);
                }
                if (ab_odd)
                    a_n[^1] = reader.ReadInt32(nBits);

                return f;

                float f(float x)
                {
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



            object ICistronInterpreter.Interpret(BitArrayReadOnlySegment cistron) => Interpret(cistron)!;
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
