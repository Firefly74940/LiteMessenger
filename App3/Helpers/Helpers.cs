using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace App3
{
    class Helpers
    {

    }
    static class Exts
    {
        public static void SortSlow<T>(this ObservableCollection<T> collection, Comparison<T> comparison)
        {
            var sortableList = new List<T>(collection);
            sortableList.Sort(comparison);

            for (int i = 0; i < sortableList.Count; i++)
            {
                collection.Move(collection.IndexOf(sortableList[i]), i);
            }
        }
    }

    class SortingData
    {
        public int Index;
        public int Order;
        public int NewOrder;
    }

}
