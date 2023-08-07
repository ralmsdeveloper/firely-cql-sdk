﻿/* 
 * Copyright (c) 2023, NCQA and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/cql-sdk/main/LICENSE
 */

using Cql.Operators;
using Hl7.Cql.Comparers;
using Hl7.Cql.Conversion;
using Hl7.Cql.Firely.Comparers;
using Hl7.Cql.Iso8601;
using Hl7.Cql.Operators;
using Hl7.Cql.Runtime;
using Hl7.Cql.ValueSets;
using Hl7.Fhir.Model;

namespace Hl7.Cql.Firely
{
    /// <summary>
    /// A CqlContext that uses the Firely SDK POCO model.
    /// </summary>
    internal static class FirelyCqlContext
    {
        /// <summary>
        /// Factory method for creating the CqlContext.
        /// </summary>
        public static CqlContext Create(Bundle? bundle = null,
            IDictionary<string, object>? parameters = null,
            IValueSetDictionary? valueSets = null,
            DateTimeOffset? now = null,
            DefinitionDictionary<Delegate>? delegates = null)
        {
            var vss = valueSets ?? new HashValueSetDictionary();
            var unitConverter = new UnitConverter();
            var typeResolver = new FirelyTypeResolver(ModelInfo.ModelInspector);
            IDataRetriever dataRetriever = bundle != null
                ? new BundleDataRetriever(bundle, vss)
                : new CompositeDataRetriever();

            var cqlComparers = new CqlComparers();
            var operators = CqlOperators.Create(typeResolver,
                FirelyTypeConverter.Create(Fhir.Model.ModelInfo.ModelInspector),
                dataRetriever,
                cqlComparers,
                (IValueSetDictionary)vss,
                unitConverter,
                new DateTimeIso8601(now ?? DateTimeOffset.UtcNow, DateTimePrecision.Millisecond),
                FirelyEnumComparer.Default);
            cqlComparers
                .AddIntervalComparisons(operators)
                .AddFirelyComparers();
            var ctx = new CqlContext(operators, delegates, parameters);
            return ctx;
        }
    }
}
