using Microsoft.Extensions.Primitives;
using System.Reflection;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using uk.andyjohnson.TakeoutExtractor.Lib;
using uk.andyjohnson.TakeoutExtractor.Lib.Photo;
using uk.andyjohnson.TakeoutExtractor.Gui;


namespace uk.andyjohnson.TakeoutExtractor.Gui
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            // Menu options
            ViewErrorsWarnings.IsEnabled = false;

            // Global controls
            LogFileKindPicker.SelectedItem = LogFileKindPicker.Items.First(x => x.Equals(GlobalOptions.Defaults.LogFile.ToString(), 
                                                                                         StringComparison.InvariantCultureIgnoreCase));
            StopOnErrorCbx.IsChecked = GlobalOptions.Defaults.StopOnError;

            // Phot0 controls.
            PhotosExtractCbx.IsChecked = true;
            PhotosFileNameFormatTxt.Text = !string.IsNullOrEmpty(PhotoOptions.Defaults.OutputFileNameFormat) ? PhotoOptions.Defaults.OutputFileNameFormat : "";
            PhotosFileNameTimeKindPicker.SelectedItem = PhotosFileNameTimeKindPicker.Items.First(x => x.Equals(PhotoOptions.Defaults.OutputFileNameTimeKind.ToString(), 
                                                                                                               StringComparison.InvariantCultureIgnoreCase));
            PhotosUpdateExifCbx.IsChecked = PhotoOptions.Defaults.UpdateExif;
            PhotoFileOrganisationPicker.SelectedIndex = (int)PhotoOptions.Defaults.OutputFileVersionOrganisation;
            PhotosSubdirOrganisationPicker.SelectedIndex = (int)PhotoOptions.Defaults.OutputDirOrganisation;
        }


        // Results of the last extraction operation.
        private IEnumerable<ExtractorAlert>? alerts = null;

        // Has the splash been displayed?
#if RELEASE && (WINDOWS || MACCATALYST)
        private bool splashShown = false;
#endif


        protected override void OnAppearing()
        {
            base.OnAppearing();

            this.Window.ShowWindow(WindowExt.WindowState.Maximised);


#if RELEASE && (WINDOWS || MACCATALYST)
            if (!splashShown)
            {
                var splash = new SplashOverlay();
                splash.Show(this.MainGrid, new TimeSpan(0, 0, 3));
                splashShown = true;
            }
#endif
        }


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



        private async void OnStartBtnClicked(
            object sender,
            EventArgs e)
        {
            const int numFlashes = 4;   // Number of times to flash window on completion

            //
            if (string.IsNullOrEmpty(InputDirEntry.Text))
            {
                await DisplayAlert("Error", "Please specify an input folder", "Ok");
                return;
            }
            if (string.IsNullOrEmpty(OutputDirEntry.Text))
            {
                await DisplayAlert("Error", "Please specify an output folder", "Ok");
                return;
            }

            var outputDi = new DirectoryInfo(OutputDirEntry.Text);

            // If output folder doesn't exist then prompt to create it.
            if (!outputDi.Exists)
            {
                var choice = await QuestionDialog.ShowAsync("Create Output Folder?", 
                                                            "The output folder does not exist. Do you wish to create it and proceed?",
                                                            "Ok", "Cancel");
                if (choice != "Ok")
                    return;
                try
                {
                    outputDi.Create();
                }
                catch(IOException)
                {
                    await DisplayAlert("Error", "The output directory could not be created.", "Ok");
                    return;
                }
            }

            // Check for existing files / directories in the output folder
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
                LogFile = Enum.Parse<GlobalOptions.LogFileType>(LogFileKindPicker.SelectedItem.ToString()!, true),
                StopOnError = StopOnErrorCbx.IsChecked
            };
            var mediaOptions = new List<IExtractorOptions>();
            if (this.PhotosExtractCbx.IsChecked)
            {
                var photoOptions = new PhotoOptions()
                {
                    OutputFileNameFormat = PhotosFileNameFormatTxt.Text,
                    OutputFileNameTimeKind = Enum.Parse<DateTimeKind>(PhotosFileNameTimeKindPicker.SelectedItem.ToString()!, true), 
                    UpdateExif = PhotosUpdateExifCbx.IsChecked,
                    OutputFileVersionOrganisation = (PhotoFileVersionOrganisation)PhotoFileOrganisationPicker.SelectedIndex,
                    OutputDirOrganisation = (PhotoDirOrganisation)PhotosSubdirOrganisationPicker.SelectedIndex,
                    ExtractDeletedFiles = PhotosExtractDeletedCbx.IsChecked
                };
                mediaOptions.Add(photoOptions);
            }
            var extractor = new ExtractorManager(globalOptions, mediaOptions);

            // Disable the window and menu items.
            this.MainGrid.SetEnabledAll(false);
            this.MenuBarItems.SetEnabledAll(false);

            // Show the overlay that give feedback progress.
            var progressOverlay = new ProgressOverlay();
            progressOverlay.Show(MainGrid);
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
                this.Window.FlashWindow(numFlashes);
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
                this.Window.FlashWindow(numFlashes);
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

                // Re-enable the window and menu items.
                this.MainGrid.SetEnabledAll(true);
                this.MenuBarItems.SetEnabledAll(true);
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
