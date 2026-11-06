-- READ THREE SQLITE ROWS USING ONLY MUSOQ AND FILE BYTES
--
-- This is one runnable, annotated script for the "Experiments with Musoq" movie.
-- Run from the repository root:
--   musoq run docs/examples/experiments/sqlite-page/read-sqlite-page.sql --from-file --format json
--
-- The main query runs as-is and returns RowId, Version and Status.
-- Four optional probe blocks at the end let you repeat the intermediate steps.
-- Their comments explain how to run them without adding a second active query.
-- Requires a running Musoq instance with the Os provider and binary interpretation.
--
-- WHAT THE AGENT KNEW, AND WHAT IT HAD TO READ
--
-- Supplied facts: the input is SQLite; the firmware table has two TEXT columns;
-- its root is page 5; pages are 1024 bytes; the fixture uses UTF-8. Format notes
-- were supplied too. The decoded values were not supplied to the recorded agent.
-- It used successive queries to inspect bytes, establish this page's record
-- boundaries and lengths, and finally decode the values. This is a guided look
-- inside one known page, not automatic discovery of a whole database format.
--
-- The synthetic database has four tables and fifteen pages. We inspect only the
-- three firmware records. A separate SQLite check gives this expected answer:
--   RowId  Version  Status
--       1  1.1      retired
--       2  1.2      stable
--       3  1.3      canary
-- The Musoq reader below does not call SQLite or read an exported table.
--
-- 1. PAGES: THERE ARE TWO DIFFERENT KINDS OF OFFSET
--
-- A SQLite file is divided into equal-sized pages, numbered starting at 1.
-- Our page starts at file byte (5 - 1) * 1024 = 4096. Its 1024 bytes occupy
-- file positions 4096 through 5119. A position within this page starts at 0.
-- Therefore: file position = 4096 + page-relative position.
--
-- f.GetFileBytes(1024, 4096) means "read 1024 bytes, starting at file byte 4096".
-- Its argument order is COUNT first, OFFSET second. It produces a byte array.
-- The format declarations read that array; they do not create or modify a file.
-- SQLite's separate 100-byte database header is on page 1, not on our page 5.
--
-- PARAM declares a reusable input. $root refers to its value. The r'...' form is
-- a literal string with no backslash escaping. This default is relative to the
-- repository root used for the run; the companion README shows an absolute override.
PARAM(root: string = r'./docs/examples/experiments/sqlite-page/inputs')

-- 2. A LEAF-PAGE HEADER GIVES US THE ROW LOCATIONS
--
-- A table leaf page stores records directly. Its eight-byte header looks like:
--   Page offset  Bytes    Meaning in this fixture
--   +0           0D       13: this is a table leaf page
--   +1..+2       00 00    no first freeblock
--   +3..+4       00 03    three cells (stored row records)
--   +5..+6       03 D5    cell content begins at page byte 981
--   +7           00       no fragmented free bytes
-- Immediately after that header comes a list of two-byte cell pointers:
--   +8..+9       03 F1    row starts at page byte 1009 -> file byte 5105
--   +10..+11     03 E3    row starts at page byte  995 -> file byte 5091
--   +12..+13     03 D5    row starts at page byte  981 -> file byte 5077
-- A big-endian value puts its most significant byte first: 03 F1 is
-- 3 * 256 + 241 = 1009. The pointer is a location, not a row ID.
-- The records happen to sit near the page end, with free space before them.
--
-- BINARY names a layout. ushort be reads an unsigned, two-byte big-endian number.
-- AT 3 means "at byte 3 of the interpreted input". Our input is the page slice,
-- so this means file byte 4099, NOT file byte 3.
-- ushort be[CellCount] repeats that two-byte read CellCount times. Here it reads
-- three pointers at page offsets 8, 10 and 12. No loop is needed in our query.
BINARY PageHeader {
    CellCount: ushort be AT 3,
    Pointer: ushort be[CellCount] AT 8
};

