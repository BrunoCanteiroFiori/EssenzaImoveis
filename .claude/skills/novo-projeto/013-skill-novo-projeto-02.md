@.claude/skills/config-project-fullstack/SKILL.md

Eu quero que você extenda a capacidade dessa skill para criar a camada de testes do backend e a validação automática do projeto (arquitetura e nomenclatura). Ela deve continuar fazendo exatamente tudo que ela já faz (Swagger e camadas Domain, Application, Repository e Infrastructure); apenas adicionar os passos abaixo no final da configuração do backend, antes da verificação final.

Estrutura adicionada:
```
backend/
  .editorconfig                    (nomenclatura validada no build)
  tests/
    Backend.UnitTests/             (testes de unidade de Domain, Application e Infrastructure, xUnit + NSubstitute)
    Backend.ArchitectureTests/     (regras de DDD, dependência entre camadas e nomenclatura, xUnit + NetArchTest)
```

Passos (dentro de backend/):
- dotnet new xunit -n Backend.UnitTests -o tests/Backend.UnitTests --framework net10.0
- dotnet new xunit -n Backend.ArchitectureTests -o tests/Backend.ArchitectureTests --framework net10.0
- Remover o arquivo UnitTest1.cs dos dois projetos
- dotnet sln Backend.slnx add tests/Backend.UnitTests/Backend.UnitTests.csproj tests/Backend.ArchitectureTests/Backend.ArchitectureTests.csproj
- dotnet add tests/Backend.UnitTests reference src/Backend.Domain src/Backend.Application src/Backend.Infrastructure
- dotnet add tests/Backend.ArchitectureTests reference src/Backend.Api src/Backend.Application src/Backend.Domain src/Backend.Repository src/Backend.Infrastructure
- dotnet add tests/Backend.UnitTests package NSubstitute
- dotnet add tests/Backend.ArchitectureTests package NetArchTest.Rules

Convenções dos testes de unidade (as skills de entidade e de módulo seguem isso):
- Pasta e namespace espelham o que é testado: tests/Backend.UnitTests/<Modulo>/ → namespace Backend.UnitTests.<Modulo>; código da Infrastructure em tests/Backend.UnitTests/Infrastructure/
- Classe: <ClasseTestada>Tests, sealed
- Método: Metodo_Cenario_ResultadoEsperado (ex.: CreateAsync_WithValidRequest_ShouldPersistReport)
- Corpo no padrão Arrange / Act / Assert, separados por linha em branco
- Dependências (repositories, unit of work, HttpClient) substituídas com NSubstitute ou stub; nunca acessar banco nem rede nos testes de unidade
- A camada Repository não tem teste de unidade (é só acesso a dados); ela é coberta pelos testes de arquitetura e pela migration

## Testes de arquitetura

- Criar o arquivo (backend/tests/Backend.ArchitectureTests/Layers.cs):
```csharp
using System.Reflection;

namespace Backend.ArchitectureTests;

internal static class Layers
{
    public const string DomainNamespace = "Backend.Domain";
    public const string ApplicationNamespace = "Backend.Application";
    public const string RepositoryNamespace = "Backend.Repository";
    public const string InfrastructureNamespace = "Backend.Infrastructure";
    public const string ApiNamespace = "Backend.Api";

    public static readonly Assembly Domain = typeof(global::Backend.Domain.Abstractions.IUnitOfWork).Assembly;
    public static readonly Assembly Application = typeof(global::Backend.Application.DependencyInjection).Assembly;
    public static readonly Assembly Repository = typeof(global::Backend.Repository.DependencyInjection).Assembly;
    public static readonly Assembly Infrastructure = typeof(global::Backend.Infrastructure.DependencyInjection).Assembly;
    public static readonly Assembly Api = typeof(Program).Assembly;
}
```

- Criar o arquivo (backend/tests/Backend.ArchitectureTests/ArchitectureAssert.cs):
```csharp
using NetArchTest.Rules;

namespace Backend.ArchitectureTests;

internal static class ArchitectureAssert
{
    public static void Success(TestResult result)
    {
        var failingTypes = result.FailingTypeNames ?? [];
        Assert.True(result.IsSuccessful, $"Tipos que violam a regra: {string.Join(", ", failingTypes)}");
    }
}
```

