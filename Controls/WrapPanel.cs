using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace AtomFinance.WinUI.Controls;

/// <summary>
/// Arranges child elements sequentially from left to right, wrapping to the next row
/// when the available horizontal space is exceeded.
/// </summary>
public sealed partial class WrapPanel : Panel
{
    public double HorizontalSpacing { get; set; } = 16;
    public double VerticalSpacing { get; set; } = 16;

    protected override Size MeasureOverride(Size availableSize)
    {
        double maxWidth = double.IsInfinity(availableSize.Width) ? double.MaxValue : availableSize.Width;
        double currentRowWidth = 0;
        double currentRowHeight = 0;
        double totalWidth = 0;
        double totalHeight = 0;

        var childConstraint = new Size(availableSize.Width, availableSize.Height);

        foreach (var child in Children)
        {
            child.Measure(childConstraint);
            Size desired = child.DesiredSize;

            double neededWidth = currentRowWidth > 0 ? currentRowWidth + HorizontalSpacing + desired.Width : desired.Width;
            if (currentRowWidth > 0 && neededWidth > maxWidth)
            {
                totalWidth = Math.Max(totalWidth, currentRowWidth);
                totalHeight += currentRowHeight + VerticalSpacing;
                currentRowWidth = desired.Width;
                currentRowHeight = desired.Height;
            }
            else
            {
                currentRowWidth = neededWidth;
                currentRowHeight = Math.Max(currentRowHeight, desired.Height);
            }
        }

        totalWidth = Math.Max(totalWidth, currentRowWidth);
        totalHeight += currentRowHeight;

        return new Size(
            double.IsInfinity(availableSize.Width) ? totalWidth : availableSize.Width,
            totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double x = 0;
        double y = 0;
        double currentRowHeight = 0;

        foreach (var child in Children)
        {
            Size desired = child.DesiredSize;
            if (x > 0 && x + desired.Width > finalSize.Width)
            {
                x = 0;
                y += currentRowHeight + VerticalSpacing;
                currentRowHeight = 0;
            }

            child.Arrange(new Rect(x, y, desired.Width, desired.Height));
            x += desired.Width + HorizontalSpacing;
            currentRowHeight = Math.Max(currentRowHeight, desired.Height);
        }

        return finalSize;
    }
}
