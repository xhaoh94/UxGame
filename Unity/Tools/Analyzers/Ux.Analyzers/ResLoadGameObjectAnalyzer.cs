using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Ux.Analyzers;

/// <summary>
/// 禁止用 ResMgr.LoadAsset&lt;GameObject&gt; 加载预制体。
/// 那条路不会挂父级，也不会复用 LoadGameObject 的池化摆放规则；
/// 预制体一律走 LoadGameObject / LoadGameObjectAsync。
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ResLoadGameObjectAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "UXRES001";

    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "不要用 LoadAsset 加载 GameObject",
        messageFormat: "ResMgr.{0}<GameObject> 拿不到实例化后的摆放规则，请改用 ResMgr.LoadGameObject(...) / LoadGameObjectAsync(...)",
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

        // 只处理 X.LoadAsset<T>(...) / X.LoadAssetAsync<T>(...)
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        if (memberAccess.Name is not GenericNameSyntax genericName)
            return;

        var methodName = genericName.Identifier.ValueText;
        if (methodName != "LoadAsset" && methodName != "LoadAssetAsync")
            return;

        if (genericName.TypeArgumentList.Arguments.Count != 1)
            return;

        var typeArg = genericName.TypeArgumentList.Arguments[0];
        var typeSymbol = context.SemanticModel.GetTypeInfo(typeArg, context.CancellationToken).Type;
        if (typeSymbol?.ToDisplayString() != "UnityEngine.GameObject")
            return;

        // 只在 Ux.ResMgr 上生效，避免误伤 YooAsset 的 ResourcePackage.LoadAsset。
        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method)
            return;

        var containing = method.ContainingType;
        if (containing is null)
            return;

        if (containing.Name != "ResMgr")
            return;

        if (containing.ContainingNamespace?.ToDisplayString() != "Ux")
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), methodName));
    }
}
