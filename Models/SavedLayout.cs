using System.Collections.Generic;
using System.Windows;

namespace WindowManager.Models
{
    public class SavedLayout
    {
        public string Name { get; set; } = "New Layout";
        public List<LayoutZone> Zones { get; set; } = new List<LayoutZone>();
        public Rect Bounds { get; set; }
    }

    public class LayoutZone
    {
        public Rect Bounds { get; set; }
        public int Column { get; set; }
        public int Row { get; set; }
        public int ColumnSpan { get; set; } = 1;
        public int RowSpan { get; set; } = 1;
    }
}
