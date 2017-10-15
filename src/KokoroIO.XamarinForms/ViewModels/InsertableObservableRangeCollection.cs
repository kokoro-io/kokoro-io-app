using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace KokoroIO.XamarinForms.ViewModels
{
    internal class InsertableObservableRangeCollection<T> : ObservableRangeCollection<T>
    {
        public void InsertRange(int startIndex, IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            CheckReentrancy();

            var i = startIndex;
            var items = collection is List<T> ? (List<T>)collection : new List<T>(collection);
            foreach (var e in items)
            {
                Items.Insert(i++, e);
            }

            OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items, startIndex));
        }
    }
}