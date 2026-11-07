#!/usr/bin/env python3
"""Exercise credential cleanup and notarization gates with fake Apple services."""
import base64
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]
MOCK = r'''#!/usr/bin/env python3
import json, os, sys
from pathlib import Path
command = Path(sys.argv[0]).name
args = sys.argv[1:]
log = Path(os.environ['TEST_LOG'])
event = [command] + args[:2]
if command == 'security' and args[0] == 'list-keychains': event = [command] + args
with log.open('a') as out: out.write(json.dumps(event) + '\n')
if command == 'uname': print('Darwin')
elif command == 'security':
    if args[0] == 'list-keychains' and '-s' not in args:
        print('    "/original keychains/login.keychain-db"')
    if args[0] == 'create-keychain': Path(args[-1]).touch()
    if args[0] == 'delete-keychain': Path(args[-1]).unlink(missing_ok=True)
    if args[0] == 'import' and os.environ.get('MOCK_IMPORT_FAILURE'): sys.exit(1)
    if args[0] == 'find-identity' and not os.environ.get('MOCK_NO_IDENTITY'):
        print('  1) ' + 'A'*40 + ' "Developer ID Application: Test (TESTTEAM)"')
elif command == 'plutil':
    print(json.loads(Path(args[-1]).read_text())[args[1]])
elif command == 'xcrun':
    if args[:2] == ['notarytool', 'submit']:
        print(json.dumps({'id':'12345678-1234-1234-1234-123456789abc'}))
    elif args[:2] == ['notarytool', 'wait']:
        count = sum(json.loads(line)[:3] == ['xcrun','notarytool','wait'] for line in log.read_text().splitlines())
        statuses = os.environ.get('MOCK_STATUSES', 'Accepted').split(',')
        status = statuses[min(count-1, len(statuses)-1)]
        print(json.dumps({'status':status}))
        sys.exit(0 if status == 'Accepted' else 1)
    elif args[:2] == ['stapler', 'staple']:
        if os.environ.get('MOCK_STAPLE_FAILURE'): sys.exit(1)
        Path(args[2], 'ticket').touch()
    elif args[:2] == ['stapler', 'validate']:
        sys.exit(0 if Path(args[2], 'ticket').exists() else 1)
'''
CHILD = '''#!/usr/bin/env bash
set -euo pipefail
test "$MACOS_SIGNING_MODE" = developer-id
test -s "$MACOS_NOTARY_KEY_PATH"
test -e "$MACOS_SIGNING_KEYCHAIN"
test -z "${MACOS_CERTIFICATE_PASSWORD+x}"
test -z "${MACOS_CERTIFICATE_P12_BASE64+x}"
test -z "${APPLE_API_KEY_P8+x}"
touch "$RUNNER_TEMP/child-ran"
exit "${MOCK_BUILD_EXIT:-0}"
'''


class SigningTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='videoscrap-signing-test-')
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        scripts = self.root / 'repo' / 'scripts'
        scripts.mkdir(parents=True)
        self.scripts = scripts
        for name in ('build-macos-signed.sh', 'notarize-macos.sh'):
            shutil.copyfile(ROOT / 'scripts' / name, scripts / name)
        (scripts / 'build-macos.sh').write_text(CHILD)
        mocks = self.root / 'bin'
        mocks.mkdir()
        for name in ('uname', 'security', 'xcrun', 'plutil', 'sleep'):
            path = mocks / name
            path.write_text(MOCK)
            path.chmod(0o755)
        self.runner = self.root / 'runner'
        self.runner.mkdir()
        key = self.root / 'key.p8'
        key.write_text('fake-private-key')
        self.target = self.root / 'Test.app'
        self.target.mkdir()
        self.log = self.root / 'calls.jsonl'
        self.env = dict(os.environ, PATH=f'{mocks}{os.pathsep}{os.environ["PATH"]}',
                        RUNNER_TEMP=str(self.runner), TEST_LOG=str(self.log),
                        MACOS_CERTIFICATE_P12_BASE64=base64.b64encode(b'fake-cert').decode(),
                        MACOS_CERTIFICATE_PASSWORD='fake-password', APPLE_API_KEY_P8='fake-private-key',
                        APPLE_API_KEY_ID='fake-id', APPLE_API_ISSUER_ID='fake-issuer',
                        MACOS_NOTARY_KEY_PATH=str(key))

    def run_script(self, name, *args, **extra):
        return subprocess.run(['bash', str(self.scripts / name), *args],
                              env=dict(self.env, **extra), capture_output=True, text=True)

    def events(self):
        return [json.loads(line) for line in self.log.read_text().splitlines()] if self.log.exists() else []

    def assert_cleaned(self):
        self.assertEqual(list(self.runner.glob('videoscrap-signing.*')), [])
        self.assertTrue(any(e[:2] == ['security', 'delete-keychain'] for e in self.events()))
        self.assertIn(['security', 'list-keychains', '-d', 'user', '-s',
                       '/original keychains/login.keychain-db'], self.events())

    def test_success_cleans_keychain_and_does_not_inherit_raw_secrets(self):
        result = self.run_script('build-macos-signed.sh', 'arm64')
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertTrue((self.runner / 'child-ran').exists())
        self.assert_cleaned()

    def test_build_failure_preserves_failure_and_cleans_credentials(self):
        result = self.run_script('build-macos-signed.sh', 'x64', MOCK_BUILD_EXIT='17')
        self.assertEqual(result.returncode, 17, result.stderr)
        self.assert_cleaned()

    def test_invalid_certificate_never_builds_and_cleans_credentials(self):
        for flag in ('MOCK_IMPORT_FAILURE', 'MOCK_NO_IDENTITY'):
            with self.subTest(flag=flag):
                result = self.run_script('build-macos-signed.sh', 'x64', **{flag: '1'})
                self.assertNotEqual(result.returncode, 0)
                self.assertFalse((self.runner / 'child-ran').exists())
                self.assert_cleaned()

    def test_missing_secret_never_imports_or_builds(self):
        result = self.run_script('build-macos-signed.sh', 'arm64', APPLE_API_KEY_P8='')
        self.assertNotEqual(result.returncode, 0)
        self.assertFalse(any(e[0] in ('security', 'xcrun') for e in self.events()))

    def test_accepted_submission_is_stapled_and_validated(self):
        result = self.run_script('notarize-macos.sh', 'test.zip', str(self.target))
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertTrue((self.target / 'ticket').exists())
        self.assertIn(['xcrun', 'stapler', 'validate'], self.events())

    def test_rejection_never_staples(self):
        result = self.run_script('notarize-macos.sh', 'test.zip', str(self.target), MOCK_STATUSES='Invalid')
        self.assertNotEqual(result.returncode, 0)
        self.assertFalse(any(e[:2] == ['xcrun', 'stapler'] for e in self.events()))

    def test_timeout_retries_same_submission_without_uploading_twice(self):
        result = self.run_script('notarize-macos.sh', 'test.zip', str(self.target), MOCK_STATUSES='InProgress,Accepted')
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(self.events().count(['xcrun', 'notarytool', 'submit']), 1)
        self.assertEqual(self.events().count(['xcrun', 'notarytool', 'wait']), 2)

    def test_ticket_failure_is_not_reported_as_success(self):
        result = self.run_script('notarize-macos.sh', 'test.zip', str(self.target), MOCK_STAPLE_FAILURE='1')
        self.assertNotEqual(result.returncode, 0)
        self.assertFalse((self.target / 'ticket').exists())


if __name__ == '__main__':
    unittest.main(verbosity=2)
