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
    // This page shows all the events you have participated in.
    public class EventsHistory_Profile : ContentView, IProvideEventDrillDown {
        EventsResponseJSON eventHistory;

        Grid portraitView;
        Grid historyView;
        ObservableCollection<CameraCategoryElement> events = new ObservableCollection<CameraCategoryElement>();
        DataTemplate myDataTemplate = new DataTemplate(typeof(CameraEventTitleViewCell));
        ListView myEvents;
        Label noEvents = new Label {
            //BackgroundColor = GlobalStatusSingleton.ButtonColor,
            Text = "Loading...",
            HorizontalOptions = LayoutOptions.FillAndExpand,
            VerticalOptions = LayoutOptions.FillAndExpand,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            TextColor = Color.Gray,
            LineBreakMode = LineBreakMode.WordWrap,
            FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
            FontAttributes = FontAttributes.Bold,
            //IsVisible = false,
        };

        EventDetailPage eventView;
        EventCategoryImagesPage eventCategoryImgsView;

        public EventsHistory_Profile() {
            eventView = new EventDetailPage(this);
            eventCategoryImgsView = new EventCategoryImagesPage(this);

            myEvents = new ListView {
                HasUnevenRows = true,
                BackgroundColor = GlobalStatusSingleton.backgroundColor,
                //RowHeight = 32,
                ItemsSource = events,
                ItemTemplate = myDataTemplate,
                SeparatorVisibility = SeparatorVisibility.None,
                Margin = 1,
            };
            myEvents.ItemSelected += OnRowTapped;

            buildUI();
        }

        protected int buildHistoryView() {
            if (historyView == null) {
                historyView = new Grid { ColumnSpacing = 0, RowSpacing = 2, BackgroundColor = GlobalStatusSingleton.backgroundColor, };
                for (int i = 0; i < 12; i++) {
                    historyView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                }
            }
            historyView.Children.Clear();
            historyView.Children.Add(myEvents, 0, 0);
            Grid.SetRowSpan(myEvents, 12);
            historyView.Children.Add(noEvents, 0, 4);
            Grid.SetRowSpan(noEvents, 4);
            Content = historyView;
            return 1;
        }

        protected int buildUI() {
            if (portraitView == null) {
                portraitView = new Grid { ColumnSpacing = 0, RowSpacing = 0, BackgroundColor = GlobalStatusSingleton.backgroundColor, };
                portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
            buildHistoryView();
            portraitView.Children.Add(historyView);
            portraitView.Children.Add(eventView);
            portraitView.Children.Add(eventCategoryImgsView);
            switchToSelectView();
            Content = portraitView;
            return 1;
        }

        public async void OnCategoryLoad(Object sender, EventArgs args) {
            string result = "fail";
            while (result.Equals("fail")) {
                result = await requestApiCallAsync();
                if (result == null) result = "fail";
                if (result.Equals("fail")) {
                    await Task.Delay(5000);
                }
            }
            if (result.Equals(GlobalSingletonHelpers.EMPTY)) {
                noEvents.Text = "You have not entered any events";
                noEvents.IsVisible = true;
            } else {
                noEvents.IsVisible = false;
                eventHistory = JsonConvert.DeserializeObject<EventsResponseJSON>(result);
                eventHistory.events.Sort();
                eventHistory.events.Reverse();

                foreach (EventJSON evt in eventHistory.events) {
                    CameraEventTitleElement cete = new CameraEventTitleElement {
                        eventName = evt.eventName,
                        accessKey = evt.accessKey,
                        eventId = evt.eventId,
                        fullEvent = evt,
                    };
                    events.Add(cete);
                }
            }

        }

        public void AddEvent(EventJSON cerj) {
            CameraEventTitleElement cete = new CameraEventTitleElement {
                eventName = cerj.eventName,
                accessKey = cerj.accessKey,
                eventId = cerj.eventId,
                fullEvent = cerj,
            };
            events.Insert(0, cete);
        }

        protected static async Task<string> requestApiCallAsync() {
            Debug.WriteLine("DHB:EventsHistory_Profile:requestApiCallAsync start");
            string result = "fail";

            //result = await GlobalSingletonHelpers.requestFromServerAsync(HttpMethod.Get, "events/prev/10000", "");
            result = await GlobalSingletonHelpers.requestFromServerAsync(HttpMethod.Get, "events/next/0", "");
            //result = await GlobalSingletonHelpers.requestFromServerAsync(HttpMethod.Get, "events/next", "");
            //result = await GlobalSingletonHelpers.requestFromServerAsync(HttpMethod.Get, "events/next/0/100", "");
            return result;
        }

        public void OnRowTapped(Object sender, SelectedItemChangedEventArgs args) {
            if (args.SelectedItem is CameraEventTitleElement) {
                eventView.SetUIData((CameraEventTitleElement)args.SelectedItem);
                //Content = eventView.Content;
                switchToEventView();
            }
        }
        public void switchToSelectView() {
            //Content = portraitView;
            historyView.IsVisible = true;
            eventView.IsVisible = false;
            eventCategoryImgsView.IsVisible = false;
        }

        public void switchToEventView() {
            //Content = eventView.Content;  // periodic crashes.
            historyView.IsVisible = false;
            eventView.IsVisible = true;
            eventCategoryImgsView.IsVisible = false;
        }

        public void switchToCategoryImgView(CategoryJSON category) {
            eventCategoryImgsView.ActiveCategory = category;
            //Content = eventCategoryImgsView.Content;
            historyView.IsVisible = false;
            eventView.IsVisible = false;
            eventCategoryImgsView.IsVisible = true;
        }

    }
}

