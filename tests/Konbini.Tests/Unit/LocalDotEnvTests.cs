using Konbini.Api.Features.Common.Configuration;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Konbini.Tests.Unit;

public class LocalDotEnvTests
{
    [Fact]
    public void Parse_SkipsCommentsBlankAndInvalidLines()
    {
        var env = LocalDotEnv.Parse(["# 註解", "", "   ", "A=1", "沒有等號的行", "=沒有鍵"]);
        Assert.Equal(new Dictionary<string, string> { ["A"] = "1" }, env);
    }

    [Theory]
    [InlineData("A='p@ss word'", "p@ss word")]
    [InlineData("A=\"quoted\"", "quoted")]
    [InlineData("A=value\r", "value")]       // CRLF 留下的 \r
    [InlineData("A=abc==", "abc==")]         // base64 結尾的 = 要保留
    [InlineData("A=pa#ss", "pa#ss")]         // 前面沒有空白的 # 是值的一部分
    [InlineData("A=value # 註解", "value")]
    [InlineData("A='va # lue'", "va # lue")] // 引號內的 # 不是註解
    [InlineData("A=", "")]
    public void Parse_Values(string line, string expected)
        => Assert.Equal(expected, LocalDotEnv.Parse([line])["A"]);

    [Fact]
    public void ToConfiguration_BuildsLocalConnectionStringAndJwtSecret()
    {
        var config = LocalDotEnv.ToConfiguration(new Dictionary<string, string>
        {
            ["APP_DB_NAME"] = "shop", ["APP_DB_USER"] = "app", ["APP_DB_PASSWORD"] = "pw", ["JWT_SECRET"] = "secret",
        });

        Assert.Equal("Host=127.0.0.1;Port=5432;Database=shop;Username=app;Password=pw",
            config["ConnectionStrings:DefaultConnection"]);
        Assert.Equal("secret", config["Jwt:Secret"]);
    }

    [Fact]
    public void ToConfiguration_EmptyNameAndUser_UseComposeDefaults()
    {
        var config = LocalDotEnv.ToConfiguration(new Dictionary<string, string>
        {
            ["APP_DB_NAME"] = "", ["APP_DB_PASSWORD"] = "pw",
        });

        Assert.Equal("Host=127.0.0.1;Port=5432;Database=konbini;Username=konbini;Password=pw",
            config["ConnectionStrings:DefaultConnection"]);
    }

    [Fact]
    public void ToConfiguration_EmptySecrets_ProduceNoKeys()
    {
        var config = LocalDotEnv.ToConfiguration(new Dictionary<string, string>
        {
            ["APP_DB_PASSWORD"] = "", ["JWT_SECRET"] = " ",
        });

        Assert.Empty(config);
    }

    [Fact]
    public void InsertBeforeEnvironmentVariables_DotEnvOverridesAppSettings()
    {
        const string key = "LocalDotEnvTests:NoEnv";
        var config = new ConfigurationManager();
        config.AddInMemoryCollection(new Dictionary<string, string?> { [key] = "appsettings" });
        config.AddEnvironmentVariables();

        LocalDotEnv.InsertBeforeEnvironmentVariables(config, new Dictionary<string, string?> { [key] = "dotenv" });

        Assert.Equal("dotenv", config[key]);
    }

    [Fact]
    public void InsertBeforeEnvironmentVariables_EnvironmentVariableOverridesDotEnv()
    {
        // 名稱只給這個測試用，不影響其他測試
        const string key = "LocalDotEnvTests:WithEnv";
        Environment.SetEnvironmentVariable("LocalDotEnvTests__WithEnv", "env");
        try
        {
            var config = new ConfigurationManager();
            config.AddEnvironmentVariables();

            LocalDotEnv.InsertBeforeEnvironmentVariables(config, new Dictionary<string, string?> { [key] = "dotenv" });

            Assert.Equal("env", config[key]);
        }
        finally
        {
            Environment.SetEnvironmentVariable("LocalDotEnvTests__WithEnv", null);
        }
    }
}
