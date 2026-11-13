using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace AtomFinance.WinUI.Controls;

public enum SplitterTargetSide
{
    Previous,
    Next
}

/// <summary>
/// Draggable WinUI 3 split-pane divider that resizes an adjacent Grid ColumnDefinition or RowDefinition.
/// Double-tap resets the target pane to DefaultSize.
/// </summary>
public sealed partial class PaneSplitter : Grid
{
    private readonly Border _lineIndicator;
    private readonly Border _gripPill;

    private bool _isDragging;
    private bool _isPointerOver;
    private Point _dragStartPoint;
    private double _dragStartSize;

    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public static readonly DependencyProperty OrientationProperty =
        DependencyProperty.Register(
            nameof(Orientation),
            typeof(Orientation),
            typeof(PaneSplitter),
            new PropertyMetadata(Orientation.Vertical, OnLayoutPropertyChanged));

    public SplitterTargetSide TargetSide
    {
        get => (SplitterTargetSide)GetValue(TargetSideProperty);
        set => SetValue(TargetSideProperty, value);
    }

    public static readonly DependencyProperty TargetSideProperty =
        DependencyProperty.Register(
            nameof(TargetSide),
            typeof(SplitterTargetSide),
            typeof(PaneSplitter),
            new PropertyMetadata(SplitterTargetSide.Previous));

    public double MinTargetSize { get; set; } = 180;
    public double MaxTargetSize { get; set; } = 600;
    public double MinSiblingSize { get; set; } = 240;
    public double DefaultSize { get; set; } = 300;

    public event EventHandler<double>? SizeCommitted;

