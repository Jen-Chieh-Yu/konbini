using Konbini.Api.Features.Captcha.Commands;
using Konbini.Api.Features.Captcha.Endpoints;
using Konbini.Api.Features.Captcha.Errors;
using Konbini.Api.Features.Captcha.Models;
using Konbini.Api.Features.Captcha.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Konbini.Tests.Unit;

public class CaptchaStoreTests
{
    private readonly MutableTimeProvider _time = new(new DateTimeOffset(2026, 11, 18, 0, 0, 0, TimeSpan.Zero));

    [Fact]
    public void CorrectAnswer_PassesOnlyOnce()
    {
        using var store = new MemoryCaptchaStore(_time);
        var id = store.Save("AB2CD");

        Assert.True(store.TryConsume(id, "AB2CD"));
        Assert.False(store.TryConsume(id, "AB2CD"));
    }

    [Theory]
    [InlineData("ab2cd")]
    [InlineData(" AB2CD ")]
    public void Answer_IgnoresCaseAndSurroundingSpaces(string answer)
    {
        using var store = new MemoryCaptchaStore(_time);

        Assert.True(store.TryConsume(store.Save("AB2CD"), answer));
    }

    [Fact]
    public void WrongAnswer_InvalidatesTheCaptcha()
    {
        using var store = new MemoryCaptchaStore(_time);
        var id = store.Save("AB2CD");

        Assert.False(store.TryConsume(id, "XXXXX"));
        // 答錯也作廢：不能拿同一張一直猜
        Assert.False(store.TryConsume(id, "AB2CD"));
    }

    [Fact]
    public void Expired_Fails()
    {
        using var store = new MemoryCaptchaStore(_time);
        var id = store.Save("AB2CD");

        _time.Advance(CaptchaOptions.Lifetime);

        Assert.False(store.TryConsume(id, "AB2CD"));
    }

