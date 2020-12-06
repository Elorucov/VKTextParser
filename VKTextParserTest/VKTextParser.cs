using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;

namespace VKTextParserTest {
    public class VKTextParser {
        static Regex urlRegex = new Regex(@"(?:(?:http|https):\/\/)?([-a-zA-Z0-9а-яА-Я.]{2,256}\.[a-zа-я]{2,8})\b(?:\/[-a-zA-Z0-9а-яА-Я@:%_\+.~#?&//=]*)?", RegexOptions.Compiled);
        static Regex mailRegex = new Regex(@"([\w\d.]+)@([a-zA-Z0-9а-яА-Я.]{2,256}\.[a-zа-я]{2,8})", RegexOptions.Compiled);
        static Regex userRegex = new Regex(@"\[(id)(\d+)\|(.*?)\]", RegexOptions.Compiled);
        static Regex groupRegex = new Regex(@"\[(club|public|event)(\d+)\|(.*?)\]", RegexOptions.Compiled);
        static Regex linkInTextRegex = new Regex(@"\[((?:http|https):\/\/[-a-zA-Z0-9а-яА-Я./]{2,256})\|((.*)+)\]", RegexOptions.Compiled);

        #region Internal parsing methods

        private static List<string> GetWords(string plain) {
            List<string> words = new List<string>();
            string lastWord = String.Empty;
            bool inSquareBracket = false;
            foreach (char letter in plain) {
                if (letter == '[') {
                    inSquareBracket = true;
                }
                if (letter == ']') {
                    inSquareBracket = false;
                }
                if (!inSquareBracket && Char.IsWhiteSpace(letter)) {
                    words.Add(lastWord + letter);
                    lastWord = String.Empty;
                } else {
                    lastWord = lastWord + letter;
                }
            }
            words.Add(lastWord);
            return words;
        }

        private static void FixPreMatch(List<Tuple<string, string>> raw, Match match, string word) {
            if (word.Length > match.Length) {
                string pre = word.Substring(0, match.Index);
                raw.Add(new Tuple<string, string>(null, pre));
            }
        }

        private static void FixPostMatch(List<Tuple<string, string>> raw, Match match, string word) {
            if (word.Length > match.Length) {
                string post = word.Substring(match.Index + match.Length, word.Length - match.Length - match.Index);
                raw.Add(new Tuple<string, string>(null, post));
            }
        }

        private static bool ParseBracketWord(List<Tuple<string, string>> raw, Regex regex, string word, string before) {
            var match = regex.Match(word);
            if (match.Success) {
                FixPreMatch(raw, match, word);
                var parsed = new Tuple<string, string>($"{before}{match.Groups[1]}{match.Groups[2]}", match.Groups[3].Value);
                raw.Add(parsed);
                FixPostMatch(raw, match, word);
                return true;
            }
            return false;
        }

        private static bool ParseLinkInBracketWord(List<Tuple<string, string>> raw, Regex regex, string word) {
            var match = regex.Match(word);
            if (match.Success) {
                FixPreMatch(raw, match, word);
                var parsed = new Tuple<string, string>(match.Groups[1].Value, match.Groups[2].Value);
                raw.Add(parsed);
                FixPostMatch(raw, match, word);
                return true;
            }
            return false;
        }

        private static List<Tuple<string, string>> GetRaw(string plain) {
            plain = plain.Trim();
            List<Tuple<string, string>> raw = new List<Tuple<string, string>>();

            List<string> words = GetWords(plain);
            foreach (string word in words) {

                if (ParseBracketWord(raw, userRegex, word, "https://vk.com/")) continue;
                if (ParseBracketWord(raw, groupRegex, word, "https://vk.com/")) continue;
                if (ParseLinkInBracketWord(raw, linkInTextRegex, word)) continue;

                Match mailMatch = mailRegex.Match(word);
                if (mailMatch.Success) {
                    FixPreMatch(raw, mailMatch, word);
                    string mail = $"{mailMatch.Groups[1]}@{mailMatch.Groups[2]}";
                    raw.Add(new Tuple<string, string>($"mailto:{mailMatch}", mailMatch.Value));
                    FixPostMatch(raw, mailMatch, word);
                    continue;
                }

                Match urlMatch = urlRegex.Match(word);
                if (urlMatch.Success) {
                    FixPreMatch(raw, urlMatch, word);

                    string url = urlMatch.Value;
                    if (!url.StartsWith("https://") && !url.StartsWith("http://")) url = $"https://{url}";
                    raw.Add(new Tuple<string, string>(url, urlMatch.Value));
                    
                    FixPostMatch(raw, urlMatch, word);
                    continue;
                }

                raw.Add(new Tuple<string, string>(null, word));
            }

            return raw;
        }

        #endregion

        #region For RichTextBlock

        private static Run BuildRunForRTBStyle(string text, RichTextBlock rtb) {
            return new Run {
                Text = text,
                FontFamily = rtb.FontFamily,
            };
        }

        private static Hyperlink BuildHyperlinkForRTBStyle(string text, string link, RichTextBlock rtb, Action<string> clickedCallback) {
            Hyperlink h = new Hyperlink {
                FontFamily = rtb.FontFamily,
            };
            h.Inlines.Add(new Run { Text = text });
            h.Click += (a, b) => { clickedCallback?.Invoke(link); };
            return h;
        }

        public static void SetText(string plain, RichTextBlock rtb, Action<string> linksClickedCallback = null) {
            Paragraph p = new Paragraph();

            foreach (var token in GetRaw(plain)) {
                if (String.IsNullOrEmpty(token.Item1)) {
                    p.Inlines.Add(BuildRunForRTBStyle(token.Item2, rtb));
                } else {
                    Hyperlink h = BuildHyperlinkForRTBStyle(token.Item2, token.Item1, rtb, linksClickedCallback);
                    p.Inlines.Add(h);
                }
            }

            rtb.Blocks.Clear();
            rtb.Blocks.Add(p);
        }

        #endregion

        public static string GetParsedText(string plain) {
            string text = String.Empty;
            GetRaw(plain).ForEach(t => text += t.Item2);
            return text;
        }
    }
}
