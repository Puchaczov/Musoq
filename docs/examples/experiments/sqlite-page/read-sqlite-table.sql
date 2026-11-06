-- READ A SELECTED SQLITE TABLE USING MUSOQ AND ORIGINAL FILE BYTES
--
-- This complete script accepts databasePath and tableName. The defaults read
-- firmware from the synthetic database beside this file. Run from the repo root:
--   musoq run docs/examples/experiments/sqlite-page/read-sqlite-table.sql --from-file --format json
--
-- To read another database, pass a JSON object through --params-json, for example:
--   {"databasePath":"C:/data/example.db","tableName":"events"}
-- The companion README gives a complete PowerShell command. Parameters are values,
-- not SQL fragments; names containing quotes do not need hand-built SQL escaping.
--
-- This extends the movie's small page reader. The agent in that recording was
-- given page 5, 1024-byte pages, a two-TEXT-column table, UTF-8 and format notes.
-- Here the database header supplies size/encoding, the catalog supplies the table
-- root, and its page tree supplies the leaf pages. No SQLite API/provider/export
-- participates in this read path. SQLite was used separately for test fixtures
-- and independent expected results.
--
-- OUTPUT: one result per stored row, with TableName, RowId, StoredValuesJson,
-- PageNumber and CellOffset. StoredValuesJson is a JSON array in stored column
-- order. Blob values use {"$blob":"AABB"}; NULL is null, not the string "null".
-- Keeping one fixed SQL output shape lets the same query read different schemas.
-- Field names are not inferred from CREATE TABLE SQL. In INTEGER PRIMARY KEY
-- tables, the alias slot is normally stored as NULL; the key is returned as RowId.
-- ALTER TABLE defaults, generated expressions and affinity conversions are not
-- reconstructed. This is a view of stored records, not SELECT * semantics.
--
-- SCOPE: ordinary rowid tables and local record payloads; legal page sizes;
-- UTF-8/UTF-16LE/UTF-16BE; 1-9 byte varints; SQLite's ordinary stored value types.
-- Use a small, consistent, checkpointed copy of the main database file. The script
-- loads its pages into memory. It does not reconcile WAL/journals, follow overflow
-- payloads, recover deleted data, read virtual/WITHOUT ROWID tables or validate
-- every invariant in a damaged file. Oversized local payloads are rejected.
-- Recursive CTE execution limits still apply to deep trees and very wide records.
--
-- Two coordinate systems remain important throughout:
--   file offset = (page number - 1) * page size + offset within the page.
-- Page numbers start at 1; byte positions and field positions start at 0.
-- For the recorded firmware page: (5 - 1) * 1024 = 4096.
PARAM(
    databasePath: string = r'./docs/examples/experiments/sqlite-page/inputs/telemetry.db',
    tableName: string = 'firmware'
)

