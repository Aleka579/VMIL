using System.Windows;
using System.Windows.Controls;
using VMIdentityLab.VMware.Configuration;

namespace VMIdentityLab.App.Infrastructure;

internal sealed class ConfigConfirmationDialog : Window
{
    public ConfigConfirmationDialog(Window owner, VmxPreparedChange preview, bool restoring, bool approved)
    {
        Owner = owner;
        Title = Localization.Text(restoring ? "Config.RestoreTitle" : "Config.ConfirmTitle");
        Width = 660;
        SizeToContent = SizeToContent.Height;
        MaxHeight = SystemParameters.WorkArea.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new StackPanel { Margin = new(24) };
        panel.Children.Add(new TextBlock { Text = Title, FontSize = 20, FontWeight = FontWeights.SemiBold });
        panel.Children.Add(new TextBox { Text = preview.TargetPath, IsReadOnly = true, TextWrapping = TextWrapping.Wrap,
            Margin = new(0, 12, 0, 12) });
        var differences = new StackPanel();
        void RefreshDifferences(bool reveal)
        {
            differences.Children.Clear();
            foreach (var change in preview.Plan.Changes)
            {
                differences.Children.Add(new TextBlock { Text = $"{change.Key}: {ConfigPresentation.Value(change, false, reveal)} → {ConfigPresentation.Value(change, true, reveal)}",
                    FontWeight = FontWeights.SemiBold, Margin = new(0, 8, 0, 4), TextWrapping = TextWrapping.Wrap });
                differences.Children.Add(new TextBlock { Text = ConfigPresentation.Impact(change), TextWrapping = TextWrapping.Wrap });
            }
        }
        RefreshDifferences(false);
        panel.Children.Add(differences);
        if (preview.Plan.IsIdentityChange)
        {
            var reveal = new CheckBox { Content = Localization.Text("IdentityConfig.Reveal"), Margin = new(0, 12, 0, 8) };
            System.Windows.Automation.AutomationProperties.SetAutomationId(reveal, "RevealIdentityConfig");
            reveal.Checked += (_, _) => RefreshDifferences(true);
            reveal.Unchecked += (_, _) => RefreshDifferences(false);
            panel.Children.Add(reveal);
            panel.Children.Add(new TextBlock { Text = Localization.Text("IdentityConfig.Privacy"), TextWrapping = TextWrapping.Wrap });
        }
        panel.Children.Add(new TextBlock { Text = Localization.Text(approved ? "Config.BackupExplanation" : "Config.ReviewOnly"),
            TextWrapping = TextWrapping.Wrap, Margin = new(0, 18, 0, 12) });
        var acknowledge = new CheckBox { Content = Localization.Text("Config.Acknowledge"), IsEnabled = approved };
        System.Windows.Automation.AutomationProperties.SetAutomationId(acknowledge, "ConfigAcknowledgement");
        var commit = new Button { Content = Localization.Text(restoring ? "Config.CommitRestore" : "Config.Commit"), IsEnabled = false };
        System.Windows.Automation.AutomationProperties.SetAutomationId(commit, "ConfirmConfigCommit");
        acknowledge.Checked += (_, _) => commit.IsEnabled = approved;
        acknowledge.Unchecked += (_, _) => commit.IsEnabled = false;
        commit.Click += (_, _) => { if (approved && acknowledge.IsChecked == true) DialogResult = true; };
        var cancel = new Button { Content = Localization.Text("Config.Return"), IsCancel = true };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new(0, 18, 0, 0) };
        buttons.Children.Add(cancel);
        buttons.Children.Add(commit);
        panel.Children.Add(acknowledge);
        panel.Children.Add(buttons);
        Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    }
}
