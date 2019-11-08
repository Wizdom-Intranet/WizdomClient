using System.Threading.Tasks;

namespace Wizdom.Client
{
    public class DelegateTokenHandler : ITokenHandler
    {
        public delegate Task<string> GetTokenAsyncDelegate(string clientId, string resourceId = null);
        public delegate Task LogOutAsyncDelegate();

        public DelegateTokenHandler(GetTokenAsyncDelegate getTokenAsync, LogOutAsyncDelegate logOutAsync)
        {
            _getTokenAsync = getTokenAsync;
            _logOutAsync = logOutAsync;
        }
        private GetTokenAsyncDelegate _getTokenAsync;
        private LogOutAsyncDelegate _logOutAsync;
        public async Task<string> GetTokenAsync(string clientId, string resourceId = null)
        {
            return await _getTokenAsync?.Invoke(clientId, resourceId);
        }

        public async Task LogOutAsync()
        {
            await _logOutAsync?.Invoke();
            return;
        }
    }
}