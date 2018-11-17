using System.Threading.Tasks;

namespace ADL.DFS
{
    public interface IDfs
    {
        Task<Ipfs.Cid> AddTextAsync(string text);
        Task<Ipfs.Cid> AddFileAsync(string filename);
        Task<string> ReadAllTextAsync(string filename);
    }
}