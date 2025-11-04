import sys
import io

# Set UTF-8 encoding for Windows console
if sys.platform == 'win32':
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

from itrans_to_devanagari import ITRANSTranslator

translator = ITRANSTranslator()

test_cases = [
    ('namaste', 'नमस्ते'),
    ('sanskrit', 'संस्कृत'),
    ('namaskaar', 'नमस्कार'),
]

print("Testing Conjunct Handling\n")
print("=" * 60)
print(f"{'Input':<15} {'Expected':<20} {'Got':<20} {'Status'}")
print("=" * 60)

for input_text, expected in test_cases:
    result = translator.translate(input_text)
    status = "✓" if result == expected else "✗"
    print(f"{input_text:<15} {expected:<20} {result:<20} {status}")
    if result != expected:
        print(f"  -> Mismatch!")

print("=" * 60)

# Also show the characters for namaste
result = translator.translate('namaste')
print(f"\n'namaste' breakdown:")
for i, char in enumerate(result):
    print(f"  {i}: {char} (U+{ord(char):04X})")