- Criar o arquivo (backend/tests/Backend.ArchitectureTests/LayerDependencyTests.cs):
```csharp
using NetArchTest.Rules;

namespace Backend.ArchitectureTests;

public sealed class LayerDependencyTests
{
    private const string EntityFramework = "Microsoft.EntityFrameworkCore";
    private const string AspNetCore = "Microsoft.AspNetCore";
    private const string Npgsql = "Npgsql";

    [Fact]
    public void Domain_ShouldNotDependOnOtherLayers()
    {
        var result = Types.InAssembly(Layers.Domain)
            .ShouldNot()
            .HaveDependencyOnAny(Layers.ApplicationNamespace, Layers.RepositoryNamespace, Layers.InfrastructureNamespace, Layers.ApiNamespace)
            .GetResult();

        ArchitectureAssert.Success(result);
    }

    [Fact]
    public void Domain_ShouldNotDependOnFrameworks()
    {
        var result = Types.InAssembly(Layers.Domain)
            .ShouldNot()
            .HaveDependencyOnAny(EntityFramework, AspNetCore, Npgsql)
            .GetResult();

        ArchitectureAssert.Success(result);
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOnProjectLayers()
    {
        var result = Types.InAssembly(Layers.Infrastructure)
            .ShouldNot()
            .HaveDependencyOnAny(Layers.DomainNamespace, Layers.ApplicationNamespace, Layers.RepositoryNamespace, Layers.ApiNamespace)
            .GetResult();

        ArchitectureAssert.Success(result);
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOnEntityFrameworkOrAspNetCore()
    {
        var result = Types.InAssembly(Layers.Infrastructure)
            .ShouldNot()
            .HaveDependencyOnAny(EntityFramework, AspNetCore)
            .GetResult();

        ArchitectureAssert.Success(result);
    }

    [Fact]
    public void Application_ShouldNotDependOnRepositoryOrApi()
    {
        var result = Types.InAssembly(Layers.Application)
            .ShouldNot()
            .HaveDependencyOnAny(Layers.RepositoryNamespace, Layers.ApiNamespace)
            .GetResult();

        ArchitectureAssert.Success(result);
    }

    [Fact]
    public void Application_ShouldNotDependOnFrameworks()
    {
        var result = Types.InAssembly(Layers.Application)
            .ShouldNot()
            .HaveDependencyOnAny(EntityFramework, AspNetCore, Npgsql)
            .GetResult();

        ArchitectureAssert.Success(result);
    }

    [Fact]
    public void Repository_ShouldNotDependOnApplicationOrApi()
    {
        var result = Types.InAssembly(Layers.Repository)
            .ShouldNot()
            .HaveDependencyOnAny(Layers.ApplicationNamespace, Layers.ApiNamespace)
            .GetResult();

        ArchitectureAssert.Success(result);
    }

    [Fact]
    public void Controllers_ShouldNotDependOnDomainOrRepository()
    {
        var result = Types.InAssembly(Layers.Api)
            .That()
            .ResideInNamespace($"{Layers.ApiNamespace}.Controllers")
            .ShouldNot()
            .HaveDependencyOnAny(Layers.DomainNamespace, Layers.RepositoryNamespace, EntityFramework)
            .GetResult();

        ArchitectureAssert.Success(result);
    }
}
```

