// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Splat;

namespace ReactiveUI.AndroidSupport
{
    /// <summary>
    /// This is a Fragment that is both an Activity and has ReactiveObject powers
    /// (i.e. you can call RaiseAndSetIfChanged).
    /// </summary>
    /// <typeparam name="TViewModel"></typeparam>
    public class ReactiveFragment<TViewModel> : ReactiveFragment, IViewFor<TViewModel>, ICanActivate
        where TViewModel : class
    {
        protected ReactiveFragment()
        {
        }

        private TViewModel _ViewModel;

        public TViewModel ViewModel
        {
            get => _ViewModel;
            set => this.RaiseAndSetIfChanged(ref _ViewModel, value);
        }

        object IViewFor.ViewModel
        {
            get => _ViewModel;
            set => _ViewModel = (TViewModel)value;
        }
    }

    /// <summary>
    /// This is a Fragment that is both an Activity and has ReactiveObject powers
    /// (i.e. you can call RaiseAndSetIfChanged).
    /// </summary>
    public class ReactiveFragment : global::Android.Support.V4.App.Fragment, IReactiveNotifyPropertyChanged<ReactiveFragment>, IReactiveObject, IHandleObservableErrors
    {
        protected ReactiveFragment()
        {
        }

        public event PropertyChangingEventHandler PropertyChanging
        {
            add => PropertyChangingEventManager.AddHandler(this, value);
            remove => PropertyChangingEventManager.RemoveHandler(this, value);
        }

        void IReactiveObject.RaisePropertyChanging(PropertyChangingEventArgs args)
        {
            PropertyChangingEventManager.DeliverEvent(this, args);
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add => PropertyChangedEventManager.AddHandler(this, value);
            remove => PropertyChangedEventManager.RemoveHandler(this, value);
        }

        void IReactiveObject.RaisePropertyChanged(PropertyChangedEventArgs args)
        {
            PropertyChangedEventManager.DeliverEvent(this, args);
        }

        /// <summary>
        /// Represents an Observable that fires *before* a property is about to
        /// be changed.
        /// </summary>
        public IObservable<IReactivePropertyChangedEventArgs<ReactiveFragment>> Changing => this.GetChangingObservable();

        /// <summary>
        /// Represents an Observable that fires *after* a property has changed.
        /// </summary>
        public IObservable<IReactivePropertyChangedEventArgs<ReactiveFragment>> Changed => this.GetChangedObservable();

        /// <summary>
        /// When this method is called, an object will not fire change
        /// notifications (neither traditional nor Observable notifications)
        /// until the return value is disposed.
        /// </summary>
        /// <returns>An object that, when disposed, re-enables change
        /// notifications.</returns>
        public IDisposable SuppressChangeNotifications()
        {
            return IReactiveObjectExtensions.SuppressChangeNotifications(this);
        }

        public IObservable<Exception> ThrownExceptions => this.GetThrownExceptionsObservable();

        private readonly Subject<Unit> _activated = new Subject<Unit>();

        public IObservable<Unit> Activated => _activated.AsObservable();

        private readonly Subject<Unit> _deactivated = new Subject<Unit>();

        public IObservable<Unit> Deactivated => _deactivated.AsObservable();

        public override void OnPause()
        {
            base.OnPause();
            _deactivated.OnNext(Unit.Default);
        }

        public override void OnResume()
        {
            base.OnResume();
            _activated.OnNext(Unit.Default);
        }
    }
}
