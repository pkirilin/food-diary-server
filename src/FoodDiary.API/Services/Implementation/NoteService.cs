﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Domain.Dtos;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.Repositories;

namespace FoodDiary.API.Services.Implementation
{
    public class NoteService : INoteService
    {
        private readonly INoteRepository _noteRepository;
        private readonly IProductRepository _productRepository;
        private readonly INotesOrderService _notesOrderService;

        public NoteService(
            INoteRepository noteRepository,
            IProductRepository productRepository,
            INotesOrderService notesOrderService)
        {
            _noteRepository = noteRepository ?? throw new ArgumentNullException(nameof(noteRepository));
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _notesOrderService = notesOrderService ?? throw new ArgumentNullException(nameof(notesOrderService));
        }

        public async Task<IEnumerable<Note>> SearchNotesAsync(NotesSearchRequestDto request, CancellationToken cancellationToken)
        {
            var query = _noteRepository.GetQueryWithoutTracking()
                .Where(n => n.PageId == request.PageId);

            if (request.MealType.HasValue)
            {
                query = query.Where(n => n.MealType == request.MealType);
            }

            query = query.OrderBy(n => n.MealType)
                .ThenBy(n => n.DisplayOrder);
            query = _noteRepository.LoadProduct(query);

            var notes = await _noteRepository.GetListFromQueryAsync(query, cancellationToken);
            return notes;
        }

        public async Task<Note> GetNoteByIdAsync(int id, CancellationToken cancellationToken)
        {
            return await _noteRepository.GetByIdAsync(id, cancellationToken);
        }

        public async Task<IEnumerable<Note>> GetNotesByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken)
        {
            return await _noteRepository.GetListFromQueryAsync(
                _noteRepository.GetQuery().Where(n => ids.Contains(n.Id)),
                cancellationToken
            );
        }

        public async Task<ValidationResultDto> ValidateNoteDataAsync(NoteCreateEditDto noteData, CancellationToken cancellationToken)
        {
            var productForNote = await _productRepository.GetByIdAsync(noteData.ProductId, cancellationToken);
            if (productForNote == null)
            {
                return new ValidationResultDto(false, $"{nameof(noteData.ProductId)}", "Selected product not found");
            }

            return new ValidationResultDto(true);
        }

        public async Task<Note> CreateNoteAsync(Note note, CancellationToken cancellationToken)
        {
            note.DisplayOrder = await _notesOrderService.GetOrderForNewNoteAsync(note, cancellationToken);
            var addedNote = _noteRepository.Create(note);
            await _noteRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            return addedNote;
        }

        public async Task EditNoteAsync(Note note, CancellationToken cancellationToken)
        {
            _noteRepository.Update(note);
            await _noteRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteNoteAsync(Note note, CancellationToken cancellationToken)
        {
            await _notesOrderService.ReorderNotesOnDeleteAsync(note, cancellationToken);
            _noteRepository.Delete(note);
            await _noteRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        }

        public bool AllNotesFetched(IEnumerable<int> requestedIds, IEnumerable<Note> fetchedNotes)
        {
            return !requestedIds.Except(fetchedNotes.Select(n => n.Id)).Any();
        }

        public async Task DeleteNotesAsync(IEnumerable<Note> notes, CancellationToken cancellationToken)
        {
            await _notesOrderService.ReorderNotesOnDeleteRangeAsync(notes, cancellationToken);
            _noteRepository.DeleteRange(notes);
            await _noteRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> NoteCanBeMovedAsync(Note noteForMove, NoteMoveRequestDto moveRequest, CancellationToken cancellationToken)
        {
            var q = _noteRepository.GetQueryWithoutTracking()
                .Where(n => n.PageId == noteForMove.PageId && n.MealType == moveRequest.DestMeal);
            var maxDisplayOrder = await _noteRepository.GetMaxDisplayOrderFromQueryAsync(q, cancellationToken);
            return moveRequest.Position >= 0 && moveRequest.Position <= maxDisplayOrder + 1;
        }

        public async Task<Note> MoveNoteAsync(Note noteForMove, NoteMoveRequestDto moveRequest, CancellationToken cancellationToken)
        {
            await _notesOrderService.ReorderNotesOnMoveAsync(noteForMove, moveRequest, cancellationToken);

            noteForMove.MealType = moveRequest.DestMeal;
            noteForMove.DisplayOrder = moveRequest.Position;

            _noteRepository.Update(noteForMove);
            await _noteRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            return noteForMove;
        }
    }
}