// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Splat;

namespace ReactiveUI
{
    /// <summary>
    /// ObservableAsPropertyHelper is a class to help ViewModels implement
    /// "output properties", that is, a property that is backed by an
    /// Observable. The property will be read-only, but will still fire change
    /// notifications. This class can be created directly, but is more often created
    /// via the <see cref="OAPHCreationHelperMixin" /> extension methods.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class ObservableAsPropertyHelper<T> : IHandleObservableErrors, IDisposable, IEnableLogger
    {
        private T _lastValue;
        private readonly IConnectableObservable<T> _source;
        private IDisposable _inner;
        private int _activated;

        /// <summary>
        /// Constructs an ObservableAsPropertyHelper object.
        /// </summary>
        /// <param name="observable">
        /// The Observable to base the property on.
        /// </param>
        /// <param name="onChanged">
        /// The action to take when the property changes, typically this will call the
        /// ViewModel's RaisePropertyChanged method.
        /// </param>
        /// <param name="initialValue">
        /// The initial value of the property.
        /// </param>
        /// <param name="deferSubscription">
        /// A value indicating whether the <see cref="ObservableAsPropertyHelper{T}"/>
        /// should defer the subscription to the <paramref name="observable"/> source
        /// until the first call to <see cref="Value"/>, or if it should immediately
        /// subscribe to the the <paramref name="observable"/> source.
        /// </param>
        /// <param name="scheduler">
        /// The scheduler that the notifications will be provided on -
        /// this should normally be a Dispatcher-based scheduler.
        /// </param>
        public ObservableAsPropertyHelper(
            IObservable<T> observable,
            Action<T> onChanged,
            T initialValue = default(T),
            bool deferSubscription = false,
            IScheduler scheduler = null) : this(observable, onChanged, null, initialValue, deferSubscription, scheduler)
        {
        }

        /// <summary>
        /// Constructs an ObservableAsPropertyHelper object.
        /// </summary>
        /// <param name="observable">
        /// The Observable to base the property on.
        /// </param>
        /// <param name="onChanged">
        /// The action to take when the property changes, typically this will call
        /// the ViewModel's RaisePropertyChanged method.
        /// </param>
        /// <param name="onChanging">
        /// The action to take when the property changes, typically this will call
        /// the ViewModel's RaisePropertyChanging method.
        /// </param>
        /// <param name="initialValue">
        /// The initial value of the property.
        /// </param>
        /// <param name="deferSubscription">
        /// A value indicating whether the <see cref="ObservableAsPropertyHelper{T}"/>
        /// should defer the subscription to the <paramref name="observable"/> source
        /// until the first call to <see cref="Value"/>, or if it should immediately
        /// subscribe to the the <paramref name="observable"/> source.
        /// </param>
        /// <param name="scheduler">
        /// The scheduler that the notifications will provided on - this
        /// should normally be a Dispatcher-based scheduler.
        /// </param>
        public ObservableAsPropertyHelper(
            IObservable<T> observable,
            Action<T> onChanged,
            Action<T> onChanging = null,
            T initialValue = default(T),
            bool deferSubscription = false,
            IScheduler scheduler = null)
        {
            Contract.Requires(observable != null);
            Contract.Requires(onChanged != null);

            scheduler = scheduler ?? CurrentThreadScheduler.Instance;
            onChanging = onChanging ?? (_ => { });

            var subj = new ScheduledSubject<T>(scheduler);
            var exSubject = new ScheduledSubject<Exception>(CurrentThreadScheduler.Instance, RxApp.DefaultExceptionHandler);

            subj.Subscribe(x =>
            {
                onChanging(x);
                _lastValue = x;
                onChanged(x);
            }, exSubject.OnNext);

            ThrownExceptions = exSubject;

            _lastValue = initialValue;
            _source = observable.StartWith(initialValue).DistinctUntilChanged().Multicast(subj);
            if (!deferSubscription)
            {
                _inner = _source.Connect();
                _activated = 1;
            }
        }

        /// <summary>
        /// The last provided value from the Observable.
        /// </summary>
        public T Value
        {
            get
            {
                if (Interlocked.CompareExchange(ref _activated, 1, 0) == 0)
                {
                    _inner = _source.Connect();
                }

                return _lastValue;
            }
        }

        /// <summary>
        /// Gets a boolean value indicating whether the ObservableAsPropertyHelper
        /// has subscribed to the source Observable.
        /// Useful for scenarios where you use deferred subscription and want to know if
        /// the ObservableAsPropertyHelper Value has been accessed yet.
        /// </summary>
        public bool IsSubscribed => _activated > 0;

        /// <summary>
        /// Fires whenever an exception would normally terminate ReactiveUI
        /// internal state.
        /// </summary>
        public IObservable<Exception> ThrownExceptions { get; private set; }

        /// <summary>
        /// Disposes this ObservableAsPropertyHelper.
        /// </summary>
        public void Dispose()
        {
            (_inner ?? Disposable.Empty).Dispose();
            _inner = null;
        }

        /// <summary>
        /// Constructs a "default" ObservableAsPropertyHelper object. This is
        /// useful for when you will initialize the OAPH later, but don't want
        /// bindings to access a null OAPH at startup.
        /// </summary>
        /// <param name="initialValue">
        /// The initial (and only) value of the property.
        /// </param>
        /// <param name="scheduler">
        /// The scheduler that the notifications will be provided on - this should
        /// normally be a Dispatcher-based scheduler.
        /// </param>
        /// <returns></returns>
        public static ObservableAsPropertyHelper<T> Default(T initialValue = default(T), IScheduler scheduler = null)
        {
            return new ObservableAsPropertyHelper<T>(Observable<T>.Never, _ => { }, initialValue, false, scheduler);
        }
    }

