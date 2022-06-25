using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AnalyzerTemplate
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AnalyzerTemplateCodeFixProvider)), Shared]
    public class AnalyzerTemplateCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(AnalyzerTemplateAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root?.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<BinaryExpressionSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(CodeFixResources.CodeFixTitle,
                    c => ReplaceEqualsOperatorAsync(context.Document, declaration, root),
                    nameof(CodeFixResources.CodeFixTitle)), diagnostic);
        }

        private static IsPatternExpressionSyntax GetValidEqualExpression(BinaryExpressionSyntax binaryExpression)
        {
            if (binaryExpression.IsKind(SyntaxKind.NotEqualsExpression))
                return SyntaxFactory.IsPatternExpression(binaryExpression.Left,
                    SyntaxFactory.Token(SyntaxKind.IsKeyword), SyntaxFactory
                        .UnaryPattern(SyntaxFactory
                            .Token(SyntaxKind.NotKeyword), SyntaxFactory
                            .ConstantPattern(binaryExpression.Right)));

            return SyntaxFactory.IsPatternExpression(binaryExpression?.Left ?? throw new InvalidOperationException(), SyntaxFactory
                .Token(SyntaxKind.IsKeyword), SyntaxFactory
                .ConstantPattern(binaryExpression?.Right));
        }

        private static async Task<Document> ReplaceEqualsOperatorAsync(Document document, BinaryExpressionSyntax binaryExpression, SyntaxNode root)
        {
            var modifiedEqualsExpression = GetValidEqualExpression(binaryExpression);

            var updatedRoot = root.ReplaceNode(binaryExpression, modifiedEqualsExpression);
            return await Task.FromResult(document.WithSyntaxRoot(updatedRoot));
        }
    }
}