-- 1. VARIABLE-LENGTH INTEGERS
-- SQLite varints use seven data bits per byte until a byte below 128 ends the
-- number. If there are nine bytes, the ninth contributes all eight bits.
-- Examples: 7F = 127; 81 00 = 1*128 + 0 = 128; 81 80 00 = 16384.
-- Conditional B1..B8 fields stop reading after the terminator. Width counts the
-- bytes actually consumed. & 127 clears a continuation bit. ?? 0 handles absent
-- optional bytes in expressions without reading them.
-- Decimal arithmetic is exact for these 64-bit integers. The .0 factors retain
-- that precision through parameter substitution in the tested alpha CLI.
-- A raw value above the signed maximum represents a negative two's-complement
-- rowid; subtracting 2^64 recovers it. Payload lengths/type codes remain positive.
BINARY Varint {
    B0: byte,
    B1: byte WHEN B0 >= 128,
    B2: byte WHEN B1 >= 128,
    B3: byte WHEN B2 >= 128,
    B4: byte WHEN B3 >= 128,
    B5: byte WHEN B4 >= 128,
    B6: byte WHEN B5 >= 128,
    B7: byte WHEN B6 >= 128,
    B8: byte WHEN B7 >= 128,
    Width: CASE
        WHEN B0 < 128 THEN 1
        WHEN B1 < 128 THEN 2
        WHEN B2 < 128 THEN 3
        WHEN B3 < 128 THEN 4
        WHEN B4 < 128 THEN 5
        WHEN B5 < 128 THEN 6
        WHEN B6 < 128 THEN 7
        WHEN B7 < 128 THEN 8
        ELSE 9
    END,
    UnsignedValue: CASE
        WHEN Width = 1 THEN (B0 & 127) * 1.0
        WHEN Width = 2 THEN (B0 & 127) * 128.0 + ((B1 ?? 0) & 127) * 1.0
        WHEN Width = 3 THEN (B0 & 127) * 16384.0 + ((B1 ?? 0) & 127) * 128.0 + (
                (B2 ?? 0) & 127
            ) * 1.0
        WHEN
            Width = 4
        THEN
            (B0 & 127) * 2097152.0 + ((B1 ?? 0) & 127) * 16384.0 + ((B2 ?? 0) & 127) * 128.0 + (
                (B3 ?? 0) & 127
            ) * 1.0
        WHEN
            Width = 5
        THEN
            (B0 & 127) * 268435456.0 + ((B1 ?? 0) & 127) * 2097152.0 + (
                (B2 ?? 0) & 127
            ) * 16384.0 + ((B3 ?? 0) & 127) * 128.0 + ((B4 ?? 0) & 127) * 1.0
        WHEN
            Width = 6
        THEN
            (B0 & 127) * 34359738368.0 + ((B1 ?? 0) & 127) * 268435456.0 + (
                (B2 ?? 0) & 127
            ) * 2097152.0 + ((B3 ?? 0) & 127) * 16384.0 + ((B4 ?? 0) & 127) * 128.0 + (
                (B5 ?? 0) & 127
            ) * 1.0
        WHEN
            Width = 7
        THEN
            (B0 & 127) * 4398046511104.0 + ((B1 ?? 0) & 127) * 34359738368.0 + (
                (B2 ?? 0) & 127
            ) * 268435456.0 + ((B3 ?? 0) & 127) * 2097152.0 + ((B4 ?? 0) & 127) * 16384.0 + (
                (B5 ?? 0) & 127
            ) * 128.0 + ((B6 ?? 0) & 127) * 1.0
        WHEN
            Width = 8
        THEN
            (B0 & 127) * 562949953421312.0 + ((B1 ?? 0) & 127) * 4398046511104.0 + (
                (B2 ?? 0) & 127
            ) * 34359738368.0 + ((B3 ?? 0) & 127) * 268435456.0 + ((B4 ?? 0) & 127) * 2097152.0 + (
                (B5 ?? 0) & 127
            ) * 16384.0 + ((B6 ?? 0) & 127) * 128.0 + ((B7 ?? 0) & 127) * 1.0
        ELSE
            (B0 & 127) * 144115188075855872.0 + ((B1 ?? 0) & 127) * 1125899906842624.0 + (
                (B2 ?? 0) & 127
            ) * 8796093022208.0 + ((B3 ?? 0) & 127) * 68719476736.0 + (
                (B4 ?? 0) & 127
            ) * 536870912.0 + ((B5 ?? 0) & 127) * 4194304.0 + ((B6 ?? 0) & 127) * 32768.0 + (
                (B7 ?? 0) & 127
            ) * 256.0 + (B8 ?? 0) * 1.0
    END,
    Value: CASE
        WHEN UnsignedValue > 9223372036854775807.0 THEN UnsignedValue - 18446744073709551616.0
        ELSE UnsignedValue * 1.0
    END
};

-- SQLite also stores signed fixed-width integers. Three- and six-byte formats
-- combine a signed high part with an unsigned low part. For example, a high
-- byte of -1 and low word FFFF give -1*65536 + 65535 = -1.
-- Schema computed fields consume no bytes. be means big-endian: high bytes first.
BINARY Int24 {
    High: sbyte,
    Low: ushort be,
    Value: High * 65536.0 + Low
};

BINARY Int48 {
    High: short be,
    Low: uint be,
    Value: High * 4294967296.0 + Low
};

-- 2. THE DATABASE HEADER AND PAGES
-- A named nested schema uses parent.PageSize to read a size from its container.
-- This block represents a whole page, including the database header on page 1.
BINARY PageBlock {
    Data: byte[parent.PageSize]
};

