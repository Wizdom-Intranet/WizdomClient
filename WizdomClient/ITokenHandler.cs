using System.Threading.Tasks;

namespace Wizdom.Client
{
    public interface ITokenHandler
    {
        Task<string> GetTokenAsync(string clientId, string resourceId = null);
        Task LogOutAsync();
    }
}
