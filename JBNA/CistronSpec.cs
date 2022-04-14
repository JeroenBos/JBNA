﻿
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
        defaults.Add(new CistronSpec(
            Allele.JunkRatio,
            interpreter: NumberSpec.CreateUniformFloatFactory(0, 4),
            required: false,  // defaults to 0
            meta: true
        ));
        defaults.Add(new CistronSpec(
            Allele.DefaultBitMutationRate,
            interpreter: NumberSpec.CreateUniformFloatFactory(0, 0.05f),
            required: false,  /// defaults to <see cref="DefaultMutationRate"/>
            meta: true
        ));
        defaults.Add(new CistronSpec(
            Allele.DefaultBitMutationRateStdDev,
            interpreter: NumberSpec.CreateUniformFloatFactory(0, 0.05f),
            required: false,  /// defaults to <see cref="DefaultMutationRateStdDev"/>
            meta: true
        ));
        defaults.Add(new CistronSpec(
            Allele.BitInsertionRate,
            interpreter: NumberSpec.CreateUniformFloatFactory(0, 0.05f),
            required : false,  /// defaults to <see cref="DefaultBitInsertionRate"/>
            meta : true
        ));
        defaults.Add(new CistronSpec(
            Allele.BitRemovalRate,
            interpreter: NumberSpec.CreateUniformFloatFactory(0, 0.05f),
            required: false, /// defaults to <see cref="DefaultBitRemovalRate"/>
            meta: true
        ));

        Defaults = defaults;
    }
    // these are the defaults in case the alleles are missing
    internal const float DefaultMutationRate = 0.01f;
    internal const float DefaultMutationRateStdDev = DefaultMutationRate / 4;
    internal const float DefaultBitInsertionRate = 0.001f;
    internal const float DefaultBitInsertionRateStdDev = DefaultBitInsertionRate;
    internal const float DefaultBitRemovalRate = 0.001f;
    internal const float DefaultBitRemovalRateStdDev = DefaultBitRemovalRate;
    internal static readonly ImmutableArray<float> DefaultCrossoverRates = new float[] { 0.3f, 0.5f, 0.2f }.Scan((a, b) => a + b, 0f).ToImmutableArray();
    public static IReadOnlyCollection<CistronSpec> Defaults { get; }

    public CistronSpec(Allele allele, ICistronInterpreter interpreter, bool required = true, bool meta = false, IMultiCistronMerger? merger = null)
    {
        this.Allele = allele;
        this.Interpreter = interpreter ?? throw new ArgumentNullException(nameof(interpreter));
        this.Required = required;
        this.Meta = meta;
        this.Merger = merger;
    }
    public Allele Allele { get; }
    public bool Meta { get; }
    public bool Required { get; }
    public ICistronInterpreter Interpreter { get; }
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
