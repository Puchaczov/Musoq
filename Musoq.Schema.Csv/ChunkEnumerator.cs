using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Csv
{
    public class ChunkEnumerator : IEnumerator<IObjectResolver>
    {
        private readonly BlockingCollection<List<EntityResolver<string[]>>> _readedRows;

#if DEBUG
        private readonly Stopwatch _watcher = new Stopwatch();
#endif

        private List<EntityResolver<string[]>> _currentChunk;
        private int _currentIndex = -1;
        private readonly CancellationToken _token;


        public ChunkEnumerator(BlockingCollection<List<EntityResolver<string[]>>> readedRows, CancellationToken token)
        {
            _readedRows = readedRows;
            _token = token;
            _currentChunk = _readedRows.Take();
#if DEBUG
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
                    if (!_readedRows.TryTake(out _currentChunk)) continue;

                    wasTaken = true;
                    break;
                }

                if (!wasTaken)
                {
#if DEBUG
                    _watcher.Start();
                    var started = _watcher.Elapsed;
#endif
                    List<EntityResolver<string[]>> newChunk = null;
                    while (newChunk == null || newChunk.Count == 0)
                        newChunk = _readedRows.Count > 0 ? _readedRows.Take() : _readedRows.Take(_token);
                    _currentChunk = newChunk;
#if DEBUG
                    var stopped = _watcher.Elapsed;
                    Console.WriteLine($"WAITED FOR {stopped - started}");
#endif
                }

                _currentIndex = 0;
                return true;
            }
            catch (OperationCanceledException)
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