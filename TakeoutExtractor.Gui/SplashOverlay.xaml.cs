namespace uk.andyjohnson.TakeoutExtractor.Gui;

/// <summary>
/// Splash screen overlay for Window/Mac
/// </summary>
public partial class SplashOverlay : ContentView
{
    /// <summary>
    /// Constructor. Initialise a SplashOverlay object.
    /// </summary>
	public SplashOverlay()
	{
		InitializeComponent();
	}


    /// <summary>
    /// Show the overlay as child of a supplied Grid container.
    /// </summary>
    /// <param name="parent">Parent container</param>
    /// <param name="timeToShow">Length of time that the splash screen is visible.</param>
    public void Show(
        Layout parent,
        TimeSpan timeToShow)
    {
        this.ZIndex = 99;
        parent.Children.Add(this);
        this.IsVisible = true;

        Task.Factory.StartNew(() =>
        {
            Task.Delay(timeToShow).Wait();
            MainThread.BeginInvokeOnMainThread(() => this.Close());
        });
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
}