-- The first 16 bytes are SQLite's exact signature, including the final zero.
-- Header bytes 16..17 give page size; the special value 1 means 65536 bytes.
-- Byte 20 gives reserved bytes at each page end. Bytes 28..31 give page count;
-- bytes 56..59 give encoding: 1=UTF-8, 2=UTF-16LE, 3=UTF-16BE.
-- AT is absolute within this input buffer. Pages AT 0 deliberately seeks back
-- and reads the file as pages after extracting the header facts.
-- A *Check byte array consumes zero bytes when its condition is true. A size of
-- -1 deliberately fails interpretation. This uses the supported parameterized
-- schema path in the tested alpha CLI, whose CHECK serialization is incomplete.
BINARY Database {
    Signature: byte[16] magic[
        0x53,
        0x51,
        0x4C,
        0x69,
        0x74,
        0x65,
        0x20,
        0x66,
        0x6F,
        0x72,
        0x6D,
        0x61,
        0x74,
        0x20,
        0x33,
        0
    ],
    RawPageSize: ushort be oneOf[1, 512, 1024, 2048, 4096, 8192, 16384, 32768],
    PageSize: CASE
        WHEN RawPageSize = 1 THEN 65536
        ELSE RawPageSize
    END,
    Reserved: byte AT 20,
    PageCount: uint be AT 28,
    PageCountCheck: byte[
        CASE
            WHEN PageCount > 0 THEN 0
            ELSE -1
        END
    ],
    Encoding: uint be oneOf[1, 2, 3] AT 56,
    Pages: PageBlock[PageCount] AT 0
};

-- 3. B-TREE PAGE HEADERS
-- A table leaf (13 / 0D) stores records; a table branch (5 / 05) stores child-page
-- pointers. Both have cell count at +3. Leaf pointer slots begin at +8; branch
-- slots begin at +12, after the four-byte rightmost child pointer at +8.
-- Each cell pointer is a two-byte page-relative location, not a row ID.
-- The first page's b-tree header starts after its 100-byte database header.
-- The query supplies that header slice; actual cell reads still use a full page.
BINARY PageHeader {
    Kind: byte,
    CellCount: ushort be AT 3 WHEN Kind = 5
    OR Kind = 13,
    RightChild: uint be AT 8 WHEN Kind = 5,
    Pointers: ushort be[CellCount ?? 0] AT CASE
        WHEN Kind = 5 THEN 12
        ELSE 8
    END
};

-- Strict checks distinguish an unknown/non-storage table from an empty table,
-- and reject index-style roots used by WITHOUT ROWID tables. magic and oneOf
-- validate values; Interpret fails the query if a check fails.
BINARY TableExists {
    Present: byte magic 1
};

BINARY OrdinaryTable {
    Kind: byte oneOf[5, 13]
};

BINARY ChildPointer {
    PageNumber: uint be
};

-- 4. LEAF CELLS AND LOCAL PAYLOADS
-- A table-leaf cell is [payload-size varint][rowid varint][record payload].
-- The first five bytes below are a temporary descriptor copied from the database
-- header: page size, two version bytes, reserved count. The query prepends them
-- to a cell slice so this layout can check the local-payload bound itself.
-- They are not bytes stored at the beginning of an actual SQLite cell.
-- For usable page size U, a table payload of at most U-35 bytes stays local.
-- Larger payloads need overflow-page handling, which this experiment rejects.
-- Payload is then a bounded array containing only that record's original bytes.
BINARY LocalCell {
    RawPageSize: ushort be,
    _: byte[2],
    Reserved: byte,
    UsableSize: CASE
        WHEN RawPageSize = 1 THEN 65536
        ELSE RawPageSize
    END - Reserved,
    PayloadSize: Varint,
    PayloadSizeCheck: byte[
        CASE
            WHEN
                PayloadSize.Value >= 0
                AND PayloadSize.Value <= UsableSize - 35
            THEN
                0
            ELSE -1
        END
    ],
    RowId: Varint,
    Payload: byte[PayloadSize.Value]
};

