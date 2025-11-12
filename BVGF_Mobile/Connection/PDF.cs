
using SkiaSharp;
using System.Text;
using iTextIO = iText.IO.Image;
using iTextLayout = iText.Layout;
using iTextPdf = iText.Kernel.Pdf;
using iTextProperties = iText.Layout.Properties;

namespace BVGF.Connection
{
    public class PDF
    {
        // Page settings
        private const int PAGE_WIDTH = 1240;
        private const int PAGE_HEIGHT = 1754;
        private const int MARGIN = 50;
        private const float FONT_SIZE = 42f;

        private readonly string _ownerPassword = "BT@2024Secure#Protect";

        private List<byte[]> RenderTextToImages(string text)
        {
            var pages = new List<byte[]>();

            using var paint = new SKPaint
            {
                Color = SKColors.Black,
                Typeface = SKTypeface.FromFamilyName("Arial"),
                TextSize = FONT_SIZE,
                IsAntialias = true
            };

            float usableWidth = PAGE_WIDTH - (MARGIN * 2);
            var wrapped = WrapText(text, paint, usableWidth);

            int lineHeight = (int)(paint.FontMetrics.Descent - paint.FontMetrics.Ascent + 10);
            int index = 0;

            while (index < wrapped.Count)
            {
                using var bmp = new SKBitmap(PAGE_WIDTH, PAGE_HEIGHT);
                using var canvas = new SKCanvas(bmp);

                canvas.Clear(SKColors.White);

                float y = MARGIN;

                while (y < PAGE_HEIGHT - MARGIN && index < wrapped.Count)
                {
                    canvas.DrawText(wrapped[index], MARGIN, y, paint);
                    index++;
                    y += lineHeight;
                }

                using var ms = new MemoryStream();
                bmp.Encode(ms, SKEncodedImageFormat.Png, 100);
                pages.Add(ms.ToArray());
            }

            return pages;
        }

        // ----------------------------------------------------------------------
        // ✅ Word wrapping for SkiaSharp text
        // ----------------------------------------------------------------------
        private List<string> WrapText(string text, SKPaint paint, float maxWidth)
        {
            var lines = new List<string>();
            var paragraphs = text.Split('\n');

            foreach (var p in paragraphs)
            {
                var words = p.Split(' ');
                var current = new StringBuilder();

                foreach (var w in words)
                {
                    string test = (current.Length == 0) ? w : current + " " + w;

                    if (paint.MeasureText(test) > maxWidth)
                    {
                        lines.Add(current.ToString());
                        current.Clear();
                        current.Append(w);
                    }
                    else
                    {
                        if (current.Length == 0) current.Append(w);
                        else current.Append(" " + w);
                    }
                }

                if (current.Length > 0)
                    lines.Add(current.ToString());

                lines.Add(""); // Paragraph spacing
            }

            return lines;
        }

        // ----------------------------------------------------------------------
        // ✅ Convert image pages → Encrypted PDF (no copy, no print)
        // ----------------------------------------------------------------------
        private byte[] CreatePdfFromImages(List<byte[]> pages)
        {
            using var ms = new MemoryStream();

            var props = new iTextPdf.WriterProperties();
            props.SetStandardEncryption(
                userPassword: Encoding.UTF8.GetBytes(""),
                ownerPassword: Encoding.UTF8.GetBytes(_ownerPassword),
                permissions: 0, // ❌ no copy ❌ no print ❌ no edit
                encryptionAlgorithm: iTextPdf.EncryptionConstants.ENCRYPTION_AES_256
            );

            var writer = new iTextPdf.PdfWriter(ms, props);
            var pdf = new iTextPdf.PdfDocument(writer);
            var doc = new iTextLayout.Document(pdf);

            foreach (var p in pages)
            {
                pdf.AddNewPage();

                var imgData = iTextIO.ImageDataFactory.Create(p);
                var img = new iTextLayout.Element.Image(imgData);

                img.SetAutoScale(true);
                img.SetHorizontalAlignment(iTextProperties.HorizontalAlignment.CENTER);

                doc.Add(img);
            }

            doc.Close();
            return ms.ToArray();
        }

