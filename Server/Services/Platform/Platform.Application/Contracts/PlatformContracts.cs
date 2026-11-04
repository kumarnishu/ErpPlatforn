using Platform.Domain;

namespace Platform.Application.Contracts;

// These API contracts keep HTTP input/output separate from mutable EF entities.
public sealed record CreateProductRequest(string Key, string Name);
public sealed record UpdateProductRequest(string Name, CatalogStatus Status);
public sealed record CreateModuleRequest(string Key, string Name, int SortOrder = 0);
public sealed record UpdateModuleRequest(string Name, CatalogStatus Status, int SortOrder = 0);
public sealed record CreateFeatureRequest(string Key, string Name, int SortOrder = 0);
public sealed record UpdateFeatureRequest(string Name, CatalogStatus Status, int SortOrder = 0);
public sealed record CreateActionRequest(string Key, string Name);
public sealed record UpdateActionRequest(string Name, CatalogStatus Status);
public sealed record CreateTenantRequest(string Key, string Name, IReadOnlyList<int>? ProductIds);
public sealed record RegisterTenantAdminRequest(string TenantKey, string TenantName, string DisplayName, string Email, string Password, IReadOnlyList<int> ProductIds);
public sealed record LoginRequest(string Email, string Password, int TenantId);
public sealed record CreateTenantUserRequest(string DisplayName, string Email, string Password);
public sealed record LoginResponse(string AccessToken, string TokenType, DateTimeOffset ExpiresAtUtc, int TenantId, TenantMembershipRole Role);
public sealed record TenantUserCreatedResponse(string UserId, string Email, string DisplayName, int TenantId, TenantMembershipRole Role);
public sealed record UpdateTenantRequest(string Name, TenantStatus Status);
public sealed record UpdateSubscriptionRequest(TenantProductSubscriptionStatus Status, DateTimeOffset? ExpiresAtUtc);
public sealed record CreateMembershipRequest(string UserId);
public sealed record UpdateMembershipRequest(TenantMembershipStatus Status);
public sealed record AccessCheckRequest(string ProductKey, string ModuleKey, string FeatureKey, string ActionKey);
public sealed record AuthorizationDecision(bool Allowed, string? DenialCode = null);

public sealed record ProductResponse(int Id, string Key, string Name, CatalogStatus Status);
public sealed record ModuleResponse(int Id, int ProductId, string Key, string Name, CatalogStatus Status, int SortOrder);
public sealed record FeatureResponse(int Id, int ProductModuleId, string Key, string Name, CatalogStatus Status, int SortOrder);
public sealed record ActionResponse(int Id, int ProductFeatureId, string Key, string Name, CatalogStatus Status);
public sealed record TenantResponse(int Id, string Key, string Name, TenantStatus Status);
public sealed record TenantDetailResponse(TenantResponse Tenant, IReadOnlyList<SubscriptionResponse> ProductSubscriptions);
public sealed record TenantRegistrationResponse(TenantResponse Tenant, MembershipResponse Membership, IReadOnlyList<SubscriptionResponse> ProductSubscriptions);
public sealed record SubscriptionResponse(int Id, int TenantId, int ProductId, TenantProductSubscriptionStatus Status, DateTimeOffset SubscribedAtUtc, DateTimeOffset? ExpiresAtUtc);
public sealed record MembershipResponse(int Id, int TenantId, string UserId, TenantMembershipStatus Status, TenantMembershipRole Role, DateTimeOffset CreatedAtUtc);
public sealed record MembershipDetailResponse(MembershipResponse Membership, IReadOnlyList<UserActionResponse> AssignedActions);
public sealed record UserActionResponse(int Id, int TenantMembershipId, int ProductActionId, bool IsActive, DateTimeOffset AssignedAtUtc);
