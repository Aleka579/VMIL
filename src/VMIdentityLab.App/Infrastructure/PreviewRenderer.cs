using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using VMIdentityLab.App.ViewModels;
using VMIdentityLab.Core.Reports;

namespace VMIdentityLab.App.Infrastructure;

internal static class PreviewRenderer
{
    private const int Width = 1220, Height = 800;

    public static async Task RenderAsync(string path, bool english, int page, bool identity = false)
    {
        Localization.Set(english);
        var model = new MainViewModel();
        var window = new MainWindow(model);
        model.LoadSyntheticPreview();
        model.SelectedPage = page;
        if (identity) ((TabItem)window.FindName("IdentityAnalysisTab")).IsSelected = true;

        // Keep Loaded/template/virtualization behavior, without showing a preview on the desktop.
        // Removing the non-client frame makes the content exactly Width x Height DIPs.
        window.WindowStyle = WindowStyle.None;
        window.ResizeMode = ResizeMode.NoResize;
        // WPF also limits content by Win32's maximum tracking size. The preview
        // must retain its full canvas even on desktops smaller than the image.
        window.MinWidth = Width;
        window.MinHeight = Height;
        window.ShowInTaskbar = false;
        window.ShowActivated = false;
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Left = SystemParameters.VirtualScreenLeft - Width - 100;
        window.Top = SystemParameters.VirtualScreenTop - Height - 100;
        try
        {
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            window.ContentRendered += (_, _) => ready.TrySetResult();
            window.Show();
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var bytes = await window.Dispatcher.InvokeAsync(() =>
            {
                var root = window.PreviewContent;
                root.UpdateLayout();
                if (Math.Abs(root.ActualWidth - Width) > 0.01 || Math.Abs(root.ActualHeight - Height) > 0.01)
                    throw new InvalidOperationException("Preview content does not match the requested canvas size.");
                var bitmap = new RenderTargetBitmap(Width, Height, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(root);
                var encoder = new PngBitmapEncoder();
                // Previews have an opaque background. Export RGB instead of retaining
                // incidental alpha from premultiplied text antialiasing.
                encoder.Frames.Add(BitmapFrame.Create(new FormatConvertedBitmap(bitmap, PixelFormats.Bgr24, null, 0)));
                using var stream = new MemoryStream();
                encoder.Save(stream);
                return stream.ToArray();
            }, DispatcherPriority.ApplicationIdle);
            await ReportFiles.SaveBytesAsync(path, bytes);
        }
        finally
        {
            window.Close();
        }
    }
}
