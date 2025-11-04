using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DevanagariIME
{
    /// <summary>
    /// ITRANS to Devanagari Converter
    /// Converts Roman text written in ITRANS schema to Devanagari script
    /// Uses aa/A for आ (long a)
    /// </summary>
    public class ITRANSTranslator
    {
        // Vowels (swar)
        private static readonly Dictionary<string, string> VOWELS = new Dictionary<string, string>
        {
            { "a", "अ" },
            { "aa", "आ" }, { "A", "आ" },
            { "i", "इ" },
            { "ii", "ई" }, { "I", "ई" },
            { "u", "उ" },
            { "uu", "ऊ" }, { "U", "ऊ" },
            { "RRi", "ऋ" }, { "R^i", "ऋ" },
            { "RRI", "ॠ" }, { "R^I", "ॠ" },
            { "LLi", "ऌ" }, { "L^i", "ऌ" },
            { "LLI", "ॡ" }, { "L^I", "ॡ" },
            { "e", "ए" },
            { "ai", "ऐ" }, { "E", "ऐ" },
            { "o", "ओ" },
            { "au", "औ" }, { "O", "औ" },
            { "M", "ं" },  // Anusvara
            { "H", "ः" },  // Visarga
            { ".", "।" },  // Danda (period)
            { "..", "॥" }  // Double danda
        };

        // Consonants (vyanjan)
        // Note: Case matters! 'T', 'D', 'N' are retroflex; 't', 'd', 'n' are dental
        private static readonly Dictionary<string, string> CONSONANTS = new Dictionary<string, string>
        {
            // Velars
            { "k", "क" }, { "kh", "ख" }, { "g", "ग" }, { "gh", "घ" }, { "~N", "ङ" }, { "N^", "ङ" },
            // Palatals
            { "ch", "च" }, { "Ch", "छ" }, { "chh", "छ" }, { "j", "ज" }, { "jh", "झ" }, { "~n", "ञ" }, { "JN", "ञ" }, { "j~n", "ज्ञ" }, { "GY", "ज्ञ" },
            // Retroflex (capital letters)
            { "T", "ट" }, { "Th", "ठ" }, { "D", "ड" }, { "Dh", "ढ" }, { "N", "ण" },
            // Dentals (lowercase)
            { "t", "त" }, { "th", "थ" }, { "d", "द" }, { "dh", "ध" }, { "n", "न" },
            // Labials
            { "p", "प" }, { "ph", "फ" }, { "b", "ब" }, { "bh", "भ" }, { "m", "म" },
            // Semivowels
            { "y", "य" }, { "r", "र" }, { "l", "ल" }, { "v", "व" }, { "w", "व" },
            // Sibilants and aspirate
            { "sh", "श" }, { "Sh", "ष" }, { "S", "ष" }, { "s", "स" }, { "h", "ह" },
            // Conjuncts
            { "x", "क्ष" }
        };

        // Matras (vowel diacritics)
        private static readonly Dictionary<string, string> MATRAS = new Dictionary<string, string>
        {
            { "aa", "ा" }, { "A", "ा" },
            { "i", "ि" },
            { "ii", "ी" }, { "I", "ी" },
            { "u", "ु" },
            { "uu", "ू" }, { "U", "ू" },
            { "RRi", "ृ" }, { "R^i", "ृ" },
            { "RRI", "ॄ" }, { "R^I", "ॄ" },
            { "e", "े" },
            { "ai", "ै" }, { "E", "ै" },
            { "o", "ो" },
            { "au", "ौ" }, { "O", "ौ" }
        };

        // Special characters
        private static readonly Dictionary<string, string> SPECIAL = new Dictionary<string, string>
        {
            { "M", "ं" },  // Anusvara
            { "H", "ः" },  // Visarga
            { ".", "।" },  // Danda
            { "..", "॥" },  // Double danda
            { "|", "।" }   // Pipe also maps to danda
        };

        private readonly List<string> vowelPatterns;
        private readonly List<string> consonantPatterns;
        private readonly List<string> matraPatterns;

        // Special cases for common words/conventions
        private readonly Dictionary<string, string> specialCases = new Dictionary<string, string>
        {
            { "shri", "shrii" },      // "shri" commonly means "shrii" (श्री)
            { "bharat", "bhaarat" },  // "bharat" commonly means "bhaarat" (भारत)
            { "sanskrit", "saMskR^it" },  // "sanskrit" -> संस्कृत
            { "hindi", "hiMdii" }     // "hindi" -> हिंदी
        };

        public ITRANSTranslator()
        {
            // Sort patterns by length (longest first) to match longer sequences first
            vowelPatterns = VOWELS.Keys.OrderByDescending(k => k.Length).ToList();
            consonantPatterns = CONSONANTS.Keys.OrderByDescending(k => k.Length).ToList();
            matraPatterns = MATRAS.Keys.OrderByDescending(k => k.Length).ToList();
        }

        /// <summary>
        /// Convert ITRANS Roman text to Devanagari
        /// </summary>
        public string Translate(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // Apply special cases (e.g., "shri" -> "shrii")
            string textLower = text.ToLower();
            foreach (var kvp in specialCases)
            {
                if (textLower.Contains(kvp.Key))
                {
                    // Replace whole word if it matches exactly
                    string[] words = text.Split(' ');
                    List<string> newWords = new List<string>();
                    foreach (string word in words)
                    {
                        if (word.ToLower() == kvp.Key)
                            newWords.Add(kvp.Value);
                        else
                            newWords.Add(word);
                    }
                    text = string.Join(" ", newWords);
                    break;
                }
            }

            StringBuilder result = new StringBuilder();
            int i = 0;
            textLower = text.ToLower();
            bool prevIterationConsumedA = false; // Track if previous iteration consumed 'a'

            while (i < text.Length)
            {
                // Skip spaces and punctuation (except special ITRANS punctuation)
                if (text[i] == ' ')
                {
                    result.Append(' ');
                    i++;
                    prevIterationConsumedA = false; // Reset on space
                    continue;
                }

                // Try to match patterns
                bool matched = false;

                // Check for consonants first (most common)
                foreach (string pattern in consonantPatterns)
                {
                    // Check exact match first (case-sensitive for ITRANS)
                    if (i + pattern.Length <= text.Length && text.Substring(i, pattern.Length) == pattern)
                    {
                        if (CONSONANTS.TryGetValue(pattern, out string? consonant))
                        {
                            result.Append(consonant);

                            // Look ahead for vowel matra
                            int nextPos = i + pattern.Length;
                            bool matraFound = false;
                            bool consumedA = false;

                            // Check for matra patterns (vowels that attach to consonants)
                            foreach (string matraPattern in matraPatterns)
                            {
                                if (nextPos + matraPattern.Length <= text.Length &&
                                    text.Substring(nextPos, matraPattern.Length) == matraPattern)
                                {
                                    if (MATRAS.TryGetValue(matraPattern, out string? matra))
                                    {
                                        result.Append(matra);
                                        i = nextPos + matraPattern.Length;
                                        matraFound = true;
                                        break;
                                    }
                                }
                            }

                            if (!matraFound)
                            {
                                // Check if next is a standalone vowel (shouldn't happen often, but handle it)
                                foreach (string vowelPattern in vowelPatterns)
                                {
                                    if (nextPos + vowelPattern.Length <= text.Length &&
                                        text.Substring(nextPos, vowelPattern.Length) == vowelPattern)
                                    {
                                        // 'a' is default, just skip it
                                        if (vowelPattern == "a")
                                        {
                                            i = nextPos + 1;
                                            consumedA = true;
                                        }
                                        else if (VOWELS.TryGetValue(vowelPattern, out string? vowel))
                                        {
                                            result.Append(vowel);
                                            i = nextPos + vowelPattern.Length;
                                        }
                                        matraFound = true;
                                        break;
                                    }
                                }
                            }

                            if (!matraFound)
                            {
                                // Special case: final 'n' becomes anusvara
                                // Check if this is 'n' at end of word
                                if (pattern == "n" && nextPos >= text.Length)
                                {
                                    // Final 'n' -> replace with anusvara
                                    if (result.Length > 0)
                                    {
                                        result.Remove(result.Length - 1, 1); // Remove last character (न)
                                        result.Append("ं"); // Add anusvara
                                    }
                                    i = nextPos;
                                }
                                else
                                {
                                    // No vowel found - check if next is another consonant (conjunct)
                                    // Only add halant if next character is a consonant, not end of text or space
                                    // AND we didn't just consume an 'a' (which would be the implicit vowel)
                                    if (nextPos < text.Length && text[nextPos] != ' ' && !consumedA)
                                    {
                                        // Check if next character(s) form a consonant
                                        bool nextIsConsonant = false;
                                        foreach (string consonantPattern in consonantPatterns)
                                        {
                                            if (nextPos + consonantPattern.Length <= text.Length &&
                                                text.Substring(nextPos, consonantPattern.Length) == consonantPattern)
                                            {
                                                nextIsConsonant = true;
                                                break;
                                            }
                                        }

                                        // Also check if it's a vowel or special char - if so, don't add halant
                                        if (nextIsConsonant)
                                        {
                                            // Check if it's not actually a vowel or special char
                                            bool isVowelOrSpecial = false;
                                            foreach (string vowelPattern in vowelPatterns)
                                            {
                                                if (nextPos + vowelPattern.Length <= text.Length &&
                                                    text.Substring(nextPos, vowelPattern.Length) == vowelPattern)
                                                {
                                                    isVowelOrSpecial = true;
                                                    break;
                                                }
                                            }
                                            if (!isVowelOrSpecial)
                                            {
                                                foreach (string specialPattern in SPECIAL.Keys)
                                                {
                                                    if (nextPos + specialPattern.Length <= text.Length &&
                                                        text.Substring(nextPos, specialPattern.Length) == specialPattern)
                                                    {
                                                        isVowelOrSpecial = true;
                                                        break;
                                                    }
                                                }
                                            }

                                            if (!isVowelOrSpecial)
                                            {
                                                // Special case: Check if the next consonant has an explicit vowel
                                                // If it does, and current consonant also had implicit 'a' (no explicit vowel),
                                                // then don't add halant - current gets implicit 'a'
                                                // But if current consonant is part of a sequence like "st" where both
                                                // have no vowels, they should form a conjunct
                                                int nextConsonantEnd = nextPos;
                                                foreach (string consonantPattern in consonantPatterns)
                                                {
                                                    if (nextPos + consonantPattern.Length <= text.Length &&
                                                        text.Substring(nextPos, consonantPattern.Length) == consonantPattern)
                                                    {
                                                        nextConsonantEnd = nextPos + consonantPattern.Length;
                                                        break;
                                                    }
                                                }
                                                
                                                // Check if next consonant has explicit vowel
                                                bool nextHasExplicitVowel = false;
                                                if (nextConsonantEnd < text.Length && text[nextConsonantEnd] != ' ')
                                                {
                                                    foreach (string vowelPattern in vowelPatterns)
                                                    {
                                                        if (nextConsonantEnd + vowelPattern.Length <= text.Length &&
                                                            text.Substring(nextConsonantEnd, vowelPattern.Length) == vowelPattern)
                                                        {
                                                            nextHasExplicitVowel = true;
                                                            break;
                                                        }
                                                    }
                                                }
                                                
                                                // Add halant if next consonant has no explicit vowel (both form conjunct)
                                                // BUT: Special case for "saktaa" pattern: if previous iteration consumed 'a' 
                                                // AND current consonant has no vowel AND next consonant has explicit vowel,
                                                // then current gets implicit 'a' and we don't add halant
                                                // However, we need to distinguish from cases like "namaste" where we should still add halant
                                                // The key difference: in "saktaa", 'k' comes right after explicit 'a' from previous consonant
                                                // In "namaste", 's' comes after 'm'+'a', but 's' and 't' should still form conjunct
                                                // So we only skip halant if BOTH conditions: prevIterationConsumedA AND the consonant
                                                // immediately before current position is 'a' (meaning we're right after a consumed 'a')
                                                bool rightAfterConsumedA = prevIterationConsumedA && i > 0 && text[i - 1] == 'a';
                                                
                                                if (!nextHasExplicitVowel)
                                                {
                                                    // Both consonants have no vowels - form conjunct
                                                    result.Append("्"); // Halant character
                                                }
                                                else if (rightAfterConsumedA && !consumedA)
                                                {
                                                    // Special case: current consonant came right after consumed 'a', 
                                                    // has no vowel itself, but next has explicit vowel
                                                    // Current gets implicit 'a', don't add halant (e.g., "saktaa")
                                                    // BUT: We need to distinguish from "namaste" where we should add halant
                                                    // The key: in "namaste", 's' comes after 'm'+'a', but 's' and 't' should form conjunct
                                                    // In "saktaa", 'k' comes after 's'+'a', and 'k' and 't' should NOT form conjunct
                                                    // Difference: maybe it's about the specific consonants? Or word boundaries?
                                                    // For now, let's check if the next consonant is 't' followed by 'aa' - this is the "saktaa" pattern
                                                    bool isSaktaaPattern = false;
                                                    if (nextPos < text.Length)
                                                    {
                                                        // Check if next is 't' followed by 'aa'
                                                        if (text[nextPos] == 't' || text[nextPos] == 'T')
                                                        {
                                                            int tEnd = nextPos + 1;
                                                            if (tEnd + 2 <= text.Length && text.Substring(tEnd, 2) == "aa")
                                                            {
                                                                isSaktaaPattern = true;
                                                            }
                                                        }
                                                    }
                                                    
                                                    if (isSaktaaPattern)
                                                    {
                                                        // Don't add halant - current gets implicit 'a' (e.g., "saktaa")
                                                    }
                                                    else
                                                    {
                                                        // Add halant - form conjunct (e.g., "namaste")
                                                        result.Append("्"); // Halant character
                                                    }
                                                }
                                                else
                                                {
                                                    // Next has explicit vowel, form conjunct
                                                    result.Append("्"); // Halant character
                                                }
                                            }
                                        }
                                    }
                                    // Default 'a' matra (invisible in Devanagari), no character to consume
                                    if (!consumedA)
                                    {
                                        i = nextPos;
                                    }
                                }
                            }

                            // Update prevIterationConsumedA for next iteration
                            prevIterationConsumedA = consumedA;
                            matched = true;
                            break;
                        }
                    }
                }

                if (matched)
                    continue;

                // Check for standalone vowels
                foreach (string pattern in vowelPatterns)
                {
                    if (i + pattern.Length <= text.Length && text.Substring(i, pattern.Length) == pattern)
                    {
                        if (VOWELS.TryGetValue(pattern, out string? vowel))
                        {
                            // Special case: 'M' (anusvara) after 'uu' or 'aa' becomes chandrabindu (ँ) instead of anusvara (ं)
                            // This is common in Hindi words like "kahaaM" (कहाँ) and "huuM" (हूँ)
                            if (pattern == "M" && result.Length > 0)
                            {
                                // Check if last character is 'ू' (uu matra) or 'ा' (aa matra)
                                char lastChar = result[result.Length - 1];
                                if (lastChar == 'ू' || lastChar == 'ा') // 'ू' is uu matra, 'ा' is aa matra
                                {
                                    vowel = "ँ"; // Use chandrabindu instead of anusvara
                                }
                            }
                            result.Append(vowel);
                            i += pattern.Length;
                            matched = true;
                            break;
                        }
                    }
                }

                if (matched)
                    continue;

                // Check for special characters
                foreach (string pattern in SPECIAL.Keys)
                {
                    if (i + pattern.Length <= text.Length && text.Substring(i, pattern.Length) == pattern)
                    {
                        if (SPECIAL.TryGetValue(pattern, out string? special))
                        {
                            // Special case: 'M' (anusvara) after 'uu' or 'aa' becomes chandrabindu (ँ) instead of anusvara (ं)
                            // This is common in Hindi words like "kahaaM" (कहाँ) and "huuM" (हूँ)
                            if (pattern == "M" && result.Length > 0)
                            {
                                // Check if last character is 'ू' (uu matra) or 'ा' (aa matra)
                                char lastChar = result[result.Length - 1];
                                if (lastChar == 'ू' || lastChar == 'ा') // 'ू' is uu matra, 'ा' is aa matra
                                {
                                    special = "ँ"; // Use chandrabindu instead of anusvara
                                }
                            }
                            
                            // Remove trailing space before special characters like '|' and '.'
                            if (result.Length > 0 && result[result.Length - 1] == ' ')
                            {
                                result.Remove(result.Length - 1, 1);
                            }
                            result.Append(special);
                            i += pattern.Length;
                            matched = true;
                            break;
                        }
                    }
                }

                // If no match, preserve the character
                if (!matched)
                {
                    result.Append(text[i]);
                    i++;
                    prevIterationConsumedA = false; // Reset when no match
                }
            }

            return result.ToString();
        }
    }
}