- Criar o arquivo (backend/tests/Backend.ArchitectureTests/NamingConventionTests.cs):
```csharp
using Microsoft.AspNetCore.Mvc;
using NetArchTest.Rules;

namespace Backend.ArchitectureTests;

public sealed class NamingConventionTests
{
    [Fact]
    public void Interfaces_ShouldStartWithI()
    {
        var result = Types.InAssemblies([Layers.Domain, Layers.Application, Layers.Repository, Layers.Infrastructure, Layers.Api])
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();

        ArchitectureAssert.Success(result);
    }

    [Fact]
    public void Controllers_ShouldEndWithControllerAndResideInControllersNamespace()
    {
        var result = Types.InAssembly(Layers.Api)
            .That()
            .Inherit(typeof(ControllerBase))
            .And()
            .AreNotAbstract()
            .Should()
            .HaveNameEndingWith("Controller")
            .And()
            .ResideInNamespace($"{Layers.ApiNamespace}.Controllers")
            .GetResult();

        ArchitectureAssert.Success(result);
    }

    [Fact]
    public void Services_ShouldBeSealed()
    {
        var result = Types.InAssembly(Layers.Application)
            .That()
            .AreClasses()
            .And()
            .HaveNameEndingWith("Service")
            .Should()
            .BeSealed()
            .GetResult();

        ArchitectureAssert.Success(result);
    }

    [Fact]
    public void Dtos_ShouldBeSealedAndResideInDtosNamespace()
    {
        var result = Types.InAssembly(Layers.Application)
            .That()
            .HaveNameEndingWith("Request")
            .Or()
            .HaveNameEndingWith("Response")
            .Should()
            .BeSealed()
            .And()
            .ResideInNamespaceMatching(@"^Backend\.Application\.\w+\.Dtos$")
            .GetResult();

        ArchitectureAssert.Success(result);
    }

    [Fact]
    public void RepositoryInterfaces_ShouldResideInDomain()
    {
        var result = Types.InAssemblies([Layers.Application, Layers.Repository, Layers.Infrastructure, Layers.Api])
            .That()
            .AreInterfaces()
            .ShouldNot()
            .HaveNameEndingWith("Repository")
            .GetResult();

        ArchitectureAssert.Success(result);
    }

    [Fact]
    public void Repositories_ShouldBeSealedAndResideInModuleNamespace()
    {
        var result = Types.InAssembly(Layers.Repository)
            .That()
            .AreClasses()
            .And()
            .HaveNameEndingWith("Repository")
            .Should()
            .BeSealed()
            .And()
            .ResideInNamespaceMatching(@"^Backend\.Repository\.\w+$")
            .GetResult();

        ArchitectureAssert.Success(result);
    }

    [Fact]
    public void EntityConfigurations_ShouldBeSealedAndResideInModuleNamespace()
    {
        var result = Types.InAssembly(Layers.Repository)
            .That()
            .AreClasses()
            .And()
            .HaveNameEndingWith("Configuration")
            .Should()
            .BeSealed()
            .And()
            .ResideInNamespaceMatching(@"^Backend\.Repository\.\w+$")
            .GetResult();

        ArchitectureAssert.Success(result);
    }
}
```

## Testes de unidade da Infrastructure

- Criar o arquivo (backend/tests/Backend.UnitTests/Infrastructure/StringConversionsTests.cs):
```csharp
using Backend.Infrastructure.Conversions;

namespace Backend.UnitTests.Infrastructure;

public sealed class StringConversionsTests
{
    [Theory]
    [InlineData("UserProfile", "user_profile")]
    [InlineData("userProfile", "user_profile")]
    [InlineData("user-profiles", "user_profiles")]
    [InlineData("HTTPRequest", "http_request")]
    [InlineData("Report", "report")]
    public void ToSnakeCase_WithValue_ShouldReturnSnakeCase(string value, string expected)
    {
        var result = value.ToSnakeCase();

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("UserProfiles", "user-profiles")]
    [InlineData("user_profiles", "user-profiles")]
    [InlineData("Reports", "reports")]
    public void ToKebabCase_WithValue_ShouldReturnKebabCase(string value, string expected)
    {
        var result = value.ToKebabCase();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void RemoveDiacritics_WithAccents_ShouldReturnTextWithoutAccents()
    {
        var result = "Relatório de transações".RemoveDiacritics();

        Assert.Equal("Relatorio de transacoes", result);
    }
}
```

