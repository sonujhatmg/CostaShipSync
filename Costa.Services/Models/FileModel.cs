using System;

namespace Costa.Services.Models
{
    public class FileModel
    {
        public string   Name { get; set; }
        public long     Size { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }
    }
}