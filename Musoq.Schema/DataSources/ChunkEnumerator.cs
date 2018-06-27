using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Musoq.Schema.DataSources
{
    public class ChunkEnumerator<T> : IEnumerator<IObjectResolver>
    {
        private readonly BlockingCollection<IReadOnlyList<EntityResolver<T>>> _readedRows;

        private IReadOnlyList<EntityResolver<T>> _currentChunk;
        private int _currentIndex = -1;
        private readonly CancellationToken _token;

        public ChunkEnumerator(BlockingCollection<IReadOnlyList<EntityResolver<T>>> readedRows, CancellationToken token)
        {
            _readedRows = readedRows;
            _token = token;
        }

        public bool MoveNext()
        {
            if (_currentChunk != null && _currentIndex++ < _currentChunk.Count - 1)
                return true;

            try
            {
                var wasTaken = false;
                for (var i = 0; i < 10; i++)
                {
                    if (!_readedRows.TryTake(out _currentChunk) || _currentChunk == null || _currentChunk.Count == 0) continue;

                    wasTaken = true;
                    break;
                }

                if (!wasTaken)
                {
                    IReadOnlyList<EntityResolver<T>> newChunk = null;
                    while (newChunk == null || newChunk.Count == 0)
                        newChunk = _readedRows.Count > 0 ? _readedRows.Take() : _readedRows.Take(_token);

                    _currentChunk = newChunk;
                }

                _currentIndex = 0;
                return true;
            }
            catch (OperationCanceledException)
            {
                if (_readedRows.Count > 0)
                {
                    _currentChunk = _readedRows.Take();
                    while (_readedRows.Count > 0 && _currentChunk.Count == 0)
                        _currentChunk = _readedRows.Take();

                    _currentIndex = 0;
                    return _currentChunk.Count > 0;
                }

                return false;
            }
            catch (NullReferenceException)
            {
                return false;
            }
            catch (ArgumentOutOfRangeException)
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