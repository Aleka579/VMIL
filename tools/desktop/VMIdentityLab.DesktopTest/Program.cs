using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Windows.Automation;

// Development-only driver, using Windows UI Automation and standard native dialog notifications.
// Each operation targets a supplied process. This tool is excluded from the application package.
internal static class Program
{
    [DllImport("user32.dll")] private static extern nint GetParent(nint window);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassNameW(nint window, StringBuilder text, int size);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SendMessageTimeoutW(nint window, uint message, nuint wParam, nint lParam,
        uint flags, uint timeout, out nuint result);
    [DllImport("user32.dll")] private static extern bool ShowWindowAsync(nint window, int command);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(nint window, nint after, int x, int y, int width, int height, uint flags);

    private static int Main(string[] args)
    {
        try
        {
            if (args.Length < 2) throw new ArgumentException("action processId [name/id/output] [value]");
            var pid = int.Parse(args[1]);
            var windows = Windows(pid);
            if (windows.Length == 0) throw new InvalidOperationException("No desktop window for this process.");
            var elements = Elements(windows);
            var candidates = elements.Where(element => !element.Current.IsOffscreen).ToArray();
            AutomationElement Invokable(string name) => Unique(candidates.Where(element => element.Current.Name == name
                && element.TryGetCurrentPattern(InvokePattern.Pattern, out _)));
            switch (args[0])
            {
                case "inspect":
                    var data = elements.Select(element => new
                    {
                        name = element.Current.Name, id = element.Current.AutomationId,
                        type = element.Current.ControlType.ProgrammaticName,
                        handle = element.Current.NativeWindowHandle,
                        gridRows = element.TryGetCurrentPattern(GridPattern.Pattern, out var grid)
                            ? ((GridPattern)grid).Current.RowCount : (int?)null,
                        enabled = element.Current.IsEnabled, offscreen = element.Current.IsOffscreen,
                        value = !element.Current.IsPassword && element.TryGetCurrentPattern(ValuePattern.Pattern, out var pattern)
                            ? ((ValuePattern)pattern).Current.Value : null
                    }).ToArray();
                    if (args.Length > 2) Save(args[2], JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
                    else Console.WriteLine(JsonSerializer.Serialize(data));
                    break;
                case "show-window":
                    var visibleWindow = Unique(candidates.Where(window => window.Current.Name == args[2]
                        && window.Current.ControlType == ControlType.Window && window.Current.NativeWindowHandle != 0));
                    var visibleHandle = visibleWindow.Current.NativeWindowHandle;
                    GetWindowThreadProcessId(visibleHandle, out var visibleProcess);
                    if (visibleHandle == 0 || visibleProcess != pid) throw new InvalidOperationException("Window ownership could not be verified.");
                    var bounds = visibleWindow.Current.BoundingRectangle;
                    var area = System.Windows.SystemParameters.WorkArea;
                    ShowWindowAsync(visibleHandle, 9); // SW_RESTORE
                    if (!SetWindowPos(visibleHandle, -1, (int)Math.Max(area.Left, area.Left + (area.Width - bounds.Width) / 2),
                        (int)Math.Max(area.Top, area.Top + (area.Height - bounds.Height) / 2), 0, 0, 0x41)) // NOSIZE | SHOWWINDOW
                        throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                    visibleWindow.SetFocus();
                    break;
                case "focus-window":
                    Unique(windows.Where(window => window.Current.Name == args[2])).SetFocus();
                    break;
                case "invoke": Invoke(Invokable(args[2])); break;
                case "check":
                    var checkbox = Unique(candidates.Where(element => element.Current.AutomationId == args[2]
                        && element.Current.ControlType == ControlType.CheckBox
                        && element.TryGetCurrentPattern(TogglePattern.Pattern, out _)));
                    if (!checkbox.Current.IsEnabled) throw new ElementNotEnabledException();
                    var toggle = (TogglePattern)checkbox.GetCurrentPattern(TogglePattern.Pattern);
                    if (toggle.Current.ToggleState == ToggleState.Off) toggle.Toggle();
                    if (toggle.Current.ToggleState != ToggleState.On) throw new InvalidOperationException("Acknowledgement is not checked.");
                    break;
                case "cancel-collection":
                    var collect = Invokable(args[2]);
                    var cancel = Invokable(args[3]);
                    Invoke(collect);
                    Wait(() => cancel.Current.IsEnabled, 1000);
                    Invoke(cancel);
                    break;
                case "native-command":
                    var nativeButton = Unique(candidates.Where(element => element.Current.AutomationId == args[2]
                        && element.Current.NativeWindowHandle != 0));
                    if (!nativeButton.Current.IsEnabled) throw new ElementNotEnabledException();
                    var nativeClass = new StringBuilder(128);
                    GetClassNameW(nativeButton.Current.NativeWindowHandle, nativeClass, 128);
                    if (nativeClass.ToString() != "Button") throw new InvalidOperationException("Target is not a native button.");
                    var parent = GetParent(nativeButton.Current.NativeWindowHandle);
                    GetWindowThreadProcessId(parent, out var parentProcess);
                    if (parentProcess != pid) throw new InvalidOperationException("Dialog belongs to another process.");
                    // WM_COMMAND / BN_CLICKED targets the real dialog without requiring foreground BM_CLICK.
                    if (SendMessageTimeoutW(parent, 0x111, ushort.Parse(args[2]), nativeButton.Current.NativeWindowHandle, 2, 3000, out _) == 0)
                        throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                    break;
                case "select":
                    var item = Unique(elements.Where(element => element.Current.Name == args[2]
                        && element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out _)));
                    if (item.Current.IsOffscreen)
                    {
                        if (!item.TryGetCurrentPattern(ScrollItemPattern.Pattern, out var scrollItem))
                            throw new InvalidOperationException("Offscreen item cannot be scrolled into view.");
                        ((ScrollItemPattern)scrollItem).ScrollIntoView();
                    }
                    var selectedItem = (SelectionItemPattern)item.GetCurrentPattern(SelectionItemPattern.Pattern);
                    selectedItem.Select();
                    if (!selectedItem.Current.IsSelected) throw new InvalidOperationException("Item selection did not persist.");
                    break;
                case "select-option":
                    var optionCombo = Unique(candidates.Where(element => element.Current.AutomationId == args[2]
                        && element.Current.ControlType == ControlType.ComboBox));
                    var expansion = (ExpandCollapsePattern)optionCombo.GetCurrentPattern(ExpandCollapsePattern.Pattern);
                    expansion.Expand();
                    AutomationElement[] Options() => Elements(Windows(pid)).Where(element => !element.Current.IsOffscreen
                        && element.Current.Name == args[3]
                        && element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out _)).ToArray();
                    Wait(() => Options().Length > 0);
                    var option = Unique(Options());
                    var selection = (SelectionItemPattern)option.GetCurrentPattern(SelectionItemPattern.Pattern);
                    selection.Select();
                    if (!selection.Current.IsSelected) throw new InvalidOperationException("Option selection did not persist.");
                    expansion.Collapse();
                    break;
                case "expand":
                    var combo = Unique(candidates.Where(element => element.Current.AutomationId == args[2]
                        && element.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out _)));
                    ((ExpandCollapsePattern)combo.GetCurrentPattern(ExpandCollapsePattern.Pattern)).Expand();
                    break;
                case "value":
                case "typed-value":
                    var edit = Unique(candidates.Where(element => element.Current.AutomationId == args[2]
                        && element.Current.ControlType == ControlType.Edit
                        && element.TryGetCurrentPattern(ValuePattern.Pattern, out _)));
                    if (edit.Current.IsPassword) throw new InvalidOperationException("Password fields are excluded.");
                    var value = (ValuePattern)edit.GetCurrentPattern(ValuePattern.Pattern);
                    if (args[0] == "typed-value")
                    {
                        var editWindow = edit.Current.NativeWindowHandle;
                        var editClass = new StringBuilder(128);
                        GetClassNameW(editWindow, editClass, 128);
                        GetWindowThreadProcessId(editWindow, out var editProcess);
                        if (editClass.ToString() != "Edit" || editProcess != pid)
                            throw new InvalidOperationException("Target is not this process's native filename editor.");
                        value.SetValue("");
                        foreach (var character in args[3])
                            if (SendMessageTimeoutW(editWindow, 0x102, character, 1, 2, 1000, out _) == 0)
                                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                    }
                    else value.SetValue(args[3]);
                    if (value.Current.Value != args[3]) throw new InvalidOperationException("Input did not match requested text.");
                    break;
                case "wait-dialog":
                    Wait(() => Elements(Windows(pid)).Any(element => !element.Current.IsOffscreen
                        && element.Current.ControlType == ControlType.Edit
                        && element.Current.AutomationId == (args.Length > 2 ? args[2] : "1001")));
                    break;
                case "wait-ready":
                    Wait(() => Elements(Windows(pid)).Any(element => element.Current.Name == args[2]
                        && element.Current.ControlType == ControlType.Button && element.Current.IsEnabled));
                    break;
                case "wait-id":
                    Wait(() => Elements(Windows(pid)).Any(element => element.Current.AutomationId == args[2]
                        && !element.Current.IsOffscreen && element.Current.IsEnabled));
                    break;
                case "close": ((WindowPattern)windows[0].GetCurrentPattern(WindowPattern.Pattern)).Close(); break;
                default: throw new ArgumentException("Unsupported action.");
            }
            using var identity = WindowsIdentity.GetCurrent();
            Record(new { action = args[0], processId = pid, arguments = args.Skip(2).ToArray(), status = "completed",
                elevated = new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator),
                time = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8)) });
            return 0;
        }
        catch (Exception error)
        {
            Record(new { status = "failed", type = error.GetType().Name, message = error.Message,
                arguments = args, time = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8)) });
            return 1;
        }
    }

    private static AutomationElement[] Windows(int pid) => AutomationElement.RootElement.FindAll(TreeScope.Children,
        new PropertyCondition(AutomationElement.ProcessIdProperty, pid)).Cast<AutomationElement>().ToArray();

    private static AutomationElement[] Elements(AutomationElement[] windows) => windows.SelectMany(window => new[] { window }
        .Concat(window.FindAll(TreeScope.Descendants, Condition.TrueCondition).Cast<AutomationElement>())).ToArray();

    private static AutomationElement Unique(IEnumerable<AutomationElement> elements)
    {
        var matches = elements.ToArray();
        return matches.Length == 1 ? matches[0] : throw new InvalidOperationException($"Expected one target; found {matches.Length}.");
    }

    private static void Invoke(AutomationElement element)
    {
        if (!element.Current.IsEnabled) throw new ElementNotEnabledException();
        ((InvokePattern)element.GetCurrentPattern(InvokePattern.Pattern)).Invoke();
    }

    private static void Wait(Func<bool> ready, int milliseconds = 10000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(milliseconds);
        while (!ready())
        {
            if (DateTime.UtcNow >= deadline) throw new TimeoutException("Expected UI state did not appear.");
            Thread.Sleep(100);
        }
    }

    private static void Record(object value)
    {
        var json = JsonSerializer.Serialize(value);
        Console.WriteLine(json);
        var log = Environment.GetEnvironmentVariable("VMIDENTITYLAB_TEST_LOG");
        if (!string.IsNullOrWhiteSpace(log)) File.AppendAllText(log, json + Environment.NewLine);
    }

    private static void Save(string path, string contents)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
        using var writer = new StreamWriter(file);
        writer.Write(contents);
    }
}
