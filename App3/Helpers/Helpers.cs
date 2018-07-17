using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Controls;

namespace App3
{

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
        public static int LineCount(this TextBox textBox)
        {
            int asciiLine = 13;
            char line = (char)asciiLine;
            int numLines = textBox.Text.Split(line).Length;
            return numLines;
        }
    }

    class SortingData
    {
        public int Index;
        public int Order;
        public int NewOrder;
    }

}
