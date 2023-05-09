﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ncqa.Cql.ValueSetLoaders;
using Ncqa.Fhir.R4;
using Ncqa.Fhir.R4.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CoreTests
{
    [TestClass]
    public class ValueSetTests
    {
        [TestMethod]
        public void Intensional_Value_Set()
        {
            var files = new[]
            {
                @"Input\ValueSets\intensional-value-set.json",
                @"Input\ValueSets\2.16.840.1.113883.3.464.1004.1009.json",
                @"Input\ValueSets\2.16.840.1.113883.3.464.1004.1013.json",
            };
            var valueSets = files.Select(path =>
            {
                using var fs = File.OpenRead(path);
                var vs = FhirJson.Deserialize<ValueSet>(fs);
                return vs;
            }).ToArray();

            var loader = new FhirR4ValueSetLoader(valueSets);
            var vsd = loader.Load();

            Assert.IsTrue(vsd.TryGetCodesInValueSet("https://www.ncqa.org/fhir/valueset/2.16.840.1.113883.3.464.1004.1009", out var codes1009));
            Assert.IsTrue(vsd.TryGetCodesInValueSet("https://www.ncqa.org/fhir/valueset/2.16.840.1.113883.3.464.1004.1013", out var codes1013));
            Assert.IsTrue(vsd.TryGetCodesInValueSet("https://www.ncqa.org/fhir/valueset/intensional-value-set", out var intensional));

            Assert.IsTrue(codes1009.All(c => intensional.Contains(c)));
            Assert.IsTrue(codes1013.All(c => intensional.Contains(c)));
        }

        [TestMethod]
        public void Intensional_Value_Set_2_Levels()
        {
            var files = new[]
            {
                @"Input\ValueSets\intensional-value-set-2.json",
                @"Input\ValueSets\intensional-value-set.json",
                @"Input\ValueSets\2.16.840.1.113883.3.464.1004.1009.json",
                @"Input\ValueSets\2.16.840.1.113883.3.464.1004.1013.json",
            };
            var valueSets = files.Select(path =>
            {
                using var fs = File.OpenRead(path);
                var vs = FhirJson.Deserialize<ValueSet>(fs);
                return vs;
            }).ToArray();

            var loader = new FhirR4ValueSetLoader(valueSets);
            var vsd = loader.Load();

            Assert.IsTrue(vsd.TryGetCodesInValueSet("https://www.ncqa.org/fhir/valueset/2.16.840.1.113883.3.464.1004.1009", out var codes1009));
            Assert.IsTrue(vsd.TryGetCodesInValueSet("https://www.ncqa.org/fhir/valueset/2.16.840.1.113883.3.464.1004.1013", out var codes1013));
            Assert.IsTrue(vsd.TryGetCodesInValueSet("https://www.ncqa.org/fhir/valueset/intensional-value-set", out var intensional));
            Assert.IsTrue(codes1009.All(c => intensional.Contains(c)));
            Assert.IsTrue(codes1013.All(c => intensional.Contains(c)));

            Assert.IsTrue(vsd.TryGetCodesInValueSet("https://www.ncqa.org/fhir/valueset/intensional-value-set-2", out var intensional2));
            Assert.IsTrue(codes1009.All(c => intensional2.Contains(c)));
            Assert.IsTrue(codes1013.All(c => intensional2.Contains(c)));

        }

        [TestMethod]
        public void Intensional_Value_Set_Cycle() 
        {
            var files = new[]
{
                @"Input\ValueSets\intensional-value-set-3.json",
                @"Input\ValueSets\intensional-value-set-4.json",

            };
            var valueSets = files.Select(path =>
            {
                using var fs = File.OpenRead(path);
                var vs = FhirJson.Deserialize<ValueSet>(fs);
                return vs;
            }).ToArray();

            var loader = new FhirR4ValueSetLoader(valueSets);

            Assert.ThrowsException<InvalidOperationException>(() => loader.Load());
        }
    }
}
