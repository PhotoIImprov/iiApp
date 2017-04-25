using System;
using Xamarin.Forms;

namespace ImageImprov {
    public partial class CheckBox : ContentView {
    //public partial class CheckBox : ContentPage {
        Label boxLabel;
        Label textLabel;
        StackLayout checkboxObj;

        public static readonly BindableProperty TextProperty =
        BindableProperty.Create(
            "Text",
            typeof(string),
            typeof(CheckBox),
            "",  // was null, looks like this is the default value. try switching to something as I'm getting a null ptr error.
            propertyChanged: (bindable, oldvalue, newValue) =>
            {
                ((CheckBox)bindable).textLabel.Text = (string)newValue;
            });

        public static readonly BindableProperty FontSizeProperty =
        BindableProperty.Create(
            "FontSize",
            typeof(double),
            typeof(CheckBox),
            Device.GetNamedSize(NamedSize.Default, typeof(Label)),
            propertyChanged: (bindable, oldvalue, newValue) =>
            {
                CheckBox checkbox = (CheckBox)bindable;
                checkbox.boxLabel.FontSize = (double)newValue;
                checkbox.textLabel.FontSize = (double)newValue;
            });

        public static readonly BindableProperty IsCheckedProperty =
        BindableProperty.Create(
            "IsChecked",
            typeof(bool),
            typeof(CheckBox),
            false,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                // set the graphic
                CheckBox checkbox = (CheckBox)bindable;
                checkbox.boxLabel.Text = (bool)newValue ? "\u2611" : "\u2610";
                // Fire the event.
                EventHandler<bool> eventHandler = checkbox.CheckChanged;
                if (eventHandler != null) {
                    eventHandler(checkbox, (bool)newValue);
                }
            });
            
        public event EventHandler<bool> CheckChanged;

        public CheckBox() {
            //InitializeComponent();
            boxLabel = new Label { Text = "\u2610", };
            textLabel = new Label();
            checkboxObj = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Children = { boxLabel, textLabel, }
            };
            Content = checkboxObj;
            // gesture registration...
            var tapGestureRecognizer = new TapGestureRecognizer();
            tapGestureRecognizer.NumberOfTapsRequired = 1;
            tapGestureRecognizer.Tapped += OnCheckBoxTapped;
            //checkboxObj.GestureRecognizers.Add(tapGestureRecognizer);
            this.GestureRecognizers.Add(tapGestureRecognizer);
        }

        public string Text {
            set { SetValue(TextProperty, value); }
            get { return (string)GetValue(TextProperty); }
        }

        [TypeConverter(typeof(FontSizeConverter))]
        public double FontSize {
            set { SetValue(FontSizeProperty, value); }
            get { return (double)GetValue(FontSizeProperty); }
        }
        
        public bool IsChecked {
            set { SetValue(IsCheckedProperty, value); }
            get { return (bool)GetValue(IsCheckedProperty); }
        }
        
        // Tap gesture recognizer handler.
        void OnCheckBoxTapped(object sender, EventArgs args) {
            IsChecked = !IsChecked;
        }
    }
}
