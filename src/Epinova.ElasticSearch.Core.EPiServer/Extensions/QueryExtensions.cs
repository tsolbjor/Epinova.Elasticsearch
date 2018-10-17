﻿using System;
using System.Linq;
using System.Linq.Expressions;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Engine;
using Epinova.ElasticSearch.Core.Enums;
using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Models.Query;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer.Core;
using EPiServer.ServiceLocation;

namespace Epinova.ElasticSearch.Core.EPiServer.Extensions
{
    public static class QueryExtensions
    {
        public static IElasticSearchService<T> StartFrom<T>(this IElasticSearchService<T> service, ContentReference contentReference)
        {
            return service.StartFrom(contentReference.ID);
        }

        public static IElasticSearchService<T> Exclude<T>(this IElasticSearchService<T> service, ContentReference root, bool recursive = true)
        {
            return service.Exclude(root.ID, recursive);
        }

        public static IElasticSearchService<T> Exclude<T>(this IElasticSearchService<T> service, IContent root, bool recursive = true)
        {
            return service.Exclude(root.ContentLink.ID, recursive);
        }

        public static IElasticSearchService<T> BoostByAncestor<T>(this IElasticSearchService<T> service, ContentReference path, sbyte weight)
        {
            return service.BoostByAncestor(path.ID, weight);
        }

        [Obsolete("Use GetSuggestions")]
        public static string[] Suggestions<T>(this IElasticSearchService<T> service, string searchText)
        {
            return service.GetSuggestions(searchText);
        }

        /// <summary>
        /// Get auto-suggestions for the indexed word beginning with the supplied term. 
        /// </summary>
        /// <param name="service">The <see cref="IElasticSearchService"/> instance</param>
        /// <param name="searchText">The term to search for</param>
        /// <returns>An array of matching words</returns>
        public static string[] GetSuggestions<T>(this IElasticSearchService<T> service, string searchText)
        {
            var settings = ServiceLocator.Current.GetInstance<IElasticSearchSettings>();
            var engine = new SearchEngine(settings);

            return GetSuggestions(service, searchText, engine);
        }

        internal static string[] GetSuggestions<T>(this IElasticSearchService<T> service, string searchText, SearchEngine engine)
        {
            var settings = ServiceLocator.Current.GetInstance<IElasticSearchSettings>();

            var builder = new QueryBuilder(service.SearchType, settings);

            SuggestRequest request = builder.Suggest(new QuerySetup
            {
                SearchText = searchText,
                Language = service.SearchLanguage,
                Size = service.SizeValue
            });

            var elasticSuggestions = engine.GetSuggestions(request, service.SearchLanguage);

            var repository = ServiceLocator.Current.GetInstance<IAutoSuggestRepository>();
            var editorialSuggestions = repository.GetWords(Language.GetLanguageCode(service.CurrentLanguage))
                .Where(w => w?.StartsWith(searchText) == true);

            return editorialSuggestions.Concat(elasticSuggestions).Distinct().ToArray();
        }

        public static IElasticSearchService<T> Filter<T>(this IElasticSearchService<T> service,
            Expression<Func<T, CategoryList>> fieldSelector, int filterValue)
        {
            return service.Filter(fieldSelector, filterValue.ToString());
        }

        public static IElasticSearchService<T> Filter<T>(this IElasticSearchService<T> service,
            Expression<Func<T, CategoryList>> fieldSelector, string filterValue)
        {
            Tuple<string, MappingType> fieldInfo = ElasticSearchService<T>.GetFieldInfo(fieldSelector);

            return service.Filter(fieldInfo.Item1, filterValue, false);
        }
    }
}