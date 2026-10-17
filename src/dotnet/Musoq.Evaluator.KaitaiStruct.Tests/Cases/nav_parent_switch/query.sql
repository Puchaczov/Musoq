binary KNavParentSwitchSubelement { bar: byte when parent.foo = 66 };
binary KNavParentSwitchElement { foo: byte, subelement: KNavParentSwitchSubelement };
binary KNavParentSwitch { category: byte, content: KNavParentSwitchElement when category = 1 };


SELECT r.category AS A0, r.content.foo AS A1, r.content.subelement.bar AS A2
FROM #fixture.bytes() f
CROSS APPLY Interpret<KNavParentSwitch>(f.Content) r
