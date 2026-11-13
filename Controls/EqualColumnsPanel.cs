using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace AtomFinance.WinUI.Controls;

/// <summary>
/// Arranges child elements into equal-width columns spanning the full available width.
/// </summary>
public sealed partial class EqualColumnsPanel : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        int count = Children.Count;
        if (count == 0)
            return new Size(0, 0);

        bool infiniteWidth = double.IsInfinity(availableSize.Width);
        double colWidth = infiniteWidth ? double.PositiveInfinity : availableSize.Width / count;

        double maxChildWidth = 0;
        double maxChildHeight = 0;
        var childConstraint = new Size(colWidth, availableSize.Height);

        foreach (var child in Children)
        {
            child.Measure(childConstraint);
            maxChildWidth = Math.Max(maxChildWidth, child.DesiredSize.Width);
            maxChildHeight = Math.Max(maxChildHeight, child.DesiredSize.Height);
        }

        double totalWidth = infiniteWidth ? maxChildWidth * count : availableSize.Width;
        double totalHeight = double.IsInfinity(availableSize.Height) ? maxChildHeight : availableSize.Height;
        return new Size(totalWidth, Math.Max(maxChildHeight, totalHeight));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        int count = Children.Count;
        if (count == 0)
            return finalSize;

        double colWidth = finalSize.Width / count;
        for (int i = 0; i < count; i++)
        {
            Children[i].Arrange(new Rect(i * colWidth, 0, colWidth, finalSize.Height));
        }

        return finalSize;
    }
}
