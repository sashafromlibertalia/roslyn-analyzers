using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
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
        private const string ReplacedDefaultVariableName = "value";
        private const string Title = "Error";

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

            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();
            
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Title,
                    createChangedDocument: c => ReplaceMethodDeclarationAsync(context.Document, declaration, c),
                    equivalenceKey: Title),
                diagnostic);
        }

        private static MethodDeclarationSyntax BuildMethodDeclaration(MethodDeclarationSyntax method)
        {
            return SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(
                        SyntaxFactory.Token(SyntaxKind.BoolKeyword)),
                SyntaxFactory.Identifier(method.Identifier.Text))
                .WithModifiers(
                    SyntaxFactory.TokenList(method.Modifiers))
                .WithParameterList(SyntaxFactory.ParameterList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Parameter(
                                SyntaxFactory.Identifier(ReplacedDefaultVariableName))
                            .WithModifiers(
                                SyntaxFactory.TokenList(
                                    SyntaxFactory.Token(SyntaxKind.OutKeyword)))
                            .WithType(
                                SyntaxFactory.IdentifierName(method.ReturnType.ToString())))));
        }

        private static ExpressionStatementSyntax BuildDefaultAssign()
        {
            return SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(ReplacedDefaultVariableName),
                        SyntaxFactory.LiteralExpression(
                            SyntaxKind.DefaultLiteralExpression,
                            SyntaxFactory.Token(SyntaxKind.DefaultKeyword))));
        }

        private static TryStatementSyntax BuildTryStatement()
        {
            return SyntaxFactory.TryStatement(
                SyntaxFactory.SingletonList(
                    SyntaxFactory.CatchClause()
                        .WithDeclaration(
                            SyntaxFactory.CatchDeclaration(
                                    SyntaxFactory.IdentifierName("Exception"))
                                .WithIdentifier(
                                    SyntaxFactory.Identifier("e")))
                        .WithBlock(
                            SyntaxFactory.Block(
                                SyntaxFactory.SingletonList<StatementSyntax>(
                                    SyntaxFactory.ReturnStatement(
                                        SyntaxFactory.LiteralExpression(
                                            SyntaxKind.FalseLiteralExpression)))))));
        }

        private static BlockSyntax BuildTryBody(MethodDeclarationSyntax method)
        {
            var statements = method.Body.Statements;
            var modifiedExpressionsBlock = new List<StatementSyntax>();

            foreach (var statement in statements)
            {
                if (statement is IfStatementSyntax ifStatement)
                {
                    var blockIfStatement = ifStatement.Statement as BlockSyntax;
                    ReturnStatementSyntax oldReturn = null;
                    ExpressionSyntax expression = null;
                    foreach (var statementInsideIf in blockIfStatement.Statements)
                    {
                        if (statementInsideIf is ReturnStatementSyntax syntax)
                        {
                            oldReturn = syntax;
                            expression = syntax.Expression;
                        }
                    }

                    var fixedReturn= BuildStatementForDefaultAssignment(expression);
                    modifiedExpressionsBlock.Add( ifStatement.ReplaceNode(oldReturn, fixedReturn));
                }
                else if (statement is ReturnStatementSyntax returnStatement)
                {
                    var oldReturn = returnStatement;
                    var expression = returnStatement.Expression;
                    var fixedReturn = BuildStatementForDefaultAssignment(expression);
                    modifiedExpressionsBlock.Add(statement.ReplaceNode(oldReturn, fixedReturn));
                }
                else
                {
                    modifiedExpressionsBlock.Add(statement);
                }
            }

            var body = SyntaxFactory.Block(modifiedExpressionsBlock);
            body = body.AddStatements(SyntaxFactory.ReturnStatement(
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.TrueLiteralExpression)));

            return body;
        }

        private static StatementSyntax BuildStatementForDefaultAssignment(ExpressionSyntax expression)
        {
            return SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName(ReplacedDefaultVariableName), expression));
        }

        private static MethodDeclarationSyntax ModifyMethod(MethodDeclarationSyntax method)
        {
            return BuildMethodDeclaration(method)
                .WithBody(SyntaxFactory.Block(BuildDefaultAssign(),
                    BuildTryStatement()
                        .WithBlock(BuildTryBody(method))));
        }

        private static async Task<Document> ReplaceMethodDeclarationAsync(Document document, MethodDeclarationSyntax methodDeclaration, CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var root = await tree.GetRootAsync(cancellationToken) as CompilationUnitSyntax;
            
            var modifiedMethodDeclaration = ModifyMethod(methodDeclaration);
            root = root?.ReplaceNode(methodDeclaration, modifiedMethodDeclaration).NormalizeWhitespace();
            return document.WithSyntaxRoot(root);
        }
    }
}
