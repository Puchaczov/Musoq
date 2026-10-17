binary KInstanceInSizedQux {
    inst_invoked: byte at 1,
    seq_f: byte at 0 when inst_invoked > 0,
    inst_unused_by_seq: byte[2] at 2
};

binary KInstanceInSizedBar {
    seq_f: byte,
    inst: byte[3] at 5
};

binary KInstanceInSizedBaz {
    seq_f: byte,
    inst: byte[3] at 9
};

binary KInstanceInSizedWrapper {
    seq_sized: substream[4] as KInstanceInSizedQux lax,
    seq_in_stream: KInstanceInSizedBar,
    inst_in_stream: KInstanceInSizedBaz at 8,
    inst_sized: substream[4] as KInstanceInSizedQux lax at 12
};

binary KInstanceInSized {
    cont: substream[16] as KInstanceInSizedWrapper lax
};


SELECT r.cont.seq_sized.seq_f AS A0, r.cont.seq_sized.inst_invoked AS A1, r.cont.seq_sized.inst_unused_by_seq AS A2, r.cont.seq_in_stream.seq_f AS A3, r.cont.seq_in_stream.inst AS A4, r.cont.inst_in_stream.seq_f AS A5, r.cont.inst_in_stream.inst AS A6, r.cont.inst_sized.seq_f AS A7, r.cont.inst_sized.inst_invoked AS A8, r.cont.inst_sized.inst_unused_by_seq AS A9
FROM #fixture.bytes() f
CROSS APPLY Interpret<KInstanceInSized>(f.Content) r
