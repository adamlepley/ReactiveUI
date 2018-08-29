// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Splat;

namespace ReactiveUI
{
    internal class SuspensionHost : ReactiveObject, ISuspensionHost
    {
        private readonly ReplaySubject<IObservable<Unit>> _isLaunchingNew = new ReplaySubject<IObservable<Unit>>(1);

        public IObservable<Unit> IsLaunchingNew
        {
            get => _isLaunchingNew.Switch();
            set => _isLaunchingNew.OnNext(value);
        }

        private readonly ReplaySubject<IObservable<Unit>> _isResuming = new ReplaySubject<IObservable<Unit>>(1);

        public IObservable<Unit> IsResuming
        {
            get => _isResuming.Switch();
            set => _isResuming.OnNext(value);
        }

        private readonly ReplaySubject<IObservable<Unit>> _isUnpausing = new ReplaySubject<IObservable<Unit>>(1);

        public IObservable<Unit> IsUnpausing
        {
            get => _isUnpausing.Switch();
            set => _isUnpausing.OnNext(value);
        }

        private readonly ReplaySubject<IObservable<IDisposable>> _shouldPersistState = new ReplaySubject<IObservable<IDisposable>>(1);

        public IObservable<IDisposable> ShouldPersistState
        {
            get => _shouldPersistState.Switch();
            set => _shouldPersistState.OnNext(value);
        }

        private readonly ReplaySubject<IObservable<Unit>> _shouldInvalidateState = new ReplaySubject<IObservable<Unit>>(1);

        public IObservable<Unit> ShouldInvalidateState
        {
            get => _shouldInvalidateState.Switch();
            set => _shouldInvalidateState.OnNext(value);
        }

        /// <summary>
        ///
        /// </summary>
        public Func<object> CreateNewAppState { get; set; }

        private object _appState;

        /// <summary>
        ///
        /// </summary>
        public object AppState
        {
            get => _appState;
            set => this.RaiseAndSetIfChanged(ref _appState, value);
        }

        public SuspensionHost()
        {
#if COCOA
            var message = "Your AppDelegate class needs to use AutoSuspendHelper";
#elif ANDROID
            var message = "You need to create an App class and use AutoSuspendHelper";
#else
            var message = "Your App class needs to use AutoSuspendHelper";
#endif

            IsLaunchingNew = IsResuming = IsUnpausing = ShouldInvalidateState =
                Observable.Throw<Unit>(new Exception(message));

            ShouldPersistState = Observable.Throw<IDisposable>(new Exception(message));
        }
    }

    public static class SuspensionHostExtensions
    {
        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="this">The suspenstion host.</param>
        /// <returns></returns>
        public static IObservable<T> ObserveAppState<T>(this ISuspensionHost @this)
        {
            return @this.WhenAny(x => x.AppState, x => (T)x.Value)
                .Where(x => x != null);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T">The app state type.</typeparam>
        /// <param name="this">The suspenstion host.</param>
        /// <returns></returns>
        public static T GetAppState<T>(this ISuspensionHost @this)
        {
            return (T)@this.AppState;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="this">The suspenstion host.</param>
        /// <param name="driver">The suspension driver.</param>
        /// <returns></returns>
        public static IDisposable SetupDefaultSuspendResume(this ISuspensionHost @this, ISuspensionDriver driver = null)
        {
            var ret = new CompositeDisposable();
            driver = driver ?? Locator.Current.GetService<ISuspensionDriver>();

            ret.Add(@this.ShouldInvalidateState
                .SelectMany(_ => driver.InvalidateState())
                .LoggedCatch(@this, Observables.Unit, "Tried to invalidate app state")
                .Subscribe(_ => @this.Log().Info("Invalidated app state")));

            ret.Add(@this.ShouldPersistState
                .SelectMany(x => driver.SaveState(@this.AppState).Finally(x.Dispose))
                .LoggedCatch(@this, Observables.Unit, "Tried to persist app state")
                .Subscribe(_ => @this.Log().Info("Persisted application state")));

            ret.Add(Observable.Merge(@this.IsResuming, @this.IsLaunchingNew)
                .SelectMany(x => driver.LoadState())
                .LoggedCatch(@this,
                    Observable.Defer(() => Observable.Return(@this.CreateNewAppState())),
                    "Failed to restore app state from storage, creating from scratch")
                .Subscribe(x => @this.AppState = x ?? @this.CreateNewAppState()));

            return ret;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class DummySuspensionDriver : ISuspensionDriver
    {
        public IObservable<object> LoadState()
        {
            return Observable<object>.Default;
        }

        public IObservable<Unit> SaveState(object state)
        {
            return Observables.Unit;
        }

        public IObservable<Unit> InvalidateState()
        {
            return Observables.Unit;
        }
    }
}
