using ToyApi.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ToyApi;

[Generator]
internal class SourceGenerator : ISourceGenerator
{
    private static List<INamedTypeSymbol> _classes = new();

    public void Initialize(GeneratorInitializationContext context)
        => context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());

    public void Execute(GeneratorExecutionContext context)
    {
#if DEBUG
        if (!Debugger.IsAttached)
        {
            Debugger.Launch();
        }
#endif 
        if (context.SyntaxReceiver is not SyntaxReceiver syntaxReceiver)
        {
            return;
        }

        GetClasses(
            context,
            syntaxReceiver
        );

        context.AddSource(
            "ToyApi.Controllers.g.cs",
            SourceText.From(
                GenerateControllers(),
                Encoding.UTF8
            )
        );

        GenerateCommandAndQueries(context);
    }

    private static string GenerateControllers()
    {
        var sourceBuilder = new StringBuilder();

        sourceBuilder.AppendLine(
@"using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ToyApi;
");

        foreach (var clazz in _classes)
        {
            var namespaceName = clazz.ContainingNamespace.ToDisplayString();

            var attrProperties = clazz.GetAttributes().FirstOrDefault()!.NamedArguments;
            var controllerName = attrProperties
                .Where(q => q.Key == "Controller")
                .Select(q => q.Value.Value!.ToString())
                .FirstOrDefault();
            if (string.IsNullOrWhiteSpace(controllerName))
            {
                controllerName = namespaceName.Substring(namespaceName.LastIndexOf('.') + 1);
            }

            var reqMethod = ((RequestMethods)attrProperties
                .Where(q => q.Key == "RequestMethod")
                .Select(q => q.Value.Value!)
                .FirstOrDefault()).ToString();
            var methodName = clazz.Name;
            var statusCode = ((StatusCodes)attrProperties
                .Where(q => q.Key == "StatusCode")
                .Select(q => q.Value.Value!)
                .FirstOrDefault()).ToString();
            var route = attrProperties
                .Where(q => q.Key == "Route")
                .Select(q => q.Value.Value!.ToString())
                .FirstOrDefault();
            var auth = attrProperties
                .Where(q => q.Key == "Auth")
                .Select(q => q.Value.Value!)
                .FirstOrDefault();

            var policy = "";

            if (auth is not null)
            {
                policy = attrProperties
                    .Where(q => q.Key == "Policy")
                    .Select(q => q.Value.Value!.ToString())
                    .FirstOrDefault();
            }

            var fromAttr = "";
            var req = "";

            var commandExist = clazz.GetMembers()
                .Where(q => q.Name == "Command")
                .Any();
            if (commandExist)
            {
                fromAttr = "[FromBody]";
                req = "Command";
            }

            var queryExist = clazz.GetMembers()
                .Where(q => q.Name == "Query")
                .Any();
            if (queryExist)
            {
                fromAttr = "[FromQuery]";
                req = "Query";
            }

            var routeAttr = $"[Route(\"{controllerName.ToKebabCase()}\")]";
            var httpAttr = $"[Http{reqMethod}({(!string.IsNullOrWhiteSpace(route) ? $"\"{route}\"" : "").ToKebabCase()})]";
            var authAttr = $"{(auth is not null ? $"\n[Authorize{(!string.IsNullOrWhiteSpace(policy) ? $"(\"{policy}\")" : "")}]" : "")}";

            var methodNameWithParams = $"{methodName}({fromAttr} {methodName}.{req} request)";
            var dispatcher = $"{(queryExist ? "var query = " : "")}await Dispatcher.{(commandExist ? "Send(request)" : "")}{(queryExist ? "Query(request)" : "")};";
            var returnStatusCode = $"return {statusCode}({(queryExist ? "query" : "")});";

            var handlerMethod = clazz.GetMembers()
                .FirstOrDefault(q => q.Name == "Handler") as IMethodSymbol;

            if (handlerMethod?.ReturnType is not INamedTypeSymbol handlerMethodReturnType)
            {
                return string.Empty;
            }

            var returnType = $"ActionResult";

            if (handlerMethodReturnType.TypeArguments.Any())
            {
                returnType = $"ActionResult<{handlerMethodReturnType.TypeArguments.First()}>";
            }

            sourceBuilder.Append(
$@"namespace {namespaceName} 
{{
    {routeAttr}
    public partial class {controllerName}Controller : ApiControllerBase
    {{
        {httpAttr}{authAttr}
        public async Task<{returnType}> {methodNameWithParams}
        {{
            {dispatcher}

            {returnStatusCode}
        }}
    }}
}}
");
        }

        return sourceBuilder.ToString();
    }

    private static string GenerateCommandAndQueries(GeneratorExecutionContext context)
    {
        var sourceBuilder = new StringBuilder();

        return sourceBuilder.ToString();
    }

    private static void GetClasses(
       GeneratorExecutionContext context,
       SyntaxReceiver receiver
    )
    {
        var compilation = context.Compilation;

        foreach (var clazz in receiver.CandidateClasses)
        {
            var model = compilation.GetSemanticModel(clazz.SyntaxTree);
            var classSymbol = (INamedTypeSymbol)model.GetDeclaredSymbol(clazz)!;
            if (classSymbol is null)
            {
                break;
            }

            if (classSymbol.GetAttributes().Any(q => q.AttributeClass?.Name == nameof(DispatchEndpointAttribute)))
            {
                _classes.Add(classSymbol);
            }
        }
    }
}