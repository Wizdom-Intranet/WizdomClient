using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WizdomClientStd
{
    public class DelegateTokenHandler : ITokenHandler
    {
        public delegate Task<string> GetToken(string clientId, string resourceId = null);

        public DelegateTokenHandler(GetToken getToken)
        {
            _getToken = getToken;
        }
        private GetToken _getToken;
        public async Task<string> GetTokenAsync(string clientId, string resourceId = null)
        {
            return await _getToken?.Invoke(clientId, resourceId);
        }
    }
}