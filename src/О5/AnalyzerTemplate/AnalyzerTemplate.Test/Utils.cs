using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AnalyzerTemplate.Test;

public static class Utils
{
    public static async Task<ImmutableArray<Diagnostic>> GetDiagnostics(string code)
    {
        var result = await GetDiagnosticsAdvanced(code);

        return result.diagnostics;
    }

    public static async Task<(ImmutableArray<Diagnostic> diagnostics, Document document, Workspace workspace)> GetDiagnosticsAdvanced(string code)
    {
        var workspace = new AdhocWorkspace();
        var solution = workspace.CurrentSolution;
        var projectId = ProjectId.CreateNewId();

        solution = solution
            .AddProject(
                projectId,
                "MyTestProject",
                "MyTestProject",
                LanguageNames.CSharp);

        var documentId = DocumentId.CreateNewId(projectId);

        solution = solution
            .AddDocument(documentId,
            "Test0.cs",
            code);

        var project = solution.GetProject(projectId);

        project = project.AddMetadataReference(
            MetadataReference.CreateFromFile(
                typeof(object).Assembly.Location))
            .AddMetadataReferences(GetAllReferencesNeededForType(typeof(ImmutableArray)));

        if (!workspace.TryApplyChanges(project.Solution))
            throw new Exception("Unable to apply changes to the workspace");

        var compilation = await project.GetCompilationAsync();

        var compilationWithAnalyzer = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(
                new AnalyzerTemplateAnalyzer()));

        var diagnostics = await compilationWithAnalyzer.GetAllDiagnosticsAsync();
        return (diagnostics, workspace.CurrentSolution.GetDocument(documentId), workspace);
    }

    private static MetadataReference[] GetAllReferencesNeededForType(Type type)
    {
        var files = GetAllAssemblyFilesNeededForType(type);

        return files.Select(x => MetadataReference.CreateFromFile(x)).Cast<MetadataReference>().ToArray();
    }

    private static ImmutableArray<string> GetAllAssemblyFilesNeededForType(Type type)
    {
        return type.Assembly.GetReferencedAssemblies()
            .Select(x => Assembly.Load(x.FullName))
            .Append(type.Assembly)
            .Select(x => x.Location)
            .ToImmutableArray();
    }
}