using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace ImageImprov {
    class SettingsPage : ContentView {
        KeyPageNavigator defaultNavigationButtons;

        Grid settingsGrid;

        // flips between remaining logged in and out
        CheckBox maintainLoginCheckbox;
        CheckBox aspectOrFillCheckbox;

        public SettingsPage() {
            buildUI();
            Content = settingsGrid;
        }

        protected void buildUI() {
            maintainLoginCheckbox = new CheckBox { Text = "Stay logged in on restart", IsChecked = GlobalStatusSingleton.maintainLogin, };
            maintainLoginCheckbox.CheckChanged += (sender, args) =>
            {
                OnCheckBoxTapped(sender, new EventArgs());
            };
            
            aspectOrFillCheckbox = new CheckBox {
                Text = "Constrain img to original aspect ratio",
                IsChecked = GlobalSingletonHelpers.AspectSettingToBool(GlobalStatusSingleton.aspectOrFillImgs),
            };
            aspectOrFillCheckbox.CheckChanged += (sender, args) =>
            {
                OnCheckBoxTapped(sender, new EventArgs());
            };

            defaultNavigationButtons = new KeyPageNavigator { ColumnSpacing = 1, RowSpacing = 1 };

            settingsGrid = new Grid { ColumnSpacing = 0, RowSpacing = 0 };
            for (int i = 0; i < 10; i++) {
                settingsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            }
            settingsGrid.Children.Add(maintainLoginCheckbox, 0, 4);
            settingsGrid.Children.Add(aspectOrFillCheckbox, 0, 6);
            settingsGrid.Children.Add(defaultNavigationButtons, 0, 9);  // object, col, row

        }

        void OnCheckBoxTapped(object sender, EventArgs args) {
            if (sender == maintainLoginCheckbox) {
                GlobalStatusSingleton.maintainLogin = ((CheckBox)sender).IsChecked;
            } else if (sender == aspectOrFillCheckbox) {
                GlobalStatusSingleton.aspectOrFillImgs = GlobalSingletonHelpers.BoolToAspectSetting(((CheckBox)sender).IsChecked);
            }
        }

    }
}
