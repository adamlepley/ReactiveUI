// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Specialized;

namespace ReactiveUI
{
    public delegate void PropertyChangingEventHandler(
        object sender,
        PropertyChangingEventArgs e);

    public interface INotifyPropertyChanging
    {
        event PropertyChangingEventHandler PropertyChanging;
    }

    public class PropertyChangingEventArgs : EventArgs
    {
        public PropertyChangingEventArgs(string propertyName)
        {
            PropertyName = propertyName;
        }

        public string PropertyName { get; protected set; }
    }
}

namespace ReactiveUI
{
    public interface INotifyCollectionChanging
    {
        event NotifyCollectionChangedEventHandler CollectionChanging;
    }
}

#if PORTABLE || NETFX_CORE
namespace ReactiveUI
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public sealed class LocalizableAttribute : Attribute
    {
        // This is a positional argument
        public LocalizableAttribute(bool isLocalizable)
        {
        }
    }
}
#endif

// vim: tw=120 ts=4 sw=4 et :
