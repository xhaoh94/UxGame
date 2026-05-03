using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Ux.EventAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EventOnAnonymousLambdaAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "UXEVT001";

    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "禁止用匿名方法注册事件",
        messageFormat: "事件 On() 注册使用了匿名 lambda/匿名方法；Off() 几乎必然无法匹配同一个 delegate 实例。请改为： 使用具名方法；或 先把委托保存到变量，再用于 On/Off。",
        category: "用法",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not InvocationExpressionSyntax invocation)
            return;

        // We only care about: something.On(..., <delegate>)
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        if (memberAccess.Name is not IdentifierNameSyntax { Identifier.ValueText: "On" })
            return;

        var symbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol as IMethodSymbol;
        if (symbol is null)
            return;

        // Limit to the project's event registration APIs to avoid false positives.
        if (!IsUxEventRegistrationOn(symbol))
            return;

        if (invocation.ArgumentList.Arguments.Count == 0)
            return;

        // Find last argument: Action / Action<T> / etc.
        var lastArgExpr = invocation.ArgumentList.Arguments[invocation.ArgumentList.Arguments.Count - 1].Expression;

        // Disallow inline lambdas and anonymous methods (including wrapped ones like new Action(() => ...)).
        // These allocate new delegate instances, so calling Off(() => ...) will almost never match.
        foreach (var node in lastArgExpr.DescendantNodesAndSelf())
        {
            if (node is ParenthesizedLambdaExpressionSyntax or SimpleLambdaExpressionSyntax or AnonymousMethodExpressionSyntax)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, node.GetLocation()));
                break;
            }
        }
    }

    static bool IsUxEventRegistrationOn(IMethodSymbol method)
    {
        var containingType = method.ContainingType;
        if (containingType is null)
            return false;

        if (containingType.ContainingNamespace.ToDisplayString() != "Ux")
            return false;

        // 1) Direct registration on EventMgr.EventSystem
        if (containingType.Name == "EventSystem" &&
            containingType.ContainingType?.Name == "EventMgr" &&
            method.Name == "On")
        {
            return true;
        }

        // 2) Extension wrappers: EventMgrEx.On(...) / MainEventMgrEx.On(...)
        if (method.IsExtensionMethod &&
            method.Name == "On" &&
            (containingType.Name == "EventMgrEx" || containingType.Name == "MainEventMgrEx"))
        {
            return true;
        }

        return false;
    }
}