    public PaneSplitter()
    {
        Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        IsHitTestVisible = true;

        _lineIndicator = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        _gripPill = new Border
        {
            CornerRadius = new CornerRadius(2),
            Opacity = 0,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        Children.Add(_lineIndicator);
        Children.Add(_gripPill);

        Loaded += (_, _) =>
        {
            ApplyOrientationLayout();
            UpdateVisualState();
        };
        ActualThemeChanged += (_, _) => UpdateVisualState();
        PointerEntered += OnSplitterPointerEntered;
        PointerExited += OnSplitterPointerExited;
        PointerPressed += OnSplitterPointerPressed;
        PointerMoved += OnSplitterPointerMoved;
        PointerReleased += OnSplitterPointerReleased;
        PointerCaptureLost += OnSplitterPointerCaptureLost;
        DoubleTapped += OnSplitterDoubleTapped;

        ApplyOrientationLayout();
    }

    private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PaneSplitter splitter)
        {
            splitter.ApplyOrientationLayout();
        }
    }

    private void ApplyOrientationLayout()
    {
        if (Orientation == Orientation.Vertical)
        {
            Width = 7;
            Height = double.NaN;
            HorizontalAlignment = HorizontalAlignment.Center;
            VerticalAlignment = VerticalAlignment.Stretch;

            _lineIndicator.Width = 1;
            _lineIndicator.Height = double.NaN;
            _lineIndicator.HorizontalAlignment = HorizontalAlignment.Center;
            _lineIndicator.VerticalAlignment = VerticalAlignment.Stretch;

            _gripPill.Width = 4;
            _gripPill.Height = 36;

            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
        }
        else
        {
            Width = double.NaN;
            Height = 7;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Center;

            _lineIndicator.Width = double.NaN;
            _lineIndicator.Height = 1;
            _lineIndicator.HorizontalAlignment = HorizontalAlignment.Stretch;
            _lineIndicator.VerticalAlignment = VerticalAlignment.Center;

            _gripPill.Width = 36;
            _gripPill.Height = 4;

            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth);
        }
    }

    private void UpdateVisualState()
    {
        bool active = _isDragging || _isPointerOver;
        _lineIndicator.Background = ResolveThemeBrush(active ? "AccentPurpleBrush" : "BorderBrush");
        _gripPill.Background = ResolveThemeBrush("AccentPurpleBrush");
        _gripPill.Opacity = _isDragging ? 1.0 : (_isPointerOver ? 0.85 : 0.0);
    }

    private Brush? ResolveThemeBrush(string key)
    {
        string themeKey = ActualTheme == ElementTheme.Light ? "Light" : "Default";
        foreach (var dict in Application.Current.Resources.MergedDictionaries)
        {
            if (dict.ThemeDictionaries.TryGetValue(themeKey, out object? themeDictObj) &&
                themeDictObj is ResourceDictionary themeDict &&
                themeDict.TryGetValue(key, out object? val) &&
                val is Brush brush)
            {
                return brush;
            }
        }

        return Application.Current.Resources.TryGetValue(key, out object? fallback) && fallback is Brush fb
            ? fb
            : null;
    }

    private void OnSplitterPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _isPointerOver = true;
        UpdateVisualState();
    }

    private void OnSplitterPointerExited(object sender, PointerRoutedEventArgs e)
    {
        _isPointerOver = false;
        UpdateVisualState();
    }

    private void OnSplitterPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (Parent is not Grid parentGrid)
            return;

        if (!TryGetTargetSize(parentGrid, out double currentSize))
            return;

        if (CapturePointer(e.Pointer))
        {
            _isDragging = true;
            _dragStartPoint = e.GetCurrentPoint(parentGrid).Position;
            _dragStartSize = currentSize;
            UpdateVisualState();
            e.Handled = true;
        }
    }

    private void OnSplitterPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDragging || Parent is not Grid parentGrid)
            return;

        Point current = e.GetCurrentPoint(parentGrid).Position;
        double delta = Orientation == Orientation.Vertical
            ? current.X - _dragStartPoint.X
            : current.Y - _dragStartPoint.Y;

        double signedDelta = TargetSide == SplitterTargetSide.Previous ? delta : -delta;
        double desired = _dragStartSize + signedDelta;

        ApplyClampedSize(parentGrid, desired);
        e.Handled = true;
    }

    private void OnSplitterPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDragging)
            return;

        EndDrag(e.Pointer);
        e.Handled = true;
    }

    private void OnSplitterPointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        if (_isDragging)
        {
            EndDrag(null);
        }
    }

    private void OnSplitterDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (Parent is not Grid parentGrid)
            return;

        double applied = ApplyClampedSize(parentGrid, DefaultSize);
        SizeCommitted?.Invoke(this, applied);
        e.Handled = true;
    }

    private void EndDrag(Pointer? pointer)
    {
        _isDragging = false;
        if (pointer != null)
        {
            ReleasePointerCapture(pointer);
        }
        UpdateVisualState();

        if (Parent is Grid parentGrid && TryGetTargetSize(parentGrid, out double finalSize))
        {
            SizeCommitted?.Invoke(this, finalSize);
        }
    }

    private bool TryGetTargetSize(Grid parentGrid, out double size)
    {
        size = 0;
        if (Orientation == Orientation.Vertical)
        {
            int col = Grid.GetColumn(this);
            int targetIdx = TargetSide == SplitterTargetSide.Previous ? col - 1 : col + 1;
            if (targetIdx < 0 || targetIdx >= parentGrid.ColumnDefinitions.Count)
                return false;

            var def = parentGrid.ColumnDefinitions[targetIdx];
            size = def.Width.IsAbsolute ? def.Width.Value : def.ActualWidth;
            return true;
        }
        else
        {
            int row = Grid.GetRow(this);
            int targetIdx = TargetSide == SplitterTargetSide.Previous ? row - 1 : row + 1;
            if (targetIdx < 0 || targetIdx >= parentGrid.RowDefinitions.Count)
                return false;

            var def = parentGrid.RowDefinitions[targetIdx];
            size = def.Height.IsAbsolute ? def.Height.Value : def.ActualHeight;
            return true;
        }
    }

    private double ApplyClampedSize(Grid parentGrid, double desiredSize)
    {
        if (Orientation == Orientation.Vertical)
        {
            int col = Grid.GetColumn(this);
            int targetIdx = TargetSide == SplitterTargetSide.Previous ? col - 1 : col + 1;
            if (targetIdx < 0 || targetIdx >= parentGrid.ColumnDefinitions.Count)
                return desiredSize;

            double otherFixed = 0;
            for (int i = 0; i < parentGrid.ColumnDefinitions.Count; i++)
            {
                if (i == targetIdx) continue;
                var d = parentGrid.ColumnDefinitions[i];
                if (!d.Width.IsStar)
                    otherFixed += d.ActualWidth;
            }

            double maxByParent = parentGrid.ActualWidth > 0
                ? parentGrid.ActualWidth - otherFixed - MinSiblingSize
                : MaxTargetSize;
            double effectiveMax = Math.Min(MaxTargetSize, Math.Max(MinTargetSize, maxByParent));
            double clamped = Math.Clamp(desiredSize, MinTargetSize, effectiveMax);

            parentGrid.ColumnDefinitions[targetIdx].Width = new GridLength(clamped, GridUnitType.Pixel);
            return clamped;
        }
        else
        {
            int row = Grid.GetRow(this);
            int targetIdx = TargetSide == SplitterTargetSide.Previous ? row - 1 : row + 1;
            if (targetIdx < 0 || targetIdx >= parentGrid.RowDefinitions.Count)
                return desiredSize;

            double otherFixed = 0;
            for (int i = 0; i < parentGrid.RowDefinitions.Count; i++)
            {
                if (i == targetIdx) continue;
                var d = parentGrid.RowDefinitions[i];
                if (!d.Height.IsStar)
                    otherFixed += d.ActualHeight;
            }

            double maxByParent = parentGrid.ActualHeight > 0
                ? parentGrid.ActualHeight - otherFixed - MinSiblingSize
                : MaxTargetSize;
            double effectiveMax = Math.Min(MaxTargetSize, Math.Max(MinTargetSize, maxByParent));
            double clamped = Math.Clamp(desiredSize, MinTargetSize, effectiveMax);

            parentGrid.RowDefinitions[targetIdx].Height = new GridLength(clamped, GridUnitType.Pixel);
            return clamped;
        }
    }
}