    /// <summary>
    /// A collection of helpers to aid working with observable properties.
    /// </summary>
    public static class OAPHCreationHelperMixin
    {
        private static ObservableAsPropertyHelper<TRet> ObservableToProperty<TObj, TRet>(
                this TObj @this,
                IObservable<TRet> observable,
                Expression<Func<TObj, TRet>> property,
                TRet initialValue = default(TRet),
                bool deferSubscription = false,
                IScheduler scheduler = null)
            where TObj : IReactiveObject
        {
            Contract.Requires(@this != null);
            Contract.Requires(observable != null);
            Contract.Requires(property != null);

            Expression expression = Reflection.Rewrite(property.Body);

            if (expression.GetParent().NodeType != ExpressionType.Parameter)
            {
                throw new ArgumentException("Property expression must be of the form 'x => x.SomeProperty'");
            }

            var name = expression.GetMemberInfo().Name;
            if (expression is IndexExpression)
            {
                name += "[]";
            }

            var ret = new ObservableAsPropertyHelper<TRet>(observable,
                _ => @this.raisePropertyChanged(name),
                _ => @this.raisePropertyChanging(name),
                initialValue, deferSubscription, scheduler);

            return ret;
        }

        private static ObservableAsPropertyHelper<TRet> ObservableToProperty<TObj, TRet>(
                this TObj @this,
                IObservable<TRet> observable,
                string property,
                TRet initialValue = default(TRet),
                bool deferSubscription = false,
                IScheduler scheduler = null)
            where TObj : IReactiveObject
        {
            Contract.Requires(@this != null);
            Contract.Requires(observable != null);
            Contract.Requires(property != null);

            return new ObservableAsPropertyHelper<TRet>(
                observable,
                _ => @this.raisePropertyChanged(property),
                _ => @this.raisePropertyChanging(property),
                initialValue, deferSubscription, scheduler);
        }

        /// <summary>
        /// Converts an Observable to an ObservableAsPropertyHelper and
        /// automatically provides the onChanged method to raise the property
        /// changed notification.
        /// </summary>
        /// <typeparam name="TObj"></typeparam>
        /// <typeparam name="TRet"></typeparam>
        /// <param name="this">
        /// The observable to convert to an ObservableAsPropertyHelper.
        /// </param>
        /// <param name="source">
        /// The ReactiveObject that has the property.
        /// </param>
        /// <param name="property">
        /// An Expression representing the property (i.e. <c>x => x.SomeProperty</c>).
        /// </param>
        /// <param name="initialValue">
        /// The initial value of the property.
        /// </param>
        /// <param name="deferSubscription">
        /// A value indicating whether the <see cref="ObservableAsPropertyHelper{T}"/>
        /// should defer the subscription to the <paramref name="this"/> source
        /// until the first call to <see cref="ObservableAsPropertyHelper{T}.Value"/>,
        /// or if it should immediately subscribe to the the <paramref name="this"/> source.
        /// </param>
        /// <param name="scheduler">
        /// The scheduler that the notifications will be provided on - this should normally
        /// be a Dispatcher-based scheduler.
        /// </param>
        /// <returns>
        /// An initialized ObservableAsPropertyHelper; use this as the backing field
        /// for your property.
        /// </returns>
        public static ObservableAsPropertyHelper<TRet> ToProperty<TObj, TRet>(
            this IObservable<TRet> @this,
            TObj source,
            Expression<Func<TObj, TRet>> property,
            TRet initialValue = default(TRet),
            bool deferSubscription = false,
            IScheduler scheduler = null)
            where TObj : IReactiveObject
        {
            return source.ObservableToProperty(@this, property, initialValue, deferSubscription, scheduler);
        }

