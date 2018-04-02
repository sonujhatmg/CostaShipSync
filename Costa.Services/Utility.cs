using Costa.Services.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Costa.Services
{
    public static class Utility
    {
       
        private static readonly Http http = new Http();
        /// <summary>
        /// Get file names in json format
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static List<FileModel> GetFiles(string uri)
        {

            string json = http.Get(uri);

            return JsonConvert.DeserializeObject<ResourceResponseModel>(json)
                              .Files
                              .Select(x => new FileModel { CreatedOn = x.CreatedOn, ModifiedOn = x.ModifiedOn, Size = x.Size, Name = Path.GetFileName(x.Name) })
                              .ToList();


        }

    }
}
