﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using RestWithAspNet5.Hypermedia.Abstract;
using RestWithAspNet5.Hypermedia.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RestWithAspNet5.Hypermedia
{
    public abstract class ContentResponseEnricher<T> : IResponseEnricher where T : ISupportHypermedia
    {

        public ContentResponseEnricher()
        {

        }


        public virtual bool CanEnrich(Type contetType)
        {
            return contetType == typeof(T) || contetType == typeof(List<T>) || contetType == typeof(PagedSearchVO<T>);
        }

        protected abstract Task EnrichModel(T content, IUrlHelper urlHelper);
        bool IResponseEnricher.CanEnrich(ResultExecutingContext response)
        {

            if (response.Result is OkObjectResult okObjectResult)
            {
                return CanEnrich(okObjectResult.Value.GetType());
            }
            return false;
        }


        public async Task Enrich(ResultExecutingContext response)
        {
            var urlHelper = new UrlHelperFactory().GetUrlHelper(response);

            if (response.Result is OkObjectResult okObjectResult)
            {
                if (okObjectResult is T model)
                {
                    await EnrichModel(model, urlHelper);
                }
                else if (okObjectResult.Value is List<T> colletcion)
                {
                    ConcurrentBag<T> bag = new ConcurrentBag<T>(colletcion);

                    Parallel.ForEach(bag, (element) =>
                    {
                        EnrichModel(element, urlHelper);
                    });
                }
                else if (okObjectResult.Value is PagedSearchVO<T> pagedSearch)
                {
                    Parallel.ForEach(pagedSearch.List.ToList(), (element) =>
                    {
                        EnrichModel(element, urlHelper);
                    });
                }
            }
            await Task.FromResult<object>(null);
        }


    }
}
