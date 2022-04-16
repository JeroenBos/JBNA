namespace JBNA;
using TResult = System.Single;

/// <summary>
/// Converts a cistron to a pattern repeated at multiple locations.
/// </summary>
class CorrelationSpec : ICistronInterpreter<IDimensionfulDiscreteFunction>
{
    public static CorrelationSpec Create(Nature nature,
                                         ICistronInterpreter<IDimensionfulDiscreteFunction>? patternInterpreter = null,
                                         ICistronInterpreter<IIntegratedDimensionfulDiscreteFunction>? probabilityDistributionInterpreter = null)
    {
        return new CorrelationSpec(
            nature,
            patternInterpreter ?? nature.FunctionFactory.DiscretePatternInterpreter,
            nature.FunctionFactory.GetDiscreteDistributionInterpreter(probabilityDistributionInterpreter)
        );
    }


    private readonly Nature nature;
    private readonly ICistronInterpreter<IDimensionfulDiscreteFunction> subCistronInterpreter;

    private CorrelationSpec(Nature nature,
                            ICistronInterpreter<IDimensionfulDiscreteFunction> patternInterpreter,
                            ICistronInterpreter<IIntegratedDimensionfulDiscreteFunction> probabilityDistributionInterpreter)
    {
        this.nature = nature;
        this.subCistronInterpreter = SubCistronInterpreter.Create(nature, patternInterpreter, probabilityDistributionInterpreter.Normalize(), Combine);

        static IDimensionfulDiscreteFunction Combine(IDimensionfulDiscreteFunction discretePattern, IIntegratedDimensionfulDiscreteFunction probabilityFunction)
        {
            return new Pattern1D(discretePattern, probabilityFunction);
        }
    }
    record Pattern1D : IDimensionfulDiscreteFunction
    {
        private readonly IDimensionfulDiscreteFunction discretePattern;
        private readonly IIntegratedDimensionfulDiscreteFunction probabilityFunction;
        private float[]? values; // serves as a cache, assuming the same function is going to be called with the same length many times

        public Pattern1D(IDimensionfulDiscreteFunction discretePattern, IIntegratedDimensionfulDiscreteFunction probabilityFunction)
        {
            this.discretePattern = discretePattern;
            this.probabilityFunction = probabilityFunction;
        }

        private float[] Create(int dimensionLength)
        {
            int startIndex = int.MinValue;
            var result = new TResult[dimensionLength];
            for (int i = 0; i < dimensionLength; i++)
            {
                float probability = probabilityFunction.Invoke(new OneDimensionalDiscreteQuantity(i, dimensionLength));
                if (probability >= 0.5)
                {
                    if (startIndex != i - 1)
                    {
                        startIndex = i;
                    }
                    result[i] = discretePattern.Invoke(new OneDimensionalDiscreteQuantity(i - startIndex, dimensionLength));
                }
                else
                    startIndex = int.MinValue;
            }
            return result;
        }
        public float Invoke(OneDimensionalDiscreteQuantity arg)
        {
            int dimensionLength = arg.Length - arg.Start;

            if (this.values == null || this.values.Length != dimensionLength)
            {
                // the size of the dimension onto which the pattern is to be mapped
                this.values = Create(dimensionLength);
            }

            return this.values[arg.Value];
        }
    }

    public IDimensionfulDiscreteFunction Interpret(BitReader cistronReader)
    {
        return this.subCistronInterpreter.Interpret(cistronReader);
    }

    public ulong MinBitCount => 8;
    public ulong MaxBitCount => this.nature.MaxCistronLength;

}
