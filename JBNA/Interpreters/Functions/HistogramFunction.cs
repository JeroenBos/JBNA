
namespace JBNA;

/// <summary>
/// Represents a function that has a number of values (stepwise).
/// This can be repeated or streched over the target dimension.
/// </summary>
internal class HistogramFunction : ICistronInterpreter<DimensionfulFunction>, ICistronInterpreter<IDimensionfulDiscreteFunction>, ICistronInterpreter<IIntegratedDimensionfulDiscreteFunction>
{
    public static HistogramFunction ScalingFunctionInterpreter { get; } = Create(ConstantInterpreter.Create(false));
    public static HistogramFunction RepeatingFunctionInterpreter { get; } = Create(ConstantInterpreter.Create(true));
    public static HistogramFunction Create(bool repeats)
    {
        return repeats switch
        {
            false => ScalingFunctionInterpreter,
            true => RepeatingFunctionInterpreter,
        };
    }
    public static HistogramFunction Create(ICistronInterpreter<bool>? repeatsInterpreter = null)
    {
        return new HistogramFunction(repeatsInterpreter ?? BooleanInterpreter.Instance);
    }
    /// <summary>
    /// The minimum number of bits of the first number.
    /// </summary>
    const int minValueBitCount = 2;
    /// <summary>
    /// The number of bits that the first number has, which represents the number of bits per value.
    /// </summary>
    const int valueBitCountBitCount = 3;

    const int minNumberOfBitsForFirstValue = 4;

    private readonly ICistronInterpreter<bool> repeatsInterpreter;
    public ulong MinBitCount => repeatsInterpreter.MinBitCount + minValueBitCount + valueBitCountBitCount + minNumberOfBitsForFirstValue;
    public ulong MaxBitCount => int.MaxValue;

    private HistogramFunction(ICistronInterpreter<bool> repeatsInterpreter)
    {
        this.repeatsInterpreter = repeatsInterpreter;
    }

    public IIntegratedDimensionfulDiscreteFunction Interpret(BitReader cistronReader)
    {
        ulong originalindex = cistronReader.Position;
        bool repeats = this.repeatsInterpreter.Interpret(cistronReader);
        ulong originalindex1 = cistronReader.Position;
        int valueBitCount = (int)(minValueBitCount + cistronReader.ReadUInt32(valueBitCountBitCount)); // in minValueBitCount + [0, 15]
        ulong originalindex2 = cistronReader.Position;

        int valueCount = checked((int)(cistronReader.RemainingLength / (ulong)valueBitCount));
        float[] values;
        if (valueCount == 0)
        {
            Assert(cistronReader.RemainingLength >= minNumberOfBitsForFirstValue);
            values = new float[] { cistronReader.ReadSingle((int)cistronReader.RemainingLength) };
        }
        else
        {
            values = new float[valueCount];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = cistronReader.ReadSingle(valueBitCount);
            }
        }
        return new Values(values, repeats);
    }

    private sealed class Values : IIntegratedDimensionfulDiscreteFunction
    {
        private readonly float[] values;
        private readonly bool repeats;
        private int P => values.Length;
        public Values(float[] values, bool repeats)
        {
            this.values = values;
            this.repeats = repeats;
        }

        public float NormalizationConstant => 1 / values.Sum();

        public float Invoke(OneDimensionalDiscreteQuantity arg)
        {
            /// this implementation is very much similar to that of <see cref="Pattern1DInterpreter.Combine"/>

            var (L_value, L, L_offset) = arg;

            int P_offset = 0;
            int P_value;

            if (repeats)
            {
                P_value = (L_value - L_offset) % P + P_offset;
            }
            else
            {
                // no rounding, to (semi) ensure that the P_value is exclusive. Only when L_value is equal to L does this go wrong? Hence the check afterwards
                P_value = (int)((double)((L_value - L_offset) * P) / L) + P_offset;
                if (P_value >= values.Length)
                    P_value = values.Length - 1;
            }

            return values[P_value];
        }
    }

    IDimensionfulDiscreteFunction ICistronInterpreter<IDimensionfulDiscreteFunction>.Interpret(BitReader cistronReader) => this.Interpret(cistronReader);
    [DebuggerHidden] DimensionfulFunction ICistronInterpreter<DimensionfulFunction>.Interpret(BitReader cistronReader) => this.Interpret(cistronReader).Invoke;
    object ICistronInterpreter.Interpret(BitReader cistronReader) => Interpret(cistronReader);
}
