#region Imports
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;
#endregion

namespace MaterialSkin
{
    #region MaterialItemCollectionChild
    [Editor(typeof(MaterialItemCollectionEditor), typeof(UITypeEditor))]
    public class MaterialItemCollection : Collection<object>
    {
        public event EventHandler ItemUpdated;

        public void AddRange(IEnumerable<object> items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            foreach (object item in items)
            {
                Add(item);
            }
        }

        public void AddRange(string[] items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            foreach (string item in items)
            {
                Add(item);
            }
        }

        public new void Add(object item)
        {
            base.Add(item);
            OnItemUpdated();
        }

        protected override void InsertItem(int index, object item)
        {
            base.InsertItem(index, item);
            OnItemUpdated();
        }

        protected override void RemoveItem(int index)
        {
            base.RemoveItem(index);
            OnItemUpdated();
        }

        public new void Clear()
        {
            base.Clear();
            OnItemUpdated();
        }

        protected override void ClearItems()
        {
            base.ClearItems();
            OnItemUpdated();
        }

        protected virtual void OnItemUpdated()
        {
            ItemUpdated?.Invoke(this, EventArgs.Empty);
        }
    }
    #endregion

    #region MaterialListBoxItemChild
    public class MaterialListBoxItem
    {
        #region Property Region
        public string Text { get; set; }
        public string SecondaryText { get; set; }
        public object Tag { get; set; }
        //public Bitmap Icon { get; set; }
        #endregion

        #region Constructor Region
        public MaterialListBoxItem()
        {
            Text = "ListBoxItem";
            SecondaryText = string.Empty;
        }

        public MaterialListBoxItem(string text)
        {
            Text = text ?? "ListBoxItem";
            SecondaryText = string.Empty;
        }

        public MaterialListBoxItem(string text, string secondaryText)
        {
            Text = text ?? "ListBoxItem";
            SecondaryText = secondaryText ?? string.Empty;
        }

        public MaterialListBoxItem(string text, string secondaryText, object tag)
        {
            Text = text ?? "ListBoxItem";
            SecondaryText = secondaryText ?? string.Empty;
            Tag = tag;
        }

        //public MaterialListBoxItem(string text, Bitmap icon) : this(text)
        //{
        //    Icon = icon;
        //}
        #endregion

        public override string ToString()
        {
            return Text;
        }
    }
    #endregion
}