-- 5. FIND THE TABLE THROUGH SQLITE'S OWN CATALOG
-- Page 1 is the root of sqlite_schema, an ordinary table with five stored fields:
-- type, name, table name, root page number, CREATE statement.
-- Its record header starts with its own byte count, followed by five serial-type
-- varints. That count includes the count varint itself. HeaderCheck verifies it.
-- The first three fields are strings; a text type N has (N-13)/2 bytes.
-- The root page is a stored integer, whose width is described by Type3.
-- We consume those fields and leave CREATE SQL uninterpreted. It is not needed
-- to locate a table, and this script does not implement a CREATE TABLE parser.
-- Encoding is another temporary prefix, copied from header bytes 56..59.
-- Only the matching UTF string branch consumes bytes; the others remain absent.
BINARY CatalogRecord {
    Encoding: uint be,
    HeaderSize: Varint,
    Type0: Varint,
    Type1: Varint,
    Type2: Varint,
    Type3: Varint,
    Type4: Varint,
    HeaderCheck: byte[
        CASE
            WHEN
                HeaderSize.Value = HeaderSize.Width + Type0.Width + Type1.Width + Type2.Width + Type3.Width + Type4.Width
                AND Type0.Value >= 13
                AND Type0.Value % 2 = 1
                AND Type1.Value >= 13
                AND Type1.Value % 2 = 1
                AND Type2.Value >= 13
                AND Type2.Value % 2 = 1
            THEN
                0
            ELSE -1
        END
    ],
    ObjectType8: string[(Type0.Value - 13) / 2] utf8 WHEN Encoding = 1,
    ObjectType16LE: string[(Type0.Value - 13) / 2] utf16le WHEN Encoding = 2,
    ObjectType16BE: string[(Type0.Value - 13) / 2] utf16be WHEN Encoding = 3,
    Name8: string[(Type1.Value - 13) / 2] utf8 WHEN Encoding = 1,
    Name16LE: string[(Type1.Value - 13) / 2] utf16le WHEN Encoding = 2,
    Name16BE: string[(Type1.Value - 13) / 2] utf16be WHEN Encoding = 3,
    Table8: string[(Type2.Value - 13) / 2] utf8 WHEN Encoding = 1,
    Table16LE: string[(Type2.Value - 13) / 2] utf16le WHEN Encoding = 2,
    Table16BE: string[(Type2.Value - 13) / 2] utf16be WHEN Encoding = 3,
    Root1: sbyte WHEN Type3.Value = 1,
    Root2: short be WHEN Type3.Value = 2,
    Root3: Int24 WHEN Type3.Value = 3,
    Root4: int be WHEN Type3.Value = 4,
    Root6: Int48 WHEN Type3.Value = 5,
    Root8: long be WHEN Type3.Value = 6,
    RootPage: CASE
        WHEN
            Type3.Value = 0
            OR Type3.Value = 8
        THEN
            (0 * 1.0)
        WHEN Type3.Value = 9 THEN 1.0
        WHEN Type3.Value = 1 THEN Root1 * 1.0
        WHEN Type3.Value = 2 THEN Root2 * 1.0
        WHEN Type3.Value = 3 THEN Root3.Value * 1.0
        WHEN Type3.Value = 4 THEN Root4 * 1.0
        WHEN Type3.Value = 5 THEN Root6.Value * 1.0
        WHEN Type3.Value = 6 THEN Root8
        ELSE NULL
    END,
    ObjectType: ObjectType8 ?? ObjectType16LE ?? ObjectType16BE,
    Name: Name8 ?? Name16LE ?? Name16BE,
    TableName: Table8 ?? Table16LE ?? Table16BE
};

-- 6. DESCRIBE AN ARBITRARY RECORD'S FIELDS
-- A user record has the same header/body framing, but any number of type codes.
-- Keep its raw header bytes so SQL can walk the varints and accumulate field
-- offsets. The sized array also prevents a header from extending past its payload.
BINARY RecordHeader {
    HeaderSize: Varint,
    HeaderSizeCheck: byte[
        CASE
            WHEN HeaderSize.Value >= HeaderSize.Width THEN 0
            ELSE -1
        END
    ],
    TypeBytes: byte[HeaderSize.Value - HeaderSize.Width]
};

-- Serial-type sizes, in bytes:
--   0=NULL; 1/2/3/4/5/6=signed integers of 1/2/3/4/6/8 bytes; 7=8-byte real;
--   8=integer zero, 9=integer one (both consume zero body bytes).
--   Even N>=12 is a blob of (N-12)/2 bytes; odd N>=13 is text of (N-13)/2 bytes.
-- Thus text codes 19 and 27 mean 3 and 7 bytes. Code 13 is empty text; 12 is an
-- empty blob. These are byte lengths, so a UTF character may occupy several bytes.
-- Codes 10/11 are reserved. Candidate positions are inspected before SQL follows
-- the actual varint chain; FieldValue rejects reserved codes on that chain.
BINARY SerialType {
    Code: Varint,
    ByteLength: CASE
        WHEN
            Code.Value = 0
            OR Code.Value = 8
            OR Code.Value = 9
        THEN
            0
        WHEN
            (
                Code.Value >= 1
                AND Code.Value <= 4
            )
        THEN
            Code.Value * 1.0
        WHEN Code.Value = 5 THEN 6
        WHEN
            Code.Value = 6
            OR Code.Value = 7
        THEN
            8
        WHEN
            Code.Value >= 13
            AND Code.Value % 2 = 1
        THEN
            (Code.Value - 13) / 2
        WHEN Code.Value >= 12 THEN (Code.Value - 12) / 2
        ELSE -1
    END
};

