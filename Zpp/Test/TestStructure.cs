using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Zpp.Test.WrappersForPrimitives;
using Xunit;

namespace Zpp.Test
{
    /**
     * Tests testable rules that ensures a good structure e.g. ObjectCalisthenics
     */
    public class TestStructure
    {
        const string
            skipThisTestClass =
                "ObjectCalisthenicsTest is currenctly disabled."; // TODO: change to null to enable this class

        [Fact(Skip = skipThisTestClass)]
        /**
         * Rule no. 2: Don't use else-keyword
         */
        public void TestNoElse()
        {
            traverseAllCsharpFilesAndExecute((line, lineNumber, fileName) =>
            {
                Assert.False(line.GetValue().Contains("else"),
                    $"{fileName}:{lineNumber} contains an 'else'.");
            });
        }

        [Fact(Skip = skipThisTestClass)]
        /**
         * Rule no. 4: A line should not have more than two "." (access operator)
         */
        public void TestNoMoreThanTwoPoints()
        {
            string[] exceptions =
            {
                "NLog"
            };

            var regex = new Regex("(?!using)(^.*\\..*\\..*\\..*$)");

            traverseAllCsharpFilesAndExecute((line, lineNumber, fileName) =>
            {
                // skip if line contains a string mention in exceptions
                foreach (var exception in exceptions)
                {
                    if (line.GetValue().Contains(exception))
                    {
                        return;
                    }
                }

                Assert.False(regex.IsMatch(line.GetValue()),
                    $"{fileName}:{lineNumber} contains more than two '.' (Access-Operators).");
            });
        }

        [Fact(Skip = skipThisTestClass)]
        /**
         * Rule no. 6: Keep Entites small (not more than x lines, x classes per package
         * --> with some tolerance)
         */
        public void testKeepEntitiesSmall()
        {
            int maxLines = 100;
            int maxClassesPerPackage = 7;

            // check no more than maxClassesPerPackage
            traverseAllCsharpDirectoriesAndExecute((directoryInfo) =>
            {
                int fileCountInDirectory = 0;
                foreach (var file in directoryInfo.GetFiles())
                {
                    if (file.Extension.Equals(".cs"))
                    {
                        fileCountInDirectory++;
                    }
                }

                Assert.False(fileCountInDirectory > maxClassesPerPackage,
                    $"{directoryInfo.Name}: has more than {maxClassesPerPackage} classes.");
            });

            // check maxLines in every csharpFile
            traverseAllCsharpFilesAndExecute((lines, fileName) =>
            {
                Assert.False(lines.Count() > maxLines,
                    $"{fileName}: has more than {maxLines} lines.");
            });
        }

        /**
         * Rule no. 7
         */
        [Fact]
        public void TestConstructorsHaveMaximumTwoParameters()
        {
            // 3 instead of 2, becaus IDbMasterCache is always passed
            const int MAX_NUMBER_OF_PARAMETERS = 3;

            string[] exceptions =
            {
                "NLog"
            };

            var regexClassNameLine =
                new Regex("^ *public class [A-Z][a-zA-Z0-9_]+ *(: *[A-Z][a-zA-Z0-9_]+)? *$");
            var regexNamespaceNameLine = new Regex("^ *namespace [A-Z][a-zA-Z0-9_.]+ *$");

            string namespaceName = null;
            int classFoundCounter = 0;

            traverseAllCsharpFilesAndExecute((line, lineNumber, fileName) =>
            {
                // skip if line contains a string mention in exceptions
                foreach (var exception in exceptions)
                {
                    if (line.GetValue().Contains(exception))
                    {
                        return;
                    }
                }

                // namespace first
                if (regexNamespaceNameLine.IsMatch(line.GetValue()) == true)
                {
                    // reset classCounter
                    classFoundCounter = 0;
                    
                    // e.g. " namespace Zpp.Test "
                    string namespaceNameLine = regexNamespaceNameLine.Match(line.GetValue()).Value;
                    string[] namespaceNameLineSplitted = namespaceNameLine.Trim().Split(" ");
                    namespaceName = namespaceNameLineSplitted[1];
                }

                // then className
                // e.g. " public class TestStructure "
                if (regexClassNameLine.IsMatch(line.GetValue()) == true)
                {
                    // ignore inner classes
                    if (classFoundCounter > 0)
                    {
                        return;
                    }
                    classFoundCounter++;
                    if (namespaceName == null)
                    {
                        Assert.False(true, "Class cannot be defined before a namespace.");
                    }

                    string classNameLine = regexClassNameLine.Match(line.GetValue()).Value;
                    string[] classNameLineSplitted = classNameLine.Trim().Split(" ");
                    string className;
                    if (classNameLineSplitted[2].Contains(":"))
                    {
                        className = classNameLineSplitted[2].Split(":")[0];
                    }
                    else
                    {
                        className = classNameLineSplitted[2];
                    }


                    Type classTypeOfFile = Type.GetType($"{namespaceName}.{className}");

                    Assert.False(classTypeOfFile == null,
                        $"Cannot get Type of class '{namespaceName}.{className}' found in file {fileName}.");

                    ConstructorInfo[] constructorInfos = classTypeOfFile.GetConstructors();
                    foreach (var constructorInfo in constructorInfos)
                    {
                        ParameterInfo[] parameterInfos = constructorInfo.GetParameters();
                        Assert.True(parameterInfos.Length <= MAX_NUMBER_OF_PARAMETERS,
                            $"A constructor of type {classTypeOfFile.Name} " +
                            $"has more than {MAX_NUMBER_OF_PARAMETERS} parameters.");
                    }
                }
            });
        }

        private void traverseAllCsharpFilesAndExecute(Action<Lines, FileName> actionOnFile)
        {
            DirectoryInfo currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
            foreach (string file in Directory.EnumerateFiles(
                currentDirectory.Parent.Parent.Parent.FullName, "*.*", SearchOption.AllDirectories))
            {
                FileInfo fileInfo = new FileInfo(file);
                if (fileInfo.Extension.Equals(".cs"))
                {
                    var lines = File.ReadAllLines(file);
                    actionOnFile(new Lines(lines), new FileName(fileInfo.Name));
                }
            }
        }

        private void traverseAllCsharpFilesAndExecute(
            Action<Line, LineNumber, FileName> actionOnOneLine)
        {
            traverseAllCsharpFilesAndExecute((lines, fileName) =>
            {
                for (var i = 0; i < lines.GetLines().Count; i++)
                {
                    var currentLineNumber = new LineNumber(i);
                    var line = lines.GetLine(currentLineNumber);
                    // Process line
                    actionOnOneLine(line, currentLineNumber, fileName);
                }
            });
        }

        private void traverseAllCsharpDirectoriesAndExecute(Action<DirectoryInfo> actionOnDirectory)
        {
            DirectoryInfo currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
            foreach (string directory in Directory.EnumerateDirectories(
                currentDirectory.Parent.Parent.Parent.FullName, "*.*", SearchOption.AllDirectories))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(directory);
                actionOnDirectory(directoryInfo);
            }
        }
    }
}