using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageImprov {
    public class MemoryInfo {
        public long FreeMemory { get; set; }
        public long MaxMemory { get; set; }
        public long TotalMemory { get; set; }

        public long UsedMemory {
            get {
                return (TotalMemory - FreeMemory);
            }
        }

        public double HeapUsage() {
            return (double)(UsedMemory) / (double)TotalMemory;
        }

        public double Usage() {
            return (double)(UsedMemory) / (double)MaxMemory;
        }

        public override string ToString() {
            return "Used:" + UsedMemory + "; Heap:" + HeapUsage() + "; Usage:" + Usage();
        }
    }
}
