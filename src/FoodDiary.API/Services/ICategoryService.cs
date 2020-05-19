﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FoodDiary.API.Requests;
using FoodDiary.Domain.Entities;

namespace FoodDiary.API.Services
{
    public interface ICategoryService
    {
        Task<IEnumerable<Category>> GetCategoriesAsync(CancellationToken cancellationToken);

        Task<Category> GetCategoryByIdAsync(int id, CancellationToken cancellationToken);

        Task<bool> IsCategoryExistsAsync(string categoryName, CancellationToken cancellationToken);

        bool IsEditedCategoryValid(CategoryCreateEditRequest updatedCategoryData, Category originalCategory, bool isCategoryExists);

        Task<Category> CreateCategoryAsync(Category category, CancellationToken cancellationToken);

        Task EditCategoryAsync(Category category, CancellationToken cancellationToken);

        Task DeleteCategoryAsync(Category category, CancellationToken cancellationToken);

        Task<IEnumerable<Category>> GetCategoriesDropdownAsync(CategoryDropdownSearchRequest request, CancellationToken cancellationToken);
    }
}
