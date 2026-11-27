# Upgrade diagnostic consumers from v17 to v18

Use this mapping when updating a diagnostic consumer. Match the reported code,
phase and source domain, then use the diagnostic arguments, related locations,
and suggested action. The active diagnostic contract is in the
[Core SQL specification](../specs/musoq-core-language-spec.md).

| v17 code or behavior | v18 replacement |
|----------------------|-----------------|
| MQ3003 unknown table | MQ3023_TableNotDefined or MQ3085_UnknownSource |
| MQ3004 unknown function | MQ3086_UnknownCallable |
| MQ3006 invalid argument count | MQ3087_InvalidCallableArity |
| MQ3009 null reference | The role-specific symbol, cast, member, datasource, or internal code |
| MQ3013 cannot resolve method | MQ3086–MQ3089 according to the callable decision tree |
| MQ3014 invalid property access | MQ3028_UnknownProperty or the precise index/member code |
| MQ3029 unresolvable method | MQ3086–MQ3089 |
| MQ3030 construction not supported | MQ4016_UnsupportedSchemaConstruction |
| MQ3031 set operator missing keys | The applicable set-column/count/type diagnostic |
| MQ3082 ambiguous source invocation | MQ3089 for overload ambiguity or MQ3035 for owner ambiguity |
| MQ5001, MQ5002, MQ5004–MQ5007, MQ5009, MQ5012 | No active style/optimization warning; MQ5009 remains a compatibility documentation identifier only |
| MQ6001–MQ6004 | The precise parse, bind, schema, or runtime diagnostic |
| MQ7001 datasource binding failed | MQ7010_DataSourceOpenFailed |
| MQ7002 datasource iterator error | MQ7011_DataSourceReadFailed |
| MQ9999 unknown | MQ9001_InternalCompilerError or MQ9002_InternalExecutionError |
