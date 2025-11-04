"""
ITRANS to Devanagari Converter
Converts Roman text written in ITRANS schema to Devanagari script
Uses aa/A for आ (long a)
"""

# Vowels (swar)
VOWELS = {
    'a': 'अ',
    'aa': 'आ', 'A': 'आ',
    'i': 'इ',
    'ii': 'ई', 'I': 'ई',
    'u': 'उ',
    'uu': 'ऊ', 'U': 'ऊ',
    'RRi': 'ऋ', 'R^i': 'ऋ',
    'RRI': 'ॠ', 'R^I': 'ॠ',
    'LLi': 'ऌ', 'L^i': 'ऌ',
    'LLI': 'ॡ', 'L^I': 'ॡ',
    'e': 'ए',
    'ai': 'ऐ', 'E': 'ऐ',
    'o': 'ओ',
    'au': 'औ', 'O': 'औ',
    'M': 'ं',  # Anusvara
    'H': 'ः',  # Visarga
    '.': '।',  # Danda (period)
    '..': '॥',  # Double danda
}

# Consonants (vyanjan)
# Note: Case matters! 'T', 'D', 'N' are retroflex; 't', 'd', 'n' are dental
CONSONANTS = {
    # Velars
    'k': 'क', 'kh': 'ख', 'g': 'ग', 'gh': 'घ', '~N': 'ङ', 'N^': 'ङ',
    # Palatals
    'ch': 'च', 'Ch': 'छ', 'chh': 'छ', 'j': 'ज', 'jh': 'झ', '~n': 'ञ', 'JN': 'ञ', 'j~n': 'ज्ञ', 'GY': 'ज्ञ',
    # Retroflex (capital letters)
    'T': 'ट', 'Th': 'ठ', 'D': 'ड', 'Dh': 'ढ', 'N': 'ण',
    # Dentals (lowercase)
    't': 'त', 'th': 'थ', 'd': 'द', 'dh': 'ध', 'n': 'न',
    # Labials
    'p': 'प', 'ph': 'फ', 'b': 'ब', 'bh': 'भ', 'm': 'म',
    # Semivowels
    'y': 'य', 'r': 'र', 'l': 'ल', 'v': 'व', 'w': 'व',
    # Sibilants and aspirate
    'sh': 'श', 'Sh': 'ष', 'S': 'ष', 's': 'स', 'h': 'ह',
    # Conjuncts
    'x': 'क्ष',
}

# Matras (vowel diacritics)
MATRAS = {
    'aa': 'ा', 'A': 'ा',
    'i': 'ि',
    'ii': 'ी', 'I': 'ी',
    'u': 'ु',
    'uu': 'ू', 'U': 'ू',
    'RRi': 'ृ', 'R^i': 'ृ',
    'RRI': 'ॄ', 'R^I': 'ॄ',
    'e': 'े',
    'ai': 'ै', 'E': 'ै',
    'o': 'ो',
    'au': 'ौ', 'O': 'ौ',
}

# Special characters
SPECIAL = {
    'M': 'ं',  # Anusvara
    'H': 'ः',  # Visarga
    '.': '।',  # Danda
    '..': '॥',  # Double danda
}


