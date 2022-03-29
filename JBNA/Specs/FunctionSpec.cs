using JBSnorro;
using JBSnorro.Collections;
using System.Linq;
using T = System.Func<float, float>;

namespace JBNA;

internal static class FunctionSpecFactory
{
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
            var  a_n = new int[N + (ab_odd ? 1 : 0)];
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



        object ICistronInterpreter.Interpret(BitArrayReadOnlySegment cistron) => Interpret(cistron);
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
