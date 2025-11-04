import sys
import io

# Set UTF-8 encoding for Windows console
if sys.platform == 'win32':
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

from itrans_to_devanagari import MATRAS

i_char = MATRAS['i']
ii_char = MATRAS['ii']
I_char = MATRAS['I']

print(f"i  -> {i_char} (U+{ord(i_char):04X})")
print(f"ii -> {ii_char} (U+{ord(ii_char):04X})")
print(f"I  -> {I_char} (U+{ord(I_char):04X})")
print()
print("Expected:")
print("i  -> ि (U+093F) - short i matra")
print("ii -> ी (U+0940) - long i matra")
print("I  -> ी (U+0940) - long i matra")
print()
print("Match:")
print(f"i correct:  {ord(i_char) == 0x093F}")
print(f"ii correct: {ord(ii_char) == 0x0940}")
print(f"I correct:  {ord(I_char) == 0x0940}")
