using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace VKTextParserTest {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page {
        public MainPage() {
            this.InitializeComponent();
        }

        private async void OnLoaded(object sender, RoutedEventArgs e) {
            string test = "Это [club171015120|группа] приложения Laney, а [id172894294|Эльчин Оруджев] — его разработчик. Сайт разработчика: https://elor.top, почта: me@elor.top.";
            //test += "Домен с точкой [id0|@test.az]; а это — [https://vk.com/spacevk|ссылка].";
            //test += "\nABCD[id1|EFGH]IJKL[club1|MNOP]QR[https://vk.com/bagledi|ST]UVW[event1|XYZ]";

            string formatData = "{\"version\":1,\"items\":[{\"offset\":0,\"length\":7,\"type\":\"bold\",\"url\":\"\"},{\"offset\":15,\"length\":12,\"type\":\"italic\",\"url\":\"\"},{\"offset\":48,\"length\":15,\"type\":\"underline\",\"url\":\"\"},{\"offset\":65,\"length\":17,\"type\":\"link\",\"url\":\"https://elor.top\"}]}";

            await Task.Delay(100);
            Plain.Text = test;
            FormatDataInfo.Text = formatData;
        }

        private void OnTextChanging(TextBox sender, TextBoxTextChangingEventArgs args) {
            ParseForRichTextBlock();
        }

        private void ParseForRichTextBlock() {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            FormatData formatData = null;
            try {
                formatData = JsonConvert.DeserializeObject<FormatData>(FormatDataInfo.Text);
            } catch (Exception ex) {
                
            }

            Result.Blocks.Clear();
            var result = VKTextParser.ParseText(Plain.Text, formatData, OnLinkClicked);
            Paragraph p = new Paragraph();
            foreach (var inline in result.TextBlockChunks) {
                p.Inlines.Add(inline);
            }
            Result.Blocks.Add(p);

            sw.Stop();

            Paragraph p4 = new Paragraph();
            p4.Inlines.Add(new LineBreak());
            p4.Inlines.Add(new LineBreak());
            p4.Inlines.Add(new Run { Text = result.PlainText, FontSize = 14 });
            p4.Inlines.Add(new LineBreak());
            p4.Inlines.Add(new Run { Text = $"Parsing took {sw.ElapsedMilliseconds} ms.", FontSize = 12, FontStyle = Windows.UI.Text.FontStyle.Italic });
            Result.Blocks.Add(p4);
        }

        private async void OnLinkClicked(string link) {
            await new MessageDialog(link, "Link").ShowAsync();
        }
    }
}