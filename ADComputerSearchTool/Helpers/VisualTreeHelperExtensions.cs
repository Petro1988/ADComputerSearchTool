using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace ADComputerSearchTool.Helpers;

public static class VisualTreeHelperExtensions
{
    public static T? FindParent<T>(
        this DependencyObject? child)
        where T : DependencyObject
    {
        DependencyObject? current =
            child;

        while (current != null)
        {
            if (current is T requestedParent)
            {
                return requestedParent;
            }

            if (current is Visual ||
                current is Visual3D)
            {
                current =
                    VisualTreeHelper.GetParent(
                        current);
            }
            else if (current is FrameworkContentElement contentElement)
            {
                current =
                    contentElement.Parent;
            }
            else
            {
                current = null;
            }
        }

        return null;
    }
}