-- 7. DECODE ONE FIELD
-- The query builds a temporary buffer: [encoding bytes][original type varint]
-- [original field bytes]. This descriptor makes one layout reusable for every
-- stored field, without assuming a fixed table shape.
-- Numeric fields use signed big-endian or IEEE-754 double decoding. Text uses
-- exactly its declared byte length. Conditional fields consume bytes only for
-- the active type/encoding. Empty text/blob are handled explicitly by the query.
BINARY FieldValue {
    Encoding: uint be oneOf[1, 2, 3],
    Serial: Varint,
    SerialCheck: byte[
        CASE
            WHEN
                Serial.Value >= 0
                AND Serial.Value <> 10
                AND Serial.Value <> 11
            THEN
                0
            ELSE -1
        END
    ],
    Int1: sbyte WHEN Serial.Value = 1,
    Int2: short be WHEN Serial.Value = 2,
    Int3: Int24 WHEN Serial.Value = 3,
    Int4: int be WHEN Serial.Value = 4,
    Int6: Int48 WHEN Serial.Value = 5,
    Int8: long be WHEN Serial.Value = 6,
    RealValue: double be WHEN Serial.Value = 7,
    Text8: string[
        CASE
            WHEN Serial.Value >= 13 THEN (Serial.Value - 13) / 2
            ELSE 0
        END
    ] utf8 WHEN Serial.Value > 13
    AND Serial.Value % 2 = 1
    AND Encoding = 1,
    Text16LE: string[
        CASE
            WHEN Serial.Value >= 13 THEN (Serial.Value - 13) / 2
            ELSE 0
        END
    ] utf16le WHEN Serial.Value > 13
    AND Serial.Value % 2 = 1
    AND Encoding = 2,
    Text16BE: string[
        CASE
            WHEN Serial.Value >= 13 THEN (Serial.Value - 13) / 2
            ELSE 0
        END
    ] utf16be WHEN Serial.Value > 13
    AND Serial.Value % 2 = 1
    AND Encoding = 3,
    BlobValue: byte[
        CASE
            WHEN Serial.Value >= 12 THEN (Serial.Value - 12) / 2
            ELSE 0
        END
    ] WHEN Serial.Value > 12
    AND Serial.Value % 2 = 0,
    IntegerValue: CASE
        WHEN Serial.Value = 8 THEN (0 * 1.0)
        WHEN Serial.Value = 9 THEN 1.0
        WHEN Serial.Value = 1 THEN Int1 * 1.0
        WHEN Serial.Value = 2 THEN Int2 * 1.0
        WHEN Serial.Value = 3 THEN Int3.Value * 1.0
        WHEN Serial.Value = 4 THEN Int4 * 1.0
        WHEN Serial.Value = 5 THEN Int6.Value * 1.0
        WHEN Serial.Value = 6 THEN Int8
        ELSE NULL
    END,
    TextValue: CASE
        WHEN Serial.Value = 13 THEN ''
        ELSE Text8 ?? Text16LE ?? Text16BE
    END
};

-- 8. APPLY THE LAYOUTS TO THE FILE
-- os.file opens the selected file entity; GetFileBytes reads its original bytes.
-- CROSS APPLY expands a decoded record or array into query rows. WITH ORDINALITY
-- supplies a zero-based Ordinal, making page number = Ordinal + 1.
-- The one-page walkthrough uses the simpler GetFileBytes(COUNT, OFFSET) form.
-- This reader loads the whole file, then works with in-memory page/cell slices.
WITH RECURSIVE pages AS (
    SELECT
        block.Ordinal + 1 AS PageNumber,
        block.Data AS Data,
        db.PageSize AS PageSize,
        db.Reserved AS Reserved,
        h.Kind AS Kind,
        h.RightChild AS RightChild,
        h.Pointers AS Pointers,
        f.GetFileBytes(5, 16) AS PageDescriptor,
        f.GetFileBytes(4, 56) AS EncodingBytes
    FROM os.file($databasePath) f
    CROSS APPLY Interpret<Database>(f.GetFileBytes()) db
    CROSS APPLY db.Pages block WITH ORDINALITY
    CROSS APPLY Interpret<PageHeader>(
        f.EnumerableToArray(
            f.Skip(
                block.Data,
                CASE
                    WHEN block.Ordinal = 0 THEN 100
                    ELSE 0
                END
            )
        )
    ) h
),

