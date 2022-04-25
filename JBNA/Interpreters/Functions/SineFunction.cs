namespace JBNA.Interpreters.Functions;


internal class SineFunction : ICistronInterpreter<DimensionfulFunction>, ICistronInterpreter<DimensionfulContinuousFunction>, ICistronInterpreter<IDimensionfulContinuousFunction>, ICistronInterpreter<IDimensionfulDiscreteFunction>, ICistronInterpreter<IIntegratedDimensionfulDiscreteFunction>, ICistronInterpreter<IIntegratedDimensionfulContinuousFunction>
{
    // wellll what if there's a bitCount per number, but we only read 2 or 3. what about the remainder of the bits then? :S
    // maybe just do a simple minCount, and divide the bits up over the 3 numbers equally?
    private const int numberOfNumbers = 3;
    private const int minBitsPerNumber = 3;
    private const int maxBitsPerNumber = 12;
    public ulong MinBitCount => numberOfNumbers * minBitsPerNumber;
    public ulong MaxBitCount => numberOfNumbers * maxBitsPerNumber;

    private IntegratedDimensionfulDiscreteFunction Interpret(BitReader cistronReader)
    {
        if (cistronReader.RemainingLength < minBitsPerNumber || cistronReader.RemainingLength > maxBitsPerNumber)
            throw new ArgumentException();

        int bitsPerNumber = (int)cistronReader.RemainingLength / numberOfNumbers;
        var amplitude = cistronReader.ReadSingle(bitsPerNumber);
        var wavelength = cistronReader.ReadSingle(bitsPerNumber);
        var phase = cistronReader.ReadSingle(bitsPerNumber);

        return new IntegratedDimensionfulDiscreteFunction(amplitude, wavelength, phase);
    }
    record IntegratedDimensionfulDiscreteFunction(float amplitude, float wavelength, float phase) : IIntegratedDimensionfulDiscreteFunction, IIntegratedDimensionfulContinuousFunction
    {
        // TODO: should NormalizationConstant depend on the range of the input dimension (i.e. the domain)?
        public float NormalizationConstant => throw new NotImplementedException();

        public float Invoke(OneDimensionalDiscreteQuantity arg)
        {
            return amplitude * (float)Math.Sin(wavelength * arg.Value / arg.Length + phase);
        }
        public float Invoke(OneDimensionalContinuousQuantity arg)
        {
            return amplitude * (float)Math.Sin(wavelength * arg.Value / arg.Length + phase);
        }
    }
    DimensionfulFunction ICistronInterpreter<DimensionfulFunction>.Interpret(BitReader cistronReader) => Interpret(cistronReader).Invoke;
    IDimensionfulDiscreteFunction ICistronInterpreter<IDimensionfulDiscreteFunction>.Interpret(BitReader cistronReader) => Interpret(cistronReader);
    IIntegratedDimensionfulDiscreteFunction ICistronInterpreter<IIntegratedDimensionfulDiscreteFunction>.Interpret(BitReader cistronReader) => Interpret(cistronReader);
    DimensionfulContinuousFunction ICistronInterpreter<DimensionfulContinuousFunction>.Interpret(BitReader cistronReader) => Interpret(cistronReader).Invoke;
    IDimensionfulContinuousFunction ICistronInterpreter<IDimensionfulContinuousFunction>.Interpret(BitReader cistronReader) => Interpret(cistronReader);
    IIntegratedDimensionfulContinuousFunction ICistronInterpreter<IIntegratedDimensionfulContinuousFunction>.Interpret(BitReader cistronReader) => Interpret(cistronReader);
}