        /// <summary>
        /// Converts an Observable to an ObservableAsPropertyHelper and
        /// automatically provides the onChanged method to raise the property
        /// changed notification.
        /// </summary>
        /// <typeparam name="TObj"></typeparam>
        /// <typeparam name="TRet"></typeparam>
        /// <param name="this">
        /// The observable to convert to an ObservableAsPropertyHelper.
        /// </param>
        /// <param name="source">
        /// The ReactiveObject that has the property.
        /// </param>
        /// <param name="property">
        /// An Expression representing the property (i.e. <c>x => x.SomeProperty</c>).
        /// </param>
        /// <param name="result">
        /// An out param matching the return value, provided for convenience.
        /// </param>
        /// <param name="initialValue">
        /// The initial value of the property.
        /// </param>
        /// <param name="deferSubscription">
        /// A value indicating whether the <see cref="ObservableAsPropertyHelper{T}"/>
        /// should defer the subscription to the <paramref name="this"/> source
        /// until the first call to <see cref="ObservableAsPropertyHelper{T}.Value"/>,
        /// or if it should immediately subscribe to the the <paramref name="this"/> source.
        /// </param>
        /// <param name="scheduler">
        /// The scheduler that the notifications will be provided on - this should
        /// normally be a Dispatcher-based scheduler.
        /// </param>
        /// <returns>
        /// An initialized ObservableAsPropertyHelper; use this as the backing
        /// field for your property.
        /// </returns>
        public static ObservableAsPropertyHelper<TRet> ToProperty<TObj, TRet>(
            this IObservable<TRet> @this,
            TObj source,
            Expression<Func<TObj, TRet>> property,
            out ObservableAsPropertyHelper<TRet> result,
            TRet initialValue = default(TRet),
            bool deferSubscription = false,
            IScheduler scheduler = null)
            where TObj : IReactiveObject
        {
            var ret = source.ObservableToProperty(@this, property, initialValue, deferSubscription, scheduler);

            result = ret;
            return ret;
        }

        /// <summary>
        /// Converts an Observable to an ObservableAsPropertyHelper and
        /// automatically provides the onChanged method to raise the property
        /// changed notification.
        /// </summary>
        /// <typeparam name="TObj"></typeparam>
        /// <typeparam name="TRet"></typeparam>
        /// <param name="this">
        /// The observable to convert to an ObservableAsPropertyHelper.
        /// </param>
        /// <param name="source">
        /// The ReactiveObject that has the property.
        /// </param>
        /// <param name="property">
        /// The name of the property that has changed. Recommended for use with nameof() or a FODY.
        /// or a fody.
        /// </param>
        /// <param name="initialValue">
        /// The initial value of the property.
        /// </param>
        /// <param name="deferSubscription">
        /// A value indicating whether the <see cref="ObservableAsPropertyHelper{T}"/>
        /// should defer the subscription to the <paramref name="this"/> source
        /// until the first call to <see cref="ObservableAsPropertyHelper{T}.Value"/>,
        /// or if it should immediately subscribe to the the <paramref name="this"/> source.
        /// </param>
        /// <param name="scheduler">
        /// The scheduler that the notifications will be provided on - this should normally
        /// be a Dispatcher-based scheduler.
        /// </param>
        /// <returns>
        /// An initialized ObservableAsPropertyHelper; use this as the backing field
        /// for your property.
        /// </returns>
        public static ObservableAsPropertyHelper<TRet> ToProperty<TObj, TRet>(
            this IObservable<TRet> @this,
            TObj source,
            string property,
            TRet initialValue = default(TRet),
            bool deferSubscription = false,
            IScheduler scheduler = null)
            where TObj : IReactiveObject
        {
            return source.ObservableToProperty(@this, property, initialValue, deferSubscription, scheduler);
        }

        /// <summary>
        /// Converts an Observable to an ObservableAsPropertyHelper and
        /// automatically provides the onChanged method to raise the property
        /// changed notification.
        /// </summary>
        /// <typeparam name="TObj"></typeparam>
        /// <typeparam name="TRet"></typeparam>
        /// <param name="this">
        /// The observable to convert to an ObservableAsPropertyHelper.
        /// </param>
        /// <param name="source">
        /// The ReactiveObject that has the property.
        /// </param>
        /// <param name="property">
        /// The name of the property that has changed. Recommended for use with nameof() or a FODY.
        /// </param>
        /// <param name="result">
        /// An out param matching the return value, provided for convenience.
        /// </param>
        /// <param name="initialValue">
        /// The initial value of the property.
        /// </param>
        /// <param name="deferSubscription">
        /// A value indicating whether the <see cref="ObservableAsPropertyHelper{T}"/>
        /// should defer the subscription to the <paramref name="this"/> source
        /// until the first call to <see cref="ObservableAsPropertyHelper{T}.Value"/>,
        /// or if it should immediately subscribe to the the <paramref name="this"/> source.
        /// </param>
        /// <param name="scheduler">
        /// The scheduler that the notifications will be provided on - this should
        /// normally be a Dispatcher-based scheduler.
        /// </param>
        /// <returns>
        /// An initialized ObservableAsPropertyHelper; use this as the backing
        /// field for your property.
        /// </returns>
        public static ObservableAsPropertyHelper<TRet> ToProperty<TObj, TRet>(
            this IObservable<TRet> @this,
            TObj source,
            string property,
            out ObservableAsPropertyHelper<TRet> result,
            TRet initialValue = default(TRet),
            bool deferSubscription = false,
            IScheduler scheduler = null)
            where TObj : IReactiveObject
        {
            result = source.ObservableToProperty(
                @this,
                property,
                initialValue,
                deferSubscription,
                scheduler);

            return result;
        }
    }
}

// vim: tw=120 ts=4 sw=4 et :
