// Copyright (c) Nate McMaster.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils.Abstractions;
using McMaster.Extensions.CommandLineUtils.Conventions;
using McMaster.Extensions.CommandLineUtils.HelpText;
using McMaster.Extensions.CommandLineUtils.Validation;

namespace McMaster.Extensions.CommandLineUtils
{
    /// <summary>
    /// Describes a set of command line arguments, options, and execution behavior.
    /// <see cref="CommandLineApplication"/> can be nested to support subcommands.
    /// </summary>
    public interface ICommandLineApplication : IServiceProvider, IDisposable
    {
        /// <summary>
        /// Defaults to null. A link to the parent command if this is instance is a subcommand.
        /// </summary>
        CommandLineApplication? Parent { get; set; }

        /// <summary>
        /// The help text generator to use.
        /// </summary>
        IHelpTextGenerator HelpTextGenerator { get; set; }

        /// <summary>
        /// Configures the parser.
        /// </summary>
        ParserConfig ParserConfig { get; set; }

        /// <summary>
        /// The short name of the command. When this is a subcommand, it is the name of the word used to invoke the subcommand.
        /// </summary>
        string? Name { get; set; }

        /// <summary>
        /// The full name of the command to show in the help text.
        /// </summary>
        string? FullName { get; set; }

        /// <summary>
        /// A description of the command.
        /// </summary>
        string? Description { get; set; }

        /// <summary>
        /// Determines if this command appears in generated help text.
        /// </summary>
        bool ShowInHelpText { get; set; }

        /// <summary>
        /// Additional text that appears at the bottom of generated help text.
        /// </summary>
        string? ExtendedHelpText { get; set; }

        /// <summary>
        /// Available command-line options on this command. Use <see cref="GetOptions"/> to get all available options, which may include inherited options.
        /// </summary>
        List<CommandOption> Options { get; }

        /// <summary>
        /// Whether a Pager should be used to display help text.
        /// </summary>
        bool UsePagerForHelpText { get; set; }

        /// <summary>
        /// All names by which the command can be referenced. This includes <see cref="Name"/> and an aliases added in <see cref="AddName"/>.
        /// </summary>
        IEnumerable<string> Names { get; }

        /// <summary>
        /// The option used to determine if help text should be displayed. This is set by calling <see cref="HelpOption(string)"/>.
        /// </summary>
        CommandOption? OptionHelp { get; }

        /// <summary>
        /// The options used to determine if the command version should be displayed. This is set by calling <see cref="VersionOption(string, Func{string}, Func{string})"/>.
        /// </summary>
        CommandOption? OptionVersion { get; }

        /// <summary>
        /// Required command-line arguments.
        /// </summary>
        List<CommandArgument> Arguments { get; }

        /// <summary>
        /// When initialized with <see cref="ThrowOnUnexpectedArgument"/> to <c>false</c>, this will contain any unrecognized arguments.
        /// </summary>
        List<string> RemainingArguments { get; }

        /// <summary>
        /// Indicates whether the parser should throw an exception when it runs into an unexpected argument.
        /// If this field is set to false, the parser will stop parsing when it sees an unexpected argument, and all
        /// remaining arguments, including the first unexpected argument, will be stored in RemainingArguments property.
        /// </summary>
        bool ThrowOnUnexpectedArgument { get; set; }

        /// <summary>
        /// True when <see cref="OptionHelp"/> or <see cref="OptionVersion"/> was matched.
        /// </summary>
        bool IsShowingInformation { get; }

        /// <summary>
        /// <para>
        /// This property has been marked as obsolete and will be removed in a future version.
        /// The recommended replacement for setting this property is <see cref="OnExecute(Func{int})" />
        /// and for invoking this property is <see cref="Execute(string[])" />.
        /// See https://github.com/natemcmaster/CommandLineUtils/issues/275 for details.
        /// </para>
        /// <para>
        /// The action to call when this command is matched and <see cref="IsShowingInformation"/> is <c>false</c>.
        /// </para>
        /// </summary>
        Func<int> Invoke { get; set; }

