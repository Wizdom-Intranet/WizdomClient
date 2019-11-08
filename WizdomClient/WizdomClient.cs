using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Wizdom.Client
{
    public class WizdomClient
    {
        #region Init
        public WizdomClient(ITokenHandler tokenHandler, int licenseId = 0)
        {
            _wizdomLicenseId = licenseId;
            _tokenHandler = tokenHandler;
        }

        public delegate void Log(string message, LogLevel level);
        public Log log { get; set; }

        private const string clientId = "402cbaeb-c52a-43b6-b886-4ad1c44cab6a";
        private const string baseAddress = "https://api.wizdom-intranet.com";

        private int _wizdomLicenseId = 0;
        private ITokenHandler _tokenHandler;
        private List<WizdomTenant> _wizdomTenants = null;
        #endregion

        #region Public
        public enum LogLevel
        {
            info, warn, error
        }

        #region Client methods
        public async Task<Environment> ConnectAsync(int licenseId = 0, bool useProxy = false)
        {
            log?.Invoke($"Connecting to {licenseId}", LogLevel.info);
            _wizdomLicenseId = licenseId;
            return await GetObjectAsync<Environment>("/api/wizdom/noticeboard/environment", useProxy: useProxy);
        }
        public async Task DisconnectAsync()
        {
            log?.Invoke("Disconnecting", LogLevel.info);
            await _tokenHandler.LogOutAsync();
            _wizdomLicenseId = 0;
            return;
        }
        public async Task<List<WizdomTenant>> GetInstancesAsync()
        {
            return await GetTenantConfigs();
        }
        #endregion

        #region String methods
        public async Task<string> GetAsync(string path, bool useProxy = false)
        {
            return await APIStringAsync(path, HttpMethod.GET, useProxy: useProxy);
        }
        public async Task<string> PutAsync(string path, HttpContent content = null, bool useProxy = false)
        {
            return await APIStringAsync(path, HttpMethod.PUT, content: content, useProxy: useProxy);
        }
        public async Task<string> PostAsync(string path, HttpContent content = null, bool useProxy = false)
        {
            return await APIStringAsync(path, HttpMethod.POST, content: content, useProxy: useProxy);
        }
        public async Task<string> DeleteAsync(string path, bool useProxy = false)
        {
            return await APIStringAsync(path, HttpMethod.DELETE, useProxy: useProxy);
        }
        #endregion

        #region <T> methods
        public async Task<T> GetObjectAsync<T>(string path, bool useProxy = false)
        {
            return await APIObjectAsync<T>(path, HttpMethod.GET, useProxy: useProxy);
        }
        public async Task<T> PutObjectAsync<T>(string path, HttpContent content = null, bool useProxy = false)
        {
            return await APIObjectAsync<T>(path, HttpMethod.PUT, content: content, useProxy: useProxy);
        }
        public async Task<T> PostObjectAsync<T>(string path, HttpContent content = null, bool useProxy = false)
        {
            return await APIObjectAsync<T>(path, HttpMethod.POST, content: content, useProxy: useProxy);
        }
        public async Task<T> DeleteObjectAsync<T>(string path, bool useProxy = false)
        {
            return await APIObjectAsync<T>(path, HttpMethod.DELETE, useProxy: useProxy);
        }
        #endregion

        #region Response methods
        public async Task<HttpResponseMessage> GetResponseAsync(string path, bool useProxy = false)
        {
            return await APIResponseAsync(path, HttpMethod.GET, useProxy: useProxy);
        }
        public async Task<HttpResponseMessage> PutResponseAsync(string path, HttpContent content = null, bool useProxy = false)
        {
            return await APIResponseAsync(path, HttpMethod.PUT, content: content, useProxy: useProxy);
        }
        public async Task<HttpResponseMessage> PostResponseAsync(string path, HttpContent content = null, bool useProxy = false)
        {
            return await APIResponseAsync(path, HttpMethod.POST, content: content, useProxy: useProxy);
        }
        public async Task<HttpResponseMessage> DeleteResponseAsync(string path, bool useProxy = false)
        {
            return await APIResponseAsync(path, HttpMethod.DELETE, useProxy: useProxy);
        }
        #endregion

        #endregion

        #region Private
        private enum HttpMethod
        {
            GET, PUT, POST, DELETE//, PATCH
        }

        private async Task<HttpResponseMessage> APIResponseAsync(string path, HttpMethod method = HttpMethod.GET, HttpContent content = null, bool useProxy = false)
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


            
            var url = (useProxy ? baseAddress + $"/proxy/licenseid/{_wizdomLicenseId}" : tenant.WizdomHostUrl) + path;
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

        private async Task<string> APIStringAsync(string path, HttpMethod method = HttpMethod.GET, HttpContent content = null, bool useProxy = false)
        {
            var response = await APIResponseAsync(path, method, content, useProxy);
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

        private async Task<T> APIObjectAsync<T>(string path, HttpMethod method = HttpMethod.GET, HttpContent content = null, bool useProxy = false)
        {
            var response = await APIResponseAsync(path, method, content, useProxy);
            return await DeserializeStream<T>(response);
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

        private async Task<WizdomTenant> GetTenantConfig(int licenseId = 0)
        {
            var tenants = await GetTenantConfigs();
            if (licenseId > 0) return tenants?.FirstOrDefault(t => t.LicenseID == licenseId);
            return tenants?.FirstOrDefault();
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

        #endregion
    }
}
