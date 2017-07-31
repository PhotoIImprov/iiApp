using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xamarin.Forms;
using SkiaSharp;

namespace ImageImprov {
    /// <summary>
    /// Backing object for LeaderboardCell.
    /// </summary>
    public class LeaderboardElement : INotifyPropertyChanged {
        public string title { get; set; }

        // Cleverness has run out. Brutus Forcus!
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

        private SKBitmap _bitmap3;
        public SKBitmap bitmap3 {
            get { return _bitmap3; }
            set {
                _bitmap3 = value;
                OnPropertyChanged(this, "bitmap3");
            }
        }

        private SKBitmap _bitmap4;
        public SKBitmap bitmap4 {
            get { return _bitmap4; }
            set {
                _bitmap4 = value;
                OnPropertyChanged(this, "bitmap4");
            }
        }

        private SKBitmap _bitmap5;
        public SKBitmap bitmap5 {
            get { return _bitmap5; }
            set {
                _bitmap5 = value;
                OnPropertyChanged(this, "bitmap5");
            }
        }

        private SKBitmap _bitmap6;
        public SKBitmap bitmap6 {
            get { return _bitmap6; }
            set {
                _bitmap6 = value;
                OnPropertyChanged(this, "bitmap6");
            }
        }

        private SKBitmap _bitmap7;
        public SKBitmap bitmap7 {
            get { return _bitmap7; }
            set {
                _bitmap7 = value;
                OnPropertyChanged(this, "bitmap7");
            }
        }

        private SKBitmap _bitmap8;
        public SKBitmap bitmap8 {
            get { return _bitmap8; }
            set {
                _bitmap8 = value;
                OnPropertyChanged(this, "bitmap8");
            }
        }

        // Not drawn, but needed to uniquely id leaderboards.
        public long categoryId { get; set; }

        public LeaderboardElement(string title, long categoryId, IList<SKBitmap> bitmaps) {
            //public MyRowElement(string title, string msg, Color textColor) {
            this.title = title;
            this.categoryId = categoryId;
            int i = 0;
            int maxIndex = (9 < bitmaps.Count) ? 9 : bitmaps.Count;
            while (i < maxIndex) {
                PropertyInfo o = GlobalSingletonHelpers.GetProperty(this, "bitmap"+i);
                o.SetValue(this, bitmaps[i]);
                i++;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(object sender, string propertyName) {
            if (this.PropertyChanged != null) {
                PropertyChanged(sender, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
}