        /// <summary>
        /// The long-form of the version to display in generated help text.
        /// </summary>
        Func<string?>? LongVersionGetter { get; set; }

        /// <summary>
        /// The short-form of the version to display in generated help text.
        /// </summary>
        Func<string?>? ShortVersionGetter { get; set; }

        /// <summary>
        /// Subcommands.
        /// </summary>
        List<CommandLineApplication> Commands { get; }

        /// <summary>
        /// Determines if '--' can be used to separate known arguments and options from additional content passed to <see cref="RemainingArguments"/>.
        /// </summary>
        bool AllowArgumentSeparator { get; set; }

        /// <summary>
        /// <para>
        /// When enabled, the parser will treat any arguments beginning with '@' as a file path to a response file.
        /// A response file contains additional arguments that will be treated as if they were passed in on the command line.
        /// </para>
        /// <para>
        /// Defaults to <see cref="ResponseFileHandling.Disabled" />.
        /// </para>
        /// <para>
        /// Nested response false are not supported.
        /// </para>
        /// </summary>
        ResponseFileHandling ResponseFileHandling { get; set; }

        /// <summary>
        /// The way arguments and options are matched.
        /// </summary>
        StringComparison OptionsComparison { get; set; }

        /// <summary>
        /// <para>
        /// One or more options of <see cref="CommandOptionType.NoValue"/>, followed by at most one option that takes values, should be accepted when grouped behind one '-' delimiter.
        /// </para>
        /// <para>
        /// When true, the following are equivalent.
        ///
        /// <code>
        /// -abcXyellow
        /// -abcX=yellow
        /// -abcX:yellow
        /// -abc -X=yellow
        /// -ab -cX=yellow
        /// -a -b -c -Xyellow
        /// -a -b -c -X yellow
        /// -a -b -c -X=yellow
        /// -a -b -c -X:yellow
        /// </code>
        /// </para>
        /// <para>
        /// This defaults to true unless an option with a short name of two or more characters is added.
        /// </para>
        /// </summary>
        /// <remarks>
        /// <seealso href="https://www.gnu.org/software/libc/manual/html_node/Argument-Syntax.html"/>
        /// </remarks>
        bool ClusterOptions
        {
            // unless explicitly set, use the value of cluster options from the parent command
            // or default to true if this is the root command
            get;
            set;
        }

        /// <summary>
        /// Gets the default value parser provider.
        /// <para>
        /// The value parsers control how argument values are converted from strings to other types. Additional value
        /// parsers can be added so that domain specific types can converted. In-built value parsers can also be replaced
        /// for precise control of all type conversion.
        /// </para>
        /// <remarks>
        /// Value parsers are currently only used by the Attribute API.
        /// </remarks>
        /// </summary>
        ValueParserProvider ValueParsers { get; }

        /// <summary>
        /// <para>
        /// Defines the working directory of the application. Defaults to <see cref="Directory.GetCurrentDirectory"/>.
        /// </para>
        /// <para>
        /// This will be used as the base path for opening response files when <see cref="ResponseFileHandling"/> is <c>true</c>.
        /// </para>
        /// </summary>
        string WorkingDirectory { get; }

        /// <summary>
        /// The writer used to display generated help text.
        /// </summary>
        TextWriter Out { get; set; }

        /// <summary>
        /// The writer used to display generated error messages.
        /// </summary>
        TextWriter Error { get; set; }

        /// <summary>
        /// When an invalid argument is given, make suggestions in the error message
        /// about similar, valid commands or options.
        /// <para>
        /// $ git pshu
        /// Specify --help for a list of available options and commands
        /// Unrecognized command or argument 'pshu'
        ///
        /// Did you mean this?
        ///     push
        /// </para>
        /// </summary>
        bool MakeSuggestionsInErrorMessage { get; set; }

