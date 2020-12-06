using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;

namespace VKTextParserTest {
    enum MatchType { User, Group, LinkInText, Mail, Url }

    class MatchInfo {
        public int Start { get; private set; }
        public int Length { get; private set; }
        public MatchType Type { get; private set; }
        public Match Match { get; private set; }

        public MatchInfo(int start, int length, MatchType type, Match match) {
            Start = start;
            Length = length;
            Type = type;
            Match = match;
        }
    }

    public class VKTextParser {
        static Regex urlRegex = new Regex(@"(?:(?:http|https):\/\/)?([-a-zA-Z0-9а-яА-Я.]{2,256}\.[a-zа-я]{2,8})\b(?:\/[-a-zA-Z0-9а-яА-Я@:%_\+.~#?&//=]*)?", RegexOptions.Compiled);
        static Regex mailRegex = new Regex(@"([\w\d.]+)@([a-zA-Z0-9а-яА-Я.]{2,256}\.[a-zа-я]{2,8})", RegexOptions.Compiled);
        static Regex userRegex = new Regex(@"\[(id)(\d+)\|(.*?)\]", RegexOptions.Compiled);
        static Regex groupRegex = new Regex(@"\[(club|public|event)(\d+)\|(.*?)\]", RegexOptions.Compiled);
        static Regex linkInTextRegex = new Regex(@"\[((?:http|https):\/\/vk.com\/[\w\d./]*?)\|((.*?)+?)\]", RegexOptions.Compiled);

        #region Internal parsing methods

        private static Tuple<string, string> ParseBracketWord(Match match) {
            return new Tuple<string, string>($"https://vk.com/{match.Groups[1]}{match.Groups[2]}", match.Groups[3].Value);
        }

        private static Tuple<string, string> ParseLinkInBracketWord(Match match) {
            return new Tuple<string, string>(match.Groups[1].Value, match.Groups[2].Value);
        }

        private static List<Tuple<string, string>> GetRaw(string plain) {
            plain = plain.Trim();
            List<Tuple<string, string>> raw = new List<Tuple<string, string>>();
            List<MatchInfo> allMatches = new List<MatchInfo>();

            userRegex.Matches(plain).ToList().ForEach(m => allMatches.Add(new MatchInfo(m.Index, m.Length, MatchType.User, m)));
            groupRegex.Matches(plain).ToList().ForEach(m => allMatches.Add(new MatchInfo(m.Index, m.Length, MatchType.Group, m)));
            linkInTextRegex.Matches(plain).ToList().ForEach(m => allMatches.Add(new MatchInfo(m.Index, m.Length, MatchType.LinkInText, m)));
            mailRegex.Matches(plain).ToList().ForEach(m => allMatches.Add(new MatchInfo(m.Index, m.Length, MatchType.Mail, m)));
            urlRegex.Matches(plain).ToList().ForEach(m => allMatches.Add(new MatchInfo(m.Index, m.Length, MatchType.Url, m)));

            allMatches = allMatches.OrderBy(m => m.Start).ToList();

            string word = String.Empty;
            for (int i = 0; i < plain.Length; i++) {
                var matchInfo = allMatches.Where(m => m.Start == i).FirstOrDefault();
                if (matchInfo != null) {
                    raw.Add(new Tuple<string, string>(null, word));
                    word = String.Empty;

                    Match match = matchInfo.Match;
                    switch (matchInfo.Type) {
                        case MatchType.User:
                        case MatchType.Group: raw.Add(ParseBracketWord(match)); break;
                        case MatchType.LinkInText: raw.Add(ParseLinkInBracketWord(match)); break;
                        case MatchType.Mail: raw.Add(new Tuple<string, string>($"mailto:{match}", match.Value)); break;
                        case MatchType.Url:
                            string url = match.Value;
                            if (!url.StartsWith("https://") && !url.StartsWith("http://")) url = $"https://{url}";
                            raw.Add(new Tuple<string, string>(url, match.Value));
                            break;
                    }

                    i = i + matchInfo.Length - 1;
                } else {
                    word += plain[i];
                }
            }
            raw.Add(new Tuple<string, string>(null, word));

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
