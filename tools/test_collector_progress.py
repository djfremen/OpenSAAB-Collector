import copy
import unittest
from collector_progress import load, render, validate


class ProgressTests(unittest.TestCase):
    def setUp(self):
        self.data = load()

    def test_unknown_adapter_cannot_borrow_nano_evidence(self):
        d = copy.deepcopy(self.data)
        d['adapters'][-1]['evidence'] = ['nano-owner-native']
        with self.assertRaises(ValueError):
            validate(d)

    def test_owner_test_cannot_qualify_public_build(self):
        d = copy.deepcopy(self.data)
        d['adapters'][1]['public_stages']['capture'] = 'pass_scoped'
        with self.assertRaises(ValueError):
            validate(d)

    def test_pass_requires_evidence(self):
        d = copy.deepcopy(self.data)
        d['adapters'][-1]['public_stages']['capture'] = 'pass_scoped'
        with self.assertRaises(ValueError):
            validate(d)

    def test_earlier_public_case_cannot_qualify_current_artifact(self):
        d = copy.deepcopy(self.data)
        d['adapters'][0]['public_stages']['capture'] = 'pass_scoped'
        with self.assertRaises(ValueError):
            validate(d)

    def test_historical_artifact_cannot_be_current_pass(self):
        d = copy.deepcopy(self.data)
        d['public_version'] = '0.5.0-preview.1'
        with self.assertRaises(ValueError):
            validate(d)

    def test_rejected_model_and_gaps_survive_projection(self):
        text = render(self.data)
        self.assertIn('Assessment rejected', text)
        self.assertIn('GUI button walk-through pending', text)
        self.assertIn('No live capture-to-sanitized-upload', text)


if __name__ == '__main__':
    unittest.main()
