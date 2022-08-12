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

            PhotosExtractCbx.IsChecked = true;
            PhotosFileNameFormatTxt.Text = !string.IsNullOrEmpty(PhotoOptions.Defaults.OutputFileNameFormat) ? PhotoOptions.Defaults.OutputFileNameFormat : "";
            PhotosUpdateExifCbx.IsChecked = PhotoOptions.Defaults.UpdateExif;
            PhotosKeepOriginalsCbx.IsChecked = PhotoOptions.Defaults.KeepOriginalsForEdited;
            PhotosSuffixOriginalsTxt.Text = !string.IsNullOrEmpty(PhotoOptions.Defaults.OriginalsSuffix) ? PhotoOptions.Defaults.OriginalsSuffix : "";
            PhotosSubdirOriginalsTxt.Text = !string.IsNullOrEmpty(PhotoOptions.Defaults.OriginalsSubdirName) ? PhotoOptions.Defaults.OriginalsSubdirName : "";
            PhotosSubdirOrganisationPicker.SelectedIndex = (int)PhotoOptions.Defaults.OrganiseBy;
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
            MainGrid.IsEnabled = false;
            ProgressOverlay? progressOverlay = new ProgressOverlay();
            progressOverlay.ZIndex = 99;
            // TODO: Improve how this is added to the layout.
            progressOverlay.SetValue(Grid.RowProperty, 0);
            progressOverlay.SetValue(Grid.ColumnProperty, 0);
            MainGrid.Children.Add(progressOverlay);
            //progressOverlay.IsEnabled = true;

            // Set-up extractor and options.
            var globalOptions = new GlobalOptions()
            {
                InputDir = new DirectoryInfo(InputDirEntry.Text),
                OutputDir = new DirectoryInfo(OutputDirEntry.Text),
                CreateLogFile = CreateLogFileCbx.IsChecked
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
                await DisplayAlert("Completed", "The extraction operation completed successfully" + Environment.NewLine + Environment.NewLine + FormatResults(results), "Ok");
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
                progressOverlay.IsVisible = false;
                progressOverlay = null;
                MainGrid.IsEnabled = true;
            }
        }


        private static string FormatResults(
            IEnumerable<IExtractorResults>? results)
        {
            var sb = new StringBuilder();

            sb.Append("Results:" + Environment.NewLine);
            if (results != null && results.Any())
            {
                foreach (var result in results)
                {
                    if (result is PhotoResults)
                        sb.Append("Photos and Videos: ");
                    else
                        sb.Append("Other: ");
                    sb.Append(string.Format("{0}% in {1}",
                        result.Coverage * 100M,
                        result.Duration.ToString(@"hh\h\ mm\m\ ss\s")));
                }
            }
            else
            {
                sb.Append("None");
            }

            return sb.ToString();
        }
    }
}
