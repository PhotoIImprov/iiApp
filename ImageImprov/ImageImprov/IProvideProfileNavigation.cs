using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageImprov {
    public interface IProvideProfileNavigation {
        void gotoSubmissionsPage();
        void gotoLikesPage();
        void gotoMedalsPage();
        void flipShowProfile();

        void gotoSettingsPage();
        void gotoInstructionsPage();
    }
}
