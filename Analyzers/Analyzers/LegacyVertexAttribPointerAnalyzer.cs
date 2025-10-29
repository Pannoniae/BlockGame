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

    public static readonly DiagnosticDescriptor LegacyVertexAttribIPointerRule = new DiagnosticDescriptor(
        "BG0002",
        "Legacy glVertexAttribIPointer usage detected",
        "The usage of legacy GL.VertexAttribIPointer is forbidden. Use GL.VertexAttribIFormat with GL.VertexAttribBinding instead.",
        "Performance",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "GL.VertexAttribIPointer mixes attribute format with binding state.");

    public static readonly DiagnosticDescriptor LegacyVertexAttribLPointerRule = new DiagnosticDescriptor(
        "BG0003",
        "Legacy glVertexAttribLPointer usage detected",
        "The usage of legacy GL.VertexAttribLPointer is forbidden. Use GL.VertexAttribLFormat with GL.VertexAttribBinding instead.",
        "Performance",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "GL.VertexAttribLPointer mixes attribute format with binding state.");

    public static readonly DiagnosticDescriptor LegacyVertexAttribDivisorRule = new DiagnosticDescriptor(
        "BG0004",
        "Legacy glVertexAttribDivisor usage detected",
        "The usage of legacy GL.VertexAttribDivisor is forbidden. It mixes attribute format with binding state.",
        "Performance",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "GL.VertexAttribDivisor is a legacy function that mixes attribute format with binding state. Use separate format/binding calls.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
        LegacyVertexAttribPointerRule,
            LegacyVertexAttribIPointerRule,
            LegacyVertexAttribLPointerRule,
            LegacyVertexAttribDivisorRule
    ];

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

        var methodName = memberAccess.Name.Identifier.ValueText;
        
        // Check for legacy VAO state-mixing functions
        DiagnosticDescriptor? rule = methodName switch
        {
            "VertexAttribPointer" => LegacyVertexAttribPointerRule,
            "VertexAttribIPointer" => LegacyVertexAttribIPointerRule,
            "VertexAttribLPointer" => LegacyVertexAttribLPointerRule,
            "VertexAttribDivisor" => LegacyVertexAttribDivisorRule,
            _ => null
        };

        if (rule == null)
            return;

        // Get the semantic model to check the type
        var semanticModel = context.SemanticModel;
        var typeInfo = semanticModel.GetTypeInfo(memberAccess.Expression);
        
        if (typeInfo.Type is not INamedTypeSymbol typeSymbol)
            return;

        // Check if it's being called on GL type (from Silk.NET.OpenGL)
        if (typeSymbol.Name == "GL" && 
            (typeSymbol.ContainingNamespace?.ToDisplayString() == "Silk.NET.OpenGL" ||
             typeSymbol.ContainingNamespace?.ToDisplayString() == "Silk.NET.OpenGL.Legacy"))
        {
            var diagnostic = Diagnostic.Create(
                rule,
                invocationExpr.GetLocation(),
                memberAccess.ToString());
            
            context.ReportDiagnostic(diagnostic);
        }
    }
}