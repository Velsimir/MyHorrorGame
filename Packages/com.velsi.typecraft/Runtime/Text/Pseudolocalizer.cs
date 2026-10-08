using System.Collections.Generic;
using System.Text;

namespace Velsi.Typecraft.Text
{
    public static class Pseudolocalizer
    {
        public const string Locale = "pseudo";
        private const float Expansion = 0.35f;

        private static readonly Dictionary<char, char> Accents = new Dictionary<char, char>
        {
            ['a'] = 'á', ['c'] = 'ç', ['d'] = 'ð', ['e'] = 'é', ['g'] = 'ğ', ['h'] = 'ĥ', ['i'] = 'í', ['j'] = 'ĵ',
            ['k'] = 'ķ', ['l'] = 'ł', ['n'] = 'ñ', ['o'] = 'ö', ['r'] = 'ŕ', ['s'] = 'š', ['t'] = 'ţ', ['u'] = 'ü',
            ['w'] = 'ŵ', ['y'] = 'ý', ['z'] = 'ž',
            ['A'] = 'Å', ['C'] = 'Ç', ['D'] = 'Ð', ['E'] = 'É', ['G'] = 'Ğ', ['H'] = 'Ĥ', ['I'] = 'Î', ['L'] = 'Ł',
            ['N'] = 'Ñ', ['O'] = 'Ö', ['R'] = 'Ŕ', ['S'] = 'Š', ['T'] = 'Ţ', ['U'] = 'Ü', ['W'] = 'Ŵ', ['Y'] = 'Ý',
            ['Z'] = 'Ž'
        };

        public static bool IsPseudo(string locale) => locale == Locale;

        public static string Transform(string markup)
        {
            if (string.IsNullOrEmpty(markup))
                return markup;

            var output = new StringBuilder(markup.Length * 2);
            output.Append("\\[");
            int letters = 0;
            int i = 0;

            while (i < markup.Length)
            {
                char c = markup[i];

                if (c == '\\' && i + 1 < markup.Length)
                {
                    output.Append(markup, i, 2);
                    i += 2;
                    continue;
                }

                char close = c == '{' ? '}' : c == '[' ? ']' : c == '<' ? '>' : '\0';
                int end = close != '\0' ? markup.IndexOf(close, i + 1) : -1;
                if (end > i)
                {
                    output.Append(markup, i, end - i + 1);
                    i = end + 1;
                    continue;
                }

                if (char.IsLetterOrDigit(c))
                    letters++;

                output.Append(Accents.TryGetValue(c, out char accented) ? accented : c);
                i++;
            }

            output.Append(' ').Append('·', System.Math.Max(1, (int)(letters * Expansion)));
            output.Append("\\]");
            return output.ToString();
        }
    }
}
