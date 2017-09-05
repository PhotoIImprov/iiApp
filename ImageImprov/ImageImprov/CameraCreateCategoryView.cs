using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Newtonsoft.Json;

namespace ImageImprov {
    public class CameraCreateCategoryView : ContentView {
        public const int MAX_CATEGORIES = 5;

        CameraContentPage cameraPage;
        Grid portraitView;
        iiBitmapView backButton;

        Label createEventHeaderLabel = new Label {
            BackgroundColor = GlobalStatusSingleton.SplashBackgroundColor,
            Text = "Create Your Event",
            HorizontalOptions = LayoutOptions.FillAndExpand,
            VerticalOptions = LayoutOptions.FillAndExpand,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            TextColor = Color.White,
            LineBreakMode = LineBreakMode.WordWrap,
            FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
            //FontSize = 40,
            FontAttributes = FontAttributes.Bold,
        };

        Entry newEventName = new Entry {
            Placeholder = "Event Name",
            PlaceholderColor = Color.Gray,
            TextColor = Color.Black,
            FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
            BackgroundColor = Color.White,
            HorizontalTextAlignment = TextAlignment.Center,
            HorizontalOptions = LayoutOptions.FillAndExpand,
            Margin = 1,
        };

        Label categoryLabel = new Label {
            BackgroundColor = GlobalStatusSingleton.backgroundColor,
            Text = "Category:",
            HorizontalOptions = LayoutOptions.FillAndExpand,
            VerticalOptions = LayoutOptions.FillAndExpand,
            HorizontalTextAlignment = TextAlignment.Start,
            //VerticalTextAlignment = TextAlignment.Center,
            TextColor = Color.Black,
            LineBreakMode = LineBreakMode.WordWrap,
            FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
            FontAttributes = FontAttributes.Bold,
        };

        IList<Entry> categoryNames = new List<Entry>();
        iiBitmapView addCategory;

        Label createEvent = new Label {
            BackgroundColor = GlobalStatusSingleton.ButtonColor,
            Text = "Create!",
            HorizontalOptions = LayoutOptions.FillAndExpand,
            VerticalOptions = LayoutOptions.FillAndExpand,
            HorizontalTextAlignment = TextAlignment.Center,
            //VerticalTextAlignment = TextAlignment.Center,
            TextColor = Color.White,
            LineBreakMode = LineBreakMode.WordWrap,
            FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
            FontAttributes = FontAttributes.Bold,
        };

        int numPlayers = 10;
        double uploadDuration = 24.0;
        double votingDuration = 72.0;

        public CameraCreateCategoryView(CameraContentPage parent) {
            cameraPage = parent;

            Assembly assembly = this.GetType().GetTypeInfo().Assembly;
            backButton = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.backbutton.png", assembly)) {
                Margin = 4,
            };
            TapGestureRecognizer tapped = new TapGestureRecognizer();
            tapped.Tapped += OnBackPressed;
            backButton.GestureRecognizers.Add(tapped);

            // make sure categoryNames is seeded with at least one entry box.
            Entry newCategory = new Entry {
                Placeholder = "Category Name, e.g. Red",
                PlaceholderColor = Color.Gray,
                TextColor = Color.Black,
                FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
                BackgroundColor = Color.White,
                HorizontalTextAlignment = TextAlignment.Center,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Margin = 1,
            };
            newCategory.TextChanged += PreventCommasInCategoryNames;

            categoryNames.Add(newCategory);
            addCategory = new iiBitmapView {
                Bitmap = GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.votebox.png", assembly),
                HorizontalOptions = LayoutOptions.End,
                Margin = 3,
            };
            TapGestureRecognizer catTapped = new TapGestureRecognizer();
            catTapped.Tapped += OnAddCategory;
            addCategory.GestureRecognizers.Add(catTapped);

            TapGestureRecognizer createTapped = new TapGestureRecognizer();
            createTapped.Tapped += OnCreateEvent;
            createEvent.GestureRecognizers.Add(createTapped);

            buildUI();
        }

