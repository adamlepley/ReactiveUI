﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace ReactiveUI
{
    /// <summary>
    /// Implementing this interface on a ViewModel indicates that the ViewModel
    /// is interested in Activation events. Instantiate the Activator, then call
    /// WhenActivated on your class to register what you want to happen when
    /// the View is activated. See the documentation for ViewModelActivator to
    /// read more about Activation.
    /// </summary>
    public interface ISupportsActivation
    {
        ViewModelActivator Activator { get; }
    }
}