- Criar o arquivo (backend/tests/Backend.UnitTests/Infrastructure/EnumConversionsTests.cs):
```csharp
using Backend.Infrastructure.Conversions;
using Backend.Infrastructure.Enums;

namespace Backend.UnitTests.Infrastructure;

public sealed class EnumConversionsTests
{
    [Fact]
    public void GetDescription_WithDescriptionAttribute_ShouldReturnDescription()
    {
        var result = SortDirection.Descending.GetDescription();

        Assert.Equal("Decrescente", result);
    }

    [Theory]
    [InlineData("ascending", SortDirection.Ascending)]
    [InlineData("Descending", SortDirection.Descending)]
    public void ToEnum_WithValidName_ShouldReturnEnumValue(string value, SortDirection expected)
    {
        var result = value.ToEnum<SortDirection>();

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("invalido")]
    [InlineData("99")]
    public void ToEnum_WithInvalidValue_ShouldThrowArgumentException(string value)
    {
        Assert.Throws<ArgumentException>(() => value.ToEnum<SortDirection>());
    }
}
```

- Criar o arquivo (backend/tests/Backend.UnitTests/Infrastructure/JsonConversionsTests.cs):
```csharp
using Backend.Infrastructure.Conversions;
using Backend.Infrastructure.Enums;

namespace Backend.UnitTests.Infrastructure;

public sealed class JsonConversionsTests
{
    [Fact]
    public void ToJson_WithObject_ShouldUseCamelCaseAndEnumAsText()
    {
        var value = new SampleValue("Valor de teste", SortDirection.Ascending);

        var json = value.ToJson();

        Assert.Equal("""{"name":"Valor de teste","direction":"Ascending"}""", json);
    }

    [Fact]
    public void FromJson_WithJson_ShouldDeserializeIgnoringCase()
    {
        var value = """{"NAME":"Valor de teste","direction":"Descending"}""".FromJson<SampleValue>();

        Assert.Equal(new SampleValue("Valor de teste", SortDirection.Descending), value);
    }

    private sealed record SampleValue(string Name, SortDirection Direction);
}
```

- Criar o arquivo (backend/tests/Backend.UnitTests/Infrastructure/HttpRequestClientTests.cs):
```csharp
using System.Net;
using System.Text;
using Backend.Infrastructure.Http;

namespace Backend.UnitTests.Infrastructure;

public sealed class HttpRequestClientTests
{
    [Fact]
    public async Task GetAsync_WhenResponseIsSuccess_ShouldDeserializeBody()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(HttpStatusCode.OK, """{"name":"Valor de teste"}"""));
        var client = new HttpRequestClient(httpClient);

        var response = await client.GetAsync<SampleResponse>("https://example.com/items");

        Assert.Equal("Valor de teste", response?.Name);
    }

    [Fact]
    public async Task GetAsync_WhenResponseIsError_ShouldThrowHttpRequestException()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(HttpStatusCode.InternalServerError, "{}"));
        var client = new HttpRequestClient(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync<SampleResponse>("https://example.com/items"));
    }

    private sealed record SampleResponse(string Name);

    private sealed class StubHttpMessageHandler(HttpStatusCode statusCode, string content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json"),
            });
    }
}
```

## Nomenclatura no build

