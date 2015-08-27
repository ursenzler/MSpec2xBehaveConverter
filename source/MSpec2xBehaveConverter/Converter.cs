// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Converter.cs" company="Appccelerate">
//   Copyright (c) 2008-2015
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace MSpec2xBehaveConverter
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;

    public class Converter
    {
        private string content;

        public string Convert(string contentToConvert)
        {
            this.content = contentToConvert;

            this.ReplaceUsing();

            this.ReplaceSubjects();

            return this.content;
        }

        private void ReplaceUsing()
        {
            this.content = this.content.Replace("using Machine.Specifications;", "using Xbehave;");
        }

        private void ReplaceSubjects()
        {
            int subjectIndex = this.FindIndexOf("[Subject(", 0);

            while(subjectIndex >= 0)
            {
                // subject
                int subjectStart = this.FindIndexOf("(", subjectIndex);
                int subjectEnd = this.FindIndexOf(")", subjectIndex);

                string subject = this.content.Substring(subjectStart + 1, subjectEnd - subjectStart - 2).Trim('"').Replace(" ", "_");

                // scenario
                int scenarioNameStart = this.FindIndexOf("class", subjectIndex) + 6;
                int scenarioNameEnd = this.content.IndexOfAny(new[] { '\n', ' ' }, scenarioNameStart);

                string scenarioName = this.content.Substring(scenarioNameStart, scenarioNameEnd - scenarioNameStart);

                int classStart = this.FindIndexOf("{", subjectIndex);
                int classEnd = this.FindClosingBracket(classStart);

                // public partial class <subject>
                // [Scenario]
                // public void <scenario>
                string prefix = this.content.Substring(subjectIndex, classStart - subjectIndex);
                this.content = this.content.Replace(
                    prefix, 
                    "public partial class " + subject + "\r\n    {\r\n        [Scenario]\r\n        public void " +
                    scenarioName.TrimEnd() +
                    "()\r\n        ");

                classStart = this.FindIndexOf("{", subjectIndex);
                classEnd = this.FindClosingBracket(classStart, 1);

                // private static -> = null
                Regex field = new Regex(@"(private |protected )?static (?<f>[\w<>]+ \w+);");
                this.content = field.Replace(this.content, @"    ${f} = null;");

                var noise = new[] { ' ', '\r', '\n', '\t', '(', ')', '=', '>' };

                // establish
                int establishStart = this.FindIndexOf("Establish ", classStart, classEnd);
                while (establishStart >= 0)
                {
                    int lambda = this.FindIndexOf("=>", establishStart, classEnd);

                    while (noise.Contains(this.content[lambda]))
                    {
                        lambda++;
                    }

                    if (this.content[lambda] == '{') // lambda with { }
                    {
                        int establishEnd = this.FindClosingBracket(this.FindNextOpeningBracket(establishStart));
                        int x = this.FindNextOpeningBracket(establishStart);
                        string establishAction = this.content.Substring(x, establishEnd - x);

                        this.content = this.content.Replace(
                            this.content.Substring(establishStart, establishEnd - establishStart),
                            "    \"establish\"._(() =>\r\n            " +
                            establishAction.TrimEnd().TrimEnd(';') +
                            ")");
                    }
                    else
                    {
                        int semicolon = this.FindIndexOf(";", establishStart, classEnd);
                        string establishAction = this.content.Substring(lambda, semicolon - lambda);

                        this.content = this.content.Replace(
                            this.content.Substring(establishStart, semicolon - establishStart),
                            "    \"establish" + "\"._(() =>\r\n            {\r\n        " +
                            this.Indent(establishAction.TrimEnd(), 8) +
                            ";\r\n            })");
                    }

                    classEnd = this.FindClosingBracket(classStart, 1);
                    establishStart = this.FindIndexOf("Establish ", classStart, classEnd);
                }    
            
                // because of
                int becauseStart = this.FindIndexOf("Because ", classStart, classEnd);
                while (becauseStart >= 0)
                {
                    int lambda = this.FindIndexOf("=>", becauseStart, classEnd);

                    while (noise.Contains(this.content[lambda]))
                    {
                        lambda++;
                    }

                    var because = scenarioName.Replace("_", " ").TrimEnd();
                    because = because.Substring(0, 1).ToLowerInvariant() + because.Substring(1);

                    if (this.content[lambda] == '{') // lambda with { }
                    {
                        int becauseEnd = this.FindClosingBracket(this.FindNextOpeningBracket(becauseStart));
                        int y = this.FindNextOpeningBracket(becauseStart);
                        string becauseAction = this.content.Substring(y, becauseEnd - y);

                        this.content = this.content.Replace(
                            this.content.Substring(becauseStart, becauseEnd - becauseStart),
                            "    \"" + because + "\"._(() =>\r\n            " +
                            becauseAction.TrimEnd().TrimEnd(';') +
                            ")");
                    }
                    else // lambda without { }
                    {
                        int semicolon = this.FindIndexOf(";", becauseStart, classEnd);
                        string becauseAction = this.content.Substring(lambda, semicolon - lambda);

                        this.content =
                            this.content.Substring(0, becauseStart)
                            + "    \"" + because + "\"._(() =>\r\n            {\r\n                "
                            + becauseAction.TrimEnd()
                            + ";\r\n            })"
                            + this.content.Substring(semicolon);

                        //this.content = this.content.Replace(
                        //    this.content.Substring(becauseStart, semicolon - becauseStart),
                        //    "    \"" + because + "\"._(() =>\r\n            {\r\n                " +
                        //    becauseAction.TrimEnd() +
                        //    ";\r\n            })");
                    }

                    classEnd = this.FindClosingBracket(classStart, 1);
                    becauseStart = this.FindIndexOf("Because ", classStart, classEnd);
                }
                
                // it
                int itStart = this.FindIndexOf("It ", classStart, classEnd);
                while (itStart > 0)
                {
                    int lambda = this.FindIndexOf("=>", itStart, classEnd);

                    while (noise.Contains(this.content[lambda]))
                    {
                        lambda++;
                    }
                    if (this.content[lambda] == '{') // lambda with { }
                    {
                        int itEnd = this.FindClosingBracket(this.FindNextOpeningBracket(itStart));
                        int z = this.FindNextOpeningBracket(itStart);
                        string it = this.content.Substring(itStart + 3, this.FindIndexOf(" ", itStart + 4, classEnd) - itStart - 3).Replace("_", " ");
                        string itAction = this.content.Substring(z, itEnd - z);

                        this.content = this.content.Replace(
                            this.content.Substring(itStart, itEnd - itStart),
                            "    \"it " + it + "\"._(() =>\r\n            " +
                            itAction.TrimEnd().TrimEnd(';') +
                            ")");
                    }
                    else // lambda without { }
                    {
                        int semicolon = this.FindIndexOf(";", itStart, classEnd);
                        string it = this.content.Substring(itStart + 3, this.FindIndexOf(" ", itStart + 4, classEnd) - itStart - 3).Replace("_", " ");
                        string itAction = this.content.Substring(lambda, semicolon - lambda);

                        this.content = this.content.Replace(
                            this.content.Substring(itStart, semicolon - itStart),
                            "    \"it " + it + "\"._(() =>\r\n            {\r\n                " +
                            itAction.TrimEnd() +
                            ";\r\n            })");
                    }

                    classEnd = this.FindClosingBracket(classStart, 1);
                    itStart = this.FindIndexOf("It ", classStart, classEnd);
                }

                // closing }
                int lastIt = this.FindLastIndexOf("_(() =>", classStart, classEnd);
                int lastItEnd = this.FindClosingBracket(this.FindNextOpeningBracket(lastIt));
                this.content = this.content.Substring(0, lastItEnd + 2) + "\r\n        }" + this.content.Substring(lastItEnd + 2);
                

                // and the next
                subjectIndex = this.FindIndexOf("[Subject(", 0);
            }
        }

        private int FindIndexOf(string search, int start, int end = int.MaxValue)
        {
            var findIndexOf = this.content.IndexOf(search, start, StringComparison.InvariantCulture);

            return findIndexOf <= end ? findIndexOf : -1;
        }

        private int FindLastIndexOf(string search, int start, int end)
        {
            int result = -1;
            
            var findIndexOf = this.content.IndexOf(search, start, StringComparison.InvariantCulture);
            while (findIndexOf < end && findIndexOf >= 0)
            {
                result = findIndexOf;
                findIndexOf = this.content.IndexOf(search, findIndexOf + 1, StringComparison.InvariantCulture);
            }

            return result;
        }

        private int FindNextOpeningBracket(int start)
        {
            while (this.content[start] != '{')
            {
                start++;
            }

            return start;
        }

        private string Indent(string text, int indentation)
        {
            string spaces = new string(' ', indentation);
            var result = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => spaces + line);
            return string.Join(Environment.NewLine, result);
        }

        private int FindClosingBracket(int positionOfOpeningBracket, int numberOfStillOpenBrackets = 0)
        {
            bool stillClimbing = true;
            int openBrackets = 1;
            int closingBracket = positionOfOpeningBracket + 1;
            while (stillClimbing || openBrackets > numberOfStillOpenBrackets)
            {
                switch (this.content[closingBracket])
                {
                    case '{':
                        openBrackets++;
                        break;

                    case '}':
                        openBrackets--;
                        stillClimbing = false;
                        break;
                }

                closingBracket++;
            }

            return closingBracket;
        }
    }
}