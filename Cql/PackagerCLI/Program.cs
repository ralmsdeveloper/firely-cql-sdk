﻿
using Hl7.Cql.Firely;
using Hl7.Cql;
using Hl7.Cql.CodeGeneration.NET;
using Hl7.Cql.Compiler;
using Hl7.Cql.Graph;
using Hl7.Cql.Model;
using Hl7.Cql.Packaging;
using Hl7.Cql.Primitives;
using Hl7.Cql.Runtime;
using Hl7.Cql.ValueSets;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

public static class Program
{
    public static int Main(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddCommandLine(args)
            .Build();
        if (args.Length == 0 || config["?"] != null || config["h"] != null || config["help"] != null)
            return ShowHelp();

        var elmArg = config["elm"];
        if (elmArg == null)
            return ShowHelp();
        var elmDir = new DirectoryInfo(elmArg);
        if (!elmDir.Exists)
        {
            Console.Error.WriteLine($"-elm: path {elmArg} does not exist.");
            return -1;
        }
        var cqlArg = config["cql"];

        if (cqlArg == null)
            return ShowHelp();
        var cqlDir = new DirectoryInfo(cqlArg);
        if (!cqlDir.Exists)
        {
            Console.Error.WriteLine($"-cql: path {cqlArg} does not exist.");
            return -1;
        }

        var oArg = config["o"];
        if (oArg == null)
            return ShowHelp();
        var oDir = new DirectoryInfo(oArg);
        if (!oDir.Exists)
        {
            EnsureDirectory(oDir);
        }

        var dArg = config["d"];
        bool debug = false;
        if (dArg != null && !bool.TryParse(dArg, out debug))
        {
            Console.Error.WriteLine($"-d: expected true|false, got {dArg}");
            return -1;
        }

        var csArg = config["cs"];
        var csDir = new DirectoryInfo(csArg);
        if (!csDir.Exists)
        {
            EnsureDirectory(oDir);
        }

        var fArg = config["f"];
        bool force = false;
        if (fArg != null && !bool.TryParse(fArg, out force))
        {
            Console.Error.WriteLine($"-f: expected true|false, got {fArg}");
            return -1;
        }

        var logLevel = LogLevel.Trace;
        var logFactory = LoggerFactory
            .Create(logging =>
            {
                logging.AddFilter(level => level >= logLevel);
                logging.AddConsole(console =>
                {
                    console.LogToStandardErrorThreshold = LogLevel.Error;
                });
                var logFile = Path.Combine(oDir.FullName, "build.txt");
                Log.Logger = new LoggerConfiguration()
                  .Enrich.FromLogContext()
                  .WriteTo
                  .File(logFile)
                  .CreateLogger();
                logging.AddSerilog();
            });
        var packager = new LibraryPackager();
        var packagerLogger = logFactory.CreateLogger<LibraryPackager>();
        var packages = packager.LoadPackages(elmDir);
        var graph = Hl7.Cql.Elm.ElmPackage.GetIncludedLibraries(packages.Values);
        var typeResolver = new FirelyTypeResolver(Models.Fhir401);
        var builderLogger = logFactory.CreateLogger<ExpressionBuilder>();

        var writerLogger = logFactory.CreateLogger<CSharpSourceCodeWriter>();

        var resources = packager.PackageResources(elmDir,
            cqlDir,
            graph,
            typeResolver,
            new CqlOperatorsBinding(typeResolver, FirelyTypeConverter.Default),
            new TypeManager(typeResolver),
            CanonicalUri,
            builderLogger,
            writerLogger);

        var options = new JsonSerializerOptions()
            .ForFhir(typeof(Resource).Assembly)
            .Pretty();
        foreach (var resource in resources)
        {
            var file = new FileInfo(Path.Combine(oDir.FullName, $"{resource.Id}.json"));
            using var fs = new FileStream(file.FullName, FileMode.Create, FileAccess.Write, FileShare.Read);
            JsonSerializer.Serialize(fs, resource, options);
        }
        if (csDir != null)
        {
            // Write out the C# source code to the desired output location
            foreach (var resource in resources)
            {
                if (resource is Binary binary)
                {
                    if (binary.ContentType == "text/plain")
                    {
                        var bytes = binary.Data;
                        var sourceFilePath = binary.Id.StartsWith("Tuple_")
                            ? Path.Combine(csDir.FullName, "Tuples", $"{binary.Id}.cs")
                            : Path.Combine(csDir.FullName, $"{binary.Id}.cs");
                        File.WriteAllBytes(sourceFilePath, bytes);
                    }
                }
                else if (resource is Library library && library.Content != null)
                {
                    var textPlain = library.Content
                        .SingleOrDefault(c => c.ContentType == "text/plain");
                    if (textPlain != null)
                    {
                        var bytes = textPlain.Data;
                        var sourceFilePath = Path.Combine(csDir.FullName, $"{library.Id}.cs");
                        File.WriteAllBytes(sourceFilePath, bytes);
                    }
                }
            }
        }
        return 0;
    }
    static string CanonicalUri(Resource resource)
    {
        if (string.IsNullOrWhiteSpace(resource.Id))
            throw new ArgumentException("Resource must have an id", nameof(resource));
        var path = $"#/{resource.TypeName}/{resource.Id}";
        return path;
    }


    static void EnsureDirectory(DirectoryInfo directory, int timeoutMs = 5000)
    {
        var now = DateTime.Now;
        var loop = true;
        var timeout = TimeSpan.FromMilliseconds(timeoutMs);
        while (!directory.Exists && loop)
        {
            directory.Create();
            directory.Refresh();
            if (DateTime.Now.Subtract(now) > timeout)
                throw new InvalidOperationException($"Unable to create directory {directory.FullName} after {timeout}");
        }
    }

    static int ShowHelp()
    {
        Console.WriteLine();
        Console.WriteLine("Measure Packager");
        Console.WriteLine();
        Console.WriteLine($"\t--elm <directory>\tLibrary root path");
        Console.WriteLine($"\t--cql <directory>\tCQL root path");
        Console.WriteLine($"\t--o <file>\t\tOutput location, either file name or directory");
        Console.WriteLine($"\t[--cs] <file>\tC# output location, either file name or directory");
        Console.WriteLine($"\t[--d] true|false\t\tProduce as a debug assmebly");
        Console.WriteLine($"\t[--f] true|false\tIf output file already exists, overwrite");
        Console.WriteLine();
        return -1;
    }
}