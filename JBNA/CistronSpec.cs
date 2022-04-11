
using JBSnorro;
using System.Collections.Immutable;

namespace JBNA;

/// <summary>
/// This is the class instances of which consumers are supposed to provide.
/// </summary>
public class CistronSpec
{
    static CistronSpec()
    {
        var defaults = new List<CistronSpec>();
        defaults.Add(new CistronSpec()
        {
            Meta = true,
            Interpreter = NumberSpec.CreateUniformFloatFactory(0, 4),
            Allele = Allele.JunkRatio,
            Required = false, // default to 0
        });
        defaults.Add(new CistronSpec()
        {
            Meta = true,
            Interpreter = NumberSpec.CreateUniformFloatFactory(0, 0.05f),
            Allele = Allele.DefaultMutationRate,
            Required = false,  /// defaults to <see cref="DefaultMutationRate"/>
        });
        defaults.Add(new CistronSpec()
        {
            Meta = true,
            Interpreter = NumberSpec.CreateUniformFloatFactory(0, 0.05f),
            Allele = Allele.DefaultMutationRateStdDev,
            Required = false,  /// defaults to <see cref="DefaultMutationRateStdDev"/>
        });
        defaults.Add(new CistronSpec()
        {
            Meta = true,
            Interpreter = NumberSpec.CreateUniformFloatFactory(0, 0.05f),
            Allele = Allele.BitInsertionRate,
            Required = false,  /// defaults to <see cref="DefaultBitInsertionRate"/>
        });
        defaults.Add(new CistronSpec()
        {
            Meta = true,
            Interpreter = NumberSpec.CreateUniformFloatFactory(0, 0.05f),
            Allele = Allele.BitRemovalRate,
            Required = false,  /// defaults to <see cref="DefaultBitRemovalRate"/>
        });

        Defaults = defaults;
    }
    // these are the defaults in case the alleles are missing
    internal const float DefaultMutationRate = 0.01f;
    internal const float DefaultMutationRateStdDev = DefaultMutationRate / 4;
    internal const float DefaultBitInsertionRate = 0.001f;
    internal const float DefaultBitRemovalRate = 0.001f;
    internal static readonly ImmutableArray<float> DefaultCrossoverRates = new float[] { 0.3f, 0.5f, 0.2f }.Scan((a, b) => a + b, 0f).ToImmutableArray();
    public static IReadOnlyCollection<CistronSpec> Defaults { get; }


    public Allele Allele { get; init; } = Allele.Custom;
    public bool Meta { get; init; } = false;
    public bool Required { get; init; } = true;
    public ICistronInterpreter Interpreter { get; init; } = default!;
    public IMultiCistronMerger? Merger { get; }

    public override string ToString()
    {
        var type = Interpreter.GetType().FindInterfaces((Type t, object? _) => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ICistronInterpreter<>), null);
        if (type.Length == 0)
            return $"CistronSpec(Allele.{this.Allele})";
        else
        {
            var t = type[0].GetGenericArguments()[0];
            return $"CistronSpec<{t.Name}>(Allele.{this.Allele})";
        }
    }
}
