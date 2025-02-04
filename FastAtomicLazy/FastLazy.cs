﻿// Copyright (c) Aaron Stannard 2016. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace FastAtomicLazy
{
    /// <summary>
    /// A fast, atomic lazy that only allows a single publish operation to happen,
    /// but allows executions to occur concurrently.
    /// 
    /// Does not cache exceptions. Designed for use with <typeparam name="T"/> types that are <see cref="IDisposable"/>
    /// or are otherwise considered to be expensive to allocate. Read the full explanation here: https://github.com/Aaronontheweb/FastAtomicLazy#rationale
    /// </summary>
    public sealed class FastLazy<T>
    {
        private Func<T> _producer;
        private int _status = 0;
        private T _createdValue;

        public FastLazy(Func<T> producer)
        {
            _producer = producer ?? throw new ArgumentNullException(nameof(producer));
        }

        public bool IsValueCreated => Volatile.Read(ref _status) == 2;

        public T Value
        {
            get
            {
                if (IsValueCreated)
                    return _createdValue;
                if (Interlocked.CompareExchange(ref _status, 1, 0) == 0)
                {
                    _createdValue = _producer();
                    Volatile.Write(ref _status, 2);
                    _producer = null; // release for GC
                }
                else
                {
                    SpinWait.SpinUntil(() => IsValueCreated);
                }
                return _createdValue;
            }
        }
    }
}
