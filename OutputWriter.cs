using System;
using System.IO;

namespace PdfSignerApp
{
    /// <summary>
    /// Handles output to console or file
    /// </summary>
    public class OutputWriter : IDisposable
    {
        private readonly StreamWriter? _fileWriter;
        private readonly bool _writeToFile;

        public OutputWriter(string? outputFilePath = null)
        {
            if (!string.IsNullOrEmpty(outputFilePath))
            {
                _writeToFile = true;
                _fileWriter = new StreamWriter(outputFilePath, false);
            }
        }

        public void WriteLine(string message = "")
        {
            if (_writeToFile && _fileWriter != null)
            {
                _fileWriter.WriteLine(message);
            }
            else
            {
                Console.WriteLine(message);
            }
        }

        public void Write(string message)
        {
            if (_writeToFile && _fileWriter != null)
            {
                _fileWriter.Write(message);
            }
            else
            {
                Console.Write(message);
            }
        }

        public void Flush()
        {
            _fileWriter?.Flush();
        }

        public void Dispose()
        {
            _fileWriter?.Dispose();
        }
    }
}
