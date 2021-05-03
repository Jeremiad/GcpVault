using System.Threading.Tasks;
using GcpVault.Model;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace GcpVault
{
    public static class ConfigReader
    {
        public static async Task<Vaulthost> ReadConfig(string name, string mountPoint)
        {
            var configs = JsonSerializer.Deserialize<Config>(await File.ReadAllTextAsync("settings.json"));

            return configs.Vaulthosts.Where(c => c.Name == name).Where(m => m.MountPoint == mountPoint).FirstOrDefault();
        }
    }
}
