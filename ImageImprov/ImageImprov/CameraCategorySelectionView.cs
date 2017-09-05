using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Newtonsoft.Json;

namespace ImageImprov {
    /// <summary>
    /// The view on the cameracontentPage when I'm selecting category to enter
    /// </summary>
    class CameraCategorySelectionView : ContentView, IManageCategories {
        CameraContentPage cameraPage;

        Grid portraitView;
        //Button firstCategory;
        //IList<CategoryJSON> activeCategories = new List<CategoryJSON>();
        //IDictionary<CategoryJSON, Button> categorys = new Dictionary<CategoryJSON, Button>();

        ObservableCollection<CameraCategoryElement> openCategorys = new ObservableCollection<CameraCategoryElement>();
        ListView openCategorysView;
        //DataTemplate myDataTemplate = new DataTemplate(typeof(CameraCategorySelectionCell));
        DataTemplateSelector dts = new CameraDataTemplateSelector();

        Label categoryCreationButton = new Label {
            BackgroundColor = GlobalStatusSingleton.ButtonColor,
            Text = "Create an event!",
            HorizontalOptions = LayoutOptions.FillAndExpand,
            VerticalOptions = LayoutOptions.FillAndExpand,
            HorizontalTextAlignment = TextAlignment.Center,
            //VerticalTextAlignment = TextAlignment.Center,
            TextColor = Color.White,
            LineBreakMode = LineBreakMode.WordWrap,
            FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
            FontAttributes = FontAttributes.Bold,
        };
        iiBitmapView categoryCreationImageStart;
        iiBitmapView categoryCreationImageEnd;

        Entry joinPassphrase = new Entry {
            Placeholder = "Enter join phrase then tap join to join an event",
            PlaceholderColor = Color.Gray,
            TextColor = Color.Black,
            FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
            BackgroundColor = Color.White,
            HorizontalTextAlignment = TextAlignment.Center,
            HorizontalOptions = LayoutOptions.FillAndExpand,
            Margin = 2,
        };
        Label joinLabel = new Label {
            BackgroundColor = GlobalStatusSingleton.ButtonColor,
            Text = "Join",
            HorizontalOptions = LayoutOptions.FillAndExpand,
            VerticalOptions = LayoutOptions.FillAndExpand,
            HorizontalTextAlignment = TextAlignment.Center,
            //VerticalTextAlignment = TextAlignment.Center,
            TextColor = Color.White,
            LineBreakMode = LineBreakMode.WordWrap,
            FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
            FontAttributes = FontAttributes.Bold,
        };
        iiBitmapView joinImageStart;
        iiBitmapView joinImageEnd;

        // events
        EventHandler LoadEventsRequest;

        public CameraCategorySelectionView(CameraContentPage parent) {
            cameraPage = parent;

            Assembly assembly = this.GetType().GetTypeInfo().Assembly;
            categoryCreationImageStart = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.contests_inactive.png", assembly)) {
                HorizontalOptions = LayoutOptions.Start,
                Margin = 3,
            };
            categoryCreationImageEnd = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.contests_inactive.png", assembly)) {
                HorizontalOptions = LayoutOptions.End,
                Margin = 3,
            };

            TapGestureRecognizer tap = new TapGestureRecognizer();
            tap.Tapped += OnCategoryCreateClicked;
            categoryCreationButton.GestureRecognizers.Add(tap);