- Criar o arquivo (backend/.editorconfig). Com o `EnforceCodeStyleInBuild` do Directory.Build.props, qualquer violação de nomenclatura (IDE1006) ou namespace em bloco (IDE0161) quebra o `dotnet build`:
```ini
root = true

[*]
charset = utf-8
indent_style = space
indent_size = 4
insert_final_newline = true
trim_trailing_whitespace = true

[*.{json,yml,yaml,xml,csproj,props,slnx}]
indent_size = 2

[*.cs]
# Namespace com escopo de arquivo
csharp_style_namespace_declarations = file_scoped:warning
dotnet_diagnostic.IDE0161.severity = warning

# Nomenclatura (IDE1006) validada no build via EnforceCodeStyleInBuild
dotnet_diagnostic.IDE1006.severity = warning

dotnet_naming_symbols.interfaces.applicable_kinds = interface
dotnet_naming_symbols.types.applicable_kinds = class, struct, enum, delegate
dotnet_naming_symbols.type_parameters.applicable_kinds = type_parameter
dotnet_naming_symbols.members.applicable_kinds = property, method, event
dotnet_naming_symbols.async_methods.applicable_kinds = method
dotnet_naming_symbols.async_methods.required_modifiers = async
dotnet_naming_symbols.constants.applicable_kinds = field
dotnet_naming_symbols.constants.required_modifiers = const
dotnet_naming_symbols.public_fields.applicable_kinds = field
dotnet_naming_symbols.public_fields.applicable_accessibilities = public, internal, protected, protected_internal
dotnet_naming_symbols.private_fields.applicable_kinds = field
dotnet_naming_symbols.private_fields.applicable_accessibilities = private, private_protected
dotnet_naming_symbols.parameters_and_locals.applicable_kinds = parameter, local

dotnet_naming_style.pascal_case.capitalization = pascal_case
dotnet_naming_style.begins_with_i.capitalization = pascal_case
dotnet_naming_style.begins_with_i.required_prefix = I
dotnet_naming_style.begins_with_t.capitalization = pascal_case
dotnet_naming_style.begins_with_t.required_prefix = T
dotnet_naming_style.ends_with_async.capitalization = pascal_case
dotnet_naming_style.ends_with_async.required_suffix = Async
dotnet_naming_style.underscore_camel_case.capitalization = camel_case
dotnet_naming_style.underscore_camel_case.required_prefix = _
dotnet_naming_style.camel_case.capitalization = camel_case

dotnet_naming_rule.interfaces_begin_with_i.symbols = interfaces
dotnet_naming_rule.interfaces_begin_with_i.style = begins_with_i
dotnet_naming_rule.interfaces_begin_with_i.severity = warning

dotnet_naming_rule.type_parameters_begin_with_t.symbols = type_parameters
dotnet_naming_rule.type_parameters_begin_with_t.style = begins_with_t
dotnet_naming_rule.type_parameters_begin_with_t.severity = warning

dotnet_naming_rule.async_methods_end_with_async.symbols = async_methods
dotnet_naming_rule.async_methods_end_with_async.style = ends_with_async
dotnet_naming_rule.async_methods_end_with_async.severity = warning

dotnet_naming_rule.types_pascal_case.symbols = types
dotnet_naming_rule.types_pascal_case.style = pascal_case
dotnet_naming_rule.types_pascal_case.severity = warning

dotnet_naming_rule.members_pascal_case.symbols = members
dotnet_naming_rule.members_pascal_case.style = pascal_case
dotnet_naming_rule.members_pascal_case.severity = warning

dotnet_naming_rule.constants_pascal_case.symbols = constants
dotnet_naming_rule.constants_pascal_case.style = pascal_case
dotnet_naming_rule.constants_pascal_case.severity = warning

dotnet_naming_rule.public_fields_pascal_case.symbols = public_fields
dotnet_naming_rule.public_fields_pascal_case.style = pascal_case
dotnet_naming_rule.public_fields_pascal_case.severity = warning

dotnet_naming_rule.private_fields_underscore_camel_case.symbols = private_fields
dotnet_naming_rule.private_fields_underscore_camel_case.style = underscore_camel_case
dotnet_naming_rule.private_fields_underscore_camel_case.severity = warning

dotnet_naming_rule.parameters_and_locals_camel_case.symbols = parameters_and_locals
dotnet_naming_rule.parameters_and_locals_camel_case.style = camel_case
dotnet_naming_rule.parameters_and_locals_camel_case.severity = warning

# Migrations são geradas pelo EF Core e ficam fora das regras de estilo
[src/Backend.Repository/Migrations/*.cs]
generated_code = true

# Actions de controller não usam sufixo Async (o MVC remove o sufixo e quebra CreatedAtAction)
[src/Backend.Api/Controllers/*.cs]
dotnet_naming_rule.async_methods_end_with_async.severity = none

# Testes usam o padrão Metodo_Cenario_ResultadoEsperado
[tests/**.cs]
dotnet_naming_rule.async_methods_end_with_async.severity = none
dotnet_naming_rule.members_pascal_case.severity = none
```

- Adicionar na verificação final (dentro de backend/, abortar com o erro exato se algum falhar):
	- dotnet build Backend.slnx (sem erros e sem warnings)
	- dotnet test Backend.slnx (todos os testes passando; antes de existir módulo, rodam os testes de arquitetura e os testes da Infrastructure)
	- dotnet format Backend.slnx --verify-no-changes

Não mudar absolutamente nada do que já foi feito, simplesmente adicionar essa nova capacidade na skill.
