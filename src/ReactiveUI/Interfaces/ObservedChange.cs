﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq.Expressions;
using System.Windows.Input;
using Splat;

namespace ReactiveUI
{
    /// <summary>
    /// A data-only version of IObservedChange.
    /// </summary>
    /// <typeparam name="TSender">The sender type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    public class ObservedChange<TSender, TValue> : IObservedChange<TSender, TValue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObservedChange{TSender, TValue}"/> class.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="expression">Expression describing the member.</param>
        /// <param name="value">The value.</param>
        public ObservedChange(TSender sender, Expression expression, TValue value = default(TValue))
        {
            Sender = sender;
            Expression = expression;
            Value = value;
        }

        /// <summary>
        ///
        /// </summary>
/// <inheritdoc/>
        public TSender Sender { get; private set; }

        /// <summary>
        ///
        /// </summary>
/// <inheritdoc/>
        public Expression Expression { get; private set; }

        /// <summary>
        ///
        /// </summary>
/// <inheritdoc/>
        public TValue Value { get; private set; }
    }
}

// vim: tw=120 ts=4 sw=4 et :
