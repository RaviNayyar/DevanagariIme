"""
Test script for ITRANS to Devanagari translator
"""
import sys
import io

# Set UTF-8 encoding for Windows console
if sys.platform == 'win32':
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

from itrans_to_devanagari import ITRANSTranslator

def test_translator():
    translator = ITRANSTranslator()
    
    # Test cases with expected outputs
    test_cases = [
        ('namaste', 'नमस्ते'),
        ('namaskaar', 'नमस्कार'),
        ('bharat', 'भारत'),
        ('aaiye', 'आइये'),
        ('main', 'मैं'),
        ('tum', 'तुम'),
        ('ham', 'हम'),
        ('raam', 'राम'),
        ('shri', 'श्री'),
        ('shrii', 'श्री'),  # Alternative spelling
        ('devanaagarii', 'देवनागरी'),
        ('sanskrit', 'संस्कृत'),
        ('hindi', 'हिंदी'),
    ]
    
    print("ITRANS to Devanagari Translator Test\n")
    print("=" * 60)
    print(f"{'Input':<20} {'Expected':<20} {'Got':<20} {'Status'}")
    print("=" * 60)
    
    passed = 0
    failed = 0
    
    for input_text, expected in test_cases:
        result = translator.translate(input_text)
        status = "✓" if result == expected else "✗"
        if result == expected:
            passed += 1
        else:
            failed += 1
        print(f"{input_text:<20} {expected:<20} {result:<20} {status}")
        if result != expected:
            print(f"  -> Mismatch detected!")
    
    print("=" * 60)
    print(f"Passed: {passed}, Failed: {failed}, Total: {len(test_cases)}")

if __name__ == '__main__':
    test_translator()
