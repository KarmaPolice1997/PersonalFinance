using AtomFinance.WinUI.Models;
using AtomFinance.WinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace AtomFinance.WinUI.Views;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel { get; }
    private DailyActivityItem? _hoveredDay;

    public DashboardPage()
    {
        InitializeComponent();
        NavigationCacheMode = NavigationCacheMode.Required;

        ViewModel = new DashboardViewModel(App.FinanceService, App.ShellViewModel);
        ApplySavedLayoutDimensions();

        ViewModel.WeeklyActivityUpdated += (_, _) =>
        {
            RenderWeeklyChart();
            UpdateGaugeArcs();
        };
        ViewModel.LayoutResetRequested += (_, _) => ApplySavedLayoutDimensions();
        ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(DashboardViewModel.LimitPercentage))
            {
                UpdateGaugeArcs();
            }
        };

        MainVerticalSplitter.SizeCommitted += (_, size) => ViewModel.SaveRightPanelWidth(size);
        TopRowVerticalSplitter.SizeCommitted += (_, size) => ViewModel.SaveBalanceCardWidth(size);
        CenterHorizontalSplitter.SizeCommitted += (_, size) => ViewModel.SaveTopSectionHeight(size);

        Loaded += (_, _) =>
        {
            RenderWeeklyChart();
            UpdateGaugeArcs();
        };
        ActualThemeChanged += (_, _) => RenderWeeklyChart();
        WeeklyPlotArea.SizeChanged += OnWeeklyPlotAreaSizeChanged;
    }

    private void ApplySavedLayoutDimensions()
    {
        RightPanelColumn.Width = new GridLength(ViewModel.RightPanelWidth, GridUnitType.Pixel);
        BalanceColumn.Width = new GridLength(ViewModel.BalanceCardWidth, GridUnitType.Pixel);
        TopSectionRow.Height = new GridLength(ViewModel.TopSectionHeight, GridUnitType.Pixel);
    }

    private void OnWeeklyPlotAreaSizeChanged(object sender, SizeChangedEventArgs e)
    {
        ViewModel.UpdateChartPlotHeight(e.NewSize.Height);
        RenderWeeklyChart();
    }

    private void RenderWeeklyChart()
    {
        double width = WeeklyPlotArea.ActualWidth;
        double height = WeeklyPlotArea.ActualHeight;
        var items = ViewModel.WeeklyActivities;

        if (width <= 0 || height <= 0 || items.Count == 0)
        {
            WeeklySplinePath.Data = null;
            WeeklyAreaPath.Data = null;
            WeeklyDotsCanvas.Children.Clear();
            return;
        }

        const double topPad = 14.0;
        double baselineY = Math.Max(topPad + 16.0, height - 2.0);
        double usableHeight = baselineY - topPad;
        int count = items.Count;
        double colWidth = width / count;

        var points = new Point[count];
        for (int i = 0; i < count; i++)
        {
            double x = (i + 0.5) * colWidth;
            double y = baselineY - (items[i].NormalizedValue * usableHeight);
            points[i] = new Point(x, y);
        }

        // Smooth cubic Bezier spline (horizontal tangents prevent vertical overshoot)
        var splineFigure = new PathFigure { StartPoint = points[0], IsClosed = false };
        var areaFigure = new PathFigure
        {
            StartPoint = new Point(points[0].X, baselineY),
            IsClosed = true
        };
        areaFigure.Segments.Add(new LineSegment { Point = points[0] });

        for (int i = 0; i < count - 1; i++)
        {
            double midX = (points[i].X + points[i + 1].X) / 2.0;
            var cp1 = new Point(midX, points[i].Y);
            var cp2 = new Point(midX, points[i + 1].Y);

            splineFigure.Segments.Add(new BezierSegment
            {
                Point1 = cp1,
                Point2 = cp2,
                Point3 = points[i + 1]
            });

            areaFigure.Segments.Add(new BezierSegment
            {
                Point1 = cp1,
                Point2 = cp2,
                Point3 = points[i + 1]
            });
        }

        areaFigure.Segments.Add(new LineSegment { Point = new Point(points[count - 1].X, baselineY) });

        var splineGeometry = new PathGeometry();
        splineGeometry.Figures.Add(splineFigure);
        WeeklySplinePath.Data = splineGeometry;

        var areaGeometry = new PathGeometry();
        areaGeometry.Figures.Add(areaFigure);
        WeeklyAreaPath.Data = areaGeometry;

        // Render aligned point markers
        WeeklyDotsCanvas.Children.Clear();
        Brush? lineBrush = ResolveThemeBrush("ChartLineBrush");
        Brush? mintBrush = ResolveThemeBrush("AccentMintBrush");
        Brush? surfaceBrush = ResolveThemeBrush("SurfaceCardBrush");

        for (int i = 0; i < count; i++)
        {
            var item = items[i];
            bool isHighlighted = ReferenceEquals(item, _hoveredDay) || (_hoveredDay == null && item.IsToday);
            double dotSize = isHighlighted ? 10.0 : 6.0;

            var dot = new Ellipse
            {
                Width = dotSize,
                Height = dotSize,
                Fill = isHighlighted ? (surfaceBrush ?? lineBrush) : lineBrush,
                Stroke = ReferenceEquals(item, _hoveredDay) || item.IsToday ? (mintBrush ?? lineBrush) : lineBrush,
                StrokeThickness = isHighlighted ? 2.5 : 0
            };

            Canvas.SetLeft(dot, points[i].X - dotSize / 2.0);
            Canvas.SetTop(dot, points[i].Y - dotSize / 2.0);
            WeeklyDotsCanvas.Children.Add(dot);
        }
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

    private void OnActivityColumnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is DailyActivityItem item)
        {
            _hoveredDay = item;
            ViewModel.SetHoveredDay(item);
            RenderWeeklyChart();
        }
    }

    private void OnActivityColumnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        _hoveredDay = null;
        ViewModel.SetHoveredDay(null);
        RenderWeeklyChart();
    }

    private void OnSearchBoxKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            ViewModel.GoToTransactions();
        }
    }

    private void UpdateGaugeArcs()
    {
        const double centerX = 105.0;
        const double centerY = 110.0;
        const double radius = 85.0;

        double ratio = Math.Clamp(ViewModel.LimitPercentage / 100.0, 0.0, 1.0);
        double splitAngle = Math.PI * (1.0 - ratio);

        var startLeft = new Point(centerX - radius, centerY);
        var endRight = new Point(centerX + radius, centerY);
        var splitPoint = new Point(
            centerX + radius * Math.Cos(splitAngle),
            centerY - radius * Math.Sin(splitAngle));

        GaugeHealthArc.Data = ratio <= 0.005
            ? null
            : BuildArcGeometry(startLeft, ratio >= 0.995 ? endRight : splitPoint, radius);

        GaugeTargetArc.Data = ratio >= 0.995
            ? null
            : BuildArcGeometry(ratio <= 0.005 ? startLeft : splitPoint, endRight, radius);
    }

    private static PathGeometry BuildArcGeometry(Point start, Point end, double radius)
    {
        var figure = new PathFigure
        {
            StartPoint = start,
            IsClosed = false
        };
        figure.Segments.Add(new ArcSegment
        {
            Point = end,
            Size = new Size(radius, radius),
            SweepDirection = SweepDirection.Clockwise,
            IsLargeArc = false
        });

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        return geometry;
    }
}