        /// <summary>
        /// Gets a builder that can be used to apply conventions to
        /// </summary>
        IConventionBuilder Conventions { get; }

        /// <summary>
        /// The action to call when the command executes, but there was an error validation options or arguments.
        /// The action can return a new validation result.
        /// </summary>
        Func<ValidationResult, int> ValidationErrorHandler { get; set; }

        /// <summary>
        /// A collection of validators that execute before invoking <see cref="OnExecute(Func{int})"/>.
        /// When validation fails, <see cref="ValidationErrorHandler"/> is invoked.
        /// </summary>
        ICollection<ICommandValidator> Validators { get; }

        /// <summary>
        /// Gets all command line options available to this command, including any inherited options.
        /// </summary>
        /// <returns>Command line options.</returns>
        IEnumerable<CommandOption> GetOptions();

        /// <summary>
        /// Add another name for the command.
        /// <para>
        /// Additional names can be shorter, longer, or alternative names by which a command may be invoked on the command line.
        /// </para>
        /// </summary>
        /// <param name="name">The name. Must not be null or empty.</param>
        void AddName(string name);

        /// <summary>
        /// Add a subcommand
        /// </summary>
        /// <param name="subcommand"></param>
        void AddSubcommand(CommandLineApplication subcommand);

#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        /// <summary>
        /// Adds a subcommand.
        /// </summary>
        /// <param name="name">The word used to invoke the subcommand.</param>
        /// <param name="configuration"></param>
        /// <param name="throwOnUnexpectedArg"></param>
        /// <returns></returns>
        CommandLineApplication Command(string name, Action<CommandLineApplication> configuration,
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
            bool throwOnUnexpectedArg = true);

#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        /// <summary>
        /// Adds a subcommand with model of type <typeparamref name="TModel" />.
        /// </summary>
        /// <param name="name">The word used to invoke the subcommand.</param>
        /// <param name="configuration"></param>
        /// <param name="throwOnUnexpectedArg"></param>
        /// <typeparam name="TModel">The model type of the subcommand.</typeparam>
        /// <returns></returns>
        CommandLineApplication<TModel> Command<TModel>(string name, Action<CommandLineApplication<TModel>> configuration,
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
            bool throwOnUnexpectedArg = true)
            where TModel : class;

        /// <summary>
        /// Adds a command-line option.
        /// </summary>
        /// <param name="template"></param>
        /// <param name="description"></param>
        /// <param name="optionType"></param>
        /// <returns></returns>
        CommandOption Option(string template, string description, CommandOptionType optionType);

        /// <summary>
        /// Adds a command-line option.
        /// </summary>
        /// <param name="template"></param>
        /// <param name="description"></param>
        /// <param name="optionType"></param>
        /// <param name="inherited"></param>
        /// <returns></returns>
        CommandOption Option(string template, string description, CommandOptionType optionType, bool inherited);

        /// <summary>
        /// Adds a command-line option.
        /// </summary>
        /// <param name="template"></param>
        /// <param name="description"></param>
        /// <param name="optionType"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        CommandOption Option(string template, string description, CommandOptionType optionType, Action<CommandOption> configuration);

        /// <summary>
        /// Adds a command line option.
        /// </summary>
        /// <param name="template"></param>
        /// <param name="description"></param>
        /// <param name="optionType"></param>
        /// <param name="configuration"></param>
        /// <param name="inherited"></param>
        /// <returns></returns>
        CommandOption Option(string template, string description, CommandOptionType optionType, Action<CommandOption> configuration, bool inherited);

