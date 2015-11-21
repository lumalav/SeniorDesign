using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Linq;

namespace _3DCytoFlow.Models
{
    public class CloudFilesModel
    {
        public CloudFilesModel(): this(null)
        {
            Files = new List<CloudFile>();
        }

        public CloudFilesModel(IEnumerable<IListBlobItem> list)
        {
            Files = new List<CloudFile>();
            try
            {
                if (list == null) return;

                foreach (var info in list.Select(CloudFile.CreateFromIListBlobItem).Where(info => info != null))
                {
                    Files.Add(info);
                }
            }
            catch (Exception)
            {
                // Ignore Errors when empty
            }
        }
        public List<CloudFile> Files { get; set; }
    }
}