-- 3. INSPECT THE START OF EACH CELL BEFORE TRYING TO READ TEXT
--
-- The first five bytes at the three pointers are, in hexadecimal:
--   page +1009: 0D 01 03 13 1B
--   page  +995: 0C 02 03 13 19
--   page  +981: 0C 03 03 13 19
-- A table-leaf cell contains:
--   [payload-byte count] [rowid] [record payload]
-- The record payload contains:
--   [record-header-byte count] [field type codes] [field data]
--
-- These counts, row IDs and type codes normally use SQLite variable-length
-- integers, which can occupy several bytes. If the first byte is below 128,
-- the integer ends immediately in that byte. Every relevant first byte in
-- these three prefixes is below 128, so this small reader can use byte
-- (one byte) for each of the five values.
-- This shortcut is supported by these observations, not valid for every table.
-- RowId is SQLite's stored integer record key; it is not a byte offset.
--
-- 4. THE RECORD HEADER TELLS US THE FIELD LENGTHS
--
-- In row 1, 0D 01 03 13 1B means:
--   0D = 13 payload bytes, excluding the payload-length and rowid integers
--   01 = rowid 1
--   03 = 3 record-header bytes, including this header-length byte itself
--   13 = decimal 19: the first field's serial type (stored type/length code)
--   1B = decimal 27: the second field's serial type
--
-- An odd serial type N >= 13 describes TEXT occupying (N - 13) / 2 bytes.
-- Thus type 19 occupies 3 bytes; type 27 occupies 7; type 25 occupies 6.
-- These are byte counts, not character counts. The database's text encoding
-- determines how to read the bytes. Our supplied fixture uses UTF-8.
-- The column "Version" is TEXT, so "1.1" stays a string, not a decimal number.
--
-- Row 1's complete cell is fifteen bytes:
--   0D 01 | 03 13 1B | 31 2E 31 | 72 65 74 69 72 65 64
--   cell    record     "1.1"      "retired"
--   prefix  header
-- Its record payload is 13 = 3 header bytes + 3 version bytes + 7 status bytes.
-- Its whole cell is 15 = 1 payload-length byte + 1 rowid byte + 13 payload bytes.
-- 1009 + 15 = 1024, so that cell ends exactly at the end of this page.
-- The other two cells are 14 bytes each, with payloads of 3 + 3 + 6 = 12 bytes.
-- The recorded agent checked these totals BEFORE adding the text decoder.
-- Optional probe 4 below repeats the payload and page-boundary checks.
--
-- Fields without AT are read in sequence, starting at the current cursor.
-- string[size] utf8 consumes exactly size bytes, then decodes those bytes.
-- The expressions use the type codes already read from this same record.
BINARY FirmwareCell {
    PayloadByteCount: byte,
    RowId: byte,
    HeaderByteCount: byte,
    VersionSerialType: byte,
    StatusSerialType: byte,
    Version: string[(VersionSerialType - 13) / 2] utf8,
    Status: string[(StatusSerialType - 13) / 2] utf8
};

-- 5. RUN THE FINAL QUERY
--
-- os.files lists file entities; false disables recursion. Alias f is one file.
-- Interpret<PageHeader>(...) reads a page using our declared layout. Its fields
-- become available through alias h. CROSS APPLY h.Pointer emits one query row
-- per array element; ptr.Value is the numeric page-relative pointer.
--
-- InterpretAt<FirmwareCell>(pageBytes, offset) starts the sequential cell layout
-- at that offset inside the page buffer. Its argument is a PAGE offset: we do
-- not add 4096 again when addressing a position inside the already-sliced page.
-- InterpretAt changes the starting cursor, not the origin used by any AT fields.
-- FirmwareCell has no AT fields: it reads sequentially from the chosen pointer.
--
-- ToInt32 returns a nullable integer; ?? 0 supplies the non-nullable int required
-- by InterpretAt. All observed pointers are present and valid here, so this
-- fallback is unused. It is not a general check for malformed pointers.
--
-- The runtime performs the per-file/per-pointer iteration. SELECT exposes the
-- decoded values, and ORDER BY returns the records in rowid order. Physical
-- cell locations happen to run in the opposite direction in this fixture.
SELECT cell.RowId AS RowId, cell.Version AS Version, cell.Status AS Status
FROM os.files($root, false) f
CROSS APPLY Interpret<PageHeader>(f.GetFileBytes(1024, 4096)) h
CROSS APPLY h.Pointer ptr
CROSS APPLY InterpretAt<FirmwareCell>(f.GetFileBytes(1024, 4096), ToInt32(ptr.Value) ?? 0) cell
WHERE
    f.Name = 'telemetry.db'
