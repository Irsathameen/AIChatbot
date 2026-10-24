using System.Text;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using PdfText = iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor;

namespace AIChatbot.Utility
{
    public static class PdfTextExtractor
    {
        public static async Task<string> ExtractTextFromFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("PDF file was not found.", filePath);
            }

            return await Task.Run(() =>
            {
                using var reader = new PdfReader(filePath);
                using var pdfDocument = new PdfDocument(reader);

                var builder = new StringBuilder();
                for (var pageNum = 1; pageNum <= pdfDocument.GetNumberOfPages(); pageNum++)
                {
                    var page = pdfDocument.GetPage(pageNum);
                    var pageText = PdfText.GetTextFromPage(page, new SimpleTextExtractionStrategy());
                    if (!string.IsNullOrWhiteSpace(pageText))
                    {
                        builder.AppendLine(pageText);
                    }
                }

                return builder.ToString();
            });
        }
    }
}
