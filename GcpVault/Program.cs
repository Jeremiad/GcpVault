using System;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.AuthMethods.UserPass;
using Spectre.Console;
using System.IO;
using System.Threading.Tasks;
using VaultSharp.Core;
using GcpVault.Model;
using VaultSharp.V1.Commons;
using VaultSharp.V1.SecretsEngines.GoogleCloud;
using Polly;
using System.Text.Json;
using System.Linq;
using Serilog;

namespace GcpVault
{
    class Program
    {
        private static Vaulthost config = null;
        private static Secret<GoogleCloudServiceAccountKey> secret = null;

        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();


            if (args.Count() == 2)
            {
                config = await ConfigReader.ReadConfig(args[0], args[1]);
                if (config == null)
                {
                    Log.Fatal("Configuration not found");
                    Environment.Exit(1);
                }
            }
            else
            {
                Log.Fatal("Required arguments missing:\n" +
                    "vaulthostName mountpoint");
                Environment.Exit(1);
            }

            IAuthMethodInfo authMethod = new UserPassAuthMethodInfo(config.User.Username, config.User.Password);
            var vaultSettings = new VaultClientSettings(config.Address, authMethod);
            IVaultClient vaultClient = new VaultClient(vaultSettings);

            var retryPolicy = Policy.Handle<VaultApiException>().WaitAndRetryAsync(20, retryAttempt => TimeSpan.FromSeconds(10),
                onRetryAsync: (exception, timeSpan, context)  =>
                {
                    var exModel = JsonSerializer.Deserialize<ErrorModel>(exception.Message);

                    Log.Warning("Something went wrong during credetials fetch: {0}", exception.Message);

                    if (config.UnSealToken != null)
                    {
                        var authMethod = new TokenAuthMethodInfo(config.UnSealToken);
                        var settings = new VaultClientSettings(config.Address, authMethod);
                        vaultClient = new VaultClient(settings);
                    }

                    if (exModel.Errors.Where(e => e == "Vault is sealed").Any())
                    {
                        UnSealVault(vaultClient, config);
                    }
                    else
                    {
                        Log.Warning("Something went wrong during credetials fetch: {0}");
                    }
                    return Task.FromResult(0);
                });

            await retryPolicy.ExecuteAsync(async () => secret = await vaultClient.V1.Secrets.GoogleCloud.GetServiceAccountKeyAsync("deployer", mountPoint: config.MountPoint));

            if (secret.Data != null)
            {
                byte[] byteCredentials = Convert.FromBase64String(secret.Data.Base64EncodedPrivateKeyData);
                string credential = System.Text.Encoding.UTF8.GetString(byteCredentials);
                var credentialModel = JsonSerializer.Deserialize<GcpServiceAccountModel>(credential);

                var table = new Table();
                table.AddColumn("Type");
                table.AddColumn("Project Id").Centered();
                table.AddColumn("Private Key Id").Centered();
                table.AddColumn("Expires").Centered();
                table.AddColumn("Warnings").Centered();

                string warning = string.Empty;

                if (secret.Warnings != null)
                {
                    foreach (var w in secret.Warnings)
                    {
                        warning += w + ",";
                    }
                }
                table.AddRow(credentialModel.Type, credentialModel.ProjectId, credentialModel.PrivateKeyId, DateTime.Now.AddSeconds(secret.LeaseDurationSeconds).ToString(), warning);
                AnsiConsole.Render(table);
                await File.WriteAllTextAsync("service_account.json", credential);
            }

            Log.CloseAndFlush();
        }

        private static async void UnSealVault(IVaultClient vaultClient, Vaulthost config)
        {
            Log.Information("Trying to unlock vault");

            foreach (var key in config.UnsealKeys)
            {
                await vaultClient.V1.System.UnsealAsync(key);
            }
        }
    }
}
