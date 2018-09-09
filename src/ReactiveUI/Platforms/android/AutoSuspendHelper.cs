﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using Android.App;
using Android.OS;
using Java.Lang;
using Splat;

namespace ReactiveUI
{
    public class AutoSuspendHelper : IEnableLogger
    {
        private readonly Subject<Bundle> _onCreate = new Subject<Bundle>();
        private readonly Subject<Unit> _onRestart = new Subject<Unit>();
        private readonly Subject<Unit> _onPause = new Subject<Unit>();
        private readonly Subject<Bundle> _onSaveInstanceState = new Subject<Bundle>();

        public static readonly Subject<Unit> untimelyDemise = new Subject<Unit>();

        public static Bundle LatestBundle { get; set; }

        static AutoSuspendHelper()
        {
            AppDomain.CurrentDomain.UnhandledException += (o, e) => untimelyDemise.OnNext(Unit.Default);
        }

        public AutoSuspendHelper(Application hostApplication)
        {
            hostApplication.RegisterActivityLifecycleCallbacks(new ObservableLifecycle(this));

            Observable.Merge(_onCreate, _onSaveInstanceState).Subscribe(x => LatestBundle = x);

            RxApp.SuspensionHost.IsLaunchingNew = _onCreate.Where(x => x == null).Select(_ => Unit.Default);
            RxApp.SuspensionHost.IsResuming = _onCreate.Where(x => x != null).Select(_ => Unit.Default);
            RxApp.SuspensionHost.IsUnpausing = _onRestart;

            RxApp.SuspensionHost.ShouldPersistState = Observable.Merge(
                _onPause.Select(_ => Disposable.Empty), _onSaveInstanceState.Select(_ => Disposable.Empty));

            RxApp.SuspensionHost.ShouldInvalidateState = untimelyDemise;
        }

        private class ObservableLifecycle : Java.Lang.Object, Application.IActivityLifecycleCallbacks
        {
            private readonly AutoSuspendHelper _this;

            public ObservableLifecycle(AutoSuspendHelper @this)
            {
                _this = @this;
            }

            public void OnActivityCreated(Activity activity, Bundle savedInstanceState)
            {
                _this._onCreate.OnNext(savedInstanceState);
            }

            public void OnActivityResumed(Activity activity)
            {
                _this._onRestart.OnNext(Unit.Default);
            }

            public void OnActivitySaveInstanceState(Activity activity, Bundle outState)
            {
                // NB: This is so that we always have a bundle on OnCreate, so that
                // we can tell the difference between created from scratch and resume.
                outState.PutString("___dummy_value_please_create_a_bundle", "VeryYes");
                _this._onSaveInstanceState.OnNext(outState);
            }

            public void OnActivityPaused(Activity activity)
            {
                _this._onPause.OnNext(Unit.Default);
            }

            public void OnActivityDestroyed(Activity activity)
            {
            }

            public void OnActivityStarted(Activity activity)
            {
            }

            public void OnActivityStopped(Activity activity)
            {
            }
        }
    }
}
