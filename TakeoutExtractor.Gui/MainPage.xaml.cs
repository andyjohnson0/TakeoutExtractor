using Microsoft.Extensions.Primitives;
using System.Reflection;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using uk.andyjohnson.TakeoutExtractor.Lib;
using uk.andyjohnson.TakeoutExtractor.Lib.Photo;
using Microsoft.Maui.Controls.Compatibility.Platform.UWP;

namespace uk.andyjohnson.TakeoutExtractor.Gui
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            ViewErrorsWarnings.IsEnabled = false;

            // Global controls
            CreateLogFileCbx.IsChecked = GlobalOptions.Defaults.CreateLogFile;
            StopOnErrorCbx.IsChecked = GlobalOptions.Defaults.StopOnError;

            // Phot controls.
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


        // Results of the last extraction operation.
        private IEnumerable<ExtractorAlert>? alerts = null;


        private void OnFileExitCommand(object sender, EventArgs e)
        {
            Application.Current!.Quit();
        }
        private async void OnViewAlertsCommand(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AlertsPage(this.alerts!));
        }

        private void OnHelpAboutCommand(object sender, EventArgs e)
        {
            var msg = string.Format("Takeout Extractor v{0} by Andy Johnson. See https://github.com/andyjohnson0/TakeoutExtractor for info.",
                                    Assembly.GetExecutingAssembly().GetName().Version!.ToString());
            DisplayAlert("About Takeout Extractor", msg, "Ok");
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

            // Check for existing files / directories in the output folder
            var outputDi = new DirectoryInfo(OutputDirEntry.Text);
            if (outputDi.Exists && (outputDi.GetDirectories().Length > 0 || outputDi.GetFiles().Length > 0))
            {
                var choice = await QuestionDialog.ShowAsync("Proceed?", 
                                                            "The output folder contains files and/or directories. " + 
                                                            "If they have the same names as files generated during the extraction " +
                                                            "then unneccessary duplicates may be created. Do you wish to proceed?",
                                                            "Ok", "Cancel");
                if (choice != "Ok")
                    return;
            }

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
                // An exception has occurred. Treat this as an unrecoverable error and create an alert for it.

                // Try to receover any alerts that might have been attached to the exception's Data collection,
                // either as a single object or a collection.
                var data = ex.DataDict().FirstOrDefault().Value;
                if (data is ExtractorAlert)
                    data = new ExtractorAlert?[] { data as ExtractorAlert };
                var recoveredAlerts = (data is IEnumerable<ExtractorAlert>) ? new List<ExtractorAlert>((data as IEnumerable<ExtractorAlert>)!) : new List<ExtractorAlert>();
                recoveredAlerts.Add(new ExtractorAlert(ExtractorAlertType.Error, $"An unrecoverable error occurred: {ex.Message}") { AssociatedException = ex });
                this.alerts = recoveredAlerts;
                this.ViewErrorsWarnings.IsEnabled = this.alerts?.Count() > 0;

                // Display error and give user the option to navigate to the alerts page.
                var choice = await QuestionDialog.ShowAsync("Error", $"An unrecoverable error occurred: {ex.Message}", 
                                                            "Ok", this.alerts?.Count() > 0 ? "Details" : null);
                if (choice != null && choice == "Details")
                {
                    await Navigation.PushAsync(new AlertsPage(this.alerts!));
                }
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
            this.alerts = results?.SelectMany(a => a.Alerts);
            if (this.alerts != null)
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
            this.ViewErrorsWarnings.IsEnabled = this.alerts?.Count() > 0;
            var choice = await QuestionDialog.ShowAsync("Completed", sb.ToString(), "Ok", this.alerts?.Count() > 0 ? "Details" : null);
            if (choice != null && choice == "Details")
            {
                await Navigation.PushAsync(new AlertsPage(this.alerts!));
            }
        }
    }
}