-- A branch has one left-child page number at each cell plus a rightmost child.
-- These edges describe table-tree relationships. Leaf cells are decoded later.
edges AS (
    SELECT p.PageNumber AS Parent, (p.ToInt32(p.RightChild) ?? 0) AS Child
    FROM pages p
    WHERE
        p.Kind = 5
    UNION ALL
    SELECT p.PageNumber AS Parent, (p.ToInt32(c.PageNumber) ?? 0) AS Child
    FROM pages p
    CROSS APPLY p.Pointers ptr
    CROSS APPLY InterpretAt<ChildPointer>(p.Data, p.ToInt32(ptr.Value) ?? 0) c
    WHERE
        p.Kind = 5
),

-- Start at page 1 and follow branch edges until the catalog's leaves are reached.
-- UNION ALL plus the recursive join lets the runtime perform the tree traversal.
catalog_tree AS (
    SELECT 1 AS PageNumber
    FROM os.file($databasePath) f
    UNION ALL
    SELECT e.Child AS PageNumber
    FROM catalog_tree t
    INNER JOIN edges e
        ON t.PageNumber = e.Parent
),

-- At each catalog leaf, follow its cell pointers and decode the catalog record.
-- SkipAndTake and EnumerableToArray create bounded byte slices. MergeArrays adds
-- the copied descriptors; it never changes the input file.
-- ToInt32(... ) ?? 0 meets InterpretAt's non-nullable integer offset contract.
-- The supported fixture/data layouts supply valid pointers; this fallback is not
-- a substitute for validating an arbitrary damaged file.
catalog AS (
    SELECT entry.Name AS Name, entry.TableName AS TableName, entry.RootPage AS RootPage
    FROM catalog_tree t
    INNER JOIN pages p
        ON t.PageNumber = p.PageNumber
        AND p.Kind = 13
    CROSS APPLY p.Pointers ptr
    CROSS APPLY Interpret<LocalCell>(
        p.MergeArrays(
            p.PageDescriptor,
            p.EnumerableToArray(
                p.SkipAndTake(
                    p.Data,
                    p.ToInt32(ptr.Value) ?? 0,
                    p.PageSize - p.Reserved - ptr.Value
                )
            )
        )
    ) cell
    CROSS APPLY Interpret<CatalogRecord>(p.MergeArrays(p.EncodingBytes, cell.Payload)) entry
    WHERE
        entry.ObjectType = 'table'
),

-- Match the table name as a parameter value. An empty table still has a catalog
-- entry and root page. Missing names and virtual tables without a storage root
-- fail the presence check instead of looking like an empty result.
requested_table AS (
    SELECT (f.ToInt32(c.RootPage) ?? 0) AS RootPage, present.Present AS Present
    FROM os.file($databasePath) f
    LEFT JOIN catalog c
        ON c.Name = $tableName
    CROSS APPLY Interpret<TableExists>(
        f.FromHexToBytes(
            CASE
                WHEN
                    c.Name IS NULL
                    OR c.RootPage <= 0
                THEN
                    '00'
                ELSE '01'
            END
        )
    ) present
    WHERE
        present.Present = 1
),

selected_root AS (
    SELECT requested.RootPage AS PageNumber
    FROM requested_table requested
    INNER JOIN pages p
        ON requested.RootPage = p.PageNumber
    CROSS APPLY Interpret<OrdinaryTable>(p.Data) ordinary
    WHERE
        ordinary.Kind = 5
        OR ordinary.Kind = 13
),

-- Start from that root and follow only its child pages. This separates the chosen
-- table from every other table sharing the same database file.
table_tree AS (
    SELECT selected.PageNumber AS PageNumber
    FROM selected_root selected
    UNION ALL
    SELECT e.Child AS PageNumber
    FROM table_tree t
    INNER JOIN edges e
        ON t.PageNumber = e.Parent
),

