using HexShield.Models.DTOs.Admin;

namespace HexShield.Services;

public interface IOrganizationService
{
    Task<IEnumerable<OrganizationNodeDto>> GetOrganizationHierarchyAsync();
    Task<OrganizationNodeDto?> GetNodeByIdAsync(int id);
    Task<OrganizationNodeDto> CreateNodeAsync(CreateOrganizationNodeDto dto);
    Task<OrganizationNodeDto> UpdateNodeAsync(int id, UpdateOrganizationNodeDto dto);
    Task<bool> DeleteNodeAsync(int id);
    Task<bool> AssignUserToNodeAsync(int nodeId, AssignUserToNodeDto dto);
}