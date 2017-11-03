using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace ImageImprov {
    class CameraDataTemplateSelector : DataTemplateSelector {
        private readonly DataTemplate cameraOpenCategoryDataTemplate;
        private readonly DataTemplate cameraHeaderRowDataTemplate;
        private readonly DataTemplate cameraEventTitleDataTemplate;
        private readonly DataTemplate cameraClosedCategoryDataTemplate;
        private readonly DataTemplate cameraSpacerDataTemplate;

        public CameraDataTemplateSelector() {
            this.cameraOpenCategoryDataTemplate = new DataTemplate(typeof(CameraCategorySelectionCell));
            this.cameraHeaderRowDataTemplate = new DataTemplate(typeof(CameraHeaderRowViewCell));
            this.cameraEventTitleDataTemplate = new DataTemplate(typeof(CameraEventTitleViewCell));
            this.cameraClosedCategoryDataTemplate = new DataTemplate(typeof(CameraClosedCategorySelectionCell));
            this.cameraSpacerDataTemplate = new DataTemplate(typeof(CameraSpacerRowViewCell));
        }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container) {
            DataTemplate result = null;
            if (item is CameraOpenCategoryElement) {
                result = this.cameraOpenCategoryDataTemplate;
            } else if (item is CameraEventTitleElement) {
                result = this.cameraEventTitleDataTemplate;
            } else if (item is CameraClosedCategoryElement) {
                result = this.cameraClosedCategoryDataTemplate;
            } else if (item is CameraHeaderRowElement) {
                result = this.cameraHeaderRowDataTemplate;
            } else if (item is CameraSpacerRowElement) {
                result = this.cameraSpacerDataTemplate;
            }
            return result;
        }

    }
}
