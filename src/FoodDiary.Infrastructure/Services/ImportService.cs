﻿using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Domain.Dtos;
using FoodDiary.Domain.Exceptions;
using FoodDiary.Domain.Repositories;
using FoodDiary.Domain.Services;
using FoodDiary.Import;

namespace FoodDiary.Infrastructure.Services
{
    public class ImportService : IImportService
    {
        private readonly IPageRepository _pageRepository;
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;

        private readonly IJsonParser _pagesJsonParser;
        private readonly IJsonImportDataProvider _importDataProvider;
        private readonly IJsonImporter _pagesJsonImporter;

        public ImportService(
            IPageRepository pageRepository,
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            IJsonParser pagesJsonParser,
            IJsonImportDataProvider importDataProvider,
            IJsonImporter pagesJsonEntitiesUpdater)
        {
            _pageRepository = pageRepository ?? throw new ArgumentNullException(nameof(pageRepository));
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
            _pagesJsonParser = pagesJsonParser ?? throw new ArgumentNullException(nameof(pagesJsonParser));
            _importDataProvider = importDataProvider ?? throw new ArgumentNullException(nameof(importDataProvider));
            _pagesJsonImporter = pagesJsonEntitiesUpdater ?? throw new ArgumentNullException(nameof(pagesJsonEntitiesUpdater));
        }

        public async Task<PagesJsonExportDto> DeserializePagesFromJsonAsync(Stream importFileStream, CancellationToken cancellationToken)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            try
            {
                return await JsonSerializer.DeserializeAsync<PagesJsonExportDto>(importFileStream, options, cancellationToken);
            }
            catch (JsonException)
            {
                throw new ImportException("Failed to import pages: import file has incorrect format");
            }
        }

        public async Task RunPagesJsonImportAsync(PagesJsonExportDto jsonObj, CancellationToken cancellationToken)
        {
            var pagesFromJson = _pagesJsonParser.ParsePages(jsonObj);
            var notesFromJson = _pagesJsonParser.ParseNotes(pagesFromJson);
            var productNamesFromJson = _pagesJsonParser.ParseProductNames(notesFromJson);
            var categoryNamesFromJson = _pagesJsonParser.ParseCategoryNames(notesFromJson);
            var pagesFromJsonDates = pagesFromJson.Select(p => p.Date);

            var pagesForUpdateQuery = _pageRepository.GetQuery()
                .Where(p => pagesFromJsonDates.Contains(p.Date));
            var importProductsQuery = _productRepository.GetQuery()
                .Where(p => productNamesFromJson.Contains(p.Name));
            var importCategoriesQuery = _categoryRepository.GetQuery()
                .Where(c => categoryNamesFromJson.Contains(c.Name));

            pagesForUpdateQuery = _pageRepository.LoadNotesWithProductsAndCategories(pagesForUpdateQuery);

            _importDataProvider.ExistingPages = await _pageRepository.GetDictionaryFromQueryAsync(pagesForUpdateQuery, cancellationToken);
            _importDataProvider.ExistingProducts = await _productRepository.GetDictionaryFromQueryAsync(importProductsQuery, cancellationToken);
            _importDataProvider.ExistingCategories = await _categoryRepository.GetDictionaryFromQueryAsync(importCategoriesQuery, cancellationToken);

            _pagesJsonImporter.Import(jsonObj, out var createdPages);

            _pageRepository.CreateRange(createdPages);
            await _pageRepository.SaveChangesAsync(cancellationToken);
        }
    }
}