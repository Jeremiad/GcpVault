using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GcpVault.Model
{
    public class ErrorModel
    {
        [JsonPropertyName("errors")]
        public List<string> Errors { get; set; }
    }
}
