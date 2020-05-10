﻿using System;
using System.Collections.Generic;
using FoodDiary.Domain.Entities;
using FoodDiary.Import.Services;

namespace FoodDiary.Import.Implementation
{
    class CategoryJsonImporter : ICategoryJsonImporter
    {
        private readonly IDictionary<string, Category> _existingCategoriesDictionary;

        public CategoryJsonImporter(IJsonImportDataProvider importData)
        {
            _existingCategoriesDictionary = importData?.ExistingCategories ?? throw new ArgumentNullException(nameof(importData), "Could not get existing categories dictionary");
        }

        public Category ImportCategory(string categoryNameFromJson)
        {
            // argument checks

            Category importedCategory;

            if (_existingCategoriesDictionary.ContainsKey(categoryNameFromJson))
                importedCategory = _existingCategoriesDictionary[categoryNameFromJson];
            else
            {
                importedCategory = new Category()
                {
                    Name = categoryNameFromJson
                };
            }

            return importedCategory;
        }
    }
}