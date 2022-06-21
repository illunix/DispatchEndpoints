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

        context.AddSource(
            "ToyApi.Dispatchers.g.cs",
            SourceText.From(
                GenerateCommandAndQueries(context),
                Encoding.UTF8
            )
        );

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

        foreach (var clazz in _classes)
        {
            var namespaceName = clazz.ContainingNamespace.ToDisplayString();

            var handlerMethod = clazz.GetMembers()
                .FirstOrDefault(q => q.Name == "Handler") as IMethodSymbol;

            if (handlerMethod?.ReturnType is not INamedTypeSymbol handlerMethodReturnType)
            {
                return string.Empty;
            }
            var handlerMethodParams = handlerMethod.Parameters
                .ToDictionary(q => q.Type, q => q.Name);
            var handlerMethodParamsWithoutRequest = handlerMethodParams.Where(q => q.Key.Name != "Command" && q.Key.Name != "Query").ToList();

            var propertiesBuilder = new StringBuilder();

            foreach (var param in handlerMethodParams.Where(q => q.Key.Name != "Command" && q.Key.Name != "Query"))
            {
                propertiesBuilder.AppendLine($"private readonly {param.Key} _{param.Value};");
            }

            var requestBuilder = new StringBuilder();

            var commandExist = clazz.GetMembers()
                .Any(q => q.Name == "Command");

            var queryExist = clazz.GetMembers()
                .Any(q => q.Name == "Query");

            if (commandExist && queryExist)
            {
                return string.Empty;
            }

            dynamic? type = null;

            if (handlerMethodReturnType.TypeArguments.Any())
            {
                type = handlerMethodReturnType.TypeArguments.First();
            }

            var requestInterface = type is not null ? $"IRequest<{handlerMethodReturnType.TypeArguments.FirstOrDefault()}>" : "IRequest";

            if (clazz.GetMembers()
                    .FirstOrDefault(q => q.Name == "Command" || q.Name == "Query") is not INamedTypeSymbol requestMethod)
            {
                return string.Empty;
            }

            var requestMethodName = clazz.GetMembers().Any(q => q.Name == "Command") ? "Command" : "Query";

            requestBuilder.Append($"public partial record {requestMethodName} : {requestInterface} {{ }}");

            var requestValidatorBuilder = new StringBuilder();

            var addValidationMethod = requestMethod.GetMembers().FirstOrDefault(x => x.Name == "AddValidation");
            if (addValidationMethod is not null)
            {
                var className = $"{requestMethodName}Validator";

                requestValidatorBuilder.Append($"public class {className} : AbstractValidator<{requestMethodName}> {{ public {className}() {{ {requestMethodName}.AddValidation(this); }} }}");
            }

            var constructorBuilder = new StringBuilder();

            var constructorParams = string.Join(", ", handlerMethodParamsWithoutRequest.Select(q => $"{q.Key} {q.Value}"));
            var injected = string.Join("\n", handlerMethodParamsWithoutRequest.Select(q => $"_{q.Value} = {q.Value};"));

            var useConstructor = handlerMethodParamsWithoutRequest.Any();
            if (useConstructor)
            {
                constructorBuilder.AppendLine(
                $@"public {requestMethodName}HandlerCore({constructorParams})
                {{
                    {injected}
                }}"
                );
            }

            var handleBuilder = new StringBuilder();

            var handlerParams = string.Join(", ", handlerMethodParams.Values.Select(q => q == "request" || q == "req" || q == "command" || q == "query" ? "request" : $"_{q}"));

            handleBuilder.Append(
                @$"public async Task<{type ?? ""}> Handle({requestMethodName} request, CancellationToken cancellationToken) 
            {{
                {(type is null ? $"await Handler({handlerParams});" :
                        $"return await Handler({handlerParams});")}
            }}"
            );

            sourceBuilder.Append(
@$"namespace {namespaceName}
{{
    public partial class {clazz.Name} 
    {{
        {requestBuilder}
        {requestValidatorBuilder}
        private class {requestMethodName}HandlerCore : IRequestHandler<{clazz.Name}.{requestMethodName}{(type is null ? "" : $", {type}")}>
        {{
            {propertiesBuilder}{constructorBuilder}
            {handleBuilder}
        }}
    }}
}}
");
        }

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