-- On the selected leaves, expose rowid, bounded payload and record-header bytes.
-- A cell begins at the full-page pointer even on page 1. RowId and CellOffset
-- answer different questions: record identity versus physical location.
records AS (
    SELECT
        p.PageNumber AS PageNumber,
        ptr.Value AS CellOffset,
        (p.ToInt64(cell.RowId.Value) ?? 0) AS RowId,
        cell.Payload AS Payload,
        header.HeaderSize.Value AS HeaderSize,
        header.HeaderSize.Width AS HeaderSizeWidth,
        header.TypeBytes AS TypeBytes,
        p.EncodingBytes AS EncodingBytes
    FROM table_tree t
    INNER JOIN pages p
        ON t.PageNumber = p.PageNumber
        AND p.Kind = 13
    CROSS APPLY p.Pointers ptr
    CROSS APPLY Interpret<LocalCell>(
        p.MergeArrays(
            p.PageDescriptor,
            p.EnumerableToArray(
                p.SkipAndTake(
                    p.Data,
                    p.ToInt32(ptr.Value) ?? 0,
                    p.PageSize - p.Reserved - ptr.Value
                )
            )
        )
    ) cell
    CROSS APPLY Interpret<RecordHeader>(cell.Payload) header
),

-- Inspect candidate varints at every byte of a record's type-code area. The next
-- CTE chooses only actual boundaries, advancing by each decoded varint's Width.
-- Keeping interpretation outside recursive members supports the tested CLI.
codes AS (
    SELECT
        r.PageNumber AS PageNumber,
        r.CellOffset AS CellOffset,
        r.RowId AS RowId,
        r.HeaderSize AS HeaderSize,
        r.HeaderSizeWidth AS HeaderSizeWidth,
        r.HeaderSizeWidth + headerByte.Ordinal AS TypeOffset,
        decoded.Code.Value AS SerialType,
        decoded.Code.Width AS TypeWidth,
        decoded.ByteLength AS ByteLength
    FROM records r
    CROSS APPLY r.TypeBytes headerByte WITH ORDINALITY
    CROSS APPLY InterpretAt<SerialType>(
        r.Payload,
        r.ToInt32(r.HeaderSizeWidth + headerByte.Ordinal) ?? 0
    ) decoded
),

-- Walk the header and body together. The first type begins after the header-size
-- varint; the first value begins at HeaderSize. Each next type starts TypeWidth
-- bytes later, and each next value starts ByteLength bytes later.
-- For the movie's first firmware row: header=3, type sizes=3 and 7, payload=13.
-- This gives body offsets 3 and 6, and 3+3+7=13 accounts for every payload byte.
-- Only coordinates and scalar metadata travel through recursion; payload arrays
-- stay in records and are joined back when needed.
field_positions AS (
    SELECT
        candidate.PageNumber AS PageNumber,
        candidate.CellOffset AS CellOffset,
        candidate.RowId AS RowId,
        0 AS FieldIndex,
        candidate.TypeOffset AS TypeOffset,
        candidate.HeaderSize AS BodyOffset,
        candidate.SerialType AS SerialType,
        candidate.TypeWidth AS TypeWidth,
        candidate.ByteLength AS ByteLength,
        candidate.TypeOffset + candidate.TypeWidth = candidate.HeaderSize AS IsLastField
    FROM codes candidate
    WHERE
        candidate.TypeOffset = candidate.HeaderSizeWidth
    UNION ALL
    SELECT
        previous.PageNumber AS PageNumber,
        previous.CellOffset AS CellOffset,
        previous.RowId AS RowId,
        previous.FieldIndex + 1 AS FieldIndex,
        followingCode.TypeOffset AS TypeOffset,
        previous.BodyOffset + previous.ByteLength AS BodyOffset,
        followingCode.SerialType AS SerialType,
        followingCode.TypeWidth AS TypeWidth,
        followingCode.ByteLength AS ByteLength,
        followingCode.TypeOffset + followingCode.TypeWidth = followingCode.HeaderSize AS IsLastField
    FROM field_positions previous
    INNER JOIN codes followingCode
        ON previous.PageNumber = followingCode.PageNumber
        AND previous.CellOffset = followingCode.CellOffset
        AND previous.TypeOffset + previous.TypeWidth = followingCode.TypeOffset
),

