using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Reflection;

using SkiaSharp;

namespace ImageImprov {
    /// <summary>
    /// Parent designed to provide a commonality between title row, image row, and spacer(blank) rows in the submissions page.
    /// </summary>
    public class SubmissionsRow : INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(object sender, string propertyName) {
            if (this.PropertyChanged != null) {
                PropertyChanged(sender, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class SubmissionsTitleRow : SubmissionsRow {
        public string title { get; set; }
        public long categoryId { get; set; }
    }

    public class SubmissionsImageRow : SubmissionsRow {
        /// <summary>
        /// Need to know with whom I am associated, so added images can join
        /// the correct place.
        /// </summary>
        public long categoryId { get; set; }

        private SKBitmap _bitmap0;
        public SKBitmap bitmap0 {
            get { return _bitmap0; }
            set {
                _bitmap0 = value;
                OnPropertyChanged(this, "bitmap0");
            }
        }

        private SKBitmap _bitmap1;
        public SKBitmap bitmap1 {
            get { return _bitmap1; }
            set {
                _bitmap1 = value;
                OnPropertyChanged(this, "bitmap1");
            }
        }

        private SKBitmap _bitmap2;
        public SKBitmap bitmap2 {
            get { return _bitmap2; }
            set {
                _bitmap2 = value;
                OnPropertyChanged(this, "bitmap2");
            }
        }

        private PhotoMetaJSON _bmp0Meta;
        public PhotoMetaJSON bmp0Meta {
            get { return _bmp0Meta; }
            set {
                _bmp0Meta = value;
                OnPropertyChanged(this, "bmp0Meta");
            }
        }

        private PhotoMetaJSON _bmp1Meta;
        public PhotoMetaJSON bmp1Meta {
            get { return _bmp1Meta; }
            set {
                _bmp1Meta = value;
                OnPropertyChanged(this, "bmp1Meta");
            }
        }

        private PhotoMetaJSON _bmp2Meta;
        public PhotoMetaJSON bmp2Meta {
            get { return _bmp2Meta; }
            set {
                _bmp2Meta = value;
                OnPropertyChanged(this, "bmp2Meta");
            }
        }

        public int openSlots() {
            int result = 0;
            if (_bitmap0 == null) {
                result = 3;
            } else if (_bitmap1 == null) {
                result = 2;
            } else if (_bitmap2 == null) {
                result = 1;
            }
            return result;
        }
    }

    public class SubmissionsBlankRow : SubmissionsRow { }
}
