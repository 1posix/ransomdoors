"""Non-runtime checks for the Lab source; run with `python tests/verify_patch.py`."""
from pathlib import Path
import re
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[1]
for xaml in root.glob('*.xaml'):
    ET.parse(xaml)
    codebehind = root / f'{xaml.name}.cs'
    assert codebehind.exists(), codebehind
    txt = xaml.read_text(encoding='utf-8-sig')
    cs = codebehind.read_text(encoding='utf-8-sig')
    for h in re.findall(r'\b(?:Loaded|Closing|Click|DragEnter|Drop|MouseDoubleClick)="(\w+)"', txt):
        assert h in cs, (xaml.name, h)

source = '\n'.join(f.read_text(encoding='utf-8-sig') for f in root.glob('*.cs'))
for disallowed in (
    'ExecCMDOnDeath', 'CrashOnDeath', 'CMDOnDeath', 'SetSystemCursor',
    'FileTypeRegister', 'KeyboardHook', 'DeleteSubKeyTree', 'shutdown /s',
    'Directory.Delete(temporaryFolderPath, true)', 'GetRandomLocationInTree',
):
    assert disallowed not in source, disallowed
assert 'FileMode.CreateNew' in source
assert 'GetLastInputInfo' in source
assert 'RegisterHotKey' in source
assert 'ShowBrowser' in source
assert 'RestoreWallpaper' in source
assert (root / 'Assets/Sounds/layer1.wav').exists()
assert (root / 'LICENSE.md').exists()
# Audio teardown should disarm callbacks BEFORE disposing the WAV reader.
audio = (root / 'SoundHandle.cs').read_text(encoding='utf-8-sig')
assert '_waveOut.PlaybackStopped -= OnPlaybackStopped;' in audio
assert audio.index('_waveOut.PlaybackStopped -= OnPlaybackStopped;') < audio.index('_waveOut.Stop();')
assert 'if (_disposed) return;' in audio
assert 'lock (_gate)' in audio
assert (root / 'LabDiagnostics.cs').exists()

print('PASS — XAML, handlers, required resources, and basic source safety checks')
