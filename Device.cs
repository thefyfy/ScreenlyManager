using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Security.Cryptography.Certificates;
using Windows.Storage.AccessCache;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using Windows.Web.Http.Headers;

namespace ScreenlyManager
{
    public class Device
    {
        [Newtonsoft.Json.JsonIgnore]
        private List<Asset> Assets;

        [Newtonsoft.Json.JsonIgnore]
        public bool IsUp { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public ObservableCollection<Asset> ActiveAssets
        {
            get
            {
                return new ObservableCollection<Asset>(this.Assets.FindAll(x => x.IsActive.Equals(1)));
            }
        }

        [Newtonsoft.Json.JsonIgnore]
        public ObservableCollection<Asset> InactiveAssets
        {
            get
            {
                return new ObservableCollection<Asset>(this.Assets.FindAll(x => x.IsActive.Equals(0)));
            }
        }

        [Newtonsoft.Json.JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "ip_address")]
        public string IpAddress { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "port")]
        public string Port { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "location")]
        public string Location { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "api_version")]
        public string ApiVersion { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public string HttpLink
        {
            get
            {
                return $"{IpAddress}:{Port}";
            }
        }

        [Newtonsoft.Json.JsonIgnore]
        public HttpBaseProtocolFilter HttpFilter { get; set; }

        public Device()
        {
            this.Assets = new List<Asset>();
            this.IsUp = false;

            this.HttpFilter = new HttpBaseProtocolFilter();
            this.HttpFilter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Expired);
            this.HttpFilter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
            this.HttpFilter.IgnorableServerCertificateErrors.Add(ChainValidationResult.InvalidName);
        }

        public async Task<bool> IsReachable()
        {
            try
            {
                HttpClient client = new HttpClient(this.HttpFilter);
                var cancellationTokenSource = new CancellationTokenSource(3000);

                HttpResponseMessage response = await client.GetAsync(new Uri(this.HttpLink)).AsTask(cancellationTokenSource.Token);
                if (response == null || !response.IsSuccessStatusCode)
                {
                    this.IsUp = false;
                    return false;
                }
                else
                {
                    this.IsUp = true;
                    return true;
                }
            }
            catch
            {
                this.IsUp = false;
                return false;
            }
        }


        #region Screenly's API methods

        /// <summary>
        /// Get assets trought Screenly API
        /// </summary>
        /// <returns></returns>
        public async Task GetAssetsAsync()
        {
            List<Asset> returnedAssets = new List<Asset>();
            string resultJson = string.Empty;
            string parameters = $"/api/{this.ApiVersion}assets";

            try
            {
                HttpClient request = new HttpClient();
                using (HttpResponseMessage response = await request.GetAsync(new Uri(this.HttpLink + parameters)))
                {
                    resultJson = await response.Content.ReadAsStringAsync();
                }

                if (!resultJson.Equals(string.Empty))
                    this.Assets = JsonConvert.DeserializeObject<List<Asset>>(resultJson);
            }
            catch (Exception ex)
            {
                throw new Exception($"[Device = {this.Name}; IP = {this.IpAddress}] Error while getting assets.", ex);
            }
        }

        /// <summary>
        /// Remove specific asset for selected device
        /// </summary>
        /// <param name="assetId">Asset ID</param>
        /// <returns>Boolean for result of execution</returns>
        public async Task<bool> RemoveAssetAsync(string assetId)
        {
            string resultJson = string.Empty;
            string parameters = $"/api/{this.ApiVersion}assets/{assetId}";

            try
            {
                HttpClient request = new HttpClient();
                using (HttpResponseMessage response = await request.DeleteAsync(new Uri(this.HttpLink + parameters)))
                {
                    resultJson = await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"[Device = {this.Name}; IP = {this.IpAddress}] Error when asset deleting.", ex);
            }

            return true;
        }

        /// <summary>
        /// Update specific asset
        /// </summary>
        /// <param name="a">Asset to update</param>
        /// <returns>Asset updated</returns>
        public async Task<Asset> UpdateAssetAsync(Asset a)
        {
            Asset returnedAsset = new Asset();
            JsonSerializerSettings settings = new JsonSerializerSettings();
            IsoDateTimeConverter dateConverter = new IsoDateTimeConverter
            {
                DateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fff'Z'"
            };
            settings.Converters.Add(dateConverter);

            string json = JsonConvert.SerializeObject(a, settings);
            var postData = $"model={json}";
            var data = System.Text.Encoding.UTF8.GetBytes(postData);

            string resultJson = string.Empty;
            string parameters = $"/api/{this.ApiVersion}assets/{a.AssetId}";

            try
            {
                HttpClient client = new HttpClient();
                HttpBufferContent content = new HttpBufferContent(data.AsBuffer());
                content.Headers.ContentType = new HttpMediaTypeHeaderValue("application/x-www-form-urlencoded");
                using (HttpResponseMessage response = await client.PutAsync(new Uri(this.HttpLink + parameters), content))
                {
                    resultJson = await response.Content.ReadAsStringAsync();
                }

                if (!resultJson.Equals(string.Empty))
                {
                    returnedAsset = JsonConvert.DeserializeObject<Asset>(resultJson, settings);
                }
            }
            catch (WebException ex)
            {
                using (var stream = ex.Response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    throw new Exception(reader.ReadToEnd(), ex);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"[Device = {this.Name}; IP = {this.IpAddress}] Error while updating asset.", ex);
            }

            return returnedAsset;
        }

        /// <summary>
        /// Update order of active assets throught API
        /// </summary>
        /// <param name="newOrder"></param>
        /// <returns></returns>
        public async Task UpdateOrderAssetsAsync(string newOrder)
        {
            var postData = $"ids={newOrder}";
            var data = System.Text.Encoding.UTF8.GetBytes(postData);

            string resultJson = string.Empty;
            string parameters = $"/api/{this.ApiVersion}assets/order";

            try
            {
                HttpClient client = new HttpClient();
                HttpBufferContent content = new HttpBufferContent(data.AsBuffer());
                content.Headers.ContentType = new HttpMediaTypeHeaderValue("application/x-www-form-urlencoded");
                using (HttpResponseMessage response = await client.PostAsync(new Uri(this.HttpLink + parameters), content))
                {
                    resultJson = await response.Content.ReadAsStringAsync();
                }
            }
            catch (WebException ex)
            {
                using (var stream = ex.Response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    throw new Exception(reader.ReadToEnd(), ex);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"[Device = {this.Name}; IP = {this.IpAddress}] Error while updating assets order.", ex);
            }
        }

        /// <summary>
        /// Create new asset on Raspberry using API
        /// </summary>
        /// <param name="a">New asset to create on Raspberry</param>
        /// <returns></returns>
        public async Task CreateAssetAsync(Asset a)
        {
            var originalName = a.Name;

            if (a.LocalToken != null)
            {
                var localFile = await StorageApplicationPermissions.FutureAccessList.GetFileAsync(a.LocalToken);
                Stream stream = await localFile.OpenStreamForReadAsync();
                byte[] result = new byte[(int)stream.Length];
                await stream.ReadAsync(result, 0, (int)stream.Length);
                a.Uri = await this.PostFileAssetAsync(result, localFile.Name, localFile.ContentType);
                a.Name += $" - {localFile.Name}";
            }

            Asset returnedAsset = new Asset();
            JsonSerializerSettings settings = new JsonSerializerSettings();
            IsoDateTimeConverter dateConverter = new IsoDateTimeConverter
            {
                DateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fff'Z'"
            };
            settings.Converters.Add(dateConverter);

            string json = JsonConvert.SerializeObject(a, settings);
            var postData = $"model={json}";
            var data = System.Text.Encoding.UTF8.GetBytes(postData);

            string resultJson = string.Empty;
            string parameters = $"/api/{this.ApiVersion}assets";

            a.Name = originalName;

            try
            {
                HttpClient client = new HttpClient();
                HttpBufferContent content = new HttpBufferContent(data.AsBuffer());
                content.Headers.ContentType = new HttpMediaTypeHeaderValue("application/x-www-form-urlencoded");
                using (HttpResponseMessage response = await client.PostAsync(new Uri(this.HttpLink + parameters), content))
                {
                    resultJson = await response.Content.ReadAsStringAsync();
                }

                if (!resultJson.Equals(string.Empty))
                    returnedAsset = JsonConvert.DeserializeObject<Asset>(resultJson, settings);
            }
            catch (WebException ex)
            {
                using (var stream = ex.Response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    throw new Exception(reader.ReadToEnd(), ex);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"[Device = {this.Name}; IP = {this.IpAddress}] Error while creating asset.", ex);
            }
        }

        /// <summary>
        /// Return asset identified by asset ID in param API
        /// </summary>
        /// <param name="assetId">Asset ID to find on device</param>
        /// <returns></returns>
        public async Task<Asset> GetAssetAsync(string assetId)
        {
            Asset returnedAsset = new Asset();
            JsonSerializerSettings settings = new JsonSerializerSettings();
            IsoDateTimeConverter dateConverter = new IsoDateTimeConverter
            {
                DateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fff'Z'"
            };
            settings.Converters.Add(dateConverter);

            string resultJson = string.Empty;
            string parameters = $"/api/{this.ApiVersion}assets/{assetId}";

            try
            {
                HttpClient request = new HttpClient();
                using (HttpResponseMessage response = await request.GetAsync(new Uri(this.HttpLink + parameters)))
                {
                    resultJson = await response.Content.ReadAsStringAsync();
                }

                if (!resultJson.Equals(string.Empty))
                    return JsonConvert.DeserializeObject<Asset>(resultJson);
            }
            catch (Exception ex)
            {
                throw new Exception($"[Device = {this.Name}; IP = {this.IpAddress}] Error while getting assets.", ex);
            }
            return null;
        }

        /// <summary>
        /// Send file to create asset (image or video)
        /// </summary>
        /// <param name="itemToSend">Byte array of the file</param>
        /// <param name="fileName">File's name</param>
        /// <param name="contentType">File's content type </param>
        /// <returns></returns>
        public async Task<string> PostFileAssetAsync(byte[] itemToSend, string fileName, string contentType)
        {
            string resultJson = string.Empty;
            string parameters = $"/api/{this.ApiVersion}file_asset";

            try
            {
                HttpClient request = new HttpClient();
                var content = new HttpMultipartFormDataContent();
                HttpBufferContent itemContent = new HttpBufferContent(itemToSend.AsBuffer());
                itemContent.Headers.ContentType = HttpMediaTypeHeaderValue.Parse(contentType);
                content.Add(itemContent, "file_upload", fileName);
                using (HttpResponseMessage response = await request.PostAsync(new Uri(this.HttpLink + parameters), content))
                {
                    resultJson = await response.Content.ReadAsStringAsync();
                }

                if (!resultJson.Equals(string.Empty))
                    return JsonConvert.DeserializeObject<string>(resultJson);
            }
            catch (Exception ex)
            {
                throw new Exception($"[Device = {this.Name}; IP = {this.IpAddress}] Error while sending file.", ex);
            }
            return null;
        }

        #endregion
    }
}

