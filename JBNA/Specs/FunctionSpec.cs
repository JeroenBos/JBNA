using JBSnorro;
using JBSnorro.Collections;
using JBSnorro.Diagnostics;
using System.Linq;
using T = System.Func<float, float>;

namespace JBNA;

internal class FunctionSpecFactory
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
    public Delegate Interpret1DFunction<TResult>(BitArrayReadOnlySegment cistron, Func<short, TResult> selectElement)
    {
        // is this type not just an interpreter<Delegate>?
    }
    /// <summary>
    /// Converts the cistron into a pattern.
    /// </summary>
    /// <param name="cistron">
    /// The cistron is expected to start with an indeterminate few bits describing the 'type of pattern'.
    /// Also configuration, e.g. whether the pattern repeats or is stretched to a particular size, could be at the start of the cistron.
    /// </param>
    /// <param name="selectResult">A function that interprets bits. </param>
    /// <returns>a function representing the pattern</returns>
    public Func<T, int, TResult> Interpret1DPattern<T, TResult>(BitArrayReadOnlySegment cistron, Func<short, TResult> selectElement)
    {
        Contract.Assert<NotImplementedException>(typeof(T) == typeof(int) || typeof(T) == typeof(float));

        var interpreter = new Pattern1DInterpreter<T, TResult>(Nature).Interpret(cistron);

        var function = (Func<int, TResult>)Interpret1DFunction(cistron, selectElement);
        int patternLength;
        bool repeats;
        // dimension is the dimension of the field in which the pattern plays out, it's not the size of the pattern itself (that's encoded in the cistron).
        TResult impl(T val, int dimension)
        {
            if (repeats)
            {
                return function(val % dimension);
            }
            else
            {
                return function((val * dimension) / patternLength);
            }
        }
        // in essence a pattern is just a particular function.
        // unsure about how the type T plays a role, if any. and interpolation? Could be done at the short level, maybe should be done at the TResult level, but that then requires another parameter
    }
    class Pattern1DInterpreter<T, TResult> : ICistronInterpreter<Func<T, int, TResult>>
    {
        public Nature Nature { get; }
        private Func<short, TResult> selectElement;
        public Pattern1DInterpreter(Nature nature, Func<short, TResult> selectElement)
        {
            this.Nature=nature;
            this.selectElement=selectElement;
        }
        public ulong MinBitCount => 1UL + (ulong)Nature.PatternLengthBitCount;
        public ulong MaxBitCount => throw new NotImplementedException();

        public Func<T, int, TResult> Interpret(BitArrayReadOnlySegment cistron)
        {
            var reader = cistron.ToBitReader();
            bool repeats = reader.ReadBit();
            int patternLength = reader.ReadInt32(bitCount: Nature.PatternLengthBitCount);



            if (typeof(T) == typeof(int))
            {
                var function = (Func<int, TResult>)Nature.FunctionFactory.Interpret1DFunction(cistron, this.selectElement);
                return (Func<T, int, TResult>)(object)impl;
                TResult impl(int val, int dimension)
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
            else if (typeof(T) == typeof(float))
            {
                var function = (Func<float, TResult>)Nature.FunctionFactory.Interpret1DFunction(cistron, this.selectElement);
                return (Func<T, int, TResult>)(object)impl;
                TResult impl(float val, int dimension)
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
            else
                throw new NotImplementedException(typeof(T).FullName);


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
