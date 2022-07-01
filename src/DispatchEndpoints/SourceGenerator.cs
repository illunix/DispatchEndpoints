using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace DispatchEndpoints;

[Generator]
internal class SourceGenerator : ISourceGenerator
{
    private static List<INamedTypeSymbol> _classes = new();

    private static StringBuilder _endpointsCores { get; set; } = new();

    public void Initialize(GeneratorInitializationContext context)
        => context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());

    public void Execute(GeneratorExecutionContext context)
    {
#if DEBUGATTACH
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

        var controllersBuilder = new StringBuilder();

        controllersBuilder.AppendLine(
@"using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DispatchEndpoints; 
");

        foreach (var @class in _classes)
        {
            controllersBuilder.Append(GenerateController(@class));
        }

        context.AddSource(
            "DispatchEndpoints.Controllers.g.cs",
            SourceText.From(
                controllersBuilder.ToString(),
                Encoding.UTF8
            )
        );

        foreach (var @class in _classes)
        {
            _endpointsCores.Append(GenerateEndpointCore(@class));
        }

        _endpointsCores?.Insert(0, "using DispatchEndpoints;\n");

        context.AddSource(
            "DispatchEndpoints.EndpointsCores.g.cs",
            SourceText.From(
                _endpointsCores?.ToString()!,
                Encoding.UTF8
            )
        );
    }

    private static string GenerateController(INamedTypeSymbol @class)
    {
        var sourceBuilder = new StringBuilder();

        var namespaceName = @class.ContainingNamespace.ToDisplayString();

        var attrProperties = @class.GetAttributes().FirstOrDefault()!.NamedArguments;
        var controllerName = attrProperties
            .Where(q => q.Key == "Controller")
            .Select(q => q.Value.Value!.ToString())
            .FirstOrDefault();
        if (string.IsNullOrWhiteSpace(controllerName))
        {
            controllerName = namespaceName.Substring(namespaceName.LastIndexOf('.') + 1);
        }

        var reqMethod = ((HttpRequestMethods)attrProperties
            .Where(q => q.Key == "RequestMethod")
            .Select(q => q.Value.Value!)
            .FirstOrDefault()).ToString().ToLowerInvariant().FirstCharToUpper();
        var methodName = @class.Name;

        var producesResponseTypes = attrProperties
            .Where(q => q.Key == "ProducesResponseTypes")
            .Select(q => q.Value.Values.Select(q => (HttpStatusCodes)q.Value!))
            .FirstOrDefault();

        var producesResponseTypesAttrsBuilder = new StringBuilder();

        foreach (var responseType in producesResponseTypes)
        {
            producesResponseTypesAttrsBuilder.Append($"\n\t\t[ProducesResponseType(Microsoft.AspNetCore.Http.StatusCodes.{ConvertStatusCode(responseType)})]");
        }

        var producesResponseTypesAttrs = producesResponseTypesAttrsBuilder.ToString();

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

        var commandExist = @class.GetMembers()
            .Where(q => q.Name == "Command")
            .Any();
        if (commandExist)
        {
            fromAttr = "[FromBody]";
            req = "Command";
        }

        var queryExist = @class.GetMembers()
            .Where(q => q.Name == "Query")
            .Any();
        if (queryExist)
        {
            fromAttr = "[FromQuery]";
            req = "Query";
        }

        var actionName = $"({(string.IsNullOrWhiteSpace(route) ? $"\"{@class.Name.PascalToKebabCase()}\"" : $"\"{route}\"")})";
        if (
            @class.Name == "Get" ||
            @class.Name == "GetAll" ||
            @class.Name == "GetById" ||
            @class.Name == "Create" ||
            @class.Name == "Update" ||
            @class.Name == "Delete" ||
            @class.Name == "Remove"
        ) 
        {
            actionName = "";
        }

        var routeAttr = $"[Route(\"{controllerName.PascalToKebabCase()}\")]";
        var httpAttr = $"[Http{reqMethod}{actionName}]";
        var authAttr = $"{(auth is not null ? $"\n[Authorize{(!string.IsNullOrWhiteSpace(policy) ? $"(\"{policy}\")" : "")}]" : "")}";

        var methodNameWithParams = $"{methodName}({fromAttr} {methodName}.{req} request)";
        var dispatcher = $"{(queryExist ? "var query = " : "")}await Dispatcher.{(commandExist ? "Send(request)" : "")}{(queryExist ? "Query(request)" : "")};";
        var returnStatusCode = $"return {producesResponseTypes.FirstOrDefault()}({(queryExist ? "query" : "")});";

        var handlerMethod = @class.GetMembers()
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
        {httpAttr}{authAttr}{producesResponseTypesAttrs}
        public async Task<{returnType}> {methodNameWithParams}
        {{
            {dispatcher}

            {returnStatusCode}
        }}
    }}
}}
");

        return sourceBuilder.ToString();
    }

    private static string GenerateEndpointCore(INamedTypeSymbol @class)
    {
        var sourceBuilder = new StringBuilder();

        var commandExist = @class.GetMembers()
            .Any(q => q.Name == "Command");

        var queryExist = @class.GetMembers()
            .Any(q => q.Name == "Query");

        if (
            commandExist &&
            queryExist
        )
        {
            return string.Empty;
        }

        if (@class.GetMembers()
            .FirstOrDefault(q =>
                q.Name == "Command" ||
                q.Name == "Query"
            ) is not INamedTypeSymbol requestMethod
        )
        {
            return string.Empty;
        }

        var addValidationMethod = requestMethod.GetMembers()
            .FirstOrDefault(q => q.Name == "AddValidation");

        if (addValidationMethod is not null)
        {
            _endpointsCores?.Insert(0, "using FluentValidation;\n");
        }

        var namespaceName = @class.ContainingNamespace
            .ToDisplayString();

        var handlerMethod = @class.GetMembers()
            .FirstOrDefault(q => q.Name == "Handler") as IMethodSymbol;

        if (handlerMethod?.ReturnType is not INamedTypeSymbol handlerMethodReturnType)
        {
            return string.Empty;
        }

        var handlerMethodParams = handlerMethod.Parameters
            .ToDictionary(q => q.Type, q => q.Name);

        var handlerMethodParamsWithoutRequest = handlerMethodParams
            .Where(q =>
                q.Key.Name != "Command" &&
                q.Key.Name != "Query"
            );

        var privateProperties = () =>
        {
            var privatePropertiesBuilder = new StringBuilder();

            var handlerMethodParamsWithoutRequestParameter = handlerMethodParams
                .Where(q =>
                    q.Key.Name != "Command" &&
                    q.Key.Name != "Query"
                );

            foreach (var param in handlerMethodParamsWithoutRequestParameter)
            {
                privatePropertiesBuilder.AppendLine($"private readonly {param.Key} _{param.Value};");
            }

            return privatePropertiesBuilder.ToString();
        };

        var requestMethodName = @class.GetMembers()
            .Any(q => q.Name == "Command") ? "Command" : "Query";

        dynamic requestReturnType = handlerMethodReturnType.TypeArguments
            .FirstOrDefault()!;

        var requestHandlerInterface = $"IRequestHandler<{@class.Name}.{requestMethodName}{(requestReturnType is null ? "" : $", { requestReturnType}")}>";

        var requestPropety = () =>
        {
            var requestPropertyBuilder = new StringBuilder();

            var requestInterface = requestReturnType is null ? "IRequest" : $"IRequest<{requestReturnType}>";

            requestPropertyBuilder.Append($"public partial record {requestMethodName} : {requestInterface} {{ }}\n");

            return requestPropertyBuilder.ToString();
        };

        var requestValidator = () =>
        {
            var requestValidatorBuilder = new StringBuilder();
            
            if (addValidationMethod is not null)
            {
                var className = $"{requestMethodName}Validator";

                requestValidatorBuilder.Append($"\n\t\tpublic class {className} : AbstractValidator<{requestMethodName}> {{ public {className}() {{ {requestMethodName}.AddValidation(this); }} }}\n");
            }

            return requestValidatorBuilder.ToString();
        };

        var constructor = () =>
        {
            var constructorBuilder = new StringBuilder();

            var constructorParams = string.Join(
                ", ", 
                handlerMethodParamsWithoutRequest.Select(q => $"{q.Key} {q.Value}")
            );
            var injected = string.Join(
                "\n", 
                handlerMethodParamsWithoutRequest.Select(q => $@"_{q.Value} = {q.Value};")
            );

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

            return constructorBuilder.ToString();
        };

        var handleMethod = () =>
        {
            var handleBuilder = new StringBuilder();

            var handlerParams = string.Join(
                ", ",
                handlerMethodParams.Values
                    .Select(q =>
                        q == "request" || 
                        q == "req" ||
                        q == "command" ||
                        q == "query" ? "request" : $"_{q}"
                    )
            );

            handleBuilder.Append(
@$"public async Task{(requestReturnType is null ? "" : $"<{requestReturnType}>")} Handle({requestMethodName} request, CancellationToken cancellationToken) 
            {{
                {(requestReturnType is null ? $"await Handler({handlerParams});" :
                        $"return await Handler({handlerParams});")}
            }}"
);

            return handleBuilder.ToString();
        };

        var classType = @class as ISymbol;

        sourceBuilder.Append(
@$"
namespace {namespaceName}
{{
    public partial class {@class.Name} 
    {{
        {requestPropety()}{requestValidator()}
        private class {requestMethodName}HandlerCore : {requestHandlerInterface}
        {{
            {privateProperties()}
            {constructor()}
            {handleMethod()}
        }}
    }}
}}
"
);

        return sourceBuilder.ToString();
    }

    private static string ConvertStatusCode(HttpStatusCodes statusCode) => statusCode switch
    {
        HttpStatusCodes.Ok => "Status200OK",
        HttpStatusCodes.BadRequest => "Status400BadRequest",
        HttpStatusCodes.Unathorized => "Status401Unauthorized",
        HttpStatusCodes.NotFound => "Status404NotFound",
        HttpStatusCodes.NoContent => "Status204NoContent",
        _ => ""
    };

    private static void GetClasses(
       GeneratorExecutionContext context,
       SyntaxReceiver receiver
    )
    {
        var compilation = context.Compilation;

        foreach (var @class in receiver.CandidateClasses)
        {
            var model = compilation.GetSemanticModel(@class.SyntaxTree);
            var classSymbol = (INamedTypeSymbol)model.GetDeclaredSymbol(@class)!;
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