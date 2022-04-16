namespace JBNA;

/// <summary>
/// A distribution is nothing but a normalized function.
/// </summary>
internal class DistributionInterpreter : ICistronInterpreter<DimensionfulFunction>, ICistronInterpreter<IDimensionfulDiscreteFunction>, ICistronInterpreter<IIntegratedDimensionfulDiscreteFunction>
{
    public ulong MinBitCount => functionInterpreter.MinBitCount;
    public ulong MaxBitCount => functionInterpreter.MaxBitCount;


    private readonly ICistronInterpreter<IIntegratedDimensionfulDiscreteFunction> functionInterpreter;

    public DistributionInterpreter(ICistronInterpreter<IIntegratedDimensionfulDiscreteFunction> functionInterpreter)
    {
        this.functionInterpreter = functionInterpreter;
    }


    public IIntegratedDimensionfulDiscreteFunction Interpret(BitReader cistronReader)
    {
        var unnormalizedFunction = functionInterpreter.Interpret(cistronReader);
        var normalizationConstant = unnormalizedFunction.NormalizationConstant; // computed as performance optimization
        return IIntegratedDimensionfulDiscreteFunction.Create(new DimensionfulFunction(normalizedFunction), 1);

        float normalizedFunction(OneDimensionalDiscreteQuantity arg)
        {
            return normalizationConstant * unnormalizedFunction.Invoke(arg);
        }
    }

    IDimensionfulDiscreteFunction ICistronInterpreter<IDimensionfulDiscreteFunction>.Interpret(BitReader cistronReader) => Interpret(cistronReader);
    DimensionfulFunction ICistronInterpreter<DimensionfulFunction>.Interpret(BitReader cistronReader) => Interpret(cistronReader).Invoke;
}

public interface IIntegratedDimensionfulDiscreteFunction : IDimensionfulDiscreteFunction
{
    float NormalizationConstant { get; }


    public static IIntegratedDimensionfulDiscreteFunction Create(DimensionfulFunction original, float normalizationConstant)
    {
        return new DiscreteIntegrated(original, normalizationConstant);
    }
    private record DiscreteIntegrated(DimensionfulFunction Original, float NormalizationConstant) : IIntegratedDimensionfulDiscreteFunction
    {
        float IDimensionfulDiscreteFunction.Invoke(OneDimensionalDiscreteQuantity arg) => Original.Invoke(arg);
    }
}
public interface IIntegratedDimensionfulContinuousFunction : IDimensionfulContinuousFunction
{
    float NormalizationConstant { get; }


    public static IIntegratedDimensionfulContinuousFunction Create(IDimensionfulContinuousFunction original, float normalizationConstant)
    {
        return new ContinuousIntegrated(original, normalizationConstant);
    }
    private record ContinuousIntegrated(IDimensionfulContinuousFunction Original, float NormalizationConstant) : IIntegratedDimensionfulContinuousFunction
    {
        float IDimensionfulContinuousFunction.Invoke(OneDimensionalContinuousQuantity arg) => Original.Invoke(arg);
    }
}

public static class IntegratedFunctionExtensions
{
    public static ICistronInterpreter<IIntegratedDimensionfulDiscreteFunction> Normalize(this ICistronInterpreter<IIntegratedDimensionfulDiscreteFunction> functionInterpreter)
    {
        return (functionInterpreter as DistributionInterpreter) ?? new DistributionInterpreter(functionInterpreter);
    }
}