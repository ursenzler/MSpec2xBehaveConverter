namespace MSpec2xBehaveConverter
{
    using System;
    using System.Text.RegularExpressions;

    using Appccelerate.IO.Access.Internals;

    public class Converter
    {
        private string content;

        public string Convert(string content)
        {
            this.content = content;

            this.ReplaceUsing();

            this.ReplaceSubjectWithClass();

            return this.content;
        }

        private void ReplaceUsing()
        {
            this.content = this.content.Replace("using Machine.Specifications;", "using Xbehave;");
        }

        private void ReplaceSubjectWithClass()
        {
            int subjectIndex = this.content.IndexOf("[Subject(");

            while(subjectIndex >= 0)
            {
                // subject
                int subjectStart = this.content.IndexOf("(", subjectIndex);
                int subjectEnd = this.content.IndexOf(")", subjectIndex);

                string subject = this.content.Substring(subjectStart + 1, subjectEnd - subjectStart - 2).Trim('"').Replace(" ", "_");

                // scenario
                int scenarioNameStart = this.content.IndexOf("class", subjectIndex) + 6;
                int scenarioNameEnd = this.content.IndexOfAny(new[] { '\n', ' ' }, scenarioNameStart);

                string scenarioName = this.content.Substring(scenarioNameStart, scenarioNameEnd - scenarioNameStart);

                int classStart = this.content.IndexOf("{", subjectIndex);

                // public partial class <subject>
                // [Scenario]
                // public void <scenario>
                string prefix = this.content.Substring(subjectIndex, classStart - subjectIndex);
                this.content = this.content.Replace(
                    prefix, 
                    "public partial class " + subject + "\n    {\n        [Scenario]\n        public void " +
                    scenarioName.TrimEnd() +
                    "()\n        ");

                // private static -> = null
                Regex field = new Regex(@"private static (?<f>[\w<>]+ \w+);");
                this.content = field.Replace(this.content, @"    ${f} = null;");

                // establish
                int establishStart = this.content.IndexOf("Establish ", classStart);
                while (establishStart >= 0)
                {
                    int establishEnd = this.FindClosingBracket(this.FindNextOpeningBracket(establishStart));
                    int x = this.FindNextOpeningBracket(establishStart);
                    string establishAction = this.content.Substring(x, establishEnd - x);
                
                    this.content = this.content.Replace(
                        this.content.Substring(establishStart, establishEnd - establishStart),
                        "    \"establish\"._(() =>\n            " + 
                        establishAction.TrimEnd().TrimEnd(';') + 
                        ")");

                    establishStart = this.content.IndexOf("Establish ", classStart);
                }
                
                // because of
                int becauseStart = this.content.IndexOf("Because ", classStart);
                while (becauseStart >= 0)
                {
                    int becauseEnd = this.FindClosingBracket(this.FindNextOpeningBracket(becauseStart));
                    int y = this.FindNextOpeningBracket(becauseStart);
                    string becauseAction = this.content.Substring(y, becauseEnd - y);

                    this.content = this.content.Replace(
                        this.content.Substring(becauseStart, becauseEnd - becauseStart),
                        "    \"because\"._(() =>\n            " +
                        becauseAction.TrimEnd().TrimEnd(';') +
                        ")");

                    becauseStart = this.content.IndexOf("Because ", classStart);
                }

                // it
                int itStart = this.content.IndexOf("It ", classStart);
                while (itStart > 0)
                {
                    int itEnd = this.FindClosingBracket(this.FindNextOpeningBracket(itStart));
                    int z = this.FindNextOpeningBracket(itStart);
                    string it = this.content.Substring(itStart + 3, this.content.IndexOf(" ", itStart + 4) - itStart - 3);
                    string itAction = this.content.Substring(z, itEnd - z);

                    this.content = this.content.Replace(
                        this.content.Substring(itStart, itEnd - itStart),
                        "    \"it " + it + "\"._(() =>\n            " +
                        itAction.TrimEnd().TrimEnd(';') +
                        ")");

                    itStart = this.content.IndexOf("It ", classStart);
                }

                // closing }
                int classEnd = this.FindClosingBracket(classStart, 1);
                this.content = this.content.Substring(0, classEnd - 1) + "    }\r\n    " + this.content.Substring(classEnd - 1);
                

                // and the next
                subjectIndex = this.content.IndexOf("[Subject(");
            }
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