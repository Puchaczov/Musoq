using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Performance;

/// <summary>
/// Pooled table implementation that can be reused to reduce allocations
/// Part of Phase 3 memory management optimization
/// </summary>
public class PooledTable : IReadOnlyTable
{
    private string _name;
    private IReadOnlyCollection<ISchemaColumn> _columns;
    private readonly List<IObjectResolver> _rows;
    private bool _isDisposed;

    public PooledTable(string name, IReadOnlyCollection<ISchemaColumn> columns)
    {
        _name = name;
        _columns = columns;
        _rows = new List<IObjectResolver>();
        _isDisposed = false;
    }

    public string Name => _name;
    public IReadOnlyCollection<ISchemaColumn> Columns => _columns;
    public IReadOnlyList<IReadOnlyRow> Rows 
    {
        get
        {
            // Convert ObjectResolvers to IReadOnlyRow if they implement it
            var readOnlyRows = new List<IReadOnlyRow>();
            foreach (var row in _rows)
            {
                if (row is IReadOnlyRow readOnlyRow)
                    readOnlyRows.Add(readOnlyRow);
            }
            return readOnlyRows;
        }
    }
    public int Count => _rows.Count;

    /// <summary>
    /// Get the object resolvers directly for pooled operations
    /// </summary>
    public IEnumerable<IObjectResolver> ObjectResolvers => _rows;

    /// <summary>
    /// Reset the table for reuse with new data
    /// </summary>
    public void Reset(string name, IReadOnlyCollection<ISchemaColumn> columns)
    {
        _name = name;
        _columns = columns;
        Clear();
        _isDisposed = false;
    }

    /// <summary>
    /// Clear all rows but keep the table structure for reuse
    /// </summary>
    public void Clear()
    {
        // Return rows to pool if they are pooled objects
        foreach (var row in _rows.OfType<PooledObjectResolver>())
        {
            MemoryPool.ReturnResolver(row);
        }
        
        _rows.Clear();
    }

    /// <summary>
    /// Add a row to the table, preferably using pooled resolvers
    /// </summary>
    public void AddRow(IObjectResolver row)
    {
        if (_isDisposed) 
            throw new ObjectDisposedException(nameof(PooledTable));
            
        _rows.Add(row);
    }

    /// <summary>
    /// Add multiple rows efficiently
    /// </summary>
    public void AddRows(IEnumerable<IObjectResolver> rows)
    {
        if (_isDisposed) 
            throw new ObjectDisposedException(nameof(PooledTable));
            
        _rows.AddRange(rows);
    }

    /// <summary>
    /// Create a pooled row and add it to the table
    /// </summary>
    public PooledObjectResolver AddPooledRow()
    {
        if (_isDisposed) 
            throw new ObjectDisposedException(nameof(PooledTable));
            
        var row = MemoryPool.RentResolver();
        _rows.Add(row);
        return row;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        
        Clear();
        _isDisposed = true;
        
        // Return this table to the pool
        MemoryPool.ReturnTable(this);
    }
}