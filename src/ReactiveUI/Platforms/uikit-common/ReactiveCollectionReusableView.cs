// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CoreGraphics;
using Foundation;
using UIKit;

namespace ReactiveUI
{
    public abstract class ReactiveCollectionReusableView : UICollectionReusableView,
        IReactiveNotifyPropertyChanged<ReactiveCollectionReusableView>, IHandleObservableErrors, IReactiveObject, ICanActivate
    {
        protected ReactiveCollectionReusableView(CGRect frame) : base(frame)
        {
            SetupRxObj();
        }

        protected ReactiveCollectionReusableView() : base()
        {
            SetupRxObj();
        }

        protected ReactiveCollectionReusableView(IntPtr handle) : base(handle)
        {
            SetupRxObj();
        }

        protected ReactiveCollectionReusableView(NSObjectFlag t) : base(t)
        {
            SetupRxObj();
        }

        protected ReactiveCollectionReusableView(NSCoder coder) : base(coder)
        {
            SetupRxObj();
        }

        public event PropertyChangingEventHandler PropertyChanging;

        void IReactiveObject.RaisePropertyChanging(PropertyChangingEventArgs args)
        {
            var handler = PropertyChanging;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        void IReactiveObject.RaisePropertyChanged(PropertyChangedEventArgs args)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Represents an Observable that fires *before* a property is about to
        /// be changed.
        /// </summary>
        public IObservable<IReactivePropertyChangedEventArgs<ReactiveCollectionReusableView>> Changing
        {
            get { return this.GetChangingObservable(); }
        }

        /// <summary>
        /// Represents an Observable that fires *after* a property has changed.
        /// </summary>
        public IObservable<IReactivePropertyChangedEventArgs<ReactiveCollectionReusableView>> Changed
        {
            get { return this.GetChangedObservable(); }
        }

        public IObservable<Exception> ThrownExceptions
        {
            get { return this.GetThrownExceptionsObservable(); }
        }

        private void SetupRxObj()
        {
        }

        /// <summary>
        /// When this method is called, an object will not fire change
        /// notifications (neither traditional nor Observable notifications)
        /// until the return value is disposed.
        /// </summary>
        /// <returns>An object that, when disposed, reenables change
        /// notifications.</returns>
        public IDisposable SuppressChangeNotifications()
        {
            return IReactiveObjectExtensions.SuppressChangeNotifications(this);
        }

        private Subject<Unit> _activated = new Subject<Unit>();

        public IObservable<Unit> Activated
        {
            get { return _activated.AsObservable(); }
        }

        private Subject<Unit> _deactivated = new Subject<Unit>();

        public IObservable<Unit> Deactivated
        {
            get { return _deactivated.AsObservable(); }
        }

        public override void WillMoveToSuperview(UIView newsuper)
        {
            base.WillMoveToSuperview(newsuper);
            _activated.OnNext(Unit.Default);
        }

        public override void RemoveFromSuperview()
        {
            base.RemoveFromSuperview();
            _deactivated.OnNext(Unit.Default);
        }
    }

    public abstract class ReactiveCollectionReusableView<TViewModel> : ReactiveCollectionReusableView, IViewFor<TViewModel>
        where TViewModel : class
    {
        protected ReactiveCollectionReusableView()
        {
        }

        protected ReactiveCollectionReusableView(IntPtr handle) : base(handle)
        {
        }

        protected ReactiveCollectionReusableView(NSObjectFlag t) : base(t)
        {
        }

        protected ReactiveCollectionReusableView(NSCoder coder) : base(coder)
        {
        }

        protected ReactiveCollectionReusableView(CGRect frame) : base(frame)
        {
        }

        private TViewModel _viewModel;

        public TViewModel ViewModel
        {
            get { return _viewModel; }
            set { this.RaiseAndSetIfChanged(ref _viewModel, value); }
        }

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (TViewModel)value; }
        }
    }
}
