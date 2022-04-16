namespace JBNA;

internal class Evolution<P> where P : IHomologousSet<P>
{
    private readonly Nature nature;
    private readonly Func<Nature, Genome<P>> drawGenome;
    private readonly Func<object?[], float> scoreFunction;
    private readonly Random random;
    private readonly int maxAttemptsUntilDrawGenomeIsInviable;
    private readonly int maxAttemptsUntilReproducingIsInviable;
    private readonly float fractionOfPopulationToBeReplacedByReproduction;
    private Genome<P>[] population;

    // some stats
    public int CumulativeSucceededReproductionCount { get; private set; }
    public int CumulativeFailedReproductionCount { get; private set; }
    public int NumberOfTimesThereWereFailedReproductions { get; private set; }


    /// <param name="scoreFunction">There may be extra objects appended at the end of the first argument (default cistron values). </param>
    public Evolution(
        Nature nature,
        Func<Nature, Genome<P>> drawGenome,
        Func<object?[], float> scoreFunction,
        int populationSize,
        int maxAttemptsUntilDrawGenomeIsInviable = 100,
        int maxAttemptsUntilReproducingIsInviable = 20,
        float fractionOfPopulationToBeReplacedByReproduction = 0.6f)
    {
        this.nature = nature;
        this.random = nature.Random;
        this.drawGenome = drawGenome;
        this.scoreFunction = scoreFunction;
        this.population = new Genome<P>[populationSize];
        this.maxAttemptsUntilDrawGenomeIsInviable = maxAttemptsUntilDrawGenomeIsInviable;
        this.maxAttemptsUntilReproducingIsInviable = maxAttemptsUntilReproducingIsInviable;
        this.fractionOfPopulationToBeReplacedByReproduction = fractionOfPopulationToBeReplacedByReproduction;
    }

    public float[] Evolve(int maxTime, CancellationToken cancellationToken = default)
    {
        int t = 0;
        float[] bestScores = new float[maxTime];
        try
        {
            Populate(0);
            for (; t < maxTime; t++)
            {
                Console.Write($"t={t}\t");
                var scores = Score(); // has side-effects
                bestScores[t] = scores[0];
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
        return bestScores;
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
            var individual = this.drawGenome(this.nature);
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
        throw new Exception("Max attempts of drawing genomes exceeded");
    }
    private float[] Score()
    {
        ulong maxChromosomeLength = 0;
        var scores = new float[this.population.Length];
        for (int i = 0; i < scores.Length; i++)
        {
            // minus sign is to get best performing at the beginning (for simplicity) by sorting
            scores[i] = -this.scoreFunction(this.population[i].Interpret());
            maxChromosomeLength = Math.Max(maxChromosomeLength, this.population[i].Chromosomes.Max(c => c.Length));
        }
        Array.Sort(scores, this.population);

        // undo minus sign
        for (int i = 0; i < scores.Length; i++)
            scores[i] *= -1;
        Console.WriteLine($"top={scores[0]:0}\tμ={scores.Average():0}\tσ={scores.StandardDeviation():0}\tmax_chr_len={maxChromosomeLength}");
        return scores;
    }
    private void Reproduce()
    {
        int reproductionCount = (int)(this.population.Length * this.fractionOfPopulationToBeReplacedByReproduction);

        var additionalPopulation = Enumerable.Range(0, reproductionCount)
                                             .Select(_ => DrawReproductionPair())
                                             .Select(pair => Reproduce(population[pair.Item1], population[pair.Item2], this.maxAttemptsUntilReproducingIsInviable))
                                             .Where(offspring => offspring != null)
                                             .ToArray();

        int failedCount = reproductionCount - additionalPopulation.Length;
        if (failedCount != 0)
        {
            Console.WriteLine($"{failedCount} out of {reproductionCount} reproduction attempts failed!");
        }
        additionalPopulation.CopyTo(this.population, this.population.Length - additionalPopulation.Length);
        
        // update stats
        this.CumulativeSucceededReproductionCount += additionalPopulation.Length;
        this.CumulativeFailedReproductionCount += failedCount;
        this.NumberOfTimesThereWereFailedReproductions += (failedCount == 0 ? 0 : 1);
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
        // A number of random members must be added to each round, until it's not necessary anymore. Am I doing that?
        // }
    }
    private Genome<P>? Reproduce(Genome<P> a, Genome<P> b, int maxRetries)
    {
        while (maxRetries > 0)
        {
            try
            {
                var descendant = a.Reproduce(b, random);
                descendant.Interpret();
                return descendant;
            }
            catch (GenomeInviableException)
            {
            }
            maxRetries--;
        }
        return null;
    }
}
