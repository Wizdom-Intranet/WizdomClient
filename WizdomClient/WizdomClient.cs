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

        public delegate Task<int> InstanceDecisionHandlerDelegate(List<WizdomInstance> instances);
        public InstanceDecisionHandlerDelegate InstanceDecisionHandler { get; set; }

        public delegate void LoggerDelegate(string message, LogLevel level);
        public LoggerDelegate Logger { get; set; }

        private const string clientId = "402cbaeb-c52a-43b6-b886-4ad1c44cab6a";
        private const string baseAddress = "https://api.wizdom-intranet.com";

        private int _wizdomLicenseId = 0;
        private ITokenHandler _tokenHandler;
        private List<WizdomInstance> _wizdomInstances = null;
        #endregion

        #region Public
        public enum LogLevel
        {
            info, warn, error
        }

        #region Client methods
        public async Task<Environment> ConnectAsync(int licenseId = 0, bool useProxy = false)
        {
            Logger?.Invoke($"Connecting to {licenseId}", LogLevel.info);
            _wizdomLicenseId = licenseId;
            return await GetObjectAsync<Environment>("/api/wizdom/noticeboard/environment", useProxy: useProxy);
        }
        public async Task DisconnectAsync()
        {
            Logger?.Invoke("Disconnecting", LogLevel.info);
            _wizdomInstances = null;
            _wizdomLicenseId = 0;
            await _tokenHandler.LogOutAsync();
            return;
        }
        public async Task<List<WizdomInstance>> GetInstancesAsync()
        {
            if (_wizdomInstances != null) return _wizdomInstances;

            string token = await _tokenHandler?.GetTokenAsync(clientId);

            HttpClientHandler handler = new HttpClientHandler();
            handler.AllowAutoRedirect = true;
            handler.MaxAutomaticRedirections = 10;
            var client = new HttpClient(handler);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.GetAsync(baseAddress + "/disco");

            if (response.IsSuccessStatusCode) return await DeserializeStreamAsync<List<WizdomInstance>>(response);

            throw new ServerException("Error getting instances", await response.Content.ReadAsStringAsync(), response.StatusCode, response.Headers.ToString());
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
            WizdomInstance instance = await GetInstanceAsync(_wizdomLicenseId);
            if (instance == null)
            {
                throw new AccessDeniedException("Access denied - no license found!");
            }

            string resourceid = null;
            if (string.IsNullOrEmpty(resourceid) && !useProxy)
            {
                try
                {
                    Uri uri = new Uri(instance.SharepointSiteUrl);
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


            
            var url = (useProxy ? baseAddress + $"/proxy/licenseid/{_wizdomLicenseId}" : instance.WizdomHostUrl) + path;
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
                Logger?.Invoke(s, LogLevel.info);
                return s;
            }
            else
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Logger?.Invoke("Error: Access denied!", LogLevel.error);
                    throw new AccessDeniedException("Error: Access denied!");
                }
                else
                {
                    Logger?.Invoke("Error loading data from " + path, LogLevel.error);
                    string serverResponse = await response.Content.ReadAsStringAsync();
                    Logger?.Invoke(serverResponse, LogLevel.error);
                    throw new ServerException("Error loading data from " + path, serverResponse, response.StatusCode, response.Headers.ToString());
                }
            }
        }

        private async Task<T> APIObjectAsync<T>(string path, HttpMethod method = HttpMethod.GET, HttpContent content = null, bool useProxy = false)
        {
            var response = await APIResponseAsync(path, method, content, useProxy);
            return await DeserializeStreamAsync<T>(response);
        }

        private async Task<WizdomInstance> GetInstanceAsync(int licenseId = 0)
        {
            var instances = await GetInstancesAsync();

            if (licenseId == 0 && instances?.Count > 1 && InstanceDecisionHandler != null) licenseId = await InstanceDecisionHandler(instances);

            if (licenseId > 0) return instances?.FirstOrDefault(t => t.LicenseID == licenseId);
            return instances?.FirstOrDefault();
        }

        private static async Task<T> DeserializeStreamAsync<T>(HttpResponseMessage response)
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
