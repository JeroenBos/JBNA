using JBSnorro;
using static JBSnorro.Diagnostics.Contract;

namespace JBNA;

internal class Evolution<P> where P : IHomologousSet<P>
{
    private static readonly Random rng = new Random();

    private readonly Nature nature;
    private readonly Func<Nature, Random, Genome<P>> drawGenome;
    private readonly Func<object?[], float> scoreFunction;
    private readonly Random random;
    private readonly int maxAttemptsUntilDrawGenomeIsInviable;

    private Genome<P>[] population;
    /// <param name="scoreFunction">There may be extra objects appended at the end of the first argument (default cistron values). </param>
    public Evolution(
        Nature nature,
        Func<Nature, Random, Genome<P>> drawGenome,
        Func<object?[], float> scoreFunction,
        int populationSize,
        int maxAttemptsUntilDrawGenomeIsInviable = 100,
        Random? random = null)
    {
        this.nature = nature;
        this.random = random ?? new Random(rng.Next());
        this.drawGenome = drawGenome;
        this.scoreFunction = scoreFunction;
        this.population = new Genome<P>[populationSize];
        this.maxAttemptsUntilDrawGenomeIsInviable = maxAttemptsUntilDrawGenomeIsInviable;
    }

    public float[] Evolve(int maxTime, CancellationToken cancellationToken = default)
    {
        int t = 0;
        float[] finalScores = null!;
        try
        {

            Populate(0);
            for (; t < maxTime; t++)
            {
                Console.WriteLine($"t={t}");
                finalScores = Score();
                cancellationToken.ThrowIfCancellationRequested();
                Reproduce();
            }
        }
        catch (OperationCanceledException)
        {
            Save(t, canceled: true);
            throw;
        }
        Save(t);
        return finalScores;
    }

    private void Save(int time, bool canceled = false)
    {
        // throw new NotImplementedException();
    }

    private void Populate(int startIndex)
    {
        for (int i = startIndex; i < population.Length; i++)
        {
            population[i] = this.DrawViableGenome();
        }
    }
    private Genome<P> DrawViableGenome()
    {
        for (int i = 0; i < this.maxAttemptsUntilDrawGenomeIsInviable; i++)
        {
            var individual = this.drawGenome(this.nature, this.random);
            try
            {
                individual.Interpret(); // result is cached anyway, so discarding is no problem
            }
            catch (GenomeInviableException)
            {
                continue;
            }
            return individual;
        }
        throw new Exception("Attempts exceeded");
    }
    private float[] Score()
    {
        var scores = new float[this.population.Length];
        for (int i = 0; i < scores.Length; i++)
        {
            // minus sign is to get best performing at the beginning (for simplicity) by sorting
            scores[i] = -this.scoreFunction(this.population[i].Interpret());
        }
        System.Array.Sort(scores, this.population);

        // undo minus sign
        for (int i = 0; i < scores.Length; i++)
            scores[i] *= -1;
        Console.WriteLine($"μ={scores.Average()}. σ={scores.StandardDeviation()}");
        return scores;
    }
    private void Reproduce()
    {
        int reproductionCount = (this.population.Length * 6) / 10;
        var addionalPopulation = new Genome<P>[reproductionCount];
        for (int i = 0; i < reproductionCount; i++)
        {
            var pair = this.DrawReproductionPair();
            addionalPopulation[i] = Reproduce(population[pair.Item1], population[pair.Item2]);
        }
        addionalPopulation.CopyTo(this.population, this.population.Length - reproductionCount);
    }
    private (int, int) DrawReproductionPair()
    {
        Assert(this.population.Length >= 3);

        // we simply try a linear (slope=-1) distribution of likelyhood of mating
        float u = random.NextSingle();
        int elem1 = toIndex(u, this.population.Length);

        int elem2 = elem1;
        int attempts = 0;
        while (elem2 == elem1)
        {
            u = random.NextSingle();
            elem2 = toIndex(u, this.population.Length);
            attempts++;
        }
        return (elem1, elem2);

        // https://stats.stackexchange.com/a/171631/176526

        static double draw(float u)
        {
            const int alpha = -1;
            return (Math.Sqrt(alpha * alpha - 2 * alpha + 4 * alpha * u + 1) - 1) / alpha;
        }
        static int toIndex(float u, int size)
        {
            var p = (draw(u) + 1) / 2; // shift 1 to the right in domain, so now that's from 0 to 1
            var result = p * size; // scale it by population size
            return (int)result;
        }
    }
    private Genome<P> Reproduce(Genome<P> a, Genome<P> b)
    {
        var descendant = a.Reproduce(b, random);
        // do viability check here or?
        return descendant;
    }

}
