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
			this.alerts = alerts != null ? alerts : new ExtractorAlert[0];

			InitializeComponent();
		}

        private readonly IEnumerable<ExtractorAlert> alerts;


		protected override void OnAppearing()
		{
			base.OnAppearing();

            var errorCount = alerts.Count(a => a.Type == ExtractorAlertType.Error);
            var warningCount = alerts.Count(a => a.Type == ExtractorAlertType.Warning);
            var infoCount = alerts.Count(a => a.Type == ExtractorAlertType.Information);
			alertsBreakdownCountsLabel.Text = $"Errors: {errorCount}   Warnings: {warningCount}    Infos: {infoCount}";

            alertsCollView.ItemsSource = alerts;
		}


        protected async void OnFileTapped(object sender, EventArgs args)
        {
			var tea = args as TappedEventArgs;
			var fi = tea?.Parameter as FileInfo;
			if (fi != null)
			{
				await Browser.Default.OpenAsync(fi.FullName, BrowserLaunchMode.SystemPreferred);
			}
        }


        protected async void OnDetailsTapped(object sender, EventArgs args)
        {
			var tea = args as TappedEventArgs;
			var message = tea?.Parameter as string;
			if (tea != null)
			{
				await DisplayAlert("Details", message, "Ok");
			}
        }
    }
}
