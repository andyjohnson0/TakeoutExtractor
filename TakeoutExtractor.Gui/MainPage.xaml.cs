using Microsoft.Extensions.Primitives;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using uk.andyjohnson.TakeoutExtractor.Lib;


namespace uk.andyjohnson.TakeoutExtractor.Gui
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }


        protected override void OnAppearing()
        {
#if DEBUG
            InputDirEntry.Text = "D:\\Temp\\takeout-20220331T163017Z-001\\Takeout";
            OutputDirEntry.Text = "D:\\Temp\\TakeoutData";
#endif
            CreateLogFileCbx.IsChecked = GlobalOptions.Defaults.CreateLogFile;
            StopOnErrorCbx.IsChecked = GlobalOptions.Defaults.StopOnError;

            PhotosExtractCbx.IsChecked = true;
            PhotosFileNameFormatTxt.Text = !string.IsNullOrEmpty(PhotoOptions.Defaults.OutputFileNameFormat) ? PhotoOptions.Defaults.OutputFileNameFormat : "";
            PhotosUpdateExifCbx.IsChecked = PhotoOptions.Defaults.UpdateExif;
            PhotosKeepOriginalsCbx.IsChecked = PhotoOptions.Defaults.KeepOriginalsForEdited;
            PhotosSuffixOriginalsTxt.Text = !string.IsNullOrEmpty(PhotoOptions.Defaults.OriginalsSuffix) ? PhotoOptions.Defaults.OriginalsSuffix : "";
            PhotosSubdirOriginalsTxt.Text = !string.IsNullOrEmpty(PhotoOptions.Defaults.OriginalsSubdirName) ? PhotoOptions.Defaults.OriginalsSubdirName : "";
            PhotosSubdirOrganisationPicker.SelectedIndex = (int)PhotoOptions.Defaults.OrganiseBy;

            // Do a chage event on the keep originals checkbox to ensure that associated controls are correctly enabled/disabled.
            OnPhotosKeepOriginalsChanged(this, new CheckedChangedEventArgs(PhotosKeepOriginalsCbx.IsChecked));
        }


        private void ExitCommandClicked(object sender, EventArgs e)
        {
            Application.Current!.Quit();
        }

        private void HelpCommandClicked(object sender, EventArgs e)
        {
            var msg = string.Format("Takeout Extractor v{0} by Andy Johnson. See https://github.com/andyjohnson0/TakeoutExtractor for info.",
                                    Assembly.GetExecutingAssembly().GetName().Version!.ToString());
            DisplayAlert("About TakeoutExtractor", msg, "Ok");
        }


        private async void OnInputDirButtonClicked(object sender, EventArgs e)
        {
            var dir = await FolderPicker.PickFolderAsync();
            if (dir != null)
            {
                this.InputDirEntry.Text = dir.FullName;
            }
        }


        private async void OnOutputDirButtonClicked(object sender, EventArgs e)
        {
            var dir = await FolderPicker.PickFolderAsync();
            if (dir != null)
            {
                this.OutputDirEntry.Text = dir.FullName;
            }
        }


        void OnPhotosExtractChanged(object sender, CheckedChangedEventArgs e)
        {
            PhotosOptionsGrid.IsVisible = e.Value;
        }

        void OnPhotosKeepOriginalsChanged(object sender, CheckedChangedEventArgs e)
        {
            PhotosSuffixOriginalsTxt.IsEnabled = e.Value;
            PhotosSubdirOriginalsTxt.IsEnabled = e.Value;

            // Perform required operation after examining e.Value
            //SemanticScreenReader.Announce(PhotosKeepOriginalsLbl.Text);
        }



        private async void OnStartBtnClicked(
            object sender,
            EventArgs e)
        {
            // Show the overlay that give feedback progress.
            var progressOverlay = new ProgressOverlay();
            progressOverlay.Show(MainGrid);

            // Set-up extractor and options.
            var globalOptions = new GlobalOptions()
            {
                InputDir = new DirectoryInfo(InputDirEntry.Text),
                OutputDir = new DirectoryInfo(OutputDirEntry.Text),
                CreateLogFile = CreateLogFileCbx.IsChecked,
                StopOnError = StopOnErrorCbx.IsChecked
            };
            var mediaOptions = new List<IExtractorOptions>();
            if (this.PhotosExtractCbx.IsChecked)
            {
                var photoOptions = new PhotoOptions()
                {
                    OutputFileNameFormat = PhotosFileNameFormatTxt.Text,
                    UpdateExif = PhotosUpdateExifCbx.IsChecked,
                    KeepOriginalsForEdited = PhotosKeepOriginalsCbx.IsChecked,
                    OriginalsSuffix = PhotosKeepOriginalsCbx.IsChecked ? PhotosSuffixOriginalsTxt.Text : "",
                    OriginalsSubdirName = PhotosKeepOriginalsCbx.IsChecked ? PhotosSubdirOriginalsTxt.Text : "",
                    OrganiseBy = (PhotoOptions.OutputFileOrganisation)PhotosSubdirOrganisationPicker.SelectedIndex
                };
                mediaOptions.Add(photoOptions);
            }
            var extractor = new ExtractorManager(globalOptions, mediaOptions);
            extractor.Progress += (sender, e) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    progressOverlay.ShowProgress(sender, e);
                });
            };

            // Create a cancellation token for the extraction operation, and attach a lambda to the overlay's cancel button
            // that will trigger a cancellation.
            var cancellationTokenSource = new CancellationTokenSource();
            progressOverlay.Cancelled += (sender, e) => 
            { 
                cancellationTokenSource.Cancel(); 
            };

            // Do the extraction.
            try
            {
                var results = await Task.Run(() => extractor.ExtractAsync(cancellationTokenSource.Token));
                progressOverlay.Close();
                await DisplayResults(results);
            }
            catch(OperationCanceledException)
            {
                await DisplayAlert("Cancelled", "The extraction operation was cancelled", "Ok");
            }
            catch(Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "Ok");
            }
            finally
            {
                // Hide the overlay.
                progressOverlay.Close();
                progressOverlay = null;
            }
        }


        private async Task DisplayResults(
            IEnumerable<IExtractorResults>? results)
        {
            var sb = new StringBuilder();

            sb.AppendLine("The extraction operation completed");
            sb.AppendLine();

            sb.AppendLine("Results:");
            var alerts = results?.SelectMany(a => a.Alerts);
            if (alerts != null)
            {
                var errorCount = alerts.Count(a => a.Type == ExtractorAlertType.Error);
                var warningCount = alerts.Count(a => a.Type == ExtractorAlertType.Warning);
                var infoCount = alerts.Count(a => a.Type == ExtractorAlertType.Information);
                sb.AppendLine($"{errorCount} error, {warningCount} warning, {infoCount} information");
            }
            if (results != null && results.Any())
            {
                foreach (var result in results)
                {
                    if (result is PhotoResults)
                        sb.Append("Photos and Videos: ");
                    else
                        sb.Append("Other: ");
                    sb.AppendLine(string.Format("{0}% in {1}",
                        result.Coverage * 100M,
                        result.Duration.ToString(@"hh\h\ mm\m\ ss\s")));
                }
            }
            else
            {
                sb.AppendLine("None");
            }

            // Display results. If we have alerts then add a details button to display them.
            var choice = await QuestionDialog.ShowAsync("Completed", sb.ToString(), "Ok", alerts?.Count() > 0 ? "Details" : null);
            if (choice != null && choice == "Details")
            {
                await Navigation.PushAsync(new AlertsPage(alerts!));
            }
        }
    }
}
