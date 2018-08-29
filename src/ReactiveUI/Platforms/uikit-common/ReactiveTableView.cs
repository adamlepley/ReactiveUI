// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CoreGraphics;
using Foundation;
using UIKit;

namespace ReactiveUI
{
    public abstract class ReactiveTableView : UITableView, IReactiveNotifyPropertyChanged<ReactiveTableView>, IHandleObservableErrors, IReactiveObject, ICanActivate, ICanForceManualActivation
    {
        protected ReactiveTableView()
        {
        }

        protected ReactiveTableView(NSObjectFlag t) : base(t)
        {
        }

        protected ReactiveTableView(NSCoder coder) : base(coder)
        {
        }

        protected ReactiveTableView(CGRect frame) : base(frame)
        {
        }

        protected ReactiveTableView(CGRect frame, UITableViewStyle style) : base(frame, style)
        {
        }

        protected ReactiveTableView(IntPtr handle) : base(handle)
        {
        }

        public event PropertyChangingEventHandler PropertyChanging
        {
            add { PropertyChangingEventManager.AddHandler(this, value); }
            remove { PropertyChangingEventManager.RemoveHandler(this, value); }
        }

        void IReactiveObject.RaisePropertyChanging(PropertyChangingEventArgs args)
        {
            PropertyChangingEventManager.DeliverEvent(this, args);
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { PropertyChangedEventManager.AddHandler(this, value); }
            remove { PropertyChangedEventManager.RemoveHandler(this, value); }
        }

        void IReactiveObject.RaisePropertyChanged(PropertyChangedEventArgs args)
        {
            PropertyChangedEventManager.DeliverEvent(this, args);
        }

        /// <summary>
        /// Represents an Observable that fires *before* a property is about to
        /// be changed.
        /// </summary>
        public IObservable<IReactivePropertyChangedEventArgs<ReactiveTableView>> Changing
        {
            get { return this.GetChangingObservable(); }
        }

        /// <summary>
        /// Represents an Observable that fires *after* a property has changed.
        /// </summary>
        public IObservable<IReactivePropertyChangedEventArgs<ReactiveTableView>> Changed
        {
            get { return this.GetChangedObservable(); }
        }

        public IDisposable SuppressChangeNotifications()
        {
            return IReactiveObjectExtensions.SuppressChangeNotifications(this);
        }

        public IObservable<Exception> ThrownExceptions
        {
            get { return this.GetThrownExceptionsObservable(); }
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
            (newsuper != null ? _activated : _deactivated).OnNext(Unit.Default);
        }

        void ICanForceManualActivation.Activate(bool activate)
        {
            RxApp.MainThreadScheduler.Schedule(() =>
                (activate ? _activated : _deactivated).OnNext(Unit.Default));
        }
    }

    public abstract class ReactiveTableView<TViewModel> : ReactiveTableView, IViewFor<TViewModel>
        where TViewModel : class
    {
        protected ReactiveTableView()
        {
        }

        protected ReactiveTableView(NSObjectFlag t) : base(t)
        {
        }

        protected ReactiveTableView(NSCoder coder) : base(coder)
        {
        }

        protected ReactiveTableView(CGRect frame) : base(frame)
        {
        }

        protected ReactiveTableView(CGRect frame, UITableViewStyle style) : base(frame, style)
        {
        }

        protected ReactiveTableView(IntPtr handle) : base(handle)
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
