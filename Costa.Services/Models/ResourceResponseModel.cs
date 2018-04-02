using System.Collections.Generic;

namespace Costa.Services.Models
{
    public class ResourceResponseModel
    {
        public IEnumerable<FileModel> Files { get; set; }
    }
}