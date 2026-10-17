using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace VMIdentityLab.App.Infrastructure;

// Request-scoped credentials; this dialog does not persist connection settings.
internal sealed class VmwareConnectionDialog : Window
{
    private readonly TextBox port = new() { Text = "8697", Margin = new(0, 4, 0, 12) };
    private readonly TextBox user = new() { Margin = new(0, 4, 0, 12) };
    private readonly PasswordBox password = new() { Margin = new(0, 4, 0, 18) };
    private string acceptedPassword = "";

    public int Port { get; private set; }
    public string UserName => user.Text;
    public string TakePassword()
    {
        var value = acceptedPassword;
        acceptedPassword = "";
        return value;
    }

    public VmwareConnectionDialog(Window owner, bool forConfiguration = false)
    {
        Owner = owner;
        Title = Localization.Text("Connection.Title");
        Width = 440;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new StackPanel { Margin = new(24) };
        System.Windows.Automation.AutomationProperties.SetAutomationId(port, "VmwareApiPort");
        System.Windows.Automation.AutomationProperties.SetAutomationId(user, "VmwareApiUser");
        System.Windows.Automation.AutomationProperties.SetAutomationId(password, "VmwareApiPassword");
        panel.Children.Add(new TextBlock { Text = Localization.Text(forConfiguration ? "Connection.UpdateDescription" : "Connection.Description"),
            TextWrapping = TextWrapping.Wrap, Margin = new(0, 0, 0, 16) });
        panel.Children.Add(new TextBlock { Text = Localization.Text("Connection.Port") });
        panel.Children.Add(port);
        panel.Children.Add(new TextBlock { Text = Localization.Text("Connection.User") });
        panel.Children.Add(user);
        panel.Children.Add(new TextBlock { Text = Localization.Text("Connection.Password") });
        panel.Children.Add(password);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var cancel = new Button { Content = Localization.Text("Action.CancelConnection"), IsCancel = true };
        var connect = new Button { Content = Localization.Text(forConfiguration ? "Action.ConnectAndApply" : "Action.ConnectVmware"), IsDefault = true };
        connect.Click += (_, _) =>
        {
            if (!int.TryParse(port.Text, NumberStyles.None, CultureInfo.InvariantCulture, out var number)
                || number is < 1 or > 65535 || string.IsNullOrWhiteSpace(UserName) || UserName.Contains(':') || password.Password.Length == 0)
            {
                MessageBox.Show(this, Localization.Text("Connection.Invalid"), Title);
                return;
            }
            Port = number;
            acceptedPassword = password.Password;
            DialogResult = true;
        };
        buttons.Children.Add(cancel);
        buttons.Children.Add(connect);
        panel.Children.Add(buttons);
        Content = panel;
        Closed += (_, _) => password.Clear();
    }
}
