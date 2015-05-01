﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConverterFacts.cs" company="Appccelerate">
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

namespace MSpec2xBehaveConverter.Facts
{
    using ApprovalTests;

    using FluentAssertions;

    using Xunit;

    public class ConverterFacts
    {
        private readonly Converter testee;

        public ConverterFacts()
        {
            this.testee = new Converter();
        }

        [Fact]
        public void Converts()
        {
            string result = this.testee.Convert(Scenarios.Content);

            Approvals.Verify(result);
        }

        public class Scenarios
        {
            public const string Content =
    @"//-------------------------------------------------------------------------------
// <copyright file=""when_the_bootstrapper_is_run.cs"" company=""Appccelerate"">
//   Copyright (c) 2008-2015
//
//   Licensed under the Apache License, Version 2.0 (the ""License"");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an ""AS IS"" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
// </copyright>
//-------------------------------------------------------------------------------

namespace Appccelerate.Bootstrapper
{
    using System.Collections.Generic;
    using System.Linq;
    using Appccelerate.Bootstrapper.Dummies;
    using FluentAssertions;
    using Machine.Specifications;

    [Subject(""Bootstrapping with thingies"")]
    public class When_the_bootstrapper_is_run
    {
        private static CustomExtensionStrategy Strategy;

        protected static CustomExtensionBase First;

        private static CustomExtensionBase Second;

        private static IBootstrapper<ICustomExtension> Bootstrapper;

        private static Queue<string> SequenceQueue;

        Establish context = () =>
            {
                SequenceQueue = new Queue<string>();

                Bootstrapper = new DefaultBootstrapper<ICustomExtension>();

                Strategy = new CustomExtensionStrategy(SequenceQueue);
                First = new FirstExtension(SequenceQueue);
                Second = new SecondExtension(SequenceQueue);

                Bootstrapper.Initialize(Strategy);
                Bootstrapper.AddExtension(First);
                Bootstrapper.AddExtension(Second);
            };

        Because of = () =>
            {
                Bootstrapper.Run();
            };

        It should_only_initialize_contexts_once_for_all_extensions = () =>
            {
                Strategy.RunConfigurationInitializerAccessCounter.Should().Be(1);
            };

        It should_pass_the_initialized_values_from_the_contexts_to_the_extensions = () =>
            {
                var expected = new Dictionary<string, string>
                    {
                        { ""RunTest"", ""RunTestValue"" }
                    };

                First.RunConfiguration.Should().Equal(expected);
                Second.RunConfiguration.Should().Equal(expected);

                First.Registered.Should().Be(""RunTest"");
                Second.Registered.Should().Be(""RunTest"");
            };

        It should_execute_the_extensions_and_the_extension_points_according_to_the_strategy_defined_order = () =>
            {
                var sequence = SequenceQueue;

                sequence.Should().HaveCount(9, sequence.Flatten());
                sequence.ElementAt(0).Should().BeEquivalentTo(""Action: CustomRun"");

                sequence.ElementAt(1).Should().BeEquivalentTo(""FirstExtension: Start"");
                sequence.ElementAt(2).Should().BeEquivalentTo(""SecondExtension: Start"");

                sequence.ElementAt(3).Should().BeEquivalentTo(""FirstExtension: Configure"");
                sequence.ElementAt(4).Should().BeEquivalentTo(""SecondExtension: Configure"");

                sequence.ElementAt(5).Should().BeEquivalentTo(""FirstExtension: Initialize"");
                sequence.ElementAt(6).Should().BeEquivalentTo(""SecondExtension: Initialize"");

                sequence.ElementAt(7).Should().BeEquivalentTo(""FirstExtension: Register"");
                sequence.ElementAt(8).Should().BeEquivalentTo(""SecondExtension: Register"");
            };
    }
}";
        }
    }
}