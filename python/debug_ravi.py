"""
Debug script to trace "ravi" translation
"""
import sys
import io

# Set UTF-8 encoding for Windows console
if sys.platform == 'win32':
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

from itrans_to_devanagari import ITRANSTranslator, MATRAS

def debug_translation(text):
    translator = ITRANSTranslator()
    result = translator.translate(text)
    
    print(f"Input: {text}")
    print(f"Output: {result}")
    print(f"Output (hex): {result.encode('utf-8').hex()}")
    print(f"Expected: रवि")
    print(f"Expected (hex): {'रवि'.encode('utf-8').hex()}")
    print(f"Match: {result == 'रवि'}")
    print()
    
    # Check individual characters
    print("Output characters:")
    for i, char in enumerate(result):
        print(f"  {i}: {char} (U+{ord(char):04X})")
    print()
    
    print("Expected characters:")
    for i, char in enumerate('रवि'):
        print(f"  {i}: {char} (U+{ord(char):04X})")
    print()
    
    # Check matra patterns order
    print("Matra patterns order (should check longest first):")
    for pattern in translator.matra_patterns[:10]:
        print(f"  {pattern} -> {MATRAS.get(pattern)}")

if __name__ == '__main__':
    debug_translation('ravi')