            joinImageStart = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.profile_inactive.png", assembly)) {
                HorizontalOptions = LayoutOptions.Start,
                Margin = 3,
            };
            joinImageEnd = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.profile_inactive.png", assembly)) {
                HorizontalOptions = LayoutOptions.End,
                Margin = 3,
            };
            TapGestureRecognizer joinTap = new TapGestureRecognizer();
            joinTap.Tapped += OnJoinClicked;
            joinLabel.GestureRecognizers.Add(joinTap);

            LoadEventsRequest += new EventHandler(OnEventsLoadRequest);
        }

        public int buildUI() {
            if (portraitView == null) {
                portraitView = new Grid { ColumnSpacing = 1, RowSpacing = 1, BackgroundColor = GlobalStatusSingleton.backgroundColor };
                for (int i = 0; i < 16; i++) {
                    portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                }
                portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            } else {
                // flush the old children.
                portraitView.Children.Clear();
                portraitView.IsEnabled = true;
            }
            
            openCategorysView = new ListView {
                HasUnevenRows = true,
                BackgroundColor = GlobalStatusSingleton.backgroundColor,
                //RowHeight = 32,
                ItemsSource = openCategorys,
                ItemTemplate = dts,
                SeparatorVisibility = SeparatorVisibility.None,
                Margin=1,
            };
            openCategorysView.ItemSelected += OnRowTapped;

            portraitView.Children.Add(openCategorysView, 0, 0);
            Grid.SetRowSpan(openCategorysView, 10);
            
            portraitView.Children.Add(joinPassphrase, 0, 11);
            portraitView.Children.Add(joinLabel, 0, 12);
            portraitView.Children.Add(joinImageStart, 0, 12);
            portraitView.Children.Add(joinImageEnd, 0, 12);

            portraitView.Children.Add(categoryCreationButton, 0, 14);
            portraitView.Children.Add(categoryCreationImageStart, 0, 14);
            portraitView.Children.Add(categoryCreationImageEnd, 0, 14);

            Content = portraitView;
            return 1;
        }

        public void OnCategoryLoad() {
            //activeCategories.Clear();
            // can't clear anymore as there are now 2 commands which impact this.
            // yes I can. BUT, it requires only calling event apicall from this function!
            openCategorys.Clear(); 
            lock (openCategorys) {
                CameraHeaderRowElement chre = new CameraHeaderRowElement { title = "Open Categories", };
                openCategorys.Add(chre);
                foreach (CategoryJSON category in GlobalStatusSingleton.uploadingCategories) {
                    CameraOpenCategoryElement coce = new CameraOpenCategoryElement(category);
                    if (!hasCategory(coce)) {
                        openCategorys.Add(coce);
                    }
                }
                chre = new CameraHeaderRowElement { title = "Joined Events", };
                openCategorys.Add(chre);
            }
            //firstCategory.Text = activeCategories[0].description;

            if (LoadEventsRequest != null) {
                LoadEventsRequest(this, new EventArgs());
            }
        }

        // should only be triggered from an CameraCategorySelectionView:OnCategoryLoad call!!!
        public async void OnEventsLoadRequest(object sender, EventArgs args) {
            string jsonQuery = "";
            string result = await GlobalSingletonHelpers.requestFromServerAsync(HttpMethod.Get, "event", jsonQuery);
            if (!result.Equals("fail")) {
                IList<EventJSON> evts = JsonConvert.DeserializeObject<IList<EventJSON>>(result);
                if (evts != null) {
                    lock (openCategorys) {
                        foreach (EventJSON evt in evts) {
                            AddEvent(evt);
                        }
                    }
                }
            } else {
                Debug.WriteLine("DHB:CameraCategorySelectionView:OnEventsLoadRequest event apicall failed!");
                // build a test panel...
                //CameraEventTitleElement cete = new CameraEventTitleElement { eventName = "Dummy Example", accessKey = "mace rule", eventId = 111, };
                //openCategorys.Add(cete);
            }
        }

        public void OnRowTapped(object sender, SelectedItemChangedEventArgs args) {
            if (args.SelectedItem is CameraEventTitleElement) {

            } else if (args.SelectedItem is CameraOpenCategoryElement) {
                CameraContentPage.activeCameraCategory = ((CameraOpenCategoryElement)args.SelectedItem).category;
                cameraPage.startCamera();
            } else if (args.SelectedItem is CameraClosedCategoryElement) {
                CameraContentPage.activeCameraCategory = ((CameraClosedCategoryElement)args.SelectedItem).category;
                cameraPage.startCamera();
            }
            openCategorysView.SelectedItem = null;
        }

        public void OnCategoryCreateClicked(object sender, EventArgs args) {
            cameraPage.switchToCreateCategoryView();
        }

        private bool hasCategory(CameraCategoryElement cce) {
            bool result = false;
            foreach (CameraCategoryElement existingCCE in openCategorys) {
                if ((existingCCE is CameraOpenCategoryElement) && (cce is CameraOpenCategoryElement)) {
                    if (((CameraOpenCategoryElement)existingCCE).CompareTo((CameraOpenCategoryElement)cce) == 0) {
                        result = true;
                        break;
                    }
                } else if ((existingCCE is CameraClosedCategoryElement) && (cce is CameraClosedCategoryElement)) {
                    if (((CameraClosedCategoryElement)existingCCE).CompareTo((CameraClosedCategoryElement)cce) == 0) {
                        result = true;
                        break;
                    }
                } else if ((existingCCE is CameraEventTitleElement) && (cce is CameraEventTitleElement)) {
                    if (((CameraEventTitleElement)existingCCE).CompareTo((CameraEventTitleElement)cce) == 0) {
                        result = true;
                        break;
                    }
                }
            }
            return result;
        }

        public async void OnJoinClicked(object sender, EventArgs args) {
            await joinPassphrase.FadeTo(0, 175);
            await joinLabel.FadeTo(0, 175);
            await joinPassphrase.FadeTo(1, 175);
            await joinLabel.FadeTo(1, 175);
            if ((joinPassphrase.Text!=null)&&(!joinPassphrase.Text.Equals(""))) {
                string query = "joinevent?accesskey=" + joinPassphrase.Text;
                string result = await GlobalSingletonHelpers.requestFromServerAsync(HttpMethod.Post, query, "");
                if (result.Equals("fail")) {

                } else {
                    /*
                    IList<CategoryJSON> newCategories = JsonHelper.DeserializeToList<CategoryJSON>(result);
                    foreach (CategoryJSON newCat in newCategories) {
                        CameraClosedCategoryElement coce = new CameraClosedCategoryElement(newCat);
                        
                        // This does an object memory check rather than an equivalence check.
                        // should be a way to make this work, but brute force it for now.
                        //if (!openCategorys.Contains<CameraOpenCategoryElement>(coce)) {
                        if (!hasCategory(coce)) { 
                            openCategorys.Add(coce);
                        }
                    }
                    */
                    EventJSON newEvent = JsonConvert.DeserializeObject<EventJSON>(result);
                    AddEvent(newEvent);
                }
            }
        }

        public void AddEvent(EventJSON evt) {
            // title row here.
            CameraEventTitleElement cete = new CameraEventTitleElement() { eventName = evt.eventName, accessKey = evt.accessKey, eventId = evt.eventId, };
            openCategorys.Add(cete);
            // now the categories for this event.
            foreach (CategoryJSON category in evt.categories) {
                CameraClosedCategoryElement ccce = new CameraClosedCategoryElement(category);
                if (!hasCategory(ccce)) {
                    openCategorys.Add(ccce);
                }
            }
        }
    }
}
