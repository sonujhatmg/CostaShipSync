using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Costa.Services
{
    public class Http : IHttp
    {
        public string Get(string uri)
        {
            HttpWebRequest request = (HttpWebRequest) WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

            using(HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using(Stream stream = response.GetResponseStream() ?? new MemoryStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        //public async Task<string> GetAsync(string uri)
        //{
        //    HttpWebRequest request = (HttpWebRequest) WebRequest.Create(uri);
        //    request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

        //    using(HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
        //    using(Stream stream = response.GetResponseStream() ?? new MemoryStream())
        //    using (StreamReader reader = new StreamReader(stream))
        //    {
        //        return await reader.ReadToEndAsync();
        //    }
        //}


        public string Post(string uri, string data, string contentType)
        {

            if (string.IsNullOrWhiteSpace(contentType)) throw new ArgumentNullException(contentType);

            byte[] dataBytes = Encoding.UTF8.GetBytes(data);


            HttpWebRequest request = (HttpWebRequest) WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.ContentLength = dataBytes.Length;
            request.ContentType = contentType;

            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(dataBytes, 0 , dataBytes.Length);
            }

            using(HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using(Stream stream = response.GetResponseStream() ?? new MemoryStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }

        }


        //public async Task<string> PostAsync(string uri, string data, string contentType)
        //{
        //    if (string.IsNullOrWhiteSpace(contentType)) throw new ArgumentNullException(nameof(contentType));
        //    if (string.IsNullOrWhiteSpace(data)) throw new ArgumentNullException(nameof(data));

        //    byte[] dataBytes = Encoding.UTF8.GetBytes(data);

        //    HttpWebRequest request = (HttpWebRequest) WebRequest.Create(uri);
        //    request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
        //    request.ContentType = contentType;
        //    request.ContentLength = dataBytes.Length;

        //    using (Stream stream = await request.GetRequestStreamAsync())
        //    {
        //        await stream.WriteAsync(dataBytes, 0, dataBytes.Length);
        //    }


        //    using(HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
        //    using(Stream stream = response.GetResponseStream() ?? new MemoryStream())
        //    using(StreamReader reader = new StreamReader(stream))
        //    {
        //        return await reader.ReadToEndAsync();
        //    }

        //}

        public static void Download(string uri, string toPath)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using(HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using(Stream stream = response.GetResponseStream())
            using(FileStream fileStream = new FileStream(toPath, FileMode.Create, FileAccess.ReadWrite))
            {
                stream.CopyTo(fileStream);
            }
        }
    }
}