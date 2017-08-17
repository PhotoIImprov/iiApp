using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

using Xamarin.Forms;

namespace ImageImprov {
    class SubmissionsDataTemplateSelector : DataTemplateSelector {
        private readonly DataTemplate submissionsTitleDataTemplate;
        private readonly DataTemplate submissionsImageRowDataTemplate;
        private readonly DataTemplate submissionsBlankRowDataTemplate;

        public SubmissionsDataTemplateSelector() {
            this.submissionsTitleDataTemplate = new DataTemplate(typeof(SubmissionsTitleViewCell));
            this.submissionsImageRowDataTemplate = new DataTemplate(typeof(SubmissionsImageRowViewCell));
            this.submissionsBlankRowDataTemplate = new DataTemplate(typeof(SubmissionsBlankRowViewCell));
        }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container) {
            DataTemplate result = null;
            if (item is SubmissionsImageRow) {
                result = this.submissionsImageRowDataTemplate;
            } else if (item is SubmissionsTitleRow) {
                result = this.submissionsTitleDataTemplate;
            } else if (item is SubmissionsTitleRow) {
                result = this.submissionsBlankRowDataTemplate;
            }
            return result;
        }

    }
}
