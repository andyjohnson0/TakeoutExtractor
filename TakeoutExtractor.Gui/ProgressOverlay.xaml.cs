namespace uk.andyjohnson.TakeoutExtractor.Gui;

using uk.andyjohnson.TakeoutExtractor.Lib;


public partial class ProgressOverlay : ContentView
{
	public ProgressOverlay()
	{
		InitializeComponent();
	}


	public void Show(Grid parent)
	{
        this.ZIndex = 99;
        this.SetValue(Grid.RowProperty, 0);
        this.SetValue(Grid.ColumnProperty, 0);
        parent.Children.Add(this);
    }


	public void Close()
	{
		this.IsVisible = false;
		var parent = this.Parent as Grid;
		if (parent != null)
		{
			parent.Children.Remove(this);
		}
	}


    public event EventHandler<EventArgs>? Cancelled;


	public bool IsCancelled { get; private set; } = false;


    private void OnCancelButtonClicked(object sender, EventArgs e)
	{
		CancelButton.Text = "Cancelling...";
		CancelButton.IsEnabled = false;
		IsCancelled = true;
		if (Cancelled != null)
			Cancelled(this, e);
	}



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



	private double currentMaxWidth = 0D;

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
