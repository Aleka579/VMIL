using System.Windows;
using System.Windows.Controls;
using VMIdentityLab.App.Infrastructure;
using VMIdentityLab.App.ViewModels;
using Localization = VMIdentityLab.App.Infrastructure.Localization;

namespace VMIdentityLab.App;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel model)
    {
        InitializeComponent();
        DataContext = model;
        LanguageSelector.SelectedIndex = Localization.English ? 1 : 0;
        UpdateColumnHeaders();
        Closed += (_, _) => model.Dispose();
    }

    private void LanguageChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainViewModel model)
        {
            model.ChangeLanguage(LanguageSelector.SelectedIndex == 1);
            UpdateColumnHeaders();
        }
    }

    internal FrameworkElement PreviewContent => RootContent;

    private void UpdateColumnHeaders()
    {
        static void Apply(DataGrid grid, string[] keys)
        {
            for (var index = 0; index < keys.Length; index++) grid.Columns[index].Header = Localization.Text("Column." + keys[index]);
        }
        Apply(FindingsTable, ["Category", "Name", "Value", "Status", "Source"]);
        Apply(VmxTable, ["Key", "Value", "Support"]);
        Apply(DifferencesTable, ["Name", "Left", "Right", "Result", "Reason"]);
    }
}