    [Fact]
    public void JustBeforeExpiry_Passes()
    {
        using var store = new MemoryCaptchaStore(_time);
        var id = store.Save("AB2CD");

        _time.Advance(CaptchaOptions.Lifetime - TimeSpan.FromSeconds(1));

        Assert.True(store.TryConsume(id, "AB2CD"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-id")]
    public void UnknownId_Fails(string? id)
    {
        using var store = new MemoryCaptchaStore(_time);
        store.Save("AB2CD");

        Assert.False(store.TryConsume(id, "AB2CD"));
    }
}

public class CaptchaCodeTests
{
    [Fact]
    public void Generate_UsesOnlyTheAlphabet()
    {
        for (var i = 0; i < 200; i++)
        {
            var code = CaptchaCode.Generate();
            Assert.Equal(CaptchaCode.Length, code.Length);
            Assert.All(code, c => Assert.Contains(c, CaptchaCode.Alphabet));
        }
    }

    [Fact]
    public void Alphabet_ExcludesConfusableCharacters()
        => Assert.All("01OIL", c => Assert.DoesNotContain(c, CaptchaCode.Alphabet));

    [Fact]
    public void EveryCharacterInAlphabet_HasGlyph()
        => Assert.Equal(CaptchaCode.Alphabet.Order(), CaptchaGlyphs.Defined.Order());
}

public class CaptchaOptionsTests
{
    [Fact]
    public void DevelopmentAnswer_ReadInDevelopment()
    {
        var options = CaptchaOptions.FromConfiguration(Config("konbi"), new Env(Environments.Development));

        Assert.Equal("KONBI", options.DevelopmentAnswer);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DevelopmentAnswer_Blank_IsNull(string? answer)
        => Assert.Null(CaptchaOptions.FromConfiguration(Config(answer), new Env(Environments.Development)).DevelopmentAnswer);

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void DevelopmentAnswer_OutsideDevelopment_FailsAtStartup(string environment)
        => Assert.Throws<InvalidOperationException>(() =>
            CaptchaOptions.FromConfiguration(Config("KONBI"), new Env(environment)));

    [Fact]
    public void NoDevelopmentAnswer_OutsideDevelopment_IsFine()
        => Assert.Null(CaptchaOptions.FromConfiguration(Config(null), new Env(Environments.Production)).DevelopmentAnswer);

    private static IConfiguration Config(string? answer) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["Captcha:DevelopmentAnswer"] = answer })
        .Build();

    private sealed class Env(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Konbini.Api";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

public class CaptchaEndpointFilterTests
{
    private static readonly object NextResult = new();

    [Fact]
    public async Task CorrectAnswer_CallsNext()
    {
        var store = new FakeStore(passes: true);

        var result = await Invoke(store, new CaptchaOptions(), id: "abc", answer: "AB2CD");

        Assert.Same(NextResult, result);
        Assert.Equal(("abc", "AB2CD"), store.Consumed);
    }

    [Fact]
    public async Task WrongAnswer_ReturnsInvalid_WithoutCallingNext()
    {
        var result = await Invoke(new FakeStore(passes: false), new CaptchaOptions(), id: "abc", answer: "XXXXX");

        AssertProblem(CaptchaErrors.Invalid, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public async Task MissingAnswer_ReturnsRequired(string? answer)
    {
        var store = new FakeStore(passes: true);

        var result = await Invoke(store, new CaptchaOptions(), id: "abc", answer: answer);

        AssertProblem(CaptchaErrors.Required, result);
        Assert.Null(store.Consumed);
    }

    [Fact]
    public async Task DevelopmentAnswer_PassesWithoutId()
    {
        var store = new FakeStore(passes: false);

        var result = await Invoke(store, new CaptchaOptions { DevelopmentAnswer = "KONBI" }, id: null, answer: "konbi");

        Assert.Same(NextResult, result);
        Assert.Null(store.Consumed);
    }

    [Fact]
    public async Task OtherAnswer_WithDevelopmentAnswerSet_StillChecksStore()
    {
        var result = await Invoke(new FakeStore(passes: false), new CaptchaOptions { DevelopmentAnswer = "KONBI" },
            id: "abc", answer: "XXXXX");

        AssertProblem(CaptchaErrors.Invalid, result);
    }

    private static async Task<object?> Invoke(ICaptchaStore store, CaptchaOptions options, string? id, string? answer)
    {
        var http = new DefaultHttpContext();
        if (id is not null) http.Request.Headers[CaptchaEndpointFilter.IdHeader] = id;
        if (answer is not null) http.Request.Headers[CaptchaEndpointFilter.AnswerHeader] = answer;

        return await new CaptchaEndpointFilter(store, options)
            .InvokeAsync(EndpointFilterInvocationContext.Create(http), _ => ValueTask.FromResult<object?>(NextResult));
    }

    private static void AssertProblem(Konbini.Api.Features.Common.Abstractions.Error expected, object? result)
    {
        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Equal(expected.Errors, problem.ProblemDetails.Extensions["errors"]);
    }

    private sealed class FakeStore(bool passes) : ICaptchaStore
    {
        public (string?, string)? Consumed { get; private set; }

        public string Save(string answer) => throw new NotSupportedException();

        public bool TryConsume(string? id, string answer)
        {
            Consumed = (id, answer);
            return passes;
        }
    }
}

public class CreateCaptchaHandlerTests
{
    [Fact]
    public async Task ReturnsPngDataUrl_AndStoresTheAnswer()
    {
        using var store = new MemoryCaptchaStore(TimeProvider.System);
        var renderer = new RecordingRenderer();

        var response = await new CreateCaptchaHandler(store, renderer).Handle(new(), CancellationToken.None);

        Assert.StartsWith("data:image/png;base64,", response.Image);
        Assert.Equal(120, response.ExpiresIn);
        Assert.True(store.TryConsume(response.Id, renderer.Code!));
    }

    /// <summary>實際呼叫 SkiaSharp：CI（Linux）上跑得過，代表原生檔有帶到。</summary>
    [Fact]
    public void SkiaRenderer_ProducesPng()
    {
        var png = new SkiaCaptchaImageRenderer().RenderPng(CaptchaCode.Alphabet[..CaptchaCode.Length]);

        Assert.Equal([0x89, 0x50, 0x4E, 0x47], png[..4]);
    }

    private sealed class RecordingRenderer : ICaptchaImageRenderer
    {
        public string? Code { get; private set; }

        public byte[] RenderPng(string code)
        {
            Code = code;
            return [0x89, 0x50, 0x4E, 0x47];
        }
    }
}

/// <summary>可以往前撥的時間。</summary>
internal sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
{
    private DateTimeOffset _now = now;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now += by;
}
