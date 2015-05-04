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
            int subjectIndex = this.FindIndexOf("[Subject(");

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

                // public partial class <subject>
                // [Scenario]
                // public void <scenario>
                string prefix = this.content.Substring(subjectIndex, classStart - subjectIndex);
                this.content = this.content.Replace(
                    prefix, 
                    "public partial class " + subject + "\r\n    {\r\n        [Scenario]\r\n        public void " +
                    scenarioName.TrimEnd() +
                    "()\r\n        ");

                // private static -> = null
                Regex field = new Regex(@"(private|protected) static (?<f>[\w<>]+ \w+);");
                this.content = field.Replace(this.content, @"    ${f} = null;");

                // establish
                int establishStart = this.FindIndexOf("Establish ", classStart);
                while (establishStart >= 0)
                {
                    int establishEnd = this.FindClosingBracket(this.FindNextOpeningBracket(establishStart));
                    int x = this.FindNextOpeningBracket(establishStart);
                    string establishAction = this.content.Substring(x, establishEnd - x);
                
                    this.content = this.content.Replace(
                        this.content.Substring(establishStart, establishEnd - establishStart),
                        "    \"establish\"._(() =>\r\n            " + 
                        establishAction.TrimEnd().TrimEnd(';') + 
                        ")");

                    establishStart = this.FindIndexOf("Establish ", classStart);
                }

                var noise = new[] { ' ', '\r', '\n', '\t', '(', ')', '=', '>' };
                
                // because of
                int becauseStart = this.FindIndexOf("Because ", classStart);
                while (becauseStart >= 0)
                {
                    int lambda = this.FindIndexOf("=>", becauseStart);

                    while (noise.Contains(this.content[lambda]))
                    {
                        lambda++;
                    }
                    if (this.content[lambda] == '{') // lambda with { }
                    {
                        int becauseEnd = this.FindClosingBracket(this.FindNextOpeningBracket(becauseStart));
                        int y = this.FindNextOpeningBracket(becauseStart);
                        string becauseAction = this.content.Substring(y, becauseEnd - y);

                        this.content = this.content.Replace(
                            this.content.Substring(becauseStart, becauseEnd - becauseStart),
                            "    \"because\"._(() =>\r\n            " +
                            becauseAction.TrimEnd().TrimEnd(';') +
                            ")");
                    }
                    else // lambda without { }
                    {
                        int semicolon = this.FindIndexOf(";", becauseStart);
                        string becauseAction = this.content.Substring(lambda, semicolon - lambda);

                        this.content = this.content.Replace(
                            this.content.Substring(becauseStart, semicolon - becauseStart),
                            "    \"because\"._(() =>\r\n            {\r\n                " +
                            becauseAction.TrimEnd() +
                            ";\r\n            })");
                    }

                    becauseStart = this.FindIndexOf("Because ", classStart);
                }
                
                // it
                int itStart = this.FindIndexOf("It ", classStart);
                while (itStart > 0)
                {
                    int lambda = this.FindIndexOf("=>", itStart);

                    while (noise.Contains(this.content[lambda]))
                    {
                        lambda++;
                    }
                    if (this.content[lambda] == '{') // lambda with { }
                    {
                        int itEnd = this.FindClosingBracket(this.FindNextOpeningBracket(itStart));
                        int z = this.FindNextOpeningBracket(itStart);
                        string it = this.content.Substring(itStart + 3, this.FindIndexOf(" ", itStart + 4) - itStart - 3).Replace("_", " ");
                        string itAction = this.content.Substring(z, itEnd - z);

                        this.content = this.content.Replace(
                            this.content.Substring(itStart, itEnd - itStart),
                            "    \"it " + it + "\"._(() =>\r\n            " +
                            itAction.TrimEnd().TrimEnd(';') +
                            ")");
                    }
                    else // lambda without { }
                    {
                        int semicolon = this.FindIndexOf(";", itStart);
                        string it = this.content.Substring(itStart + 3, this.FindIndexOf(" ", itStart + 4) - itStart - 3).Replace("_", " ");
                        string itAction = this.content.Substring(lambda, semicolon - lambda);

                        this.content = this.content.Replace(
                            this.content.Substring(itStart, semicolon - itStart),
                            "    \"it " + it + "\"._(() =>\r\n            {\r\n                " +
                            itAction.TrimEnd() +
                            ";\r\n            })");
                    }

                    itStart = this.FindIndexOf("It ", classStart);
                }

                // closing }
                int lastIt = this.content.LastIndexOf("_(() =>");
                int lastItEnd = this.FindClosingBracket(this.FindNextOpeningBracket(lastIt));
                this.content = this.content.Substring(0, lastItEnd + 2) + "\r\n        }" + this.content.Substring(lastItEnd + 2);
                

                // and the next
                subjectIndex = this.FindIndexOf("[Subject(");
            }
        }

        private int FindIndexOf(string search, int start =  0)
        {
            return this.content.IndexOf(search, start, StringComparison.InvariantCulture);
        }

        private int FindNextOpeningBracket(int start)
        {
            while (this.content[start] != '{')
            {
                start++;
            }

            return start;
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