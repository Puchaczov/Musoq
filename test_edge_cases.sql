-- Test what happens with out-of-bounds array access

-- Test 1: Valid array access (should work)
select Self.Array[2] from #A.Entities();

-- Test 2: Out-of-bounds array access (index beyond array length)
select Self.Array[10] from #A.Entities();

-- Test 3: Negative array index
select Self.Array[-1] from #A.Entities();

-- Test 4: String character access beyond string length
select Name[100] from #A.Entities() where Name = 'david.jones@proseware.com';

-- Test 5: String character access on null/empty string
select Name[0] from #A.Entities() where Name is null;
