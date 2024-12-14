using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Data
{
    public class R2Settings
    {
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string ServiceUrl { get; set; } = string.Empty;
        public string BucketName { get; set; } = string.Empty;
    }
}
