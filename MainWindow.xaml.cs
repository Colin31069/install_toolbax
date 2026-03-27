using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using InstallToolbox.Models;
using InstallToolbox.ViewModels;

namespace InstallToolbox;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
{
    private Point _dragStartPoint;
    private bool _isDraggingPreset;
    private bool _suppressPresetClick;

    public MainWindow()
    {
        InitializeComponent();
    }

    private bool CanReorderPresets => ExpandToggleBtn.IsChecked == true;

    private void Preset_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element || element.DataContext is not AppPreset)
        {
            return;
        }

        _dragStartPoint = e.GetPosition(this);
        _suppressPresetClick = false;
    }

    private void Preset_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!CanReorderPresets ||
            _isDraggingPreset ||
            e.LeftButton != MouseButtonState.Pressed ||
            sender is not FrameworkElement element ||
            element.DataContext is not AppPreset preset)
        {
            return;
        }

        Point currentPos = e.GetPosition(this);
        bool movedEnough =
            System.Math.Abs(currentPos.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
            System.Math.Abs(currentPos.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance;

        if (!movedEnough)
        {
            return;
        }

        _isDraggingPreset = true;
        _suppressPresetClick = true;

        try
        {
            DragDrop.DoDragDrop(element, new DataObject(typeof(AppPreset), preset), DragDropEffects.Move);
        }
        finally
        {
            _isDraggingPreset = false;
            Dispatcher.BeginInvoke(
                () => _suppressPresetClick = false,
                DispatcherPriority.ContextIdle);
        }

        e.Handled = true;
    }

    private void Preset_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_suppressPresetClick)
        {
            return;
        }

        e.Handled = true;
        _suppressPresetClick = false;
    }

    private void Preset_Click(object sender, RoutedEventArgs e)
    {
        if (_suppressPresetClick)
        {
            e.Handled = true;
            _suppressPresetClick = false;
            return;
        }

        if (sender is not FrameworkElement element ||
            element.DataContext is not AppPreset preset ||
            DataContext is not MainViewModel viewModel)
        {
            return;
        }

        if (viewModel.ApplyPresetCommand.CanExecute(preset))
        {
            viewModel.ApplyPresetCommand.Execute(preset);
        }
    }

    private void Preset_DragOver(object sender, DragEventArgs e)
    {
        if (!CanReorderPresets ||
            !TryGetPresetFromDragEvent(e, out var sourcePreset) ||
            sender is not FrameworkElement targetElement ||
            targetElement.DataContext is not AppPreset targetPreset ||
            sourcePreset == targetPreset)
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void Preset_Drop(object sender, DragEventArgs e)
    {
        if (!CanReorderPresets ||
            !TryGetPresetFromDragEvent(e, out var sourcePreset) ||
            sender is not FrameworkElement targetElement ||
            targetElement.DataContext is not AppPreset targetPreset ||
            sourcePreset == targetPreset ||
            DataContext is not MainViewModel viewModel)
        {
            e.Handled = true;
            return;
        }

        int sourceIndex = viewModel.Presets.IndexOf(sourcePreset);
        int targetIndex = viewModel.Presets.IndexOf(targetPreset);

        if (sourceIndex >= 0 && targetIndex >= 0 && sourceIndex != targetIndex)
        {
            viewModel.Presets.Move(sourceIndex, targetIndex);
            viewModel.SavePresetOrder();
        }

        e.Handled = true;
    }

    private static bool TryGetPresetFromDragEvent(DragEventArgs e, out AppPreset preset)
    {
        if (e.Data.GetData(typeof(AppPreset)) is AppPreset draggedPreset)
        {
            preset = draggedPreset;
            return true;
        }

        preset = null!;
        return false;
    }
}
