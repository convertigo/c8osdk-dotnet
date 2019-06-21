using System.Threading.Tasks;

namespace Convertigo.SDK
{
    public interface C8oPromiseSync<T>
    {
        Task<T> Async();
        T Sync();
    }
}
