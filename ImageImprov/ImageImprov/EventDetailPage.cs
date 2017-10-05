using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace ImageImprov {
    public class EventDetailPage : ContentView {
        IProvideEventDrillDown callingPage;

        private Grid portraitView;
        private iiBitmapView backButton;

        private Label eventName = new Label {
            BackgroundColor = GlobalStatusSingleton.ButtonColor,
            HorizontalOptions = LayoutOptions.FillAndExpand,
            VerticalOptions = LayoutOptions.FillAndExpand,
            HorizontalTextAlignment = TextAlignment.Center,
            //VerticalTextAlignment = TextAlignment.Center,
            TextColor = Color.White,
            LineBreakMode = LineBreakMode.WordWrap,
            FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
            FontAttributes = FontAttributes.Bold,
        };
        
        private Label eventCreator = new Label {
            BackgroundColor = GlobalStatusSingleton.backgroundColor,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.FillAndExpand,
            //HorizontalTextAlignment = TextAlignment.Center,
            //VerticalTextAlignment = TextAlignment.Center,
            TextColor = Color.Black,
            LineBreakMode = LineBreakMode.WordWrap,
            FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
            FontAttributes = FontAttributes.Bold,
        };
        private Label eventStartDate = new Label {
            BackgroundColor = GlobalStatusSingleton.backgroundColor,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.FillAndExpand,
            //HorizontalTextAlignment = TextAlignment.Center,
            //VerticalTextAlignment = TextAlignment.Center,
            TextColor = Color.Black,
            LineBreakMode = LineBreakMode.WordWrap,
            FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
            FontAttributes = FontAttributes.Bold,
        };
        private Label eventMaxPlayers = new Label {
            BackgroundColor = GlobalStatusSingleton.backgroundColor,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.FillAndExpand,
            //HorizontalTextAlignment = TextAlignment.Center,
            //VerticalTextAlignment = TextAlignment.Center,
            TextColor = Color.Black,
            LineBreakMode = LineBreakMode.WordWrap,
            FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
            FontAttributes = FontAttributes.Bold,
        };
        private Label joinPhrase = new Label {
            BackgroundColor = GlobalStatusSingleton.backgroundColor,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.FillAndExpand,
            //HorizontalTextAlignment = TextAlignment.Center,
            //VerticalTextAlignment = TextAlignment.Center,
            TextColor = Color.Black,
            LineBreakMode = LineBreakMode.WordWrap,
            FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
            FontAttributes = FontAttributes.Bold,
        };

        private Label categoryHeader = new Label {
            Text = "Categories:",
            BackgroundColor = GlobalStatusSingleton.backgroundColor,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.FillAndExpand,
            //HorizontalTextAlignment = TextAlignment.Center,
            //VerticalTextAlignment = TextAlignment.Center,
            TextColor = Color.Black,
            LineBreakMode = LineBreakMode.WordWrap,
            FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
            FontAttributes = FontAttributes.Bold,
        };

        ListView eventCategories;
        private ObservableCollection<string> catNames = new ObservableCollection<string>();
        IList<CategoryJSON> categories = new List<CategoryJSON>();

        //DataTemplate myDataTemplate = new DataTemplate(typeof(string));

        public EventDetailPage(IProvideEventDrillDown parent) {
            this.callingPage = parent;
            buildUI();
        }

        public void SetUIData(CameraEventTitleElement cete) {
            EventJSON evt = cete.rawEvent;
            if (evt != null) {
                Device.BeginInvokeOnMainThread(() => {
                    eventName.Text = evt.eventName;
                    eventCreator.Text =       "Created by:  " + evt.createdBy;
                    eventMaxPlayers.Text =    "Max Players: " + evt.maxPlayers;
                    if (evt.categories.Count > 0) {
                        eventStartDate.Text = "Start time:  " +evt.categories[0].start.ToString();
                    }
                    joinPhrase.Text =         "Join Phrase: " + evt.accessKey;

                    // hmm, should use a list view here instead...
                    // get everything else up and running, then alter this.
                    catNames.Clear();
                    foreach (CategoryJSON category in evt.categories) {
                        catNames.Add(category.description);
                        categories.Add(category);
                    }
                });
            }
        }

        private int buildUI() {
            Assembly assembly = this.GetType().GetTypeInfo().Assembly;

            if (portraitView == null) {
                portraitView = new Grid { ColumnSpacing = 1, RowSpacing = 1, BackgroundColor = GlobalStatusSingleton.backgroundColor };
                for (int i = 0; i < 16; i++) {
                    portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                }
                portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                backButton = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.backbutton.png", assembly)) {
                    HorizontalOptions = LayoutOptions.Start,
                    Margin = 4,
                };
                TapGestureRecognizer backTap = new TapGestureRecognizer();
                backTap.Tapped += OnBackPressed;
                backButton.GestureRecognizers.Add(backTap);

                //eventCategories = new ListView { ItemsSource = catNames, ItemTemplate = myDataTemplate, };
                eventCategories = new ListView { ItemsSource = catNames, };
                eventCategories.ItemSelected += OnRowTapped;
            } else {
                // flush the old children.
                portraitView.Children.Clear();
                portraitView.IsEnabled = true;
            }
            portraitView.Children.Add(eventName, 0, 0);
            portraitView.Children.Add(backButton, 0, 0);
            portraitView.Children.Add(eventCreator, 0, 2);
            portraitView.Children.Add(eventMaxPlayers, 0, 3);
            portraitView.Children.Add(eventStartDate, 0, 4);
            portraitView.Children.Add(categoryHeader, 0, 5);
            portraitView.Children.Add(eventCategories, 0, 6);
            Grid.SetRowSpan(eventCategories, 8);
            Content = portraitView;
            return 1;
        }

        public void OnBackPressed(object Sender, EventArgs args) {
            // can only come to CreateCategory from the select view, therefore I know 
            // that's where i want to return.
            // not working on ios for some reason. am i getting here?
            Debug.WriteLine("DHB:EventDetailPage:OnBackPressed");
            callingPage.switchToSelectView();
        }

        public void OnRowTapped(object sender, SelectedItemChangedEventArgs args) {
            if (args.SelectedItem is string) {
                //cameraPage.eventDrillDown((CameraEventTitleElement)args.SelectedItem);
                CategoryJSON theCat = GlobalSingletonHelpers.getCategoryByDescription(categories, (string)args.SelectedItem);
                if (theCat != null) { 
                    callingPage.switchToCategoryImgView(theCat);
                }
            }
            eventCategories.SelectedItem = null;
        }
    }
}
