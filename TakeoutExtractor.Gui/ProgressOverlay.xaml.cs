using uk.andyjohnson.TakeoutExtractor.Lib;
using uk.andyjohnson.TakeoutExtractor.Lib.Photo;


namespace uk.andyjohnson.TakeoutExtractor.Gui
{ 
	/// <summary>
	/// Overlay containing a cancel button and an updatable progress area in a frame.
	/// Looks a bit like a dialog box.
	/// </summary>
	public partial class ProgressOverlay : ContentView
	{
		/// <summary>
		/// Constructor. Initialise a ProgressOverlay object.
		/// </summary>
		public ProgressOverlay()
		{
			InitializeComponent();
		}


		/// <summary>
		/// Show the overlay as child of a supplied Grid container.
		/// </summary>
		/// <param name="parent">Parent container</param>
		public void Show(Layout parent)
		{
			this.ZIndex = 99;
			parent.Children.Add(this);
			this.IsVisible = true;
		}


		/// <summary>
		/// Hide the overlay and remove it from its parent.
		/// </summary>
		public void Close()
		{
			this.IsVisible = false;
			var parent = this.Parent as Grid;
			if (parent != null)
			{
				parent.Children.Remove(this);
			}
		}


		/// <summary>
		/// Cancel event. Fired if cancel button was clicked.
		/// </summary>
		public event EventHandler<EventArgs>? Cancelled;


		/// <summary>
		/// True if cancel button was clicked.
		/// </summary>
		public bool IsCancelled { get; private set; } = false;


		/// <summary>
		/// Display progress information.
		/// </summary>
		/// <param name="sender">Sender of the progress information. Can be null.</param>
		/// <param name="e">Extraction progress information.</param>
		public void ShowProgress(
			object? sender,
			ProgressEventArgs e)
		{ 
			if (sender is PhotoExtractor)
				this.Title.Text = "Extracting Photos and Videos";
			else
				this.Title.Text = "Extracting Data";

			const int reqMaxFileNameLen = 50;
			this.SourceLabel.Text = "Copying " + e.SourceFile.CompactName(reqMaxFileNameLen);
			this.DestinationLabel.Text = "To " + e.DesinationFile.CompactName(reqMaxFileNameLen);
		}


		/// <summary>
		/// Handle cancel button click.
		/// Sets IsCancelled to true and fires the Cancelled event.
		/// </summary>
		/// <param name="sender">Sender</param>
		/// <param name="e">Click event args</param>
		private void OnCancelButtonClicked(object sender, EventArgs e)
		{
			CancelButton.Text = "Cancelling...";
			CancelButton.IsEnabled = false;
			IsCancelled = true;
			if (Cancelled != null)
				Cancelled(this, e);
		}




		private double currentMaxWidth = 0D;

		/// <summary>
		/// Override size changes caused by updating the progress info.
		/// Ensure that the bounding box only expands horizontally, and never contracts. This avoids distracting flickering.
		/// </summary>
		/// <param name="sender">Sender</param>
		/// <param name="e">Aregs</param>
		private void Border_SizeChanged(object sender, EventArgs e)
		{
			var bdr = sender as Border;
			if (bdr?.Width > currentMaxWidth)
			{
				currentMaxWidth = bdr.Width;
				bdr.MinimumWidthRequest = currentMaxWidth;
			}
		}
	}
}