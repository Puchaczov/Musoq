binary KIfStructArgStr {
    len: byte,
    str: string[len] utf8
};

binary KIfStructArgTuple {
    num1: byte,
    num2: byte
};

binary KIfStructOperation {
    opcode: byte,
    arg_tuple: KIfStructArgTuple when opcode = 0x54,
    arg_str: KIfStructArgStr when opcode = 0x53
};

binary KIfStruct {
    op1: KIfStructOperation,
    op2: KIfStructOperation,
    op3: KIfStructOperation
};

SELECT r.op1.opcode AS A0, r.op1.arg_tuple AS A1, r.op1.arg_str.str AS A2, r.op2.opcode AS A3, r.op2.arg_tuple.num1 AS A4, r.op2.arg_tuple.num2 AS A5, r.op2.arg_str AS A6, r.op3.opcode AS A7, r.op3.arg_tuple AS A8, r.op3.arg_str.str AS A9
FROM #fixture.bytes() f
CROSS APPLY Interpret<KIfStruct>(f.Content) r
