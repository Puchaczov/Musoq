"""Self-tests of the read-only campaign checker; synthetic fixture data only.
Run: python -m unittest discover -s campaigns/recovery-v3/tools -p 'test_*.py'
These are NOT Musoq engine tests and do NOT count toward campaign completion.
"""
from __future__ import annotations
import copy
import hashlib
import json
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path

from check_campaign import inspect_campaign, unique_object

BUNDLE_ROOT = Path(__file__).resolve().parents[4]


class CampaignCheckerTests(unittest.TestCase):
    def setUp(self) -> None:
        self.tmp = tempfile.TemporaryDirectory()
        self.root = Path(self.tmp.name)
        self.path = self.root / 'musoq-recovery-campaign.json'
        self.document = json.loads((BUNDLE_ROOT / self.path.name).read_text(encoding='utf-8'))
        shutil.copyfile(BUNDLE_ROOT / 'musoq-recovery-campaign.goal.md', self.root / 'musoq-recovery-campaign.goal.md')
        for source in self.document['sources']:
            dst = self.root / source['file']
            dst.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(BUNDLE_ROOT / source['file'], dst)
        # The real ledger advances during the campaign.  Checker self-tests
        # need a deterministic pending baseline so their synthetic completion
        # and dependency mutations remain independent of campaign progress.
        for scope in self.document['scopes']:
            scope.update(state='pending', completed=False, testsPassed=False)
            for obligation in scope['obligations']:
                obligation.update(status='pending', evidenceIds=[])
            scope['progress'] = {
                'attempt': 0,
                'novelCandidates': 0,
                'duplicateCandidates': 0,
                'metamorphicVariants': 0,
                'productionFixes': 0,
                'cleanStreak': 0,
                'findingIds': [],
            }
            scope['completion'] = {
                'completedAt': None,
                'validatedFromCommit': None,
                'testedPayloadSha256': None,
                'reportPath': None,
                'reportSha256': None,
                'fullGateEvidence': [],
                'reviewEvidence': [],
                'testCommands': [],
                'testSummary': None,
                'commitMessage': None,
                'notes': None,
            }
            scope['blocker'] = None
        self.save()

    def tearDown(self) -> None:
        self.tmp.cleanup()

    def save(self) -> None:
        self.path.write_text(json.dumps(self.document), encoding='utf-8')

    def errors(self, evidence: bool = False) -> list[str]:
        self.save()
        return inspect_campaign(self.path, evidence=evidence)[1]

    def test_initial_bundle_is_structurally_valid(self) -> None:
        self.assertEqual([], self.errors())

    def test_duplicate_json_key_rejected(self) -> None:
        with self.assertRaises(ValueError):
            unique_object([('completed', False), ('completed', True)])

    def test_deleted_scope_rejected(self) -> None:
        self.document['scopes'].pop()
        self.assertTrue(any('Scope IDs/order' in e for e in self.errors()))

    def test_bypassed_dependency_rejected(self) -> None:
        self.document['scopes'][2]['dependsOn'] = []
        self.assertTrue(any('dependency mismatch' in e for e in self.errors()))

    def test_fake_completion_without_evidence_rejected(self) -> None:
        s = self.document['scopes'][0]
        s.update(state='completed', completed=True, testsPassed=True)
        errors = self.errors()
        self.assertTrue(any('obligation evidence' in e for e in errors))
        self.assertTrue(any('missing fullGateEvidence' in e for e in errors))

    def test_pending_scope_cannot_claim_final_pass(self) -> None:
        self.document['scopes'][0]['testsPassed'] = True
        self.assertTrue(any('Incomplete scope claims' in e for e in self.errors()))

    def test_unknown_source_heading_rejected(self) -> None:
        self.document['scopes'][1]['sources'] = [{'sourceId':'core', 'sections':['Not a real heading'], 'selectorKind':'heading'}]
        self.assertTrue(any('Missing exact heading' in e for e in self.errors()))

    def test_tampered_authority_rejected(self) -> None:
        source = next(s for s in self.document['sources'] if s['id'] == 'core')
        with (self.root / source['file']).open('a', encoding='utf-8') as out:
            out.write('\nTampered fixture.\n')
        self.assertTrue(any('Source snapshot changed' in e for e in self.errors()))

    def test_goal_drift_rejected(self) -> None:
        self.document['goalPrompt'] += '\nSilently skip the full gate.'
        self.assertTrue(any('goal prompts differ' in e for e in self.errors()))

    def test_duplicate_obligation_rejected(self) -> None:
        s = self.document['scopes'][0]
        s['obligations'].append(copy.deepcopy(s['obligations'][0]))
        self.assertTrue(any('Duplicate obligation' in e for e in self.errors()))

    def test_full_gate_cannot_be_disabled(self) -> None:
        self.document['fullGate']['requiredAfterEveryScope'] = False
        self.assertTrue(any('Full gate must be required' in e for e in self.errors()))

    def test_active_scope_requires_finished_predecessor(self) -> None:
        self.document['scopes'][1]['state'] = 'in_progress'
        self.assertTrue(any('unfinished prerequisite' in e for e in self.errors()))

    def prepare_synthetic_completion(self) -> dict:
        """Build structural fixture evidence, not a claimed real test execution."""
        s = self.document['scopes'][0]
        s.update(state='completed', completed=True, testsPassed=True)
        for ob in s['obligations']:
            ob.update(status='evidenced', evidenceIds=['synthetic-fixture'])
        report = self.root / 'report.md'
        report.write_text('Synthetic checker test fixture; not Musoq evidence.')
        run = {'scopeId':s['id'], 'outcome':'passed', 'testedPayloadSha256':'a'*64,
               'expectedTargets':['fixture-a/net10.0','fixture-b/net10.0'],
               'commands':[{'exitCode':0}], 'results':[
                   {'targetId':t,'passed':1,'failed':0,'skipped':0,'total':1,'discovered':1,'aborted':False}
                   for t in ['fixture-a/net10.0','fixture-b/net10.0']]}
        s['completion'].update(completedAt='2026-09-05T00:00:00Z', validatedFromCommit='b'*40,
             testedPayloadSha256='a'*64, reportPath='report.md',
             reportSha256=hashlib.sha256(report.read_bytes()).hexdigest(),
             reviewEvidence=[{'kind':'synthetic'}], testCommands=[{'kind':'synthetic'}],
             testSummary='Synthetic checker fixture only.', commitMessage=f'test(campaign): {s["id"]} fixture')
        self.store_run(run)
        return run

    def store_run(self, run: dict) -> None:
        ep = self.root / 'gate.json'
        ep.write_text(json.dumps(run))
        self.document['scopes'][0]['completion']['fullGateEvidence'] = [
            {'path':'gate.json','sha256':hashlib.sha256(ep.read_bytes()).hexdigest()}]

    def test_missing_project_result_rejected(self) -> None:
        run = self.prepare_synthetic_completion()
        run['results'].pop()
        self.store_run(run)
        self.assertTrue(any('test target results' in e for e in self.errors(True)))

    def test_fabricated_test_total_rejected(self) -> None:
        run = self.prepare_synthetic_completion()
        run['results'][0]['total'] = 100
        self.store_run(run)
        self.assertTrue(any('Unreconciled/empty' in e for e in self.errors(True)))

    def test_stale_source_payload_rejected(self) -> None:
        run = self.prepare_synthetic_completion()
        run['testedPayloadSha256'] = 'c'*64
        self.store_run(run)
        self.assertTrue(any('tested payload differs' in e for e in self.errors(True)))

    def test_aborted_child_rejected(self) -> None:
        run = self.prepare_synthetic_completion()
        run['results'][0]['aborted'] = True
        self.store_run(run)
        self.assertTrue(any('Failed/aborted target' in e for e in self.errors(True)))

    def test_zero_test_target_rejected(self) -> None:
        run = self.prepare_synthetic_completion()
        for k in ('passed','failed','skipped','total','discovered'):
            run['results'][0][k] = 0
        self.store_run(run)
        self.assertTrue(any('Unreconciled/empty' in e for e in self.errors(True)))

    def test_stale_result_digest_rejected(self) -> None:
        self.prepare_synthetic_completion()
        with (self.root / 'gate.json').open('a') as f:
            f.write(' ')
        self.assertTrue(any('Full gate digest mismatch' in e for e in self.errors(True)))


    def git_command(self, *args: str) -> None:
        subprocess.run(['git', '-C', str(self.root), *args], check=True,
                       capture_output=True, text=True, timeout=30)

    def initialize_fixture_git(self) -> None:
        self.git_command('init', '--quiet')
        self.git_command('config', 'user.name', 'Checker Fixture')
        self.git_command('config', 'user.email', 'fixture@example.invalid')
        self.git_command('add', '.')
        self.git_command('commit', '--quiet', '-m', 'fixture: initial pending ledger')

    def commit_fixture_completion(self) -> None:
        self.prepare_synthetic_completion()
        self.save()
        self.git_command('add', '.')
        self.git_command('commit', '--quiet', '-m',
                         'test(campaign): REC-084 synthetic completion\n\n'
                         'Recovery-Campaign: recovery-v3\nScope-ID: REC-084')

    @unittest.skipUnless(shutil.which('git'), 'Git unavailable for temporary fixture test')
    def test_committed_scope_mapping_verified(self) -> None:
        self.initialize_fixture_git()
        self.commit_fixture_completion()
        self.assertEqual([], inspect_campaign(self.path, commits=True)[1])

    @unittest.skipUnless(shutil.which('git'), 'Git unavailable for temporary fixture test')
    def test_uncommitted_completion_is_not_complete(self) -> None:
        self.initialize_fixture_git()
        self.prepare_synthetic_completion()
        self.save()
        errors = inspect_campaign(self.path, commits=True)[1]
        self.assertTrue(any('uncommitted completed' in e for e in errors))

    @unittest.skipUnless(shutil.which('git'), 'Git unavailable for temporary fixture test')
    def test_duplicate_completion_commits_rejected(self) -> None:
        self.initialize_fixture_git()
        self.commit_fixture_completion()
        self.git_command('commit', '--quiet', '--allow-empty', '-m',
                         'test(campaign): REC-084 duplicate fixture\n\n'
                         'Recovery-Campaign: recovery-v3\nScope-ID: REC-084')
        self.assertTrue(any('Expected one completion commit' in e
                            for e in inspect_campaign(self.path, commits=True)[1]))


if __name__ == '__main__':
    unittest.main()
