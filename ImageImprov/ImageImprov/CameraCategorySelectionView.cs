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
        Frame categoryFrame = new Frame() { OutlineColor = Color.Black, };
        ListView openCategorysView;
        //DataTemplate myDataTemplate = new DataTemplate(typeof(CameraCategorySelectionCell));
        DataTemplateSelector dts = new CameraDataTemplateSelector();

        iiBitmapView categoryCreationButton;

        Frame joinPassphraseFrame = new Frame() { OutlineColor = Color.Black, };
        Entry joinPassphrase = new Entry {
            Placeholder = "Type your join phrase then tap play\n to join an event",
            PlaceholderColor = Color.Gray,
            TextColor = Color.Black,
            FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
            BackgroundColor = Color.White,
            HorizontalTextAlignment = TextAlignment.Center,
            HorizontalOptions = LayoutOptions.FillAndExpand,
            Margin = 2,
        };
        iiBitmapView joinImageEnd;
        public void clearJoinPassphrase() { joinPassphrase.Text = ""; }

        // events
        EventHandler LoadEventsRequest;

        public CameraCategorySelectionView(CameraContentPage parent) {
            cameraPage = parent;

            Assembly assembly = this.GetType().GetTypeInfo().Assembly;
            categoryCreationButton = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.createaneventbutton.png", assembly)) {
                Scaling = true,
                EnsureSquare = false,
                //MinimumHeightRequest = 80,
                //MinimumWidthRequest = .8*360,
                //HorizontalOptions = LayoutOptions.CenterAndExpand,
                Margin = 2,
            };
            categoryCreationButton.SizeChanged += CheckCatButton;

            TapGestureRecognizer tap = new TapGestureRecognizer();
            tap.Tapped += OnCategoryCreateClicked;
            categoryCreationButton.GestureRecognizers.Add(tap);


            //joinPassphraseFrame.Content = joinPassphrase;
            joinImageEnd = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.play.png", assembly)) {
                HorizontalOptions = LayoutOptions.End,
                Margin = 3,
            };
            TapGestureRecognizer joinTap = new TapGestureRecognizer();
            joinTap.Tapped += OnJoinClicked;
            //joinLabel.GestureRecognizers.Add(joinTap);
            joinImageEnd.GestureRecognizers.Add(joinTap);

            LoadEventsRequest += new EventHandler(OnEventsLoadRequest);
        }

        public int buildUI() {
            int rowSet1 = 12;
            if (portraitView == null) {
                portraitView = new Grid { ColumnSpacing = 1, RowSpacing = 1, BackgroundColor = GlobalStatusSingleton.backgroundColor };
                for (int i = 0; i < rowSet1; i++) {
                    portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                }
                portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                //portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(3, GridUnitType.Star) });
                portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                for (int i = 0; i < 8; i++) {
                    portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                }
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

            portraitView.Children.Add(categoryFrame, 0, 0);
            Grid.SetRowSpan(categoryFrame, rowSet1);
            Grid.SetColumnSpan(categoryFrame, 8);
            portraitView.Children.Add(openCategorysView, 0, 0);
            Grid.SetRowSpan(openCategorysView, rowSet1);
            Grid.SetColumnSpan(openCategorysView, 8);

            
            portraitView.Children.Add(joinPassphraseFrame, 0, rowSet1);
            Grid.SetColumnSpan(joinPassphraseFrame, 8);
            portraitView.Children.Add(joinPassphrase, 0, rowSet1);
            Grid.SetColumnSpan(joinPassphrase, 8);
            portraitView.Children.Add(joinImageEnd, 7, rowSet1);

            portraitView.Children.Add(categoryCreationButton, 1, rowSet1+1);
            Grid.SetColumnSpan(categoryCreationButton, 6);
            
            Grid.SetRowSpan(categoryCreationButton, 2);
            //portraitView.Children.Add(categoryCreationImageStart, 0, 14);
            //portraitView.Children.Add(categoryCreationImageEnd, 0, 14);

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

                CameraSpacerRowElement csre = new CameraSpacerRowElement();
                openCategorys.Add(csre);

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
                cameraPage.eventDrillDown((CameraEventTitleElement)args.SelectedItem);
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
            await joinImageEnd.FadeTo(0, 175);
            await joinPassphrase.FadeTo(1, 175);
            await joinImageEnd.FadeTo(1, 175);
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
                    //AddEvent(newEvent);
                    cameraPage.AddEvent(newEvent);  // calls this.addevent, but also the profile page's.
                }
            }
        }

        public void AddEvent(EventJSON evt) {
            // title row here.
            CameraEventTitleElement cete = new CameraEventTitleElement() {
                eventName = evt.eventName, accessKey = evt.accessKey, eventId = evt.eventId, rawEvent = evt, };
            openCategorys.Add(cete);
            // now the categories for this event.
            int uploadCategoryCount = 0;
            foreach (CategoryJSON category in evt.categories) {
                if (category.state == CategoryJSON.UPLOAD) {
                    CameraClosedCategoryElement ccce = new CameraClosedCategoryElement(category);
                    if (!hasCategory(ccce)) {
                        openCategorys.Add(ccce);
                        uploadCategoryCount++;
                    }
                } else if (category.state == CategoryJSON.PENDING) {
                    CameraClosedCategoryElement ccce = new CameraClosedCategoryElement(category);
                    if (!hasCategory(ccce)) {
                        openCategorys.Add(ccce);
                        uploadCategoryCount++;
                    }
                }
            }
            if (uploadCategoryCount == 0) cete.stillUploading = false;
        }

        public void CheckCatButton(object sender, EventArgs args) {
            Debug.WriteLine("DHB:CameraCategorySelectionView:CheckCatButton - well?");
            //categoryCreationButton.testRedraw(); // fail.
            portraitView.RaiseChild(categoryCreationButton);
        }
    }
}
