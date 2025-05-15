using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenVectorFormat.ReaderWriter.UnitTests
{
    public class TempDir : IDisposable
    {
        private string path;
        private bool disposedValue;

        public TempDir()
        {
            path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(path);
        }

        public override string ToString() => path;

        public static implicit operator string(TempDir tDir) => tDir.path;
        public static implicit operator DirectoryInfo(TempDir tDir) => new DirectoryInfo(tDir.path);

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                try
                {
                    Directory.Delete(path, true);
                }
                catch
                {
                    //wait shortly to give concurrent threads etc. the chance to release the files
                    Thread.Sleep(100);
                    try
                    {
                        Directory.Delete(path, true);
                    }
                    catch { }
                }
                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        ~TempDir()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
