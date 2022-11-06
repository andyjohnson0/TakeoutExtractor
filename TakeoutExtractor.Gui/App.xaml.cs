namespace TakeoutExtractor.Gui
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new uk.andyjohnson.TakeoutExtractor.Gui.AppShell();
        }
    }
}