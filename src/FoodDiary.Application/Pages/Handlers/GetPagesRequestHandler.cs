﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Application.Pages.Requests;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Repositories;
using MediatR;

namespace FoodDiary.Application.Pages.Handlers
{
    public class GetPagesRequestHandler : IRequestHandler<GetPagesRequest, List<Page>>
    {
        private readonly IPageRepository _pageRepository;

        public GetPagesRequestHandler(IPageRepository pageRepository)
        {
            _pageRepository = pageRepository ?? throw new ArgumentNullException(nameof(pageRepository));
        }

        public Task<List<Page>> Handle(GetPagesRequest request, CancellationToken cancellationToken)
        {
            var query = _pageRepository.GetQueryWithoutTracking();

            if (request.StartDate.HasValue)
                query = query.Where(p => p.Date >= request.StartDate);
            if (request.EndDate.HasValue)
                query = query.Where(p => p.Date <= request.EndDate);

            switch (request.SortOrder)
            {
                case SortOrder.Ascending:
                    query = query.OrderBy(p => p.Date);
                    break;
                case SortOrder.Descending:
                    query = query.OrderByDescending(p => p.Date);
                    break;
                default:
                    break;
            }

            query = _pageRepository.LoadNotesWithProducts(query);
            return _pageRepository.GetListFromQueryAsync(query, cancellationToken);
        }
    }
}