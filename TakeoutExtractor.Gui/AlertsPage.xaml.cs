using System.Globalization;
using System.Windows.Input;
using uk.andyjohnson.TakeoutExtractor.Lib;

namespace uk.andyjohnson.TakeoutExtractor.Gui
{
	/// <summary>
	/// Display a collection of ExtractorAlert objects.
	/// </summary>
	public partial class AlertsPage : ContentPage
	{
		/// <summary>
		/// Constructor. Initialise an AlertsPage object.
		/// </summary>
		/// <param name="alerts">Collection of ExtractrorAlert objects to display.</param>
		public AlertsPage(IEnumerable<ExtractorAlert> alerts)
		{
			this.alerts = alerts;

			InitializeComponent();

		}

        private readonly IEnumerable<ExtractorAlert> alerts;

		protected override void OnAppearing()
		{
			base.OnAppearing();

            alertsCollView.ItemsSource = alerts;
		}


        // This is a binding so it has to be public
        public ICommand FileTapCommand => new Command<string>(async (url) => await Browser.Default.OpenAsync(url, BrowserLaunchMode.SystemPreferred));
	}
}
