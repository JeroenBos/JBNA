using JBSnorro;
using JBSnorro.Collections;
using JBSnorro.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBNA
{
    /// <summary>
    /// Splits a Cistron up into multiple cistrons, assuming they're separated by Nature.StopCodon. 
    /// These subcistrons don't have start codons (keys) to signify them; they are ordered.
    /// </summary>
    public class SubCistronInterpreter : ICistronInterpreter<List<BitArrayReadOnlySegment>>
    {
        public ulong MinBitCount { get; }
        public ulong MaxBitCount { get; }

        private readonly (int MinBitCount, ulong MaxBitCount)[] _specs;
        private readonly int minCistronCount;
        private readonly Nature nature;

        public SubCistronInterpreter(Nature nature, params (int MinBitCount, ulong MaxBitCount)[] subCistronSpec) 
            : this(nature, subCistronSpec.Length, subCistronSpec)
        {
        }
        public SubCistronInterpreter(Nature nature, int minCistronCount, params (int MinBitCount, ulong MaxBitCount)[] subCistronSpec)
        {
            Contract.Requires(nature != null);
            Contract.Requires(subCistronSpec != null);
            Contract.Requires(0 <= minCistronCount && minCistronCount <= subCistronSpec.Length);
            Contract.Requires(Contract.ForAll(subCistronSpec, spec => spec.MinBitCount >= 0 && (ulong)spec.MinBitCount <= spec.MaxBitCount));

            this.nature = nature;
            this._specs = subCistronSpec.ToArray();
            this.minCistronCount = minCistronCount;
            (this.MinBitCount, this.MaxBitCount) = ComputeMinMaxBitCount();


            (ulong, ulong) ComputeMinMaxBitCount()
            {
                var min = this._specs.Sum(spec => (long)spec.MinBitCount);
                var max = this._specs.Sum(spec => (long)spec.MaxBitCount);
                return ((ulong)min, (ulong)max);
            }
        }

        public List<BitArrayReadOnlySegment> Interpret(BitArrayReadOnlySegment cistron)
        {
            var result = impl(cistron, this.nature).ToList();
            if (result.Count < this.minCistronCount)
                throw new GenomeInviableException($"Not enough subcistrons in cistron ({result.Count} < {this.minCistronCount})");
            return result;


            static IEnumerable<BitArrayReadOnlySegment> impl(BitArrayReadOnlySegment cistron, Nature nature)
            {
                int count = 0;
                ulong startBitIndex = 0;
                while (true)
                {
                    long stopIndex = cistron.IndexOf(nature.SubCistronStopCodon.Value, nature.SubCistronStopCodon.Length, startBitIndex);
                    if (stopIndex == -1)
                    {
                        yield return cistron[new Range((int)startBitIndex, Index.End)];
                        yield break;
                    }

                    long nestStartIndex = stopIndex + nature.SubCistronStopCodon.Length;
                    Contract.Assert<NotImplementedException>(nestStartIndex  <= int.MaxValue);
                    var range = new Range((int)startBitIndex, (int)stopIndex);
                    yield return cistron[range];

                    startBitIndex  = (ulong)nestStartIndex;
                    count++;
                }
            }

        }
    }
}
