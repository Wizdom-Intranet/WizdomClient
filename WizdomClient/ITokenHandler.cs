using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WizdomClientStd
{
    public interface ITokenHandler
    {
        Task<string> GetTokenAsync(string clientId, string resourceId = null);
    }
}
