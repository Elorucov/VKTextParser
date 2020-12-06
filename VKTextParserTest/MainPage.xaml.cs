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
            string test = "Это [club171015120|группа] приложения Laney, а [id172894294|Эльчин Оруджев] — его разработчик. Сайт разработчика: https://elor.top, почта: me@elor.top.\n";
            test += "Домен с точкой [id0|@test.az]; а это — [https://vk.com/spacevk|ссылка].";
            test += "\nABCD[id1|EFGH]IJKL[club1|MNOP]QR[https://vk.com/bagledi|ST]UVW[event1|XYZ]";

            await Task.Delay(100);
            Plain.Text = test;
        }

        private void OnTextChanging(TextBox sender, TextBoxTextChangingEventArgs args) {
            ParseForRichTextBlock(sender.Text);
            ParseForString(sender.Text);
        }

        private void ParseForRichTextBlock(string plain) {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            VKTextParser.SetText(plain, Result, OnLinkClicked);
            sw.Stop();

            Paragraph p = new Paragraph();
            p.Inlines.Add(new Run { Text = $"Parsing and rendering took {sw.ElapsedMilliseconds} ms.", FontSize = 12, FontStyle = Windows.UI.Text.FontStyle.Italic });
            Result.Blocks.Add(p);
        }

        private void ParseForString(string plain) {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            string text = VKTextParser.GetParsedText(plain);
            sw.Stop();

            Paragraph p = new Paragraph();
            p.Inlines.Add(new LineBreak());
            p.Inlines.Add(new LineBreak());
            p.Inlines.Add(new Run { Text = text, FontSize = 14 });
            p.Inlines.Add(new LineBreak());
            p.Inlines.Add(new Run { Text = $"Parsing took {sw.ElapsedMilliseconds} ms.", FontSize = 12, FontStyle = Windows.UI.Text.FontStyle.Italic });
            Result.Blocks.Add(p);
        }

        private async void OnLinkClicked(string link) {
            await new MessageDialog(link, "Link").ShowAsync();
        }
    }
}