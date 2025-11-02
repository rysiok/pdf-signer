using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;

namespace PdfSigner.Tests.Utilities;

/// <summary>
/// Helper class for creating test PDF files
/// </summary>
public static class TestPdfGenerator
{
    /// <summary>
    /// Creates a simple test PDF with specified content
    /// </summary>
    /// <param name="outputPath">Path where to save the PDF</param>
    /// <param name="content">Text content to include</param>
    public static void CreateSimplePdf(string outputPath, string content = "This is a test PDF for signing.")
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        
        using var writer = new PdfWriter(outputPath);
        using var pdf = new PdfDocument(writer);
        using var document = new Document(pdf);
        
        document.Add(new Paragraph(content));
        document.Add(new Paragraph($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}"));
        document.Add(new Paragraph("This document is for testing purposes only."));
    }

    /// <summary>
    /// Creates a multi-page test PDF
    /// </summary>
    /// <param name="outputPath">Path where to save the PDF</param>
    /// <param name="pageCount">Number of pages to create</param>
    public static void CreateMultiPagePdf(string outputPath, int pageCount = 3)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        
        using var writer = new PdfWriter(outputPath);
        using var pdf = new PdfDocument(writer);
        using var document = new Document(pdf);
        
        for (int i = 1; i <= pageCount; i++)
        {
            if (i > 1)
            {
                document.Add(new AreaBreak());
            }
            
            document.Add(new Paragraph($"Page {i} of {pageCount}"));
            document.Add(new Paragraph($"This is the content of page {i}."));
            document.Add(new Paragraph("Lorem ipsum dolor sit amet, consectetur adipiscing elit. " +
                                     "Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua."));
        }
    }

    /// <summary>
    /// Creates an empty PDF (minimal content)
    /// </summary>
    /// <param name="outputPath">Path where to save the PDF</param>
    public static void CreateEmptyPdf(string outputPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        
        using var writer = new PdfWriter(outputPath);
        using var pdf = new PdfDocument(writer);
        using var document = new Document(pdf);
        
        // Just add a minimal paragraph to make it valid
        document.Add(new Paragraph(" "));
    }

    /// <summary>
    /// Creates multiple test PDFs in a directory
    /// </summary>
    /// <param name="directory">Directory to create PDFs in</param>
    /// <param name="count">Number of PDFs to create</param>
    /// <param name="prefix">Filename prefix</param>
    public static List<string> CreateMultiplePdfs(string directory, int count = 3, string prefix = "test")
    {
        Directory.CreateDirectory(directory);
        var filePaths = new List<string>();
        
        for (int i = 1; i <= count; i++)
        {
            var fileName = $"{prefix}{i:D2}.pdf";
            var filePath = Path.Combine(directory, fileName);
            CreateSimplePdf(filePath, $"Test document #{i} for batch processing.");
            filePaths.Add(filePath);
        }
        
        return filePaths;
    }

    /// <summary>
    /// Creates a corrupted/invalid PDF file for error testing
    /// </summary>
    /// <param name="outputPath">Path where to save the invalid PDF</param>
    public static void CreateInvalidPdf(string outputPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        
        // Create a file that looks like PDF but is actually corrupted
        File.WriteAllText(outputPath, "%PDF-1.4\nThis is not a valid PDF content\n%%EOF");
    }
}