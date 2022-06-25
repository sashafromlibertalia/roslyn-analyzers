using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = AnalyzerTemplate.Test.Verifiers.CSharpCodeFixVerifier<
    AnalyzerTemplate.AnalyzerTemplateAnalyzer,
    AnalyzerTemplate.AnalyzerTemplateCodeFixProvider>;

namespace AnalyzerTemplate.Test;

[TestClass]
public class AnalyzerTemplateUnitTest
{
    [TestMethod]
    public async Task FixesWithClass()
    {
        const string oldCode = @"using System;

namespace AnalyzerTemplateTest
{
    internal class Student
    {
        public Student()
        {
        }
    }

    internal static class Test
    {
        public static Student TryGetValue()
        {
            Random rd = new Random();
            int num = rd.Next(1, 5);
            if (num == 1) 
            {
                return new Student();
            }

            if (num == 2) 
            {
                return new Student();
            }

            return new Student();
        }
       
        private static void Main(string[] args)
        {
        }
    }
}";

        const string fixCode = @"using System;

namespace AnalyzerTemplateTest
{
    internal class Student
    {
        public Student()
        {
        }
    }

    internal static class Test
    {
        public static bool TryGetValue(out Student value)
        {
            value = default;
            try
            {
                Random rd = new Random();
                int num = rd.Next(1, 5);
                if (num == 1)
                {
                    value = new Student();
                }

                if (num == 2)
                {
                    value = new Student();
                }

                value = new Student();
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        private static void Main(string[] args)
        {
        }
    }
}";

        var (diagnostics, document, workspace) = await Utils.GetDiagnosticsAdvanced(oldCode);
        var diagnostic = diagnostics[0];
        var codeFixProvider = new AnalyzerTemplateCodeFixProvider();

        CodeAction registeredCodeAction = null;
        var context = new CodeFixContext(document, diagnostic, (codeAction, _) =>
        {
            if (registeredCodeAction != null)
                throw new Exception("Code action was registered more than once");

            registeredCodeAction = codeAction;

        }, CancellationToken.None);

        await codeFixProvider.RegisterCodeFixesAsync(context);

        if (registeredCodeAction == null)
            throw new Exception("Code action was not registered");

        var operations = await registeredCodeAction.GetOperationsAsync(CancellationToken.None);

        foreach(var operation in operations)
        {
            operation.Apply(workspace, CancellationToken.None);
        }

        var updatedDocument = workspace.CurrentSolution.GetDocument(document.Id);
        var newCode = (await updatedDocument?.GetTextAsync()!).ToString();
     
        Assert.AreEqual(fixCode, newCode);
    }
}