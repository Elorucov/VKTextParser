using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Windows.UI.Text;
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

    public enum TextChunkType : byte { Plain = 1, Bold = 2, Italic = 4, Underline = 8, Link = 16 }

    public class TextChunk {
        public string Text { get; private set; }
        public TextChunkType Type { get; private set; }
        public string Url { get; private set; }

        public TextChunk(string text, TextChunkType type = TextChunkType.Plain, string url = null) {
            Text = text;
            Type = type;
            Url = url;
        }
    }

    public class TextParsingResult {
        public string PlainText { get; set; }

        public List<TextChunk> Chunks { get; set; }

        public List<Inline> TextBlockChunks { get; set; }
    }

    public class VKTextParser {
        static Regex urlRegex = new Regex(@"(?:(?:http|https):\/\/)?([a-z0-9.\-]*\.)?([-a-zA-Z0-9а-яА-Я]{1,256})\.([-a-zA-Z0-9а-яА-Я]{2,8})\b(?:\/[-a-zA-Z0-9а-яА-Я@:%_\+.~#?!&\/=]*)?", RegexOptions.Compiled);
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

        public static TextParsingResult ParseText(string plain, FormatData formatData = null, Action<string> linkClickedCallback = null) {
            TextParsingResult result = new TextParsingResult();
            FormatData fdata = formatData ?? new FormatData();
            if (fdata.Items == null) fdata.Items = new List<FormatDataItem>();

            // Parse inline links to add it to FormatData object.
            var raw = GetRaw(plain);
            StringBuilder sb = new StringBuilder();
            foreach (var rawData in raw) {
                if (!string.IsNullOrEmpty(rawData.Item1)) {
                    fdata.Items.Add(new FormatDataItem { 
                        Type = FormatDataTypes.LINK,
                        Url = rawData.Item1,
                        Offset = sb.Length,
                        Length = rawData.Item2.Length,
                    });
                }
                sb.Append(rawData.Item2);
            }
            result.PlainText = sb.ToString();

            // Create chunks
            result.Chunks = new List<TextChunk>();
            StringBuilder chunkSB = new StringBuilder();
            TextChunkType tcType = TextChunkType.Plain;
            string url = null;
            if (fdata.Items.Count > 0) {
                for (int i = 0; i < result.PlainText.Length; i++) {
                    var intersects = fdata.Items.Where(fdi => fdi.Offset <= i && fdi.Offset + fdi.Length > i);
                    if (intersects.Count() == 0) { // если буква не имеет никаких стилей или ссылок
                        if (tcType != TextChunkType.Plain) {
                            result.Chunks.Add(new TextChunk(chunkSB.ToString(), tcType, url));
                            tcType = TextChunkType.Plain;
                            chunkSB.Clear();
                        }
                        chunkSB.Append(result.PlainText[i]);
                    } else { // если имеет стили и ссылки
                        TextChunkType tcType2 = TextChunkType.Plain;
                        string url2 = null;
                        foreach (var fdi in intersects) {
                            switch (fdi.Type) {
                                case FormatDataTypes.BOLD:
                                    tcType2 = tcType2 | TextChunkType.Bold;
                                    break;
                                case FormatDataTypes.ITALIC:
                                    tcType2 = tcType2 | TextChunkType.Italic;
                                    break;
                                case FormatDataTypes.UNDERLINE:
                                    tcType2 = tcType2 | TextChunkType.Underline;
                                    break;
                                case FormatDataTypes.LINK:
                                    tcType2 = tcType2 | TextChunkType.Link;
                                    url2 = fdi.Url;
                                    break;
                            }
                        }
                        if (tcType2 != tcType || url != url2) {
                            result.Chunks.Add(new TextChunk(chunkSB.ToString(), tcType, url));
                            tcType = tcType2;
                            url = url2;
                            chunkSB.Clear();
                        }
                        chunkSB.Append(result.PlainText[i]);
                    }
                }
                result.Chunks.Add(new TextChunk(chunkSB.ToString(), tcType, url));
                chunkSB.Clear();

                result.TextBlockChunks = new List<Inline>();
                foreach (var chunk in result.Chunks) {
                    if (chunk.Type.HasFlag(TextChunkType.Link) && Uri.IsWellFormedUriString(chunk.Url, UriKind.Absolute)) {
                        Hyperlink hl = new Hyperlink();
                        hl.Click += (a, b) => linkClickedCallback?.Invoke(chunk.Url);
                        hl.Inlines.Add(new Run { Text = chunk.Text });
                        if (chunk.Type.HasFlag(TextChunkType.Bold)) hl.FontWeight = FontWeights.SemiBold;
                        if (chunk.Type.HasFlag(TextChunkType.Italic)) hl.FontStyle = FontStyle.Italic;
                        if (chunk.Type.HasFlag(TextChunkType.Underline)) hl.TextDecorations = TextDecorations.Underline;
                        result.TextBlockChunks.Add(hl);
                    } else {
                        Run run = new Run { Text = chunk.Text };
                        if (chunk.Type.HasFlag(TextChunkType.Bold)) run.FontWeight = FontWeights.SemiBold;
                        if (chunk.Type.HasFlag(TextChunkType.Italic)) run.FontStyle = FontStyle.Italic;
                        if (chunk.Type.HasFlag(TextChunkType.Underline)) run.TextDecorations = TextDecorations.Underline;
                        result.TextBlockChunks.Add(run);
                    }
                }
            } else {
                result.TextBlockChunks = new List<Inline>() {
                    new Run() { Text = result.PlainText },
                };
            }

            return result;
        }
    }
}
