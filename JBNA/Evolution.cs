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
    private readonly RandomNewMembersTracker newMembersTracker;
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
        this.newMembersTracker = new RandomNewMembersTracker(this.population.Length, random);
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
                bestScores[t] = scores.First();
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
            scores[i] = this.scoreFunction(this.population[i].Interpret());
            maxChromosomeLength = Math.Max(maxChromosomeLength, this.population[i].Chromosomes.Max(c => c.Length));
        }

        newMembersTracker.InformAboutScoresBeforeSort(scores);
        Array.Sort(scores, this.population, InterfaceWraps.GetReversedComparer<float>());
        newMembersTracker.InformAboutScoresAfterSort(scores);

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

        int randomizedPopulationCount = drawNewRandomPopulation();
        additionalPopulation.CopyTo(this.population, this.population.Length - additionalPopulation.Length - randomizedPopulationCount);

        // update stats
        this.CumulativeSucceededReproductionCount += additionalPopulation.Length;
        this.CumulativeFailedReproductionCount += failedCount;
        this.NumberOfTimesThereWereFailedReproductions += (failedCount == 0 ? 0 : 1);
    }
    private int drawNewRandomPopulation()
    {
        int count = this.newMembersTracker.GetNumberOfMembersToIntroduce();
        Populate(this.population.Length - count);
        return count;
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
        }
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

/// <summary>
/// Tracks how the randomly initialized members are doing, and determines how many of them should be inserted in the next round.
/// </summary>
sealed class RandomNewMembersTracker
{
    private const float maxIntroductionRatio = 0.10f;
    private const int fadeOutNumberOfSteps = 20;
    private readonly Random random;
    private readonly int populationSize;
    /// <summary>
    /// The number of successive time steps the randomly initialized members performed worst.
    /// </summary>
    private int successiveLastPlaceStepCount = 0;
    private const int noMemberIntroducedLastStep = -2;
    private int numberOfMembersIntroducedLastStep = noMemberIntroducedLastStep;
    private float bestScoreByNewRandomMember = float.NaN;
    private int timestepLastIntroducedMembers;
    private bool printedMessage;

    public RandomNewMembersTracker(int populationSize, Random random)
    {
        Requires(populationSize > 1);
        Requires(random != null);

        this.populationSize = populationSize;
        this.random = random;
    }


    public void InformAboutScoresBeforeSort(IReadOnlyList<float> scores)
    {
        Requires(this.numberOfMembersIntroducedLastStep != -1, $"{nameof(GetNumberOfMembersToIntroduce)} must be called first, and must be called before {nameof(InformAboutScoresAfterSort)}");
        if (this.numberOfMembersIntroducedLastStep == noMemberIntroducedLastStep)
        {
            // we're at time=0 now
            this.bestScoreByNewRandomMember = float.NaN;
        }
        else if (numberOfMembersIntroducedLastStep == 0)
        {
            this.bestScoreByNewRandomMember = float.NaN;
        }
        else
        {
            this.bestScoreByNewRandomMember = scores.Take(^numberOfMembersIntroducedLastStep..).Max();
        }
    }
    public void InformAboutScoresAfterSort(IReadOnlyList<float> scores)
    {
        Requires(this.numberOfMembersIntroducedLastStep != -1, $"Must be called after {nameof(InformAboutScoresAfterSort)}, and {nameof(GetNumberOfMembersToIntroduce)} must be called first");

        if (!float.IsNaN(this.bestScoreByNewRandomMember))
        {
            if (scores.ElementAt(^numberOfMembersIntroducedLastStep) == bestScoreByNewRandomMember)
            {
                this.successiveLastPlaceStepCount++;
            }
            else
            {
                this.successiveLastPlaceStepCount = 0;
            }
        }
        this.numberOfMembersIntroducedLastStep = -1;
    }
    public int GetNumberOfMembersToIntroduce()
    {
        int numberOfMembersToIntroduce;
        if (this.successiveLastPlaceStepCount > fadeOutNumberOfSteps)
        {
            if (!printedMessage)
            {
                Console.WriteLine("Stopping introducing randomized popultation members");
                printedMessage = true;
            }
            numberOfMembersToIntroduce = 0;
        }
        else
        {
            float ratioOfPopulationToIntroduce = Math.Max(0, maxIntroductionRatio * (1 - this.successiveLastPlaceStepCount / fadeOutNumberOfSteps));
            numberOfMembersToIntroduce = (int)Math.Ceiling(this.populationSize * ratioOfPopulationToIntroduce);
            this.timestepLastIntroducedMembers++;
        }

        this.numberOfMembersIntroducedLastStep = numberOfMembersToIntroduce;
        return numberOfMembersToIntroduce;
    }
}