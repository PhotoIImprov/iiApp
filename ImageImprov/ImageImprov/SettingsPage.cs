using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace ImageImprov {
    public class SettingsPage : ContentView {
        //KeyPageNavigator defaultNavigationButtons;

        Grid settingsGrid;

        // flips between remaining logged in and out
        //CheckBox maintainLoginCheckbox;
        Switch maintainLoginCheckbox;
        Label maintainLoginLabel;
        //CheckBox aspectOrFillCheckbox;
        StackLayout maintainLogin;

        Label versionLabel;

        public SettingsPage() {
            buildUI();
            Content = settingsGrid;
        }

        protected void buildUI() {
            if (GlobalSingletonHelpers.isEmailAddress(GlobalStatusSingleton.username)) {
                //maintainLoginCheckbox = new CheckBox { Text = "Stay logged in on restart", IsChecked = GlobalStatusSingleton.maintainLogin, };
                maintainLoginCheckbox = new Switch() { IsToggled = GlobalStatusSingleton.maintainLogin, };
                maintainLoginCheckbox.Toggled += (sender, args) =>
                {
                    OnCheckBoxTapped(sender, new EventArgs());
                };
                maintainLoginLabel = new Label() { Text = "Stay logged in on restart", TextColor = Color.Black, VerticalTextAlignment=TextAlignment.Center };
                maintainLogin = new StackLayout() {
                    Orientation = StackOrientation.Horizontal,
                    Spacing = 3,
                };
                maintainLogin.Children.Add(maintainLoginCheckbox);
                maintainLogin.Children.Add(maintainLoginLabel);
            }
            /*
            aspectOrFillCheckbox = new CheckBox {
                Text = "Constrain img to original aspect ratio",
                IsChecked = GlobalSingletonHelpers.AspectSettingToBool(GlobalStatusSingleton.aspectOrFillImgs),
            };
            aspectOrFillCheckbox.CheckChanged += (sender, args) =>
            {
                OnCheckBoxTapped(sender, new EventArgs());
            };
            */
            //defaultNavigationButtons = new KeyPageNavigator(GlobalSingletonHelpers.getUploadingCategoryDesc()) { ColumnSpacing = 1, RowSpacing = 1 };
            if (GlobalStatusSingleton.version != null) {
                versionLabel = new Label { Text = GlobalStatusSingleton.version, TextColor = Color.Black, };
            }

            Grid someGrid = new Grid { ColumnSpacing = 0, RowSpacing = 0 };
            for (int i = 0; i < 8; i++) {
                someGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            }
            for (int i = 0; i < 6; i++) {
                someGrid.ColumnDefinitions.Add(new ColumnDefinition{ Width = new GridLength(1, GridUnitType.Star) });
            }
            if (maintainLogin  != null) {
                someGrid.Children.Add(maintainLogin, 1, 3);
                Grid.SetColumnSpan(maintainLogin, 4);
            }
            someGrid.Children.Add(versionLabel, 1, 7);
            Grid.SetColumnSpan(versionLabel, 4);
            //settingsGrid.Children.Add(aspectOrFillCheckbox, 0, 6);
            //settingsGrid.Children.Add(defaultNavigationButtons, 0, 9);  // object, col, row
            settingsGrid = someGrid;
        }

        void OnCheckBoxTapped(object sender, EventArgs args) {
            if (sender == maintainLoginCheckbox) {
                GlobalStatusSingleton.maintainLogin = ((Switch)sender).IsToggled;
                App myApp = ((App)Application.Current);
                myApp.Properties["maintainLogin"] = GlobalStatusSingleton.maintainLogin.ToString();
                myApp.SavePropertiesAsync();
            }
            //else if (sender == aspectOrFillCheckbox) {
              //  GlobalStatusSingleton.aspectOrFillImgs = GlobalSingletonHelpers.BoolToAspectSetting(((CheckBox)sender).IsChecked);
            //}
        }

    }
}