        // ----------------------------------------------------------------------
        // ✅ PUBLIC: Member PDF
        // ----------------------------------------------------------------------
        public byte[] GenerateMemberPdf(
            string name, string company, string category, string city,
            string mobile1, string mobile2 = null, string mobile3 = null,
            string telephone = null, string email1 = null, string email2 = null,
            string email3 = null, string address = null)
        {
            var sb = new StringBuilder();

            sb.AppendLine("BT Address Book");
            sb.AppendLine($"Member Details — {name}");
            sb.AppendLine("--------------------------------------------------");

            sb.AppendLine($"\nName      : {name}");
            sb.AppendLine($"Company   : {company}");
            sb.AppendLine($"Category  : {category}");
            sb.AppendLine($"City      : {city}");
            if (!string.IsNullOrWhiteSpace(address)) sb.AppendLine($"Address   : {address}");

            sb.AppendLine("\nContact Information");
            sb.AppendLine($"Mobile 1  : {mobile1}");
            if (!string.IsNullOrWhiteSpace(mobile2)) sb.AppendLine($"Mobile 2  : {mobile2}");
            if (!string.IsNullOrWhiteSpace(mobile3)) sb.AppendLine($"Mobile 3  : {mobile3}");
            if (!string.IsNullOrWhiteSpace(telephone)) sb.AppendLine($"Telephone : {telephone}");

            if (!string.IsNullOrWhiteSpace(email1) ||
                !string.IsNullOrWhiteSpace(email2) ||
                !string.IsNullOrWhiteSpace(email3))
            {
                sb.AppendLine("\nEmail Addresses");
                if (!string.IsNullOrWhiteSpace(email1)) sb.AppendLine($"Email 1   : {email1}");
                if (!string.IsNullOrWhiteSpace(email2)) sb.AppendLine($"Email 2   : {email2}");
                if (!string.IsNullOrWhiteSpace(email3)) sb.AppendLine($"Email 3   : {email3}");
            }

            sb.AppendLine($"\nGenerated : {DateTime.Now:dd-MMM-yyyy HH:mm}");

            var pages = RenderTextToImages(sb.ToString());
            return CreatePdfFromImages(pages);
        }

        // ----------------------------------------------------------------------
        // ✅ PUBLIC: All Members PDF
        // ----------------------------------------------------------------------
        public byte[] GenerateAllMembersPdf(List<MemberPdfData> members)
        {
            var sb = new StringBuilder();

            sb.AppendLine("BT Address Book — All Members Report");
            sb.AppendLine($"Total Members: {members.Count}");
            sb.AppendLine($"Date: {DateTime.Now:dd-MMM-yyyy HH:mm}");
            sb.AppendLine("--------------------------------------------------");

            foreach (var m in members)
            {
                sb.AppendLine($"\nName     : {m.Name}");
                sb.AppendLine($"Company  : {m.Company}");
                sb.AppendLine($"Category : {m.Category}");
                sb.AppendLine($"City     : {m.City}");
                sb.AppendLine($"Mobile   : {m.Mobile1}");

                if (!string.IsNullOrWhiteSpace(m.Address))
                    sb.AppendLine($"Address  : {m.Address}");

                sb.AppendLine("--------------------------------------------------");
            }

            var pages = RenderTextToImages(sb.ToString());
            return CreatePdfFromImages(pages);
        }
    }

    // ----------------------------------------------------------------------
    // ✅ Data model
    // ----------------------------------------------------------------------
    public class MemberPdfData
    {
        public string Name { get; set; }
        public string Company { get; set; }
        public string Category { get; set; }
        public string City { get; set; }
        public string Mobile1 { get; set; }
        public string Mobile2 { get; set; }
        public string Mobile3 { get; set; }
        public string Telephone { get; set; }
        public string Email1 { get; set; }
        public string Email2 { get; set; }
        public string Email3 { get; set; }
        public string Address { get; set; }
    }
}