        /// <summary>
        /// Adds a command line option with values that should be parsable into <typeparamref name="T" />.
        /// </summary>
        /// <param name="template"></param>
        /// <param name="description"></param>
        /// <param name="optionType"></param>
        /// <param name="configuration"></param>
        /// <param name="inherited"></param>
        /// <typeparam name="T">The type of the values on the option</typeparam>
        /// <returns>The option</returns>
        CommandOption<T> Option<T>(string template, string description, CommandOptionType optionType, Action<CommandOption> configuration, bool inherited);

#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        /// <summary>
        /// Adds a command line argument
        /// </summary>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="multipleValues"></param>
        /// <returns></returns>
        CommandArgument Argument(string name, string description, bool multipleValues = false)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
            ;

#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        /// <summary>
        /// Adds a command line argument.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="configuration"></param>
        /// <param name="multipleValues"></param>
        /// <returns></returns>
        CommandArgument Argument(string name, string description, Action<CommandArgument> configuration, bool multipleValues = false)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
            ;

#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        /// <summary>
        /// Adds a command line argument with values that should be parsable into <typeparamref name="T" />.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="configuration"></param>
        /// <param name="multipleValues"></param>
        /// <typeparam name="T">The type of the values on the option</typeparam>
        /// <returns></returns>
        CommandArgument<T> Argument<T>(string name, string description, Action<CommandArgument> configuration, bool multipleValues = false)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
            ;

        /// <summary>
        /// Defines a callback for when this command is invoked.
        /// </summary>
        /// <param name="invoke"></param>
        void OnExecute(Func<int> invoke);

        /// <summary>
        /// <para>
        /// This method is obsolete and will be removed in a future version.
        /// The recommended alternative is <see cref="CommandLineApplication.OnExecuteAsync" />.
        /// See https://github.com/natemcmaster/CommandLineUtils/issues/275 for details.
        /// </para>
        /// <para>
        /// Defines an asynchronous callback.
        /// </para>
        /// </summary>
        /// <param name="invoke"></param>
        void OnExecute(Func<Task<int>> invoke);

        /// <summary>
        /// Defines an asynchronous callback.
        /// </summary>
        /// <param name="invoke"></param>
        void OnExecuteAsync(Func<CancellationToken, Task<int>> invoke);

        /// <summary>
        /// Adds an action to be invoked when all command line arguments have been parsed and validated.
        /// </summary>
        /// <param name="action">The action to be invoked</param>
        void OnParsingComplete(Action<ParseResult> action);

        /// <summary>
        /// Parses an array of strings, matching them against <see cref="CommandLineApplication.Options"/>, <see cref="CommandLineApplication.Arguments"/>, and <see cref="CommandLineApplication.Commands"/>.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>The result of parsing.</returns>
        ParseResult Parse(params string[] args);

        /// <summary>
        /// Parses an array of strings using <see cref="CommandLineApplication.Parse"/>.
        /// <para>
        /// If <see cref="CommandLineApplication.OptionHelp"/> was matched, the generated help text is displayed in command line output.
        /// </para>
        /// <para>
        /// If <see cref="CommandLineApplication.OptionVersion"/> was matched, the generated version info is displayed in command line output.
        /// </para>
        /// <para>
        /// If there were any validation errors produced from <see cref="CommandLineApplication.GetValidationResult"/>, <see cref="CommandLineApplication.ValidationErrorHandler"/> is invoked.
        /// </para>
        /// <para>
        /// If the parse result matches this command, <see cref="CommandLineApplication.Invoke"/> will be invoked.
        /// </para>
        /// </summary>
        /// <param name="args"></param>
        /// <returns>The return code from <see cref="CommandLineApplication.Invoke"/>.</returns>
        int Execute(params string[] args);

        /// <summary>
        /// Parses an array of strings using <see cref="CommandLineApplication.Parse"/>.
        /// <para>
        /// If <see cref="CommandLineApplication.OptionHelp"/> was matched, the generated help text is displayed in command line output.
        /// </para>
        /// <para>
        /// If <see cref="CommandLineApplication.OptionVersion"/> was matched, the generated version info is displayed in command line output.
        /// </para>
        /// <para>
        /// If there were any validation errors produced from <see cref="CommandLineApplication.GetValidationResult"/>, <see cref="CommandLineApplication.ValidationErrorHandler"/> is invoked.
        /// </para>
        /// <para>
        /// If the parse result matches this command, <see cref="CommandLineApplication.Invoke"/> will be invoked.
        /// </para>
        /// </summary>
        /// <param name="args"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>The return code from <see cref="CommandLineApplication.Invoke"/>.</returns>
        Task<int> ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
            ;

