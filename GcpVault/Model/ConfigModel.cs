using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace GcpVault.Model
{
    public class Config
    {
        [JsonPropertyName("vaulthosts")]
        public List<Vaulthost> Vaulthosts { get; set; }
    }

    public class Vaulthost
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("address")]
        public string Address { get; set; }
        [JsonPropertyName("mountpoint")]
        public string MountPoint { get; set; }

        [JsonPropertyName("user")]
        public User User { get; set; }

        [JsonPropertyName("unsealkeys")]
        public string[] UnsealKeys { get; set; }

        [JsonPropertyName("unsealtoken")]
        public string UnSealToken { get; set; }
    }

    public class User
    {
        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("password")]
        public string Password { get; set; }
    }
}
