using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Musoq.Schema.DataSources
{
    public class ChunkEnumerator<T> : IEnumerator<IObjectResolver>
    {
        private readonly BlockingCollection<IReadOnlyList<EntityResolver<T>>> _readedRows;

#if DEBUG
        private readonly Stopwatch _watcher = new Stopwatch();
#endif

        private IReadOnlyList<EntityResolver<T>> _currentChunk;
        private int _currentIndex = -1;
        private readonly CancellationToken _token;

        public ChunkEnumerator(BlockingCollection<IReadOnlyList<EntityResolver<T>>> readedRows, CancellationToken token)
        {
            _readedRows = readedRows;
            _token = token;
            _currentChunk = _readedRows.Take();

#if DEBUG
            if (_currentChunk == null && Debugger.IsAttached)
                Debugger.Break();

            _watcher.Start();
#endif
        }

        public bool MoveNext()
        {
            if (_readedRows.Count == 0 && _token.IsCancellationRequested && _currentIndex == _currentChunk.Count)
                return false;

            if (_currentIndex++ < _currentChunk.Count - 1)
                return true;

            try
            {
                var wasTaken = false;
                for (var i = 0; i < 10; i++)
                {
                    if (!_readedRows.TryTake(out _currentChunk) || _currentChunk.Count == 0) continue;

                    wasTaken = true;
                    break;
                }

                if (!wasTaken)
                {
#if DEBUG
                    _watcher.Start();
                    var started = _watcher.Elapsed;
#endif
                    IReadOnlyList<EntityResolver<T>> newChunk = null;
                    while (newChunk == null || newChunk.Count == 0)
                        newChunk = _readedRows.Count > 0 ? _readedRows.Take() : _readedRows.Take(_token);

                    _currentChunk = newChunk;
#if DEBUG
                    var stopped = _watcher.Elapsed;
                    Console.WriteLine($"WAITED FOR {stopped - started}");
#endif
                }

#if DEBUG
                if (_currentChunk == null && Debugger.IsAttached)
                    Debugger.Break();
#endif

                _currentIndex = 0;
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (NullReferenceException)
            {
                return false;
            }
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        public IObjectResolver Current => _currentChunk[_currentIndex];

        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }
    }
}