-- Decode the selected byte ranges. ValueJson retains strings/escapes, numbers,
-- NULL and explicitly tagged hex blobs. ToJson escapes text correctly; values
-- are not assembled by guessing how quotes or control characters should look.
-- To inspect one field per output row, replace the FINAL SELECT below with:
--   SELECT d.RowId, d.FieldIndex, d.StorageClass, d.ValueJson
--   FROM decoded_values d ORDER BY d.RowId, d.FieldIndex
decoded_values AS (
    SELECT
        $tableName AS TableName,
        p.RowId AS RowId,
        p.FieldIndex AS FieldIndex,
        p.IsLastField AS IsLastField,
        CASE
            WHEN p.SerialType = 0 THEN 'null'
            WHEN
                (
                    p.SerialType >= 1
                    AND p.SerialType <= 6
                )
                OR p.SerialType = 8
                OR p.SerialType = 9
            THEN
                'integer'
            WHEN p.SerialType = 7 THEN 'real'
            WHEN
                p.SerialType >= 12
                AND p.SerialType % 2 = 0
            THEN
                'blob'
            ELSE 'text'
        END AS StorageClass,
        CASE
            WHEN p.SerialType = 0 THEN 'null'
            WHEN
                (
                    p.SerialType >= 1
                    AND p.SerialType <= 6
                )
                OR p.SerialType = 8
                OR p.SerialType = 9
            THEN
                original.ToJson(original.ToInt64(v.IntegerValue))
            WHEN p.SerialType = 7 THEN original.ToJson(v.RealValue)
            WHEN p.SerialType = 12 THEN '{"$blob":""}'
            WHEN
                p.SerialType > 12
                AND p.SerialType % 2 = 0
            THEN
                '{"$blob":' + original.ToJson(original.ToHex(v.BlobValue, '')) + '}'
            ELSE original.ToJson(v.TextValue)
        END AS ValueJson,
        p.PageNumber AS PageNumber,
        p.CellOffset AS CellOffset
    FROM field_positions p
    INNER JOIN records original
        ON p.PageNumber = original.PageNumber
        AND p.CellOffset = original.CellOffset
    CROSS APPLY Interpret<FieldValue>(
        original.MergeArrays(
            original.EncodingBytes,
            original.EnumerableToArray(
                original.SkipAndTake(
                    original.Payload,
                    original.ToInt32(p.TypeOffset) ?? 0,
                    p.TypeWidth
                )
            ),
            original.EnumerableToArray(
                original.SkipAndTake(
                    original.Payload,
                    original.ToInt32(p.BodyOffset) ?? 0,
                    original.ToInt32(p.ByteLength) ?? 0
                )
            )
        )
    ) v
),

-- Build each output JSON array in field-position order through a recursive join.
-- This avoids depending on an aggregate's encounter order. It also preserves
-- records whose fields all occupy zero bytes, such as NULL/zero/one/empty values.
row_values AS (
    SELECT
        firstValue.RowId AS RowId,
        firstValue.PageNumber AS PageNumber,
        firstValue.CellOffset AS CellOffset,
        firstValue.FieldIndex AS FieldIndex,
        firstValue.IsLastField AS IsLastField,
        '[' + firstValue.ValueJson AS AccumulatedJson
    FROM decoded_values firstValue
    WHERE
        firstValue.FieldIndex = 0
    UNION ALL
    SELECT
        previous.RowId AS RowId,
        previous.PageNumber AS PageNumber,
        previous.CellOffset AS CellOffset,
        nextValue.FieldIndex AS FieldIndex,
        nextValue.IsLastField AS IsLastField,
        previous.AccumulatedJson + ',' + nextValue.ValueJson AS AccumulatedJson
    FROM row_values previous
    INNER JOIN decoded_values nextValue
        ON previous.PageNumber = nextValue.PageNumber
        AND previous.CellOffset = nextValue.CellOffset
        AND previous.FieldIndex + 1 = nextValue.FieldIndex
)
-- FINAL SELECT: keep the last assembly step for each stored row and close its
-- JSON array. ORDER BY RowId is logical key order, not physical cell order.
-- Format reference: https://www.sqlite.org/fileformat.html
SELECT
    $tableName AS TableName,
    assembled.RowId AS RowId,
    assembled.AccumulatedJson + ']' AS StoredValuesJson,
    assembled.PageNumber AS PageNumber,
    assembled.CellOffset AS CellOffset
FROM row_values assembled
WHERE
    assembled.IsLastField
ORDER BY assembled.RowId
