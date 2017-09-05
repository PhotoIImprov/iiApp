using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageImprov {
    /// <summary>
    /// Category/Event management routines exposed to aid event management.
    /// </summary>
    public interface IManageCategories {
        void AddEvent(EventJSON cerj);
    }
}
