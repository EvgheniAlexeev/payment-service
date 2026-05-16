// FILE: src/PaymentService.Shared/Markers.cs
// VERSION: 2.0.0
// MODULE: M-SHARED
// PURPOSE: Marker interfaces for Wolverine and domain contracts
// SEMANTIC_TAG: [MARKERS, DOMAIN_CONTRACT]
// START_MODULE M-SHARED-MARKERS

namespace PaymentService.Shared;

/// <summary>
/// <para><strong>@contract:</strong> M-SHARED</para>
/// <para><strong>@version:</strong> 2.1.0</para>
/// <para><strong>@since:</strong> 2.0.0</para>
/// <para><strong>@purpose:</strong> Marker interface for Wolverine domain commands</para>
/// <para><strong>@module-type:</strong> UTILITY (marker interface)</para>
/// <para><strong>@stability:</strong> STABLE</para>
/// </summary>
public interface ICommand { }

/// <summary>
/// <para><strong>@contract:</strong> M-SHARED</para>
/// <para><strong>@version:</strong> 2.1.0</para>
/// <para><strong>@since:</strong> 2.0.0</para>
/// <para><strong>@purpose:</strong> Marker interface for domain events</para>
/// <para><strong>@module-type:</strong> UTILITY (marker interface)</para>
/// <para><strong>@stability:</strong> STABLE</para>
/// </summary>
public interface IEvent { }

/// <summary>
/// <para><strong>@contract:</strong> M-SHARED</para>
/// <para><strong>@version:</strong> 2.1.0</para>
/// <para><strong>@since:</strong> 2.0.0</para>
/// <para><strong>@purpose:</strong> Marker interface for request DTOs</para>
/// <para><strong>@module-type:</strong> UTILITY (marker interface)</para>
/// <para><strong>@stability:</strong> STABLE</para>
/// </summary>
public interface IRequest { }

/// <summary>
/// <para><strong>@contract:</strong> M-SHARED</para>
/// <para><strong>@version:</strong> 2.1.0</para>
/// <para><strong>@since:</strong> 2.0.0</para>
/// <para><strong>@purpose:</strong> Marker interface for response DTOs</para>
/// <para><strong>@module-type:</strong> UTILITY (marker interface)</para>
/// <para><strong>@stability:</strong> STABLE</para>
/// </summary>
public interface IResponse { }
