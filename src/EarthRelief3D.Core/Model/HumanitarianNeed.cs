namespace EarthRelief3D.Core.Model;

/// <summary>
/// One row of the UN humanitarian needs overview (HNO) for a country and a cluster: the people in need and the people the
/// plan targets. A cluster with no row means "not reported in this file", never zero.
/// </summary>
/// <param name="Iso3">The country, already resolved to a boundary unit.</param>
/// <param name="Cluster">The cluster code: ALL (the total), FSC, WSH, SHL or HEA. Never the free-text description.</param>
/// <param name="Population">The country's population as the plan counts it; filled only on the ALL row.</param>
/// <param name="InNeed">People in need; null when the cell is blank.</param>
/// <param name="Targeted">People the plan targets; null when the cell is blank.</param>
/// <param name="PlanYear">The year of the plan (the file's year).</param>
/// <param name="Basis">
/// The file's own description of the ALL row ("Final", "Plan caseload", "GHO Estimates"), shown beside the figure: a "GHO
/// Estimates" total is not from the country's own overview. Null on cluster rows.
/// </param>
public sealed record HumanitarianNeed(string Iso3, string Cluster, long? Population, long? InNeed, long? Targeted, int PlanYear,
    SourceCitation Citation, string? Basis = null);
