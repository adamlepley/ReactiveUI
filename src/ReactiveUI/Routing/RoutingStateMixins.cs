﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;

namespace ReactiveUI
{
    public static class RoutingStateMixins
    {
        /// <summary>
        /// Locate the first ViewModel in the stack that matches a certain Type.
        /// </summary>
        /// <typeparam name="T">The view model type.</typeparam>
        /// <param name="this">The routing state.</param>
        /// <returns>The matching ViewModel or null if none exists.</returns>
        public static T FindViewModelInStack<T>(this RoutingState @this)
            where T : IRoutableViewModel
        {
            return @this.NavigationStack.Reverse().OfType<T>().FirstOrDefault();
        }

        /// <summary>
        /// Returns the currently visible ViewModel.
        /// </summary>
        /// <param name="this">The routing state.</param>
        /// <returns>The matching ViewModel or null if none exists.</returns>
        public static IRoutableViewModel GetCurrentViewModel(this RoutingState @this)
        {
            return @this.NavigationStack.LastOrDefault();
        }
    }
}
