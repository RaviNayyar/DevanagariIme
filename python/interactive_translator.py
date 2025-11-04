"""
Interactive ITRANS to Devanagari Translator
Type ITRANS text and see Devanagari output in real-time
Press Ctrl+C to exit
"""
import sys
import io

# Set UTF-8 encoding for Windows console
if sys.platform == 'win32':
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
    sys.stdin = io.TextIOWrapper(sys.stdin.buffer, encoding='utf-8')

from itrans_to_devanagari import ITRANSTranslator

def main():
    translator = ITRANSTranslator()
    
    print("=" * 60)
    print("ITRANS to Devanagari Interactive Translator")
    print("=" * 60)
    print("Type ITRANS text and press Enter to see Devanagari output")
    print("Type 'quit' or 'exit' to stop")
    print("=" * 60)
    print()
    
    while True:
        try:
            user_input = input("ITRANS: ").strip()
            
            if user_input.lower() in ['quit', 'exit', 'q']:
                print("Goodbye!")
                break
            
            if not user_input:
                continue
            
            result = translator.translate(user_input)
            print(f"Devanagari: {result}")
            print()
            
        except KeyboardInterrupt:
            print("\nGoodbye!")
            break
        except EOFError:
            break

if __name__ == '__main__':
    main()
