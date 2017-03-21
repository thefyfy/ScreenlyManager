using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace ScreenlyManager
{
    public class Device
    {
        [Newtonsoft.Json.JsonIgnore]
        public List<Asset> Assets;

        [Newtonsoft.Json.JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "ip_address")]
        public string IpAddress { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "port")]
        public string Port { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "location")]
        public string Location { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public string HttpLink
        {
            get
            {
                return "http://" + IpAddress + ":" + Port;
            }
        }

        public Device()
        {
            this.Assets = new List<Asset>();
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
            string parameters = "/api/assets";

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(this.HttpLink + parameters);
                request.Method = "GET";
                using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                {
                    Stream dataStream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(dataStream);
                    resultJson = reader.ReadToEnd();
                }

                if (!resultJson.Equals(string.Empty))
                    this.Assets = JsonConvert.DeserializeObject<List<Asset>>(resultJson);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting assets.", ex);
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
            string parameters = "/api/assets/" + assetId;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(this.HttpLink + parameters);
                request.Method = "DELETE";
                using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                {
                    Stream dataStream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(dataStream);
                    resultJson = reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error when asset deleting.", ex);
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
            var postData = "model=" + json;
            var data = System.Text.Encoding.UTF8.GetBytes(postData);

            string resultJson = string.Empty;
            string parameters = "/api/assets/" + a.AssetId;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(this.HttpLink + parameters);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                Stream dataStream = await request.GetRequestStreamAsync();
                dataStream.Write(data, 0, data.Length);

                using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    resultJson = reader.ReadToEnd();
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
                throw new Exception("Error while updating asset.", ex);
            }

            return returnedAsset;
        }

        //public Asset CreateAsset(Device d, Asset a)
        //{
        //    Asset returnedAsset = new Asset();
        //    JsonSerializerSettings settings = new JsonSerializerSettings();
        //    IsoDateTimeConverter dateConverter = new IsoDateTimeConverter
        //    {
        //        DateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fff'Z'"
        //    };
        //    settings.Converters.Add(dateConverter);

        //    string json = JsonConvert.SerializeObject(a, settings);
        //    var postData = "model=" + json;
        //    var data = Encoding.UTF8.GetBytes(postData);

        //    string resultJson = string.Empty;
        //    string parameters = "/api/assets";

        //    try
        //    {
        //        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(d.HttpLink + parameters);
        //        request.Method = "POST";
        //        request.ContentType = "application/x-www-form-urlencoded";
        //        request.ContentLength = data.Length;
        //        Stream dataStream = request.GetRequestStream();
        //        dataStream.Write(data, 0, data.Length);
        //        dataStream.Close();

        //        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
        //        {
        //            Stream dataResponseStream = response.GetResponseStream();
        //            StreamReader reader = new StreamReader(dataResponseStream);
        //            resultJson = reader.ReadToEnd();
        //            reader.Close();
        //            dataResponseStream.Close();
        //        }

        //        if (!resultJson.Equals(string.Empty))
        //            returnedAsset = JsonConvert.DeserializeObject<Asset>(resultJson, settings);
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception("Error while creating asset.", ex);
        //    }
        //    return returnedAsset;
        //}

        //public Asset GetAsset(Device d, string assetId)
        //{
        //    Asset returnedAsset = new Asset();
        //    JsonSerializerSettings settings = new JsonSerializerSettings();
        //    IsoDateTimeConverter dateConverter = new IsoDateTimeConverter
        //    {
        //        DateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fff'Z'"
        //    };
        //    settings.Converters.Add(dateConverter);

        //    string resultJson = string.Empty;
        //    string parameters = "/api/assets/" + assetId;

        //    try
        //    {
        //        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(d.HttpLink + parameters);
        //        request.Method = "GET";
        //        using (HttpWebResponse response = (HttpWebResponse)request.())
        //        {
        //            Stream dataStream = response.GetResponseStream();
        //            StreamReader reader = new StreamReader(dataStream);
        //            resultJson = reader.ReadToEnd();
        //            reader.Close();
        //            dataStream.Close();
        //        }

        //        if (!resultJson.Equals(string.Empty))
        //            returnedAsset = JsonConvert.DeserializeObject<Asset>(resultJson, settings);
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception(string.Format("Error while getting asset {0}", assetId), ex);
        //    }

        //    return returnedAsset;
        //}

        #endregion
    }
}

