using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageImprov {
    /// <summary>
    /// This is the interface that defines all the file service function calls needed.
    /// </summary>
    public interface IFileServices {
        IList<string> getImageImprovFileNames();
        byte[] loadImageBytes(string filename);
        int determineNumImagesTaken();
    }
}
