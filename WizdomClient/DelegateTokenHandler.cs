using System.Threading.Tasks;

namespace Wizdom.Client
{
    public class DelegateTokenHandler : ITokenHandler
    {
        public delegate Task<string> GetToken(string clientId, string resourceId = null);
        public delegate Task LogOut();

        public DelegateTokenHandler(GetToken getToken)
        {
            _getToken = getToken;
        }
        private GetToken _getToken;
        private LogOut _logOut;
        public async Task<string> GetTokenAsync(string clientId, string resourceId = null)
        {
            return await _getToken?.Invoke(clientId, resourceId);
        }

        public async Task LogOutAsync()
        {
            await _logOut?.Invoke();
            return;
        }
    }
}