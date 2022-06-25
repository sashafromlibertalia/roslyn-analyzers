using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = AnalyzerTemplate.Test.Verifiers.CSharpCodeFixVerifier<
    AnalyzerTemplate.AnalyzerTemplateAnalyzer,
    AnalyzerTemplate.AnalyzerTemplateCodeFixProvider>;

namespace AnalyzerTemplate.Test;

[TestClass]
public class AnalyzerTemplateUnitTest
{
    private const string Beginning = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        internal static class Test
        {
            static void Main(string[] args)
            {
                int? val = null;";


    private const string Ending = @"             {
                } 
            }
        }
    }";

    [TestMethod]
    public async Task FixesIsNull()
    {
        var builder = new StringBuilder();
        var test = builder
            .Clear()
            .Append(Beginning)
            .Append(@"if (val == null)")
            .Append(Ending)
            .ToString();
        var fixedTest = builder
            .Clear()
            .Append(Beginning)
            .Append(@"if (val is null)")
            .Append(Ending)
            .ToString();
        var expected = VerifyCS
            .Diagnostic("NullEqualityAnalyzer")
            .WithSpan(15, 37, 15, 48);
        await VerifyCS.VerifyCodeFixAsync(test, expected, fixedTest);
    }

     [TestMethod]
     public async Task FixesIsNotNull()
     {
         var builder = new StringBuilder();
         var test = builder
             .Clear()
             .Append(Beginning)
             .Append(@"if (val != null)")
             .Append(Ending)
             .ToString();
         var fixedTest = builder
             .Clear()
             .Append(Beginning)
             .Append(@"if (val is not null)")
             .Append(Ending)
             .ToString();
         var expected = VerifyCS
             .Diagnostic("NullEqualityAnalyzer")
             .WithSpan(15, 37, 15, 48);
         await VerifyCS.VerifyCodeFixAsync(test, expected, fixedTest);
     }

    [TestMethod]
    public async Task CodeStyleIsOkWithoutFix()
    {
        var builder = new StringBuilder();
        var test = builder
            .Clear()
            .Append(Beginning)
            .Append(@"if (val is null)")
            .Append(Ending)
            .ToString();

        await VerifyCS.VerifyAnalyzerAsync(test);
    }
}