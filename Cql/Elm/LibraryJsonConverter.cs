﻿using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hl7.Cql.Elm
{
    internal class LibraryJsonConverter : JsonConverter<Library>
    {
        public override Library? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (JsonDocument.TryParseValue(ref reader, out var doc))
            {
                var root = doc.RootElement;
                if (root.TryGetProperty("library", out var libraryElement))
                {
                    var libJson = libraryElement.GetRawText();
                    var converters = options.Converters
                        .Except(options.Converters.OfType<LibraryJsonConverter>())
                        .ToArray();
                    var newOptions = new JsonSerializerOptions
                    {
                        TypeInfoResolver = options.TypeInfoResolver,
                        MaxDepth = options.MaxDepth,
                    };
                    foreach (var converter in converters)
                    {
                        newOptions.Converters.Add(converter);
                    }
                    var lib = JsonSerializer.Deserialize<Library>(libJson, newOptions);
                    return lib;
                }
                else return null;
            }
            else return null;
        }

        public override void Write(Utf8JsonWriter writer, Library value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("library");
            var libJson = JsonSerializer.Serialize<Library>(value);
            writer.WriteRawValue(libJson);
            writer.WriteEndObject();
        }
    }
}
