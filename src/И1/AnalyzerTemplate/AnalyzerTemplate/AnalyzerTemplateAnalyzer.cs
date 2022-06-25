using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AnalyzerTemplate
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AnalyzerTemplateAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "NullEqualityAnalyzer";

        private static readonly LocalizableString Title = "Hey, honey, your code-style sucks.";
        private static readonly LocalizableString MessageFormat = "Equations to null in such way is a Java-like == sucks.";
        private static readonly LocalizableString Description = "Please, use is / is not.";
        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat,
            Category, DiagnosticSeverity.Warning, true, Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeNullEqualityNode, SyntaxKind.EqualsExpression);
            context.RegisterSyntaxNodeAction(AnalyzeNullEqualityNode, SyntaxKind.NotEqualsExpression);
        }

        private static void AnalyzeNullEqualityNode(SyntaxNodeAnalysisContext context)
        {
            var equalsExpressions = (BinaryExpressionSyntax)context.Node;
            if (!equalsExpressions.OperatorToken.IsKind(SyntaxKind.EqualsEqualsToken) &&
                !equalsExpressions.OperatorToken.IsKind(SyntaxKind.ExclamationEqualsToken))
                return;

            var leftValue = context.SemanticModel.GetTypeInfo(equalsExpressions.Left).Type;
            var rightValue = context.SemanticModel.GetTypeInfo(equalsExpressions.Right).Type;

            if (rightValue != null && leftValue != null && (!leftValue.IsValueType || !rightValue.IsValueType))
                return;

            var diagnostic = Diagnostic.Create(Rule, equalsExpressions.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}