        int buildUI() {
            if (portraitView == null) {
                portraitView = new Grid { ColumnSpacing = 0, RowSpacing = 2, };
                for (int i = 0; i < 32; i++) {
                    portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                }
                portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8, GridUnitType.Star) });
                portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            } else {
                portraitView.Children.Clear();
            }
            portraitView.Children.Add(createEventHeaderLabel, 0, 0);
            Grid.SetColumnSpan(createEventHeaderLabel, 3);
            Grid.SetRowSpan(createEventHeaderLabel, 4);
            portraitView.Children.Add(backButton, 0, 1);
            Grid.SetRowSpan(backButton, 2);

            portraitView.Children.Add(newEventName, 1, 5);
            Grid.SetRowSpan(newEventName, 2);

            portraitView.Children.Add(categoryLabel, 0, 7);
            Grid.SetColumnSpan(categoryLabel, 2);
            Grid.SetRowSpan(categoryLabel, 2);

            int x = 9;
            foreach (Entry e in categoryNames) {
                portraitView.Children.Add(e, 1, x);
                Grid.SetRowSpan(e, 2);
                x+=2;
            }
            x+=2;
            if (categoryNames.Count < MAX_CATEGORIES) {
                portraitView.Children.Add(addCategory, 1, x);
                Grid.SetRowSpan(addCategory, 2);
                x+=2;
            }
            Label playerLabel = new Label { Text = "Max Players: 10", TextColor = Color.Gray };
            portraitView.Children.Add(playerLabel, 1, x);
            Grid.SetRowSpan(playerLabel, 2);
            x += 2;
            Label startTimeLabel = new Label { Text = "Start Time: Now", TextColor = Color.Gray };
            portraitView.Children.Add(startTimeLabel, 1, x);
            Grid.SetRowSpan(startTimeLabel, 2);
            x += 2;
            Label uploadLabel = new Label { Text = "Upload Duration: 24 hrs", TextColor = Color.Gray };
            portraitView.Children.Add(uploadLabel, 1, x);
            Grid.SetRowSpan(uploadLabel, 2);
            x += 2;
            Label votingLabel = new Label { Text = "Voting Duration: 72 hrs", TextColor = Color.Gray };
            portraitView.Children.Add(votingLabel , 1, x);
            Grid.SetRowSpan(votingLabel, 2);
            x += 2;
            if (x < 30) x = 30;
            portraitView.Children.Add(createEvent, 1, x);
            Grid.SetRowSpan(createEvent, 2);
            Content = portraitView;
            return 1;
        }

        public void OnBackPressed(object Sender, EventArgs args) {
            // can only come to CreateCategory from the select view, therefore I know 
            // that's where i want to return.
            cameraPage.switchToSelectView();
        }

        public void PreventCommasInCategoryNames(object Sender, EventArgs args) {
            Entry inEntry = (Entry)Sender;
            if (inEntry.Text.Contains(",")) {
                inEntry.Text = inEntry.Text.Replace(",", "");
            }
        }

        public void OnAddCategory(object Sender, EventArgs args) {
            Entry newCategory = new Entry {
                Placeholder = "Category Name, e.g. Red",
                PlaceholderColor = Color.Gray,
                TextColor = Color.Black,
                FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
                BackgroundColor = Color.White,
                HorizontalTextAlignment = TextAlignment.Center,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Margin = 1,
            };
            newCategory.TextChanged += PreventCommasInCategoryNames;
            categoryNames.Add(newCategory);
            Device.BeginInvokeOnMainThread(() => buildUI());
        }

        public async void OnCreateEvent(object Sender, EventArgs args) {
            await createEvent.FadeTo(0, 175);
            await createEvent.FadeTo(1, 175);
            CreateEventJSON create = new CreateEventJSON {
                categories = buildCategoryNames(),
                eventName = newEventName.Text,
                gamesExcluded = buildGamesExcluded(),
                numPlayers = this.numPlayers,
                startTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm"),
                uploadDuration = (int)this.uploadDuration,
                votingDuration = (int)this.votingDuration,
            };
            string jsonQuery = JsonConvert.SerializeObject(create);
            string result = await GlobalSingletonHelpers.requestFromServerAsync(HttpMethod.Post, "newevent", jsonQuery);
            if (result.Equals("fail")) {
                createEvent.Text = "Tap to retry create";
            } else {
                CreateEventResponseJSON cerj = JsonConvert.DeserializeObject<CreateEventResponseJSON>(result);
                //MasterPage mp = ((MasterPage)Application.Current.MainPage);
                //mp.thePages.judgingPage.fireLoadChallenge();
                //cameraPage.latestPassphrase = cerj.accessKey;
                cameraPage.AddEvent(cerj);
                cameraPage.switchToSelectView();
            }
        }

        private IList<string> buildCategoryNames() {
            IList<string> cats = new List<string>();
            foreach (Entry e in categoryNames) {
                if ((e.Text != null) && (e.Text.Trim() != "")) {
                    cats.Add(e.Text);
                }
            }
            return cats;
        }
        private IList<string> buildGamesExcluded() {
            return new List<string>();
        }

    }
}
