﻿using System;
using System.Collections.Generic;
using FoodDiary.Domain.Dtos;
using FoodDiary.Domain.Entities;
using FoodDiary.Import.Services;

namespace FoodDiary.Import.Core
{
    class JsonImporter : IJsonImporter
    {
        private readonly IPageJsonImporter _pageImporter;

        public JsonImporter(IPageJsonImporter pageImporter)
        {
            _pageImporter = pageImporter ?? throw new ArgumentNullException(nameof(pageImporter));
        }

        public void Import(PagesJsonExportDto jsonObj, out List<Page> createdPages)
        {
            // argument checks

            createdPages = new List<Page>();

            foreach (var pageFromJson in jsonObj.Pages)
            {
                _pageImporter.ImportPage(pageFromJson, out var createdPage);
                
                if (createdPage != null)
                    createdPages.Add(createdPage);
            }
        }
    }
}