        /// <summary>
        /// Helper method that adds a help option.
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        CommandOption HelpOption(string template);

        /// <summary>
        /// Helper method that adds a help option.
        /// </summary>
        /// <param name="template"></param>
        /// <param name="inherited"></param>
        /// <returns></returns>
        CommandOption HelpOption(string template, bool inherited);

#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        /// <summary>
        /// Helper method that adds a version option from known versions strings.
        /// </summary>
        /// <param name="template"></param>
        /// <param name="shortFormVersion"></param>
        /// <param name="longFormVersion"></param>
        /// <returns></returns>
        CommandOption VersionOption(string template,
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
            string? shortFormVersion,
            string? longFormVersion = null);

#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        /// <summary>
        /// Helper method that adds a version option.
        /// </summary>
        /// <param name="template"></param>
        /// <param name="shortFormVersionGetter"></param>
        /// <param name="longFormVersionGetter"></param>
        /// <returns></returns>
        CommandOption VersionOption(string template,
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
            Func<string?>? shortFormVersionGetter,
            Func<string?>? longFormVersionGetter = null);

        /// <summary>
        /// Show short hint that reminds users to use help option.
        /// </summary>
        void ShowHint();

        /// <summary>
        /// Show full help.
        /// </summary>
        void ShowHelp();

        /// <summary>
        /// Show full help.
        /// </summary>
        /// <param name="usePager">Use a console pager to display help text, if possible.</param>
        void ShowHelp(bool usePager);

        /// <summary>
        /// This method has been marked as obsolete and will be removed in a future version.
        /// The recommended replacement is <see cref="CommandLineApplication.ShowHelp()" />.
        /// </summary>
        /// <param name="commandName">The subcommand for which to show help. Leave null to show for the current command.</param>
#pragma warning disable RS0027 // Public API with optional parameter(s) should have the most parameters amongst its public overloads.
        void ShowHelp(string? commandName = null)
#pragma warning restore RS0027 // Public API with optional parameter(s) should have the most parameters amongst its public overloads.
            ;

        /// <summary>
        /// Produces help text describing command usage.
        /// </summary>
        /// <returns>The help text.</returns>
        string GetHelpText();

        /// <summary>
        /// This method has been marked as obsolete and will be removed in a future version.
        /// The recommended replacement is <see cref="CommandLineApplication.GetHelpText()" />
        /// </summary>
        /// <param name="commandName"></param>
        /// <returns></returns>
        string GetHelpText(string? commandName = null);

        /// <summary>
        /// Displays version information that includes <see cref="CommandLineApplication.FullName"/> and <see cref="CommandLineApplication.LongVersionGetter"/>.
        /// </summary>
        void ShowVersion();

        /// <summary>
        /// Produces text describing version of the command.
        /// </summary>
        /// <returns>The version text.</returns>
        string GetVersionText();

        /// <summary>
        /// Gets <see cref="CommandLineApplication.FullName"/> and <see cref="CommandLineApplication.ShortVersionGetter"/>.
        /// </summary>
        /// <returns></returns>
        string GetFullNameAndVersion();

        /// <summary>
        /// Traverses up <see cref="CommandLineApplication.Parent"/> and displays the result of <see cref="CommandLineApplication.GetFullNameAndVersion"/>.
        /// </summary>
        void ShowRootCommandFullNameAndVersion();

        /// <summary>
        /// Validates arguments and options.
        /// </summary>
        /// <returns>The first validation result that is not <see cref="ValidationResult.Success"/> if there is an error.</returns>
        ValidationResult GetValidationResult();
    }
}
