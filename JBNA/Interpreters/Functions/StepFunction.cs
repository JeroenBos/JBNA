namespace JBNA.Interpreters;


internal class StepFunction : ICistronInterpreter<DimensionfulFunction>, ICistronInterpreter<DimensionfulContinuousFunction>, ICistronInterpreter<IDimensionfulContinuousFunction>, ICistronInterpreter<IDimensionfulDiscreteFunction>, ICistronInterpreter<IIntegratedDimensionfulDiscreteFunction>, ICistronInterpreter<IIntegratedDimensionfulContinuousFunction>
{
    public static StepFunction Singleton { get; } = new StepFunction();
    private const int numberOfNumbers = 2;
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
        var deltaFraction = cistronReader.ReadSingle(bitsPerNumber);

        return new IntegratedDimensionfulDiscreteFunction(amplitude, deltaFraction);
    }
    record IntegratedDimensionfulDiscreteFunction(float amplitude, float delta) : IIntegratedDimensionfulDiscreteFunction, IIntegratedDimensionfulContinuousFunction
    {
        // TODO: should NormalizationConstant depend on the range of the input dimension (i.e. the domain)?
        public float NormalizationConstant => throw new NotImplementedException();

        public float Invoke(OneDimensionalDiscreteQuantity arg)
        {
            return Invoke(arg.ToContinuous());
        }
        public float Invoke(OneDimensionalContinuousQuantity arg)
        {
            // the delta is a fraction of the domain, not an absolute value _in_ the domain
            // I guess is the domain has infinite measure, the it should be an absolute value? probably not practical
            if (arg.Value - arg.Start > arg.Length * delta)
            {
                return amplitude;
            }
            return 0;
        }
    }
    private StepFunction() { }
    DimensionfulFunction ICistronInterpreter<DimensionfulFunction>.Interpret(BitReader cistronReader) => Interpret(cistronReader).Invoke;
    IDimensionfulDiscreteFunction ICistronInterpreter<IDimensionfulDiscreteFunction>.Interpret(BitReader cistronReader) => Interpret(cistronReader);
    IIntegratedDimensionfulDiscreteFunction ICistronInterpreter<IIntegratedDimensionfulDiscreteFunction>.Interpret(BitReader cistronReader) => Interpret(cistronReader);
    DimensionfulContinuousFunction ICistronInterpreter<DimensionfulContinuousFunction>.Interpret(BitReader cistronReader) => Interpret(cistronReader).Invoke;
    IDimensionfulContinuousFunction ICistronInterpreter<IDimensionfulContinuousFunction>.Interpret(BitReader cistronReader) => Interpret(cistronReader);
    IIntegratedDimensionfulContinuousFunction ICistronInterpreter<IIntegratedDimensionfulContinuousFunction>.Interpret(BitReader cistronReader) => Interpret(cistronReader);
}
