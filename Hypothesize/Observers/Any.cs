﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Hypothesize.Observers
{
    internal sealed class Any<T> : IObserve<T>
    {
        private readonly Action<T> _assert;

        public Any(Action<T> assert) => 
            _assert = assert;

        async Task IObserve<T>.Observe(ChannelReader<T> reader, TimeSpan window, CancellationToken token)
        {
            var exceptions = new List<Exception>();

            try
            {
                using var source = CancellationTokenSource.CreateLinkedTokenSource(token);
                source.CancelAfter(window);

                await foreach (var message in reader.ReadAllAsync(source.Token))
                {
                    try
                    {
                        _assert(message);
                        return;
                    }
                    catch (Exception e)
                    {
                        exceptions.Add(e);
                    }

                    source.CancelAfter(window);
                }
            }
            catch (OperationCanceledException)
            {
                throw exceptions.Any() 
                    ? new AggregateException(exceptions) 
                    : new TimeoutException();
            }
        }
    }
}