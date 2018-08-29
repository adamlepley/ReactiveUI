// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ReactiveUI.Legacy;

namespace ReactiveUI.Winforms.Legacy
{
    [Obsolete("ReactiveList is no longer supported. We suggest replacing it with DynamicData https://github.com/rolandpheasant/dynamicdata")]
    public class ReactiveBindingList<T> : ReactiveList<T>,
        IList<T>, ICollection<T>, IEnumerable<T>,
        ICollection, IEnumerable, IList, IBindingList,
        ICancelAddNew, IRaiseItemChangedEvents
    {
        public ReactiveBindingList() : this(null)
        {
        }

        public void CancelNew(int itemIndex)
        {
            // throw new NotImplementedException();
        }

        public void EndNew(int itemIndex)
        {
            // throw new NotImplementedException();
        }

        public bool RaisesItemChangedEvents => ChangeTrackingEnabled;

        /// <summary>
        /// ReactiveBindingList constructor.
        /// </summary>
        /// <param name="items">The items.</param>
        public ReactiveBindingList(IEnumerable<T> items)
            : base(items)
        {
        }

        protected override void RaiseCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            base.RaiseCollectionChanged(e);
            if (ListChanged != null)
            {
                e.AsListChangedEventArgs().ForEach(x => ListChanged(this, x));
            }
        }

        public object AddNew()
        {
            return Activator.CreateInstance<T>();
        }

        public void AddIndex(PropertyDescriptor property)
        {
            throw new NotSupportedException();
        }

        public void ApplySort(PropertyDescriptor property, ListSortDirection direction)
        {
            throw new NotSupportedException();
        }

        public int Find(PropertyDescriptor property, object key)
        {
            throw new NotSupportedException();
        }

        public void RemoveIndex(PropertyDescriptor property)
        {
            throw new NotSupportedException();
        }

        public void RemoveSort()
        {
            throw new NotSupportedException();
        }

        public bool AllowNew => true;

        public bool AllowEdit => true;

        public bool AllowRemove => true;

        public bool SupportsChangeNotification => true;

        public bool SupportsSearching => false;

        public bool SupportsSorting => false;

        public bool IsSorted => false;

        public PropertyDescriptor SortProperty => null;

        public ListSortDirection SortDirection => ListSortDirection.Ascending;

        public event ListChangedEventHandler ListChanged;
    }
}
