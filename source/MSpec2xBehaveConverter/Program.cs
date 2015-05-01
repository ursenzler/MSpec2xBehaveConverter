namespace MSpec2xBehaveConverter
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Appccelerate.CommandLineParser;
    using Appccelerate.IO;
    using Appccelerate.IO.Access;

    public static class Program
    {
        static void Main(string[] args)
        {

            List<AbsoluteFilePath> paths = null;

            var configuration = CommandLineParserConfigurator.Create()
                .WithPositional(v => paths = v.Split(';').Select(x => new AbsoluteFilePath(Path.GetFullPath(x))).ToList())
                .BuildConfiguration();

            var parser = new CommandLineParser(configuration);

            var result = parser.Parse(args);

            if (!result.Succeeded)
            {
                Console.WriteLine(result.Message);
                return;
            }

            foreach (AbsoluteFilePath path in paths)
            {
                ConvertFile(path);
            }
        }

        private static void ConvertFile(AbsoluteFilePath path)
        {
            var factory = new AccessFactory();

            IFile file = factory.CreateFile();
            string content = file.ReadAllText(path);

            var converter = new Converter();

            string newContent = converter.Convert(content);

            file.WriteAllText(path, newContent);
        }
    }
}
