using JBSnorro;
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
        public int MinBitCount { get; } = 8;
        public int MaxBitCount { get; } = int.MaxValue;
        public int MaxByteCount => MaxBitCount;


        private readonly float domainMin;
        private readonly float domainMax;
        public FourierFunctionCistronInterpreter(float domainMin, float domainMax)
        {
            this.domainMin = domainMin;
            this.domainMax = domainMax;
        }

        public T Interpret(ReadOnlySpan<byte> cistron)
        {
            Assert(cistron.Length > 0);

            float twoPiOverP = 2 * (float)Math.PI / (this.domainMax - this.domainMin);
            byte a_0 = cistron[0];
            bool ab_odd = cistron.Length % 2 == 0;
            int N = (cistron.Length - 1) / 2;
            byte[] a_n = new byte[N + (ab_odd ? 1 : 0)];
            byte[] b_n = new byte[N];
            Assert(a_n.Length + b_n.Length + 1 == cistron.Length);
            for (int i = 1; i < N; i++)
            {
                a_n[i] = cistron[2 * i - 1];
                b_n[i] = cistron[2 * i];
            }
            if (ab_odd)
                a_n[^1] = cistron[^1];


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



        object ICistronInterpreter.Interpret(ReadOnlySpan<byte> cistron) => Interpret(cistron);
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
