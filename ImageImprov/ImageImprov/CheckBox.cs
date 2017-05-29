using System;
using Xamarin.Forms;

namespace ImageImprov {
    public partial class CheckBox : ContentView {
        //public partial class CheckBox : ContentPage {
        Image checkedImage = new Image {
                Source = ImageSource.FromResource("ImageImprov.IconImages.checkedBox.png"), Aspect=Aspect.Fill,
                HorizontalOptions = LayoutOptions.StartAndExpand,
                WidthRequest = 32
        };
        Image uncheckedImage = new Image {
            Source = ImageSource.FromResource("ImageImprov.IconImages.uncheckedBox.png"), Aspect = Aspect.Fill,
            HorizontalOptions = LayoutOptions.StartAndExpand,
            WidthRequest = 32
        };

        Image boxImage;
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
                //checkbox.boxLabel.FontSize = (double)newValue;
                checkbox.textLabel.FontSize = (double)newValue;
            });

        //static int count = 0;
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
                checkbox.boxImage = (bool)newValue ? checkbox.checkedImage : checkbox.uncheckedImage;
                // Fire the event.
                EventHandler<bool> eventHandler = checkbox.CheckChanged;
                if (eventHandler != null) {
                    eventHandler(checkbox, (bool)newValue);
                }
            });
            
        public event EventHandler<bool> CheckChanged;

        public CheckBox() {
            //InitializeComponent();
            boxImage = uncheckedImage;
            textLabel = new Label { TextColor = Color.Black, };
            checkboxObj = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                Children = { boxImage, textLabel, }
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
            set {
                SetValue(IsCheckedProperty, value);
                checkboxObj = new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    HorizontalOptions = LayoutOptions.CenterAndExpand,
                    Children = { boxImage, textLabel, }
                };
                Content = checkboxObj;
            }
            get { return (bool)GetValue(IsCheckedProperty); }
        }
        
        // Tap gesture recognizer handler.
        void OnCheckBoxTapped(object sender, EventArgs args) {
            IsChecked = !IsChecked;
            // image not updating on screen. why not?
            // because I was setting to a static object. issue gone bye bye now. :)
        }
    }
}