class ITRANSTranslator:
    def __init__(self):
        # Sort patterns by length (longest first) to match longer sequences first
        self.vowel_patterns = sorted(VOWELS.keys(), key=len, reverse=True)
        self.consonant_patterns = sorted(CONSONANTS.keys(), key=len, reverse=True)
        self.matra_patterns = sorted(MATRAS.keys(), key=len, reverse=True)
        
        # Special cases for common words/conventions
        self.special_cases = {
            'shri': 'shrii',  # "shri" commonly means "shrii" (श्री)
            'bharat': 'bhaarat',  # "bharat" commonly means "bhaarat" (भारत)
            'sanskrit': 'saMskR^it',  # "sanskrit" -> संस्कृत
            'hindi': 'hiMdii',  # "hindi" -> हिंदी
        }
        
    def translate(self, text):
        """
        Convert ITRANS Roman text to Devanagari
        """
        if not text:
            return ''
        
        # Apply special cases (e.g., "shri" -> "shrii")
        original_text = text
        text_lower = text.lower()
        for special, replacement in self.special_cases.items():
            if special in text_lower:
                # Replace whole word if it matches exactly
                words = text.split()
                new_words = []
                for word in words:
                    if word.lower() == special:
                        new_words.append(replacement)
                    else:
                        new_words.append(word)
                text = ' '.join(new_words)
                break
        
        result = []
        i = 0
        text_lower = text.lower()
        
        while i < len(text):
            # Skip spaces and punctuation (except special ITRANS punctuation)
            if text[i] == ' ':
                result.append(' ')
                i += 1
                continue
            
            # Try to match patterns
            matched = False
            
            # Check for consonants first (most common)
            for pattern in self.consonant_patterns:
                # Check exact match first (case-sensitive for ITRANS)
                if text[i:].startswith(pattern):
                    consonant = CONSONANTS.get(pattern)
                    if consonant:
                            result.append(consonant)
                            
                            # Look ahead for vowel matra
                            next_pos = i + len(pattern)
                            matra_found = False
                            
                            # Check for matra patterns (vowels that attach to consonants)
                            for matra_pattern in self.matra_patterns:
                                if text[next_pos:].startswith(matra_pattern):
                                    # Special case: 'a' is default/invisible, just skip it
                                    if matra_pattern == 'a':
                                        i = next_pos + 1
                                        matra_found = True
                                        break
                                    else:
                                        result.append(MATRAS.get(matra_pattern))
                                        i = next_pos + len(matra_pattern)
                                        matra_found = True
                                        break
                            
                            if not matra_found:
                                # Check if next is a standalone vowel (shouldn't happen often, but handle it)
                                for vowel_pattern in self.vowel_patterns:
                                    if text[next_pos:].startswith(vowel_pattern):
                                        # 'a' is default, just skip it
                                        if vowel_pattern == 'a':
                                            i = next_pos + 1
                                        else:
                                            vowel = VOWELS.get(vowel_pattern)
                                            if vowel:
                                                result.append(vowel)
                                                i = next_pos + len(vowel_pattern)
                                        matra_found = True
                                        break
                            
                            if not matra_found:
                                # Special case: final 'n' becomes anusvara
                                # Check if this is 'n' at end of word
                                if pattern == 'n' and next_pos >= len(text):
                                    # Final 'n' -> replace with anusvara
                                    result[-1] = 'ं'  # Replace last character (न) with anusvara
                                    i = next_pos
                                else:
                                    # No vowel found - check if next is another consonant (conjunct)
                                    # Only add halant if next character is a consonant, not end of text or space
                                    if next_pos < len(text) and text[next_pos] != ' ':
                                        # Check if next character(s) form a consonant
                                        next_is_consonant = False
                                        for consonant_pattern in self.consonant_patterns:
                                            if text[next_pos:].startswith(consonant_pattern):
                                                next_is_consonant = True
                                                break
                                        
                                        # Also check if it's a vowel or special char - if so, don't add halant
                                        if next_is_consonant:
                                            # Check if it's not actually a vowel or special char
                                            is_vowel_or_special = False
                                            for vowel_pattern in self.vowel_patterns:
                                                if text[next_pos:].startswith(vowel_pattern):
                                                    is_vowel_or_special = True
                                                    break
                                            for special_pattern in SPECIAL.keys():
                                                if text[next_pos:].startswith(special_pattern):
                                                    is_vowel_or_special = True
                                                    break
                                            
                                            if not is_vowel_or_special:
                                                # Add halant (्) to form conjunct
                                                result.append('्')  # Halant character
                                    # Default 'a' matra (invisible in Devanagari), no character to consume
                                    i = next_pos
                            
                            matched = True
                            break
            
            if matched:
                continue
            
            # Check for standalone vowels
            for pattern in self.vowel_patterns:
                if text[i:].startswith(pattern):
                    vowel = VOWELS.get(pattern)
                    if vowel:
                        result.append(vowel)
                        i += len(pattern)
                        matched = True
                        break
            
            if matched:
                continue
            
            # Check for special characters
            for pattern in SPECIAL.keys():
                if text[i:].startswith(pattern):
                    result.append(SPECIAL[pattern])
                    i += len(pattern)
                    matched = True
                    break
            
            # If no match, preserve the character
            if not matched:
                result.append(text[i])
                i += 1
        
        return ''.join(result)


if __name__ == '__main__':
    import sys
    import io
    
    # Set UTF-8 encoding for Windows console
    if sys.platform == 'win32':
        sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
    
    # Test the translator
    translator = ITRANSTranslator()
    
    test_cases = [
        'namaste',
        'namaskaar',
        'bharat',
        'aaiye',
        'main',
        'tum',
        'ham',
        'raam',
        'shri',
        'devanaagarii',
    ]
    
    print("ITRANS to Devanagari Translator Test\n")
    print("-" * 50)
    for test in test_cases:
        result = translator.translate(test)
        print(f"{test:20} -> {result}")
