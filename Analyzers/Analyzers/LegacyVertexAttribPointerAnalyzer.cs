using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class LegacyVertexAttribPointerAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor LegacyVertexAttribPointerRule = new DiagnosticDescriptor(
        "BG0001",
        "Legacy glVertexAttribPointer usage detected",
        "The usage of legacy GL.VertexAttribPointer is forbidden. Use GL.VertexAttribFormat with GL.VertexAttribBinding instead.",
        "Performance",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "GL.VertexAttribPointer is a piece of shit function because it modifies where the data is SOURCED from too.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => 
        ImmutableArray.Create(LegacyVertexAttribPointerRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocationExpr = (InvocationExpressionSyntax)context.Node;
        
        // Check if this is a member access expression (e.g., GL.VertexAttribPointer)
        if (invocationExpr.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        // Check if the method name is VertexAttribPointer
        if (memberAccess.Name.Identifier.ValueText != "VertexAttribPointer")
            return;

        // Get the semantic model to check the type
        var semanticModel = context.SemanticModel;
        var typeInfo = semanticModel.GetTypeInfo(memberAccess.Expression);
        
        if (typeInfo.Type is not INamedTypeSymbol typeSymbol)
            return;

        // Check if it's being called on GL type (from Silk.NET.OpenGL)
        if (typeSymbol.Name == "GL" && 
            (typeSymbol.ContainingNamespace?.ToDisplayString() == "Silk.NET.OpenGL" ||
             typeSymbol.ContainingNamespace?.ToDisplayString() == "Silk.NET.Legacy.OpenGL"))
        {
            var diagnostic = Diagnostic.Create(
                LegacyVertexAttribPointerRule,
                invocationExpr.GetLocation(),
                memberAccess.ToString());
            
            context.ReportDiagnostic(diagnostic);
        }
    }
}