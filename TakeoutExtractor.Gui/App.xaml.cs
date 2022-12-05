namespace uk.andyjohnson.TakeoutExtractor.Gui
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            SetAppTheme(Application.Current!.RequestedTheme);

            MainPage = new AppShell();
        }


        public static void SetAppTheme(AppTheme theme)
        {
            // System theme has changed
            switch (theme)
            {
                case AppTheme.Dark:
                    Application.Current!.UserAppTheme = AppTheme.Dark;
                    break;
                case AppTheme.Light:
                    Application.Current!.UserAppTheme = AppTheme.Light;
                    break;
                default:
                    Application.Current!.UserAppTheme = AppTheme.Light;
                    break;
            }
        }
    }
}