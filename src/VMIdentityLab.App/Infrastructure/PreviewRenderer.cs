using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using VMIdentityLab.App.ViewModels;
using VMIdentityLab.Core.Reports;

namespace VMIdentityLab.App.Infrastructure;

internal static class PreviewRenderer
{
    public static async Task RenderAsync(string path, bool english, int page)
    {
        Localization.Set(english);
        var model = new MainViewModel();
        var window = new MainWindow(model);
        model.LoadSyntheticPreview();
        model.SelectedPage = page;
        // A hidden presentation source lets DataGrid finish its Loaded layout.
        window.ShowInTaskbar = false;
        window.ShowActivated = false;
        window.Opacity = 0;
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Left = -10000;
        window.Top = -10000;
        window.Show();
        var bytes = await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var root = window.PreviewContent;
            root.Measure(new Size(1220, 800));
            root.Arrange(new Rect(0, 0, 1220, 800));
            root.UpdateLayout();
            var bitmap = new RenderTargetBitmap(1220, 800, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(root);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = new MemoryStream();
            encoder.Save(stream);
            return stream.ToArray();
        }, DispatcherPriority.ApplicationIdle);
        await ReportFiles.SaveBytesAsync(path, bytes);
        window.Close();
    }
}
