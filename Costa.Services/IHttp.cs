using System.Threading.Tasks;

namespace Costa.Services
{
    public interface IHttp
    {
        string Get(string uri);
        //Task<string> GetAsync(string uri);
        string Post(string uri, string data, string contentType);
        //Task<string> PostAsync(string uri, string data, string contentType);
    }
}