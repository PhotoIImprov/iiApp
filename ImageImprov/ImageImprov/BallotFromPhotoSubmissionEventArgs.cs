using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageImprov {
    /// <summary>
    /// Created by CameraPage and passed to JudingPage when a photo for a new 
    /// category is successfully submitted.
    /// </summary>
    class BallotFromPhotoSubmissionEventArgs : EventArgs {
        public string ballotString { get; set; }
    }
}
