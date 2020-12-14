using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Xamarin.Forms;
using Xamarin.Forms.DataGrid;

namespace Busmonitor.Extension
{
  public static class DataGridExtensions
  {
    /// <summary>
    /// This is already in source of datagrid but not yet released via nuget.
    /// TODO Remove when released 
    /// https://github.com/akgulebubekir/Xamarin.Forms.DataGrid/blob/cbf3885098a66aa94de357bbe6b0e62ac22099b9/Xamarin.Forms.DataGrid/DataGrid.xaml.cs
    /// </summary>
    /// <param name="grid"></param>
    /// <param name="item"></param>
    /// <param name="position"></param>
    /// <param name="animated"></param>
    public static void ScrollTo(this DataGrid grid, object item, ScrollToPosition position, bool animated = true)
    {
      //var fields = grid.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Select(me => me.Name);
      grid.GetPrivateFieldValue<ListView>("_listView").ScrollTo(item, position, animated);
    }
  }
}