ORDER BY
    cell.RowId
    -- OPTIONAL PROBES: REPEAT THE INVESTIGATION IN THIS SAME FILE
    --
    -- The blocks below are comments, so running this whole file executes only the
    -- final query above. To try a probe, keep the PARAM and BINARY declarations above
    -- "5. RUN THE FINAL QUERY", and REPLACE the final SELECT with the chosen block's
    -- SQL between its PROBE and END PROBE markers; omit the markers themselves.
    -- Probe 3 or 4 also adds its own small BINARY declarations.
    -- Expected results are described before each block. These examples were executed.
    -- Probe 1: ask only for raw bytes. The byte array is displayed as hexadecimal.
    -- Expected: 0D 00 00 00 03 03 D5 00. No text-record schema is involved yet.
    /* PROBE 1
SELECT f.ToHex(f.GetFileBytes(8, 4096), ' ') AS HeaderHex
FROM os.files($root, false) f
WHERE
    f.Name = 'telemetry.db'
END PROBE 1 */
    -- Probe 2: apply the page layout and list its pointers. Count = 3 in every row.
    -- Sorted page offsets: 981, 995, 1009; file offsets: 5077, 5091, 5105.
    /* PROBE 2
SELECT h.CellCount AS CellCount, ptr.Value AS PageOffset, 4096 + ptr.Value AS FileOffset
FROM os.files($root, false) f
CROSS APPLY Interpret<PageHeader>(f.GetFileBytes(1024, 4096)) h
CROSS APPLY h.Pointer ptr
WHERE
    f.Name = 'telemetry.db'
ORDER BY ptr.Value
END PROBE 2 */
    -- Probe 3: read only the first five bytes at each pointer, not the text body.
    -- Output numbers are decimal: for example, page +1009 gives 13, 1, 3, 19, 27.
    -- All five values in each row are below 128: the observed varints are one byte.
    /* PROBE 3
BINARY CellPrefix {
    B0: byte,
    B1: byte,
    B2: byte,
    B3: byte,
    B4: byte
};

SELECT
    ptr.Value AS PageOffset,
    prefix.B0 AS PayloadByteCount,
    prefix.B1 AS RowId,
    prefix.B2 AS HeaderByteCount,
    prefix.B3 AS VersionSerialType,
    prefix.B4 AS StatusSerialType
FROM os.files($root, false) f
CROSS APPLY Interpret<PageHeader>(f.GetFileBytes(1024, 4096)) h
CROSS APPLY h.Pointer ptr
CROSS APPLY InterpretAt<CellPrefix>(f.GetFileBytes(1024, 4096), ToInt32(ptr.Value) ?? 0) prefix
WHERE
    f.Name = 'telemetry.db'
ORDER BY ptr.Value
END PROBE 3 */
    -- Probe 4: first bound the payload using its declared size, then read its header
    -- and field byte ranges. byte[N] is an N-byte array; it does not decode text.
    -- Here header length 3 is one header-length byte plus two one-byte type codes.
    -- Expected payload sizes: 13, 12, 12; version lengths: 3, 3, 3; status: 7, 6, 6.
    -- Every PayloadAccountingOk, HeaderSizeOk and CellFitsPage result must be true.
    /* PROBE 4
BINARY TableLeafCell {
    PayloadByteCount: byte,
    RowId: byte,
    Payload: byte[PayloadByteCount]
};

BINARY RecordPayload {
    HeaderByteCount: byte,
    VersionSerialType: byte,
    StatusSerialType: byte,
    VersionBytes: byte[(VersionSerialType - 13) / 2],
    StatusBytes: byte[(StatusSerialType - 13) / 2]
};

SELECT
    cell.RowId AS RowId,
    cell.PayloadByteCount AS PayloadBytes,
    rec.HeaderByteCount AS HeaderBytes,
    Length(rec.VersionBytes) AS VersionBytes,
    Length(rec.StatusBytes) AS StatusBytes,
    (
        cell.PayloadByteCount = rec.HeaderByteCount + Length(rec.VersionBytes) + Length(
            rec.StatusBytes
        )
    ) AS PayloadAccountingOk,
    (rec.HeaderByteCount = 3) AS HeaderSizeOk,
    (ptr.Value + 2 + cell.PayloadByteCount <= 1024) AS CellFitsPage
FROM os.files($root, false) f
CROSS APPLY Interpret<PageHeader>(f.GetFileBytes(1024, 4096)) h
CROSS APPLY h.Pointer ptr
CROSS APPLY InterpretAt<TableLeafCell>(f.GetFileBytes(1024, 4096), ToInt32(ptr.Value) ?? 0) cell
CROSS APPLY Interpret<RecordPayload>(cell.Payload) rec
WHERE
    f.Name = 'telemetry.db'
ORDER BY cell.RowId
END PROBE 4 */
    -- COVERAGE
    -- This experiment reads the supplied leaf page and its two short, nonempty TEXT
    -- fields. It assumes the observed one-byte integers, valid pointers and UTF-8.
    -- It does not implement general varints, catalog discovery, branch traversal,
    -- other serial types, overflow pages, indexes, WITHOUT ROWID tables or WAL/
    -- journal reconciliation. The short final reader is not a full file validator.
    -- SQLite itself was used separately to verify the fixture and expected answer.
    -- Format reference: https://www.sqlite.org/fileformat.html
