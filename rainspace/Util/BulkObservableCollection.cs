using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System;
using System.Linq;

namespace rainspace.Util
{
    /// <summary>
    /// 支持批量添加的 ObservableCollection，减少 UI 刷新次数。
    /// AddRange 只触发一次 Reset 通知，而非每个 item 触发一次。
    /// </summary>
    public class BulkObservableCollection<T> : ObservableCollection<T>
    {
        private bool _suppressNotification = false;

        public void AddRange(IEnumerable<T> items)
        {
            _suppressNotification = true;
            try
            {
                foreach (var item in items)
                {
                    Items.Add(item);
                }
            }
            finally
            {
                _suppressNotification = false;
            }
            // 只触发一次 Reset 通知
            base.OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Reset));
        }

        public void ReplaceAll(IEnumerable<T> items)
        {
            var replacement = items as IList<T> ?? items.ToList();
            if (Items.SequenceEqual(replacement))
            {
                return;
            }

            _suppressNotification = true;
            try
            {
                Items.Clear();
                foreach (var item in replacement)
                {
                    Items.Add(item);
                }
            }
            finally
            {
                _suppressNotification = false;
            }

            base.OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Incrementally reconciles this collection with the requested order.
        /// Existing items are moved instead of resetting the entire collection,
        /// so virtualized UI controls can keep their realized containers alive.
        /// </summary>
        public void SynchronizeWith(IEnumerable<T> items)
        {
            var target = items as IList<T> ?? items.ToList();
            var comparer = EqualityComparer<T>.Default;

            for (int targetIndex = 0; targetIndex < target.Count; targetIndex++)
            {
                T targetItem = target[targetIndex];
                if (targetIndex < Count && comparer.Equals(this[targetIndex], targetItem))
                {
                    continue;
                }

                int existingIndex = -1;
                for (int i = targetIndex + 1; i < Count; i++)
                {
                    if (comparer.Equals(this[i], targetItem))
                    {
                        existingIndex = i;
                        break;
                    }
                }

                if (existingIndex >= 0)
                {
                    Move(existingIndex, targetIndex);
                }
                else
                {
                    Insert(targetIndex, targetItem);
                }
            }

            while (Count > target.Count)
            {
                RemoveAt(Count - 1);
            }
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!_suppressNotification)
                base.OnCollectionChanged(e);
        }
    }
}
