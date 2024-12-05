using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VKTextParserTest {
    public static class FormatDataTypes {
        public const string BOLD = "bold";
        public const string ITALIC = "italic";
        public const string UNDERLINE = "underline";
        public const string LINK = "link";
    }

    public class FormatDataItem {
        [JsonProperty("offset")]
        public int Offset { get; set; }

        [JsonProperty("length")]
        public int Length { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public class FormatData {
        [JsonProperty("version")]
        public ushort Version { get; set; } = 1;

        [JsonProperty("items")]
        public List<FormatDataItem> Items { get; set; }
    }
}
