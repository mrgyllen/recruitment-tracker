using api.Application.Common.Interfaces;
using api.Application.Common.Models;
using api.Application.Common.Security;
using api.Domain.Enums;

namespace api.Application.TodoLists.Queries.GetTodos;

[Authorize]
public record GetTodosQuery : IRequest<TodosVm>;

public class GetTodosQueryHandler : IRequestHandler<GetTodosQuery, TodosVm>
{
    private readonly IApplicationDbContext _context;

    public GetTodosQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TodosVm> Handle(GetTodosQuery request, CancellationToken cancellationToken)
    {
        return new TodosVm
        {
            PriorityLevels = Enum.GetValues(typeof(PriorityLevel))
                .Cast<PriorityLevel>()
                .Select(p => new LookupDto { Id = (int)p, Title = p.ToString() })
                .ToList(),

            Lists = await _context.TodoLists
                .AsNoTracking()
                .Select(t => new TodoListDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Colour = t.Colour,
                    Items = t.Items.Select(i => new TodoItemDto
                    {
                        Id = i.Id,
                        ListId = i.ListId,
                        Title = i.Title,
                        Done = i.Done,
                        Priority = (int)i.Priority,
                        Note = i.Note,
                    }).ToArray(),
                })
                .OrderBy(t => t.Title)
                .ToListAsync(cancellationToken)
        };
    }
}
