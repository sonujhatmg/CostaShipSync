using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Costa.Services.Models;

namespace Costa.Services
{
    public class FileServices
    {
        public FileServices(HttpContextBase httpContext)
        {
            this.HttpContext = httpContext;
        }

        public HttpContextBase HttpContext { private get; set; }

        public string ResourcesDirectory => Path.Combine(this.HttpContext.Server.MapPath("~/"), "Resources");

        private HttpRequestBase Request => this.HttpContext.Request;

        public IEnumerable<FileModel> GetFiles(string resourceArea)
        {

            string rootPath = this.HttpContext.Server.MapPath("~/");
            string absoluteUri = this.Request.Url?.AbsoluteUri;
            string resourcePath = Path.Combine(this.HttpContext.Server.MapPath("~/"), "Resources");

            string[] files = Directory.GetFiles(resourcePath, "*", SearchOption.AllDirectories);
            
            
            return files.Select(x => new FileInfo(x))
                        .Select(x => new FileModel
                        {
                            Name = absoluteUri + Regex.Replace(x.FullName.Replace(rootPath, ""), @"\\", "/"),
                            Size = x.Length,
                            CreatedOn = x.CreationTimeUtc,
                            ModifiedOn = x.LastWriteTimeUtc
                        });
        }

    }
}
