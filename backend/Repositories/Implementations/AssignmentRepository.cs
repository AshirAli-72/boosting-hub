using BoostingHub.backend.Data;
using BoostingHub.backend.Repositories.Interfaces;

namespace BoostingHub.backend.Repositories.Implementations;

// TaskAssignment feature removed.
// This class is left only as a placeholder to avoid breaking DI registrations.
// Remove DI registration/usages as needed.
public class AssignmentRepository : Repository<object>, IAssignmentRepository
{
    public AssignmentRepository(ApplicationDbContext context) : base(context) { }
}

