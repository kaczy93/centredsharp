using System.Text;

namespace CentrED; 

public abstract class MulProvider{
    protected MulProvider(String filePath) {
        Stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        Reader = new BinaryReader(Stream, Encoding.UTF8);
    }
    
    protected FileStream Stream { get; }
    protected BinaryReader Reader { get; }
}