﻿using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Hypothesize.Observers
{
    internal sealed class All<T> : IObserve<T>
    {
        private readonly Action<T> _assert;

        public All(Action<T> assert) => 
            _assert = assert;

        async Task IObserve<T>.Observe(ChannelReader<T> reader, TimeSpan window, CancellationToken token)
        {
            using var source = CancellationTokenSource.CreateLinkedTokenSource(token);
            source.CancelAfter(window);

            try
            {
                await foreach (var item in reader.ReadAllAsync(source.Token))
                {
                    _assert(item);
                    source.CancelAfter(window);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}