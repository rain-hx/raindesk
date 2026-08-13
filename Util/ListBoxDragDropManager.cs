// Copyright (C) Josh Smith - January 2007
// WinUI 3 Migration: Simplified version
using System;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace raindesk.Util
{
    #region ListBoxDragDropManager

    /// <summary>
    /// WinUI 3 simplified drag-drop manager for ListBox/ListView.
    /// </summary>
    /// <typeparam name="ItemType">The type of the ListBox's items.</typeparam>
    public class ListBoxDragDropManager<ItemType> where ItemType : class
    {
        double dragAdornerOpacity;
        bool isDragInProgress;
        ListView listBox = null!;
        bool showDragAdorner;

        public ListBoxDragDropManager()
        {
            this.dragAdornerOpacity = 0.7;
            this.showDragAdorner = true;
        }

        public ListBoxDragDropManager(ListView listBox)
            : this()
        {
            this.ListBox = listBox;
        }

        public ListBoxDragDropManager(ListView listBox, double dragAdornerOpacity)
            : this(listBox)
        {
            this.DragAdornerOpacity = dragAdornerOpacity;
        }

        public ListBoxDragDropManager(ListView listBox, bool showDragAdorner)
            : this(listBox)
        {
            this.ShowDragAdorner = showDragAdorner;
        }

        public double DragAdornerOpacity
        {
            get { return this.dragAdornerOpacity; }
            set
            {
                if (this.IsDragInProgress)
                    throw new InvalidOperationException("Cannot set the DragAdornerOpacity property during a drag operation.");

                if (value < 0.0 || value > 1.0)
                    throw new ArgumentOutOfRangeException("DragAdornerOpacity", value, "Must be between 0 and 1.");

                this.dragAdornerOpacity = value;
            }
        }

        public bool IsDragInProgress
        {
            get { return this.isDragInProgress; }
            private set { this.isDragInProgress = value; }
        }

        public ListView ListBox
        {
            get { return listBox; }
            set
            {
                if (this.IsDragInProgress)
                    throw new InvalidOperationException("Cannot set the ListBox property during a drag operation.");

                this.listBox = value;

                if (this.listBox != null)
                {
                    this.listBox.AllowDrop = true;
                    this.listBox.DragItemsStarting += ListBox_DragItemsStarting;
                    this.listBox.Drop += ListBox_Drop;
                    this.listBox.DragOver += ListBox_DragOver;
                }
            }
        }

#pragma warning disable CS0067 // Event is never used
        public event EventHandler<ProcessDropEventArgs<ItemType>>? ProcessDrop;
#pragma warning restore CS0067

        public bool ShowDragAdorner
        {
            get { return this.showDragAdorner; }
            set
            {
                if (this.IsDragInProgress)
                    throw new InvalidOperationException("Cannot set the ShowDragAdorner property during a drag operation.");

                this.showDragAdorner = value;
            }
        }

        private void ListBox_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            if (e.Items.Count > 0)
            {
                var item = e.Items[0] as ItemType;
                if (item != null)
                {
                    e.Data.SetData("Item", item);
                    this.IsDragInProgress = true;
                }
            }
        }

        private void ListBox_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
        }

        private void ListBox_Drop(object sender, DragEventArgs e)
        {
            this.IsDragInProgress = false;
        }
    }

    #endregion // ListBoxDragDropManager

    #region ProcessDropEventArgs

    public class ProcessDropEventArgs<ItemType> : EventArgs where ItemType : class
    {
        ObservableCollection<ItemType> itemsSource;
        ItemType dataItem;
        int oldIndex;
        int newIndex;

        internal ProcessDropEventArgs(
            ObservableCollection<ItemType> itemsSource,
            ItemType dataItem,
            int oldIndex,
            int newIndex)
        {
            this.itemsSource = itemsSource;
            this.dataItem = dataItem;
            this.oldIndex = oldIndex;
            this.newIndex = newIndex;
        }

        public ObservableCollection<ItemType> ItemsSource { get { return this.itemsSource; } }
        public ItemType DataItem { get { return this.dataItem; } }
        public int OldIndex { get { return this.oldIndex; } }
        public int NewIndex { get { return this.newIndex; } }
    }

    #endregion // ProcessDropEventArgs
}
