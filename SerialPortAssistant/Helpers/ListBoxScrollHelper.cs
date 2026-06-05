using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace SerialPortAssistant.Helpers;

public static class ListBoxScrollHelper
{
  public static void ScrollToLatest(ListBox listBox)
  {
    if (listBox.Items.Count == 0)
      return;

    listBox.Dispatcher.BeginInvoke(() =>
    {
      if (listBox.Items.Count == 0)
        return;

      listBox.UpdateLayout();
      var lastItem = listBox.Items[^1];
      listBox.ScrollIntoView(lastItem);

      if (FindScrollViewer(listBox) is { } scrollViewer)
        scrollViewer.ScrollToEnd();
    }, DispatcherPriority.Loaded);
  }

  private static ScrollViewer? FindScrollViewer(DependencyObject root)
  {
    for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
    {
      var child = VisualTreeHelper.GetChild(root, i);
      if (child is ScrollViewer viewer)
        return viewer;

      var nested = FindScrollViewer(child);
      if (nested != null)
        return nested;
    }

    return null;
  }
}
