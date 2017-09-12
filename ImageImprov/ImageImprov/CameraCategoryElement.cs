using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ImageImprov {
    /// <summary>
    /// Common class all the elements used to display categories and events in the competition selector use.
    /// </summary>
    public class CameraCategoryElement : INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(object sender, string propertyName) {
            if (this.PropertyChanged != null) {
                PropertyChanged(sender, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class CameraHeaderRowElement : CameraCategoryElement {
        public string title { get; set;  }
    }

    public class CameraEventTitleElement : CameraCategoryElement, IComparable<CameraEventTitleElement> {
        private string _eventName;
        public string eventName {
            get {
                return "  " + _eventName;
            }
            set {
                _eventName = value;
                OnPropertyChanged(this, "eventName");
            }
        }

        private string _accessKey;
        public string accessKey {
            get {
                string result = "Join phrase: " + _accessKey;
                if (!stillUploading) result += "; voting only";
                return result;
            }
            set {
                _accessKey = value;
                OnPropertyChanged(this, "accessKey");
            }
        }

        private bool _stillUploading = true;
        public bool stillUploading {
            get {
                return _stillUploading;
            }
            set {
                _stillUploading = value;
                OnPropertyChanged(this, "accessKey");
            }
        }

        public long eventId { get; set; }

        public int CompareTo(CameraEventTitleElement b) {
            // need both to uniquely id.  actually, access key alone should be sufficient....
            //string comp = eventName + "_" + accessKey;
            //return (comp.CompareTo(b.eventName + "_" + b.accessKey));
            return _accessKey.CompareTo(b._accessKey);
        }
    }

    /// <summary>
    /// Backing object for CameraCategorySelectionCell
    /// </summary>
    public class CameraOpenCategoryElement : CameraCategoryElement, IComparable<CameraOpenCategoryElement> {
        /// <summary>
        /// Be careful, we are using category.descrption as our backing, but not updating properly if that changes!
        /// </summary>
        public CategoryJSON category;
        public string categoryName {
            get {
                return (category != null) ? category.description : " Grr... ";
            }
            set {
                category.description = value;
                OnPropertyChanged(this, "categoryName");
            }
        }

        public CameraOpenCategoryElement(CategoryJSON inCategory) {
            category = inCategory;

            PropertyInfo o = GlobalSingletonHelpers.GetProperty(this, "categoryName");
            o.SetValue(this, category.description);
        }

        public int CompareTo(CameraOpenCategoryElement b) {
            if (this.category.categoryId > b.category.categoryId) return 1;
            else if (this.category.categoryId < b.category.categoryId) return -1;
            else return 0;
        }
    }

    /// <summary>
    /// Backing object for CameraCategorySelectionCell
    /// </summary>
    public class CameraClosedCategoryElement : CameraCategoryElement, IComparable<CameraClosedCategoryElement> {
        /// <summary>
        /// Be careful, we are using category.descrption as our backing, but not updating properly if that changes!
        /// </summary>
        public CategoryJSON category;
        public string categoryName {
            get {
                return (category != null) ? category.description : " Grr... ";
            }
            set {
                category.description = value;
                OnPropertyChanged(this, "categoryName");
            }
        }

        public CameraClosedCategoryElement(CategoryJSON inCategory) {
            category = inCategory;

            PropertyInfo o = GlobalSingletonHelpers.GetProperty(this, "categoryName");
            o.SetValue(this, category.description);
        }

        public int CompareTo(CameraClosedCategoryElement b) {
            if (this.category.categoryId > b.category.categoryId) return 1;
            else if (this.category.categoryId < b.category.categoryId) return -1;
            else return 0;
        }
    }
}
