using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace InstallToolbox;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private Point _dragStartPoint;

    private void Preset_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
    }

    private void Preset_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && sender is FrameworkElement element)
        {
            Point currentPos = e.GetPosition(null);
            if (System.Math.Abs(currentPos.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                System.Math.Abs(currentPos.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                var data = element.DataContext as InstallToolbox.Models.AppPreset;
                if (data != null)
                {
                    DragDrop.DoDragDrop(element, data, DragDropEffects.Move);
                }
            }
        }
    }

    private void Preset_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(typeof(InstallToolbox.Models.AppPreset)) is InstallToolbox.Models.AppPreset sourcePreset)
        {
            if (sender is FrameworkElement targetElement && targetElement.DataContext is InstallToolbox.Models.AppPreset targetPreset)
            {
                var vm = DataContext as InstallToolbox.ViewModels.MainViewModel;
                if (vm != null && sourcePreset != targetPreset)
                {
                    int sourceIndex = vm.Presets.IndexOf(sourcePreset);
                    int targetIndex = vm.Presets.IndexOf(targetPreset);
                    if (sourceIndex >= 0 && targetIndex >= 0)
                    {
                        vm.Presets.Move(sourceIndex, targetIndex);
                        vm.SavePresetOrder();
                    }
                }
            }
        }
    }
}