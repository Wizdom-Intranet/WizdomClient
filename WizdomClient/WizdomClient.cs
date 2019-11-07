using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace WizdomClientStd
{
    public class WizdomClient
    {
        public WizdomClient(ITokenHandler tokenHandler, int licenseId = 0)
        {
            _wizdomLicenseId = licenseId;
            _tokenHandler = tokenHandler;
        }
        public enum HttpMethod
        {
            GET, PUT, POST, DELETE//, PATCH
        }
        public enum LogLevel
        {
            info, warn, error
        }
        public delegate void Log(string message, LogLevel level);

        private const string clientId = "402cbaeb-c52a-43b6-b886-4ad1c44cab6a";
        private const string baseAddress = "https://api.wizdom-intranet.com";
        private int _wizdomLicenseId = 0;
        private ITokenHandler _tokenHandler;
        public Log log { get; set; }
        private List<WizdomTenant> _wizdomTenants = null;

        private async Task<WizdomTenant> GetTenantConfig(int licenseId = 0)
        {
            var tenants = await GetTenantConfigs();
            if (licenseId > 0) return tenants?.FirstOrDefault(t => t.LicenseID == licenseId);
            return tenants?.FirstOrDefault();
        }

        private async Task<List<WizdomTenant>> GetTenantConfigs()
        {
            if (_wizdomTenants != null) return _wizdomTenants;

            string token = await _tokenHandler?.GetTokenAsync(clientId);

            HttpClientHandler handler = new HttpClientHandler();
            handler.AllowAutoRedirect = true;
            handler.MaxAutomaticRedirections = 10;
            var client = new HttpClient(handler);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.GetAsync(baseAddress + "/disco");

            if (response.IsSuccessStatusCode) return await DeserializeStream<List<WizdomTenant>>(response);

            throw new ServerException("Error getting instances", await response.Content.ReadAsStringAsync(), response.StatusCode, response.Headers.ToString());
        }

        public async Task<Environment> ConnectAsync(int licenseId = 0, bool useProxy = false)
        {
            _wizdomLicenseId = licenseId;
            return await GetObjectFromAPIAsync<Environment>("/api/wizdom/noticeboard/environment", useProxy: useProxy);
        }

        public async Task<bool> DisconnectAsync()
        {
            //TODO: call tokenhandler and log out, wipe local stored wizdom instance - etc...
            _wizdomLicenseId = 0;
            return true;
        }

        public async Task<List<WizdomTenant>> GetInstancesAsync()
        {
            return await GetTenantConfigs();
        }

        public async Task<T> GetObjectFromAPIAsync<T>(string path, HttpMethod method = HttpMethod.GET, HttpContent content = null, bool useProxy = false)
        {
            var response = await InvokeAPIAsync(path, method, content, useProxy);
            return await DeserializeStream<T>(response);
        }

        private static async Task<T> DeserializeStream<T>(HttpResponseMessage response)
        {
            using (Stream stream = await response.Content.ReadAsStreamAsync())
            using (StreamReader streamreader = new StreamReader(stream))
            using (JsonReader reader = new JsonTextReader(streamreader))
            {
                JsonSerializer serializer = new JsonSerializer();
                return serializer.Deserialize<T>(reader);
            }
        }

        public async Task<string> GetJsonFromAPIAsync(string path, HttpMethod method = HttpMethod.GET, HttpContent content = null, bool useProxy = false)
        {
            var response = await InvokeAPIAsync(path, method, content, useProxy);
            if (response.IsSuccessStatusCode)
            {
                string s = await response.Content.ReadAsStringAsync();
                log?.Invoke(s, LogLevel.info);
                return s;
            }
            else
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    log?.Invoke("Error: Access denied!", LogLevel.error);
                    throw new AccessDeniedException("Error: Access denied!");
                }
                else
                {
                    log?.Invoke("Error loading data from " + path, LogLevel.error);
                    string serverResponse = await response.Content.ReadAsStringAsync();
                    log?.Invoke(serverResponse, LogLevel.error);
                    throw new ServerException("Error loading data from " + path, serverResponse, response.StatusCode, response.Headers.ToString());
                }
            }
        }

        public async Task<HttpResponseMessage> InvokeAPIAsync(string path, HttpMethod method = HttpMethod.GET, HttpContent content = null, bool useProxy = false)
        {
            WizdomTenant tenant = await GetTenantConfig(_wizdomLicenseId);
            if (tenant == null)
            {
                throw new AccessDeniedException("Access denied - no license found!");
            }

            string resourceid = null;
            if (string.IsNullOrEmpty(resourceid) && !useProxy)
            {
                try
                {
                    Uri uri = new Uri(tenant.SharepointSiteUrl);
                    resourceid = uri.Scheme + "://" + uri.Host + "/";
                }
                catch (Exception)
                {
                    //Ignore...
                }
            }

            string token = await _tokenHandler?.GetTokenAsync(clientId, resourceid);

            HttpClientHandler handler = new HttpClientHandler();
            handler.AllowAutoRedirect = true;
            handler.MaxAutomaticRedirections = 10;
            var client = new HttpClient(handler);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Add("x-wizdom-rest", "WizdomClient");

            var url = (useProxy ? baseAddress : tenant.WizdomHostUrl) + path;
            HttpResponseMessage response;
            switch (method)
            {
                case HttpMethod.PUT:
                    response = await client.PutAsync(url, content);
                    break;
                case HttpMethod.POST:
                    response = await client.PostAsync(url, content);
                    break;
                //case HttpMethod.PATCH:
                //    response = await Client.PatchAsync(url, content);
                //    break;
                case HttpMethod.DELETE:
                    response = await client.DeleteAsync(url);
                    break;
                case HttpMethod.GET:
                default:
                    response = await client.GetAsync(url);
                    break;
            }
            return response;
        }

    }
}
