# Starter-bundle validation

This report validates the prepared files, not Musoq behavior.

- Campaign JSON validates against the supplied schema; all three schemas are well formed.
- 82 unique new scope IDs, REC-084 through REC-165, in sequential dependency order.
- 64 engine scopes, 12 packaged-interface scopes and 6 empirical scopes.
- 344 unique coverage-obligation IDs; all start pending with no claimed evidence.
- 11 supplied source/history snapshots preserve their recorded SHA-256 hashes.
- Every scope source-heading / JSON-pointer reference resolves in its supplied snapshot.
- Embedded and standalone full goal prompts match after trailing-newline normalization.
- Checker self-tests: **21 passed, 0 failed, 0 skipped**. These use synthetic
  ledger/evidence and disposable Git fixtures; they are not engine test results.

No Musoq checkout, live CLI/service, model trial or human study was run here.

Self-test command:

```text
python -m unittest discover -s campaigns/recovery-v3/tools -p 'test_*.py' -v
```

Checker command:

```text
python campaigns/recovery-v3/tools/check_campaign.py